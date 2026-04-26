[CmdletBinding()]
param(
  [string]$SharedFolder = 'C:\source\npm-shared'
)

# Fail fast on script errors and undefined-variable mistakes.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Resolve the shared helper script so this package can reuse the standard npm build-and-pack flow.
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$helperScriptPath = Join-Path (Split-Path -Parent $scriptDirectory) 'publish-npm-package-to-shared-folder.ps1'

# Delegate the actual install, build, and pack steps to the common helper.
& $helperScriptPath -PackageDirectory $scriptDirectory -SharedFolder $SharedFolder
if ($LASTEXITCODE -ne 0) {
  throw 'Failed to publish chill-sharp-ng-client to the shared folder.'
}
