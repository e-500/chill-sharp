[CmdletBinding()]
param(
  [string]$SharedFolder = 'C:\source\npm-shared'
)

# Fail fast on script errors and undefined-variable mistakes.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Resolve the package location, the repo root, and the local packages ui-core must consume.
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $scriptDirectory)
$sharedClientPackageDirectories = @(
  (Join-Path $repositoryRoot 'extra\chill-sharp-ts-client'),
  (Join-Path $repositoryRoot 'extra\chill-sharp-ng-client')
)
$packageJsonPath = Join-Path $scriptDirectory 'package.json'
$distPath = Join-Path $scriptDirectory 'dist'
$templateScriptsPath = Join-Path $repositoryRoot 'extra\chill-sharp-ui-template'
$nodeModulesPath = Join-Path $scriptDirectory 'node_modules'
$ngPackagrPackagePath = Join-Path $nodeModulesPath 'ng-packagr\package.json'

# Run npm commands from the current ui-core folder and surface failures with task-specific messages.
function Invoke-NpmCommand {
  param(
    [Parameter(Mandatory = $true)]
    [string[]]$Arguments,

    [Parameter(Mandatory = $true)]
    [string]$FailureMessage
  )

  & npm @Arguments
  if ($LASTEXITCODE -ne 0) {
    throw $FailureMessage
  }
}

# Read package metadata from another local package so this script can locate its shared-folder archive.
function Get-PackageArchiveName {
  param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory
  )

  $packageJsonPath = Join-Path $PackageDirectory 'package.json'
  if (-not (Test-Path -LiteralPath $packageJsonPath)) {
    throw "Could not find package.json at '$packageJsonPath'."
  }

  $package = Get-Content -LiteralPath $packageJsonPath -Raw | ConvertFrom-Json
  $packageName = [string]$package.name
  $packageVersion = [string]$package.version

  if ([string]::IsNullOrWhiteSpace($packageName)) {
    throw "Package name is missing from '$packageJsonPath'."
  }

  if ([string]::IsNullOrWhiteSpace($packageVersion)) {
    throw "Package version is missing from '$packageJsonPath'."
  }

  $normalizedPackageName = $packageName -replace '^@', '' -replace '/', '-'
  return "$normalizedPackageName-$packageVersion.tgz"
}

# Install the freshly published shared client archives into ui-core before building.
function Install-SharedClientPackages {
  $archivePaths = foreach ($packageDirectory in $sharedClientPackageDirectories) {
    $archiveName = Get-PackageArchiveName -PackageDirectory $packageDirectory
    $archivePath = Join-Path $SharedFolder $archiveName

    if (-not (Test-Path -LiteralPath $archivePath)) {
      throw "Expected shared package archive '$archivePath' was not found."
    }

    $archivePath
  }

  Write-Host "Installing shared client package archives into '$scriptDirectory'..."
  Invoke-NpmCommand `
    -Arguments (@('install', '--no-save') + $archivePaths) `
    -FailureMessage 'npm install for shared client packages failed.'
}

# Ensure ui-core has its own build tooling, then refresh the shared tarball dependencies it consumes.
function Ensure-BuildDependencies {
  Install-SharedClientPackages

  if (-not (Test-Path -LiteralPath $ngPackagrPackagePath)) {
    Write-Host "Build dependencies are missing in '$nodeModulesPath'. Running npm install..."
    Invoke-NpmCommand -Arguments @('install') -FailureMessage "npm install failed. Install dependencies in '$scriptDirectory' and try again."
  }
}

# Load and validate ui-core package metadata before starting the build/publish flow.
if (-not (Test-Path -LiteralPath $packageJsonPath)) {
  throw "Could not find package.json at '$packageJsonPath'."
}

$package = Get-Content -LiteralPath $packageJsonPath -Raw | ConvertFrom-Json
$packageName = [string]$package.name
$packageVersion = [string]$package.version

if ([string]::IsNullOrWhiteSpace($packageName)) {
  throw 'Package name is missing from package.json.'
}

if ([string]::IsNullOrWhiteSpace($packageVersion)) {
  throw 'Package version is missing from package.json.'
}

# Build the Angular library from its own folder so relative config paths resolve correctly.
Write-Host "Building $packageName $packageVersion from '$scriptDirectory'..."
Push-Location $scriptDirectory
try {
  Ensure-BuildDependencies
  Invoke-NpmCommand -Arguments @('run', 'build') -FailureMessage 'npm run build failed.'
}
finally {
  Pop-Location
}

if (-not (Test-Path -LiteralPath $distPath)) {
  throw "Build completed but dist folder was not found at '$distPath'."
}

$templateCustomizationPath = Join-Path $distPath 'template-customization'
New-Item -ItemType Directory -Path $templateCustomizationPath -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $templateScriptsPath 'upgrade.ps1') -Destination (Join-Path $templateCustomizationPath 'upgrade.ps1.template') -Force
Copy-Item -LiteralPath (Join-Path $templateScriptsPath 'upgrade.sh') -Destination (Join-Path $templateCustomizationPath 'upgrade.sh.template') -Force
if (Test-Path -LiteralPath (Join-Path $distPath '.agents')) {
  Remove-Item -LiteralPath (Join-Path $distPath '.agents') -Recurse -Force
}
Copy-Item -LiteralPath (Join-Path $templateScriptsPath '.agents') -Destination (Join-Path $distPath '.agents') -Recurse -Force

# Ensure the destination folder exists and compute the expected tarball name for the built package.
if (-not (Test-Path -LiteralPath $SharedFolder)) {
  Write-Host "Creating shared folder '$SharedFolder'..."
  New-Item -ItemType Directory -Path $SharedFolder | Out-Null
}

$archiveName = (($packageName -replace '^@', '') -replace '/', '-') + "-$packageVersion.tgz"
$archivePath = Join-Path $SharedFolder $archiveName

# Pack the dist output into a shared-folder archive that other projects can install directly.
Write-Host "Packing built library from '$distPath' into '$SharedFolder'..."
& npm pack $distPath --pack-destination $SharedFolder | Out-Host
if ($LASTEXITCODE -ne 0) {
  throw 'npm pack failed.'
}

if (-not (Test-Path -LiteralPath $archivePath)) {
  throw "npm pack completed, but the file was not found at '$archivePath'."
}

# Print the produced archive and the install command consumers should use.
Write-Host ''
Write-Host 'Package published to shared folder successfully.'
Write-Host "Archive: $archivePath"
Write-Host "Install with: npm install $archivePath"
