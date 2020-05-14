# PAC file tester for PowerShell

Read and test proxy autoconfig files with PowerShell.

`Find-ProxyForURL -PacFile .\proxy.pac -Url https://example.com`

## TODO:

- `Get-ProxyForUrl` or similar that returns `FindProxyForURL()` output as addresses- split at semicolons and without 'PROXY' etc.

- `Test-PacFile` to ensure it's reasonably well-formed woudld be good.
