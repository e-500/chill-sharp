[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishScriptPath = Join-Path $scriptPath 'extra\publish.ps1'

if (-not (Test-Path -LiteralPath $publishScriptPath)) {
  throw "Could not find publish script at '$publishScriptPath'."
}

& $publishScriptPath
