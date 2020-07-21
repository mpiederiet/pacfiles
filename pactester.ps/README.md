# PAC file tester for PowerShell

Read and test proxy autoconfig files with PowerShell.

## Installation

pactester is available from the PowerShell Gallery, and can be installed using `Install-Module -Name pactester`

## Usage

`Find-ProxyForURL -PacFile .\proxy.pac -Url https://example.com`

`Find-ProxyForURL -PacFile .\proxy.pac -Url https://example.com -MyIPAddress 1.2.3.4`

## TODO:

- `Get-ProxyForUrl` or similar that returns `FindProxyForURL()` output as addresses- split at semicolons and without 'PROXY' etc.

- `Test-PacFile` to ensure it's reasonably well-formed would be good.
