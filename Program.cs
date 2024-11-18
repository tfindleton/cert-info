using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Linq;

namespace CertInfo
{
    /// <summary>
    /// Provides functionality to retrieve and display SSL certificate information for a given hostname.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main entry point of the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        static void Main(string[] args)
        {
            // Check if the required arguments are provided
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: cert-info <hostname|url> [port]");
                return;
            }

            // Parse command-line arguments
            string input = args[0];
            string hostname = GetHostnameFromInput(input);
            int port = args.Length > 1 && int.TryParse(args[1], out int parsedPort) ? parsedPort : 443;

            if (string.IsNullOrEmpty(hostname))
            {
                WriteLineWithColor("Invalid input. Please provide a valid hostname or URL.", ConsoleColor.Red);
                return;
            }

            try
            {
                Console.WriteLine($"Fetching certificate details for {hostname}:{port}...");
                X509Certificate2 cert = GetCertificate(hostname, port);

                if (cert != null)
                {
                    Console.WriteLine();
                    WriteLineWithColor("╔════════════════════════════╗", ConsoleColor.Cyan);
                    WriteLineWithColor("║  Certificate Information   ║", ConsoleColor.Cyan);
                    WriteLineWithColor("╚════════════════════════════╝", ConsoleColor.Cyan);
                    Console.WriteLine();

                    // Display subject details with better formatting
                    WriteProperty("Subject", cert.Subject.Split(',')[0].Replace("CN=", ""), ConsoleColor.Yellow);
                    WriteProperty("Issuer", cert.Issuer.Split(',')[0].Replace("CN=", ""), ConsoleColor.Yellow);

                    // Calculate days until expiration
                    var daysUntilExpiration = (cert.NotAfter - DateTime.Now).TotalDays;
                    
                    WriteProperty("Valid From", cert.NotBefore.ToString("yyyy-MM-dd HH:mm:ss"), ConsoleColor.Green);

                    // Enhanced expiration display
                    if (cert.NotAfter < DateTime.Now)
                    {
                        WriteProperty("Valid Until", $"{cert.NotAfter:yyyy-MM-dd HH:mm:ss}", ConsoleColor.Red);
                        WriteProperty("Status", $"EXPIRED ({Math.Abs(daysUntilExpiration):F0} days ago)", ConsoleColor.Red);
                    }
                    else if (daysUntilExpiration <= 30)
                    {
                        WriteProperty("Valid Until", $"{cert.NotAfter:yyyy-MM-dd HH:mm:ss}", ConsoleColor.DarkYellow);
                        WriteProperty("Status", $"EXPIRING SOON (in {daysUntilExpiration:F0} days)", ConsoleColor.DarkYellow);
                    }
                    else
                    {
                        WriteProperty("Valid Until", $"{cert.NotAfter:yyyy-MM-dd HH:mm:ss}", ConsoleColor.Green);
                        WriteProperty("Status", $"VALID (expires in {daysUntilExpiration:F0} days)", ConsoleColor.Green);
                    }

                    WriteProperty("Thumbprint", FormatThumbprint(cert.Thumbprint), ConsoleColor.Magenta);
                    Console.WriteLine();
                }
                else
                {
                    WriteLineWithColor("Could not retrieve certificate information.", ConsoleColor.Red);
                }
            }
            catch (Exception ex)
            {
                WriteLineWithColor($"Error: {ex.Message}", ConsoleColor.Red);
            }
        }

        /// <summary>
        /// Extracts the hostname from a given input string, which can be either a URL or hostname.
        /// </summary>
        /// <param name="input">The input string containing either a URL or hostname.</param>
        /// <returns>The extracted hostname, or the original input if parsing fails.</returns>
        private static string GetHostnameFromInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentNullException(nameof(input));

            try
            {
                Uri uri = new Uri(input.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? input : $"https://{input}");
                return uri.Host;
            }
            catch (UriFormatException)
            {
                // If input is not a valid URI, return it as-is (assume it's a hostname)
                return input;
            }
        }

        /// <summary>
        /// Retrieves the SSL certificate for the specified hostname and port.
        /// </summary>
        /// <param name="hostname">The hostname to connect to.</param>
        /// <param name="port">The port number to connect to.</param>
        /// <returns>The X509Certificate2 instance if successful; null otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when hostname is null or empty.</exception>
        /// <exception cref="SocketException">Thrown when connection cannot be established.</exception>
        private static X509Certificate2 GetCertificate(string hostname, int port)
        {
            if (string.IsNullOrWhiteSpace(hostname))
                throw new ArgumentNullException(nameof(hostname));

            if (port <= 0 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535");

            try
            {
                using var client = new TcpClient();
                // Set timeout to avoid hanging
                if (!client.ConnectAsync(hostname, port).Wait(5000))
                {
                    throw new TimeoutException($"Connection to {hostname}:{port} timed out");
                }

                using var sslStream = new SslStream(
                    client.GetStream(),
                    false,
                    (sender, certificate, chain, errors) => true);

                // Set timeout for SSL authentication
                sslStream.AuthenticateAsClient(hostname);

                return sslStream.RemoteCertificate != null 
                    ? new X509Certificate2(sslStream.RemoteCertificate) 
                    : null;
            }
            catch (Exception ex) when (ex is SocketException || ex is TimeoutException)
            {
                throw new InvalidOperationException($"Failed to connect to {hostname}:{port}", ex);
            }
        }

        static void WriteLineWithColor(string message, ConsoleColor color)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = previousColor;
        }

        static void WriteProperty(string label, string value, ConsoleColor valueColor)
        {
            Console.Write($"{label}: ".PadRight(15));
            WriteLineWithColor(value, valueColor);
        }

        /// <summary>
        /// Formats a certificate thumbprint with spaces between each pair of characters.
        /// </summary>
        /// <param name="thumbprint">The certificate thumbprint to format.</param>
        /// <returns>A formatted string with spaces between each pair of characters.</returns>
        private static string FormatThumbprint(string thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
                return string.Empty;

            return string.Join(" ", Enumerable.Range(0, thumbprint.Length / 2)
                .Select(i => thumbprint.Substring(i * 2, 2)));
        }
    }
}
