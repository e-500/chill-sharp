[CmdletBinding()]
param(
  [string]$SharedFolder = 'C:\source\npm-shared'
)

# Fail fast on script errors and undefined-variable mistakes.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Explain explicitly that this package is intentionally excluded from the npm shared-folder flow.
Write-Host "Skipping Python package publish for '$SharedFolder'."
Write-Host 'chill-sharp-py-client is not published to the npm shared folder.'
