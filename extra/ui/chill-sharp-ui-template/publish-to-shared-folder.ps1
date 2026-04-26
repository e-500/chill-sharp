[CmdletBinding()]
param(
  [string]$SharedFolder = 'C:\source\npm-shared'
)

# Fail fast on script errors and undefined-variable mistakes.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Explain explicitly that the template is an app shell and not a package published to the shared folder.
Write-Host "Skipping template publish for '$SharedFolder'."
Write-Host 'chill-sharp-ui-template is a private application shell, not a shared package.'
