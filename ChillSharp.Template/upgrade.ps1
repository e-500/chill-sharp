[CmdletBinding()]
param(
  [string]$Version = 'latest'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectFiles = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.csproj' -File)
if ($projectFiles.Count -eq 0) {
  throw "Could not find a .csproj file in '$PSScriptRoot'."
} elseif ($projectFiles.Count -gt 1) {
  throw "Found multiple .csproj files in '$PSScriptRoot': $($projectFiles.Name -join ', ')."
}
$projectPath = $projectFiles[0].FullName

$arguments = @('add', $projectPath, 'package', 'ChillSharp')
if (-not [string]::IsNullOrWhiteSpace($Version) -and $Version -ne 'latest') {
  $arguments += @('--version', $Version)
}

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
  throw "Failed to update ChillSharp."
}

if ([string]::IsNullOrWhiteSpace($Version) -or $Version -eq 'latest') {
  Write-Host 'Updated ChillSharp to the latest stable version from the configured NuGet sources.'
} else {
  Write-Host "Updated ChillSharp to $Version from the configured NuGet sources."
}
