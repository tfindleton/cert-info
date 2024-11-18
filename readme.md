# Certificate Information Tool

![CI](https://github.com//tfindleton/cert-info/workflows/CI/badge.svg)

A command-line utility that retrieves and displays SSL/TLS certificate information for any given hostname or URL.

## Features

- Fetches SSL certificate details from any host
- Displays certificate validity period and expiration status
- Shows remaining days until expiration with color-coded warnings
- Supports both hostnames and URLs as input
- Handles custom port specification (default: 443)
- Formats certificate thumbprint for improved readability
- Implements connection timeout protection (5 seconds)

## Installation

Requires .NET Core 8.0

```bash
# Clone the repository
git clone https://github.com/yourusername/cert-info.git

# Build the project
dotnet build
```

## Usage

```bash
cert-info <hostname|url> [port]
```

### Parameters

- `hostname|url`: Required. The target hostname or URL
  - Examples: `google.com`, `https://google.com`
- `port`: Optional. The port number (default: 443)

### Examples

Check certificate for a hostname:
```bash
cert-info github.com
```

Check certificate with custom port:
```bash
cert-info smtp.gmail.com 587
```

Check certificate using HTTPS URL:
```bash
cert-info https://www.github.com
```

### Sample Output

```
Fetching certificate details for github.com:443...

╔════════════════════════════╗
║  Certificate Information   ║
╚════════════════════════════╝

Subject:       github.com
Issuer:        Sectigo ECC Domain Validation Secure Server CA
Valid From:    2024-03-06 16:00:00
Valid Until:   2025-03-07 15:59:59
Status:        VALID (expires in 109 days)
Thumbprint:    E7 03 5B CC 1C 18 77 1F 79 2F 90 86 6B 6C 1D F8 DF AA BD C0
```

## Status Indicators

The tool uses color-coding to indicate certificate status:

- 🟢 **Green**: Valid certificate with more than 30 days until expiration
- 🟡 **Yellow**: Certificate expiring within 30 days
- 🔴 **Red**: Expired certificate or connection error
- 🔵 **Cyan**: Interface elements
- 🟣 **Purple**: Certificate thumbprint

## Error Handling

The tool provides clear error messages for common issues:

- Invalid hostname or URL format
- Connection timeouts (5-second limit)
- Network connectivity problems
- Invalid port numbers
- SSL/TLS handshake failures

## Technical Details

- Written in C# (.NET Core 8.0)
- Uses `System.Net.Security` for SSL/TLS operations
- Implements proper connection disposal and timeout handling
- Supports both IPv4 and IPv6 connections