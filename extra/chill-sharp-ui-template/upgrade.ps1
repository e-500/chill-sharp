[CmdletBinding()]
param(
  [string]$SharedFolder = 'C:\source\npm-shared',
  [switch]$SkipConfirmation
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageJsonPath = Join-Path $scriptDirectory 'package.json'
$packagesFolder = Join-Path $scriptDirectory 'packages'

function Resolve-ConfirmedFolderPath {
  param(
    [Parameter(Mandatory = $true)]
    [string]$InitialPath,

    [Parameter(Mandatory = $true)]
    [string]$Label,

    [switch]$SkipPrompt
  )

  $selectedPath = $InitialPath

  if (-not $SkipPrompt) {
    Write-Host "$Label shared folder suggestion: $InitialPath"
    $enteredPath = [string](Read-Host 'Press Enter to confirm, or type a different path')
    if (-not [string]::IsNullOrWhiteSpace($enteredPath)) {
      $selectedPath = $enteredPath
    }

    $confirmation = [string](Read-Host "Continue with '$selectedPath'? [Y/n]")
    if (-not [string]::IsNullOrWhiteSpace($confirmation) -and $confirmation -notmatch '^(?i:y|yes)$') {
      Write-Host 'Upgrade cancelled.'
      exit 0
    }
  }

  if (-not (Test-Path -LiteralPath $selectedPath)) {
    throw "$Label shared folder '$selectedPath' was not found."
  }

  return [System.IO.Path]::GetFullPath($selectedPath)
}

function Get-LatestArchive {
  param(
    [Parameter(Mandatory = $true)]
    [string]$FolderPath,

    [Parameter(Mandatory = $true)]
    [string]$ArchivePrefix
  )

  $pattern = '^' + [System.Text.RegularExpressions.Regex]::Escape($ArchivePrefix) + '-(?<Version>\d+\.\d+\.\d+(?:[-+][0-9A-Za-z\.-]+)?)\.tgz$'

  $candidates = foreach ($file in Get-ChildItem -LiteralPath $FolderPath -Filter "$ArchivePrefix-*.tgz" -File -ErrorAction Stop) {
    if ($file.Name -notmatch $pattern) {
      continue
    }

    [pscustomobject]@{
      File = $file
      VersionText = $matches.Version
      Version = [System.Management.Automation.SemanticVersion]::Parse($matches.Version)
    }
  }

  $latestArchive = $candidates | Sort-Object Version -Descending | Select-Object -First 1
  if ($null -eq $latestArchive) {
    throw "Could not find a '$ArchivePrefix-<version>.tgz' archive in '$FolderPath'."
  }

  return $latestArchive
}

function Test-CommandAvailable {
  param(
    [Parameter(Mandatory = $true)]
    [string]$CommandName
  )

  return $null -ne (Get-Command $CommandName -ErrorAction SilentlyContinue)
}

if (-not (Test-Path -LiteralPath $packageJsonPath)) {
  throw "Could not find package.json at '$packageJsonPath'."
}

if (-not (Test-Path -LiteralPath $packagesFolder)) {
  New-Item -ItemType Directory -Path $packagesFolder | Out-Null
}

$resolvedSharedFolder = Resolve-ConfirmedFolderPath -InitialPath $SharedFolder -Label 'npm' -SkipPrompt:$SkipConfirmation

$packageDefinitions = @(
  [pscustomobject]@{ PackageName = '@chill-sharp/ui-core'; ArchivePrefix = 'chill-sharp-ui-core' }
  [pscustomobject]@{ PackageName = '@chill-sharp/ng-client'; ArchivePrefix = 'chill-sharp-ng-client' }
  [pscustomobject]@{ PackageName = '@chill-sharp/ts-client'; ArchivePrefix = 'chill-sharp-ts-client' }
)

$packageJson = Get-Content -LiteralPath $packageJsonPath -Raw | ConvertFrom-Json
if ($null -eq $packageJson.dependencies) {
  throw "package.json at '$packageJsonPath' does not define a dependencies object."
}

foreach ($definition in $packageDefinitions) {
  $latestArchive = Get-LatestArchive -FolderPath $resolvedSharedFolder -ArchivePrefix $definition.ArchivePrefix
  $destinationArchivePath = Join-Path $packagesFolder $latestArchive.File.Name

  foreach ($existingArchive in Get-ChildItem -LiteralPath $packagesFolder -Filter "$($definition.ArchivePrefix)-*.tgz" -File -ErrorAction SilentlyContinue) {
    if (-not $existingArchive.FullName.Equals($destinationArchivePath, [System.StringComparison]::OrdinalIgnoreCase)) {
      Remove-Item -LiteralPath $existingArchive.FullName -Force
    }
  }

  Copy-Item -LiteralPath $latestArchive.File.FullName -Destination $destinationArchivePath -Force
  $packageJson.dependencies | Add-Member -NotePropertyName $definition.PackageName -NotePropertyValue "file:./packages/$($latestArchive.File.Name)" -Force

  Write-Host "Copied $($definition.PackageName) $($latestArchive.VersionText) to '$destinationArchivePath'."
}

$packageJson | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $packageJsonPath

if (Test-CommandAvailable -CommandName 'npm') {
  Write-Host 'Refreshing package-lock.json with npm install --package-lock-only --ignore-scripts...'
  Push-Location $scriptDirectory
  try {
    & npm install --package-lock-only --ignore-scripts
    if ($LASTEXITCODE -ne 0) {
      Write-Warning 'npm install --package-lock-only --ignore-scripts did not complete successfully. Local archives were copied, but package-lock.json may still need a manual refresh.'
    }
  }
  finally {
    Pop-Location
  }
}
else {
  Write-Warning "npm was not found on PATH. Local archives were copied, but package-lock.json was not refreshed."
}

Write-Host 'UI template dependencies were upgraded from the shared npm folder.'
