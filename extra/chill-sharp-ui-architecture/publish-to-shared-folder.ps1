[CmdletBinding()]
param(
  [string]$SharedFolder = 'C:\source\npm-shared'
)

# Fail fast on script errors and undefined-variable mistakes.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Publish the shared TS and Angular client packages first, then publish ui-core on top of them.
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$dependencyScripts = @(
  (Join-Path $scriptDirectory '..\chill-sharp-ts-client\publish-to-shared-folder.ps1'),
  (Join-Path $scriptDirectory '..\chill-sharp-ng-client\publish-to-shared-folder.ps1')
)
$uiCorePublishScriptPath = Join-Path $scriptDirectory '..\chill-sharp-ui-core\publish-to-shared-folder.ps1'

# Ensure the dependency archives exist in the shared folder before ui-core tries to consume them.
foreach ($dependencyScriptPath in $dependencyScripts) {
  if (-not (Test-Path -LiteralPath $dependencyScriptPath)) {
    throw "Could not find dependency publish script at '$dependencyScriptPath'."
  }

  & $dependencyScriptPath -SharedFolder $SharedFolder
  if ($LASTEXITCODE -ne 0) {
    throw "Dependency publish script failed: '$dependencyScriptPath'."
  }
}

# After the dependencies are refreshed, publish the shared Angular UI package itself.
if (-not (Test-Path -LiteralPath $uiCorePublishScriptPath)) {
  throw "Could not find ui-core publish script at '$uiCorePublishScriptPath'."
}

& $uiCorePublishScriptPath -SharedFolder $SharedFolder
if ($LASTEXITCODE -ne 0) {
  throw 'ui-core publish-to-shared-folder.ps1 failed.'
}
