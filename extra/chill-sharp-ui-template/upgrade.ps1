[CmdletBinding()]
param(
  [string]$Version = 'latest'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$packages = @(
  "@chill-sharp/ui-core@$Version",
  "@chill-sharp/ng-client@$Version",
  "@chill-sharp/ts-client@$Version"
)

Push-Location $scriptDirectory
try {
  Write-Host "Installing published ChillSharp packages at '$Version'..."
  & npm install @packages
  if ($LASTEXITCODE -ne 0) {
    throw 'npm install failed.'
  }
}
finally {
  Pop-Location
}

Write-Host 'Published ChillSharp UI dependencies were upgraded.'
