[CmdletBinding()]
param(
  [string]$SharedFolder = 'C:\source\nuget-shared',
  [switch]$SkipConfirmation
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$templateProjectPath = Join-Path $scriptDirectory 'Gdf.csproj'
$localPackageFolder = Join-Path $scriptDirectory 'nupkgs'
$restoreStateFolder = Join-Path $scriptDirectory 'obj'

function Get-NuGetGlobalPackagesFolder {
  if (-not [string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)) {
    return [System.IO.Path]::GetFullPath($env:NUGET_PACKAGES)
  }

  return [System.IO.Path]::GetFullPath((Join-Path $HOME '.nuget\packages'))
}

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

function Get-LatestSharedPackage {
  param(
    [Parameter(Mandatory = $true)]
    [string]$FolderPath
  )

  $candidates = foreach ($file in Get-ChildItem -LiteralPath $FolderPath -Filter 'ChillSharp.*.nupkg' -File -ErrorAction Stop) {
    if ($file.Name -notmatch '^ChillSharp\.(?<Version>\d+\.\d+\.\d+(?:[-+][0-9A-Za-z\.-]+)?)\.nupkg$') {
      continue
    }

    [pscustomobject]@{
      File = $file
      VersionText = $matches.Version
      Version = [System.Management.Automation.SemanticVersion]::Parse($matches.Version)
    }
  }

  $latestPackage = $candidates | Sort-Object Version -Descending | Select-Object -First 1
  if ($null -eq $latestPackage) {
    throw "Could not find a ChillSharp.<version>.nupkg archive in '$FolderPath'."
  }

  return $latestPackage
}

function Set-ChillSharpPackageReferenceVersion {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$PackageVersion
  )

  $projectContents = Get-Content -LiteralPath $ProjectPath -Raw
  $pattern = '<PackageReference Include="ChillSharp" Version="[^"]+"\s*/>'
  $replacement = "<PackageReference Include=`"ChillSharp`" Version=`"$PackageVersion`" />"

  if (-not [System.Text.RegularExpressions.Regex]::IsMatch($projectContents, $pattern)) {
    throw "Could not find the ChillSharp package reference in '$ProjectPath'."
  }

  $updatedContents = [System.Text.RegularExpressions.Regex]::Replace($projectContents, $pattern, $replacement)
  Set-Content -LiteralPath $ProjectPath -Value $updatedContents
}

function Remove-ChillSharpGlobalPackageCache {
  param(
    [Parameter(Mandatory = $true)]
    [string]$PackageVersion
  )

  $globalPackagesFolder = Get-NuGetGlobalPackagesFolder
  $cachedPackagePath = Join-Path (Join-Path $globalPackagesFolder 'chillsharp') $PackageVersion

  if (-not (Test-Path -LiteralPath $cachedPackagePath)) {
    return $null
  }

  Remove-Item -LiteralPath $cachedPackagePath -Recurse -Force
  return $cachedPackagePath
}

function Remove-StaleRestoreState {
  param(
    [Parameter(Mandatory = $true)]
    [string]$RestoreStateFolderPath
  )

  if (-not (Test-Path -LiteralPath $RestoreStateFolderPath)) {
    return @()
  }

  $removedPaths = New-Object System.Collections.Generic.List[string]

  foreach ($pattern in @('project.assets.json', 'project.nuget.cache', '*.nuget.g.props', '*.nuget.g.targets')) {
    foreach ($item in Get-ChildItem -LiteralPath $RestoreStateFolderPath -Filter $pattern -File -ErrorAction SilentlyContinue) {
      Remove-Item -LiteralPath $item.FullName -Force
      $removedPaths.Add($item.FullName)
    }
  }

  return $removedPaths
}

if (-not (Test-Path -LiteralPath $templateProjectPath)) {
  throw "Could not find template project at '$templateProjectPath'."
}

if (-not (Test-Path -LiteralPath $localPackageFolder)) {
  New-Item -ItemType Directory -Path $localPackageFolder | Out-Null
}

$resolvedSharedFolder = Resolve-ConfirmedFolderPath -InitialPath $SharedFolder -Label 'NuGet' -SkipPrompt:$SkipConfirmation
$latestPackage = Get-LatestSharedPackage -FolderPath $resolvedSharedFolder
$destinationArchivePath = Join-Path $localPackageFolder $latestPackage.File.Name

foreach ($existingPackage in Get-ChildItem -LiteralPath $localPackageFolder -Filter 'ChillSharp.*.nupkg' -File -ErrorAction SilentlyContinue) {
  if (-not $existingPackage.FullName.Equals($destinationArchivePath, [System.StringComparison]::OrdinalIgnoreCase)) {
    Remove-Item -LiteralPath $existingPackage.FullName -Force
  }
}

Copy-Item -LiteralPath $latestPackage.File.FullName -Destination $destinationArchivePath -Force
Set-ChillSharpPackageReferenceVersion -ProjectPath $templateProjectPath -PackageVersion $latestPackage.VersionText
$removedGlobalCachePath = Remove-ChillSharpGlobalPackageCache -PackageVersion $latestPackage.VersionText
$removedRestoreStatePaths = Remove-StaleRestoreState -RestoreStateFolderPath $restoreStateFolder

Write-Host "Copied ChillSharp $($latestPackage.VersionText) from '$($latestPackage.File.FullName)' to '$destinationArchivePath'."
Write-Host "Updated Gdf.csproj to ChillSharp $($latestPackage.VersionText)."

if ($null -ne $removedGlobalCachePath) {
  Write-Host "Removed cached global package '$removedGlobalCachePath' so NuGet will re-extract ChillSharp $($latestPackage.VersionText)."
}

if ($removedRestoreStatePaths.Count -gt 0) {
  Write-Host 'Removed stale restore state:'
  foreach ($removedRestoreStatePath in $removedRestoreStatePaths) {
    Write-Host " - $removedRestoreStatePath"
  }
}
