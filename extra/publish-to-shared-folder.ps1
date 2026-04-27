[CmdletBinding()]
param(
  [string]$SharedFolder = 'C:\source\npm-shared'
)

# Fail fast on script errors and undefined-variable mistakes.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Resolve the package-level publish scripts that make up the shared JS publish flow.
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishScriptPaths = @(
  (Join-Path $scriptDirectory 'chill-sharp-ts-client\publish-to-shared-folder.ps1'),
  (Join-Path $scriptDirectory 'chill-sharp-ng-client\publish-to-shared-folder.ps1'),
  (Join-Path $scriptDirectory 'chill-sharp-ui-core\publish-to-shared-folder.ps1'),
  (Join-Path $scriptDirectory 'chill-sharp-react-client\publish-to-shared-folder.ps1'),
  (Join-Path $scriptDirectory 'chill-sharp-vue-client\publish-to-shared-folder.ps1')
)

# Run each package publisher in order so dependencies are produced before dependents.
foreach ($publishScriptPath in $publishScriptPaths) {
  if (-not (Test-Path -LiteralPath $publishScriptPath)) {
    throw "Could not find publish script at '$publishScriptPath'."
  }

  & $publishScriptPath -SharedFolder $SharedFolder
  if ($LASTEXITCODE -ne 0) {
    throw "Publish script failed: '$publishScriptPath'."
  }
}

# Print a short success message after the full workspace flow completes.
Write-Host ''
Write-Host 'extra packages published to shared folder successfully.'
