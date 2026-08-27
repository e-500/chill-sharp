[CmdletBinding()]
param(
  [string]$SharedFolder = 'C:\source\nuget-shared',
  [switch]$SkipConfirmation
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$UpgradeScriptVersion = 0
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$csprojFiles = @(Get-ChildItem -LiteralPath $scriptDirectory -Filter '*.csproj' -File)
if ($csprojFiles.Count -eq 0) {
  throw "Could not find a .csproj file in '$scriptDirectory'."
} elseif ($csprojFiles.Count -gt 1) {
  throw "Found multiple .csproj files in '$scriptDirectory': $($csprojFiles.Name -join ', ')."
}
$templateProjectPath = $csprojFiles[0].FullName
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

function Extract-ChillSharpSkills {
  param(
    [Parameter(Mandatory = $true)]
    [string]$NupkgPath,

    [Parameter(Mandatory = $true)]
    [string]$TargetFolder
  )

  $agentsDir = Join-Path $TargetFolder '.agents'
  if (-not (Test-Path -LiteralPath $agentsDir)) {
    New-Item -ItemType Directory -Path $agentsDir | Out-Null
  }

  $skillsTargetDir = Join-Path $agentsDir 'skills'
  if (Test-Path -LiteralPath $skillsTargetDir) {
    Remove-Item -LiteralPath $skillsTargetDir -Recurse -Force | Out-Null
  }
  New-Item -ItemType Directory -Path $skillsTargetDir | Out-Null

  Add-Type -AssemblyName System.IO.Compression
  $archive = [System.IO.Compression.ZipFile]::OpenRead($NupkgPath)
  try {
    foreach ($entry in $archive.Entries) {
      if ($entry.FullName -match '^\.agents/skills/(?<RelativePath>.+)$') {
        $relativePath = $Matches.RelativePath
        $relativePath = $relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar
        $destPath = Join-Path $skillsTargetDir $relativePath
        $destDir = Split-Path -Parent $destPath
        if (-not (Test-Path -LiteralPath $destDir)) {
          New-Item -ItemType Directory -Path $destDir | Out-Null
        }
        if (-not $destPath.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
          [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destPath, $true)
        }
      }
    }
  }
  finally {
    $archive.Dispose()
  }
}

function Update-UpgradeScriptIfNewer {
  param(
    [Parameter(Mandatory = $true)]
    [string]$NupkgPath,

    [Parameter(Mandatory = $true)]
    [string]$ScriptPath
  )

  $temporaryScriptPath = [System.IO.Path]::GetTempFileName()
  Add-Type -AssemblyName System.IO.Compression
  $archive = [System.IO.Compression.ZipFile]::OpenRead($NupkgPath)
  try {
    $entry = $archive.GetEntry('template-customization/upgrade.ps1.template')
    if ($null -eq $entry) {
      return $false
    }

    [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $temporaryScriptPath, $true)
    $packagedContents = Get-Content -LiteralPath $temporaryScriptPath -Raw
    $versionMatch = [System.Text.RegularExpressions.Regex]::Match(
      $packagedContents,
      '(?m)^\s*\$UpgradeScriptVersion\s*=\s*(?<Version>\d+)\s*$')

    if (-not $versionMatch.Success) {
      throw "The packaged upgrade script does not define a valid `$UpgradeScriptVersion."
    }

    $packagedVersion = [int]$versionMatch.Groups['Version'].Value
    if ($packagedVersion -le $UpgradeScriptVersion) {
      return $false
    }

    Copy-Item -LiteralPath $temporaryScriptPath -Destination $ScriptPath -Force
    Write-Host "Updated upgrade.ps1 from internal version $UpgradeScriptVersion to $packagedVersion. Rerun the script to continue the package upgrade."
    return $true
  }
  finally {
    $archive.Dispose()
    if (Test-Path -LiteralPath $temporaryScriptPath) {
      Remove-Item -LiteralPath $temporaryScriptPath -Force
    }
  }
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
if (Update-UpgradeScriptIfNewer -NupkgPath $destinationArchivePath -ScriptPath $MyInvocation.MyCommand.Path) {
  exit 0
}
Set-ChillSharpPackageReferenceVersion -ProjectPath $templateProjectPath -PackageVersion $latestPackage.VersionText
Extract-ChillSharpSkills -NupkgPath $destinationArchivePath -TargetFolder $scriptDirectory
$removedGlobalCachePath = Remove-ChillSharpGlobalPackageCache -PackageVersion $latestPackage.VersionText
$removedRestoreStatePaths = @(Remove-StaleRestoreState -RestoreStateFolderPath $restoreStateFolder)

Write-Host "Copied ChillSharp $($latestPackage.VersionText) from '$($latestPackage.File.FullName)' to '$destinationArchivePath'."
Write-Host "Updated $($csprojFiles[0].Name) to ChillSharp $($latestPackage.VersionText)."
Write-Host "Extracted and updated agent skills in '.agents/skills/'."

if ($null -ne $removedGlobalCachePath) {
  Write-Host "Removed cached global package '$removedGlobalCachePath' so NuGet will re-extract ChillSharp $($latestPackage.VersionText)."
}

if ($removedRestoreStatePaths.Count -gt 0) {
  Write-Host 'Removed stale restore state:'
  foreach ($removedRestoreStatePath in $removedRestoreStatePaths) {
    Write-Host " - $removedRestoreStatePath"
  }
}
