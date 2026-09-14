[CmdletBinding()]
param(
  [string]$SharedFolder = 'C:\source\npm-shared'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$helperScriptPath = Join-Path (Split-Path -Parent $scriptDirectory) 'publish-npm-package-to-shared-folder.ps1'

& $helperScriptPath -PackageDirectory $scriptDirectory -SharedFolder $SharedFolder
if ($LASTEXITCODE -ne 0) {
  throw 'Failed to publish @chill-sharp/create-app to the shared folder.'
}
