[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)]
  [string]$PackageDirectory,

  [string]$SharedFolder = 'C:\source\npm-shared'
)

# Fail fast on script errors and undefined-variable mistakes.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Run npm commands from the target package directory and convert npm failures into clear script errors.
function Invoke-NpmCommand {
  param(
    [Parameter(Mandatory = $true)]
    [string[]]$Arguments,

    [Parameter(Mandatory = $true)]
    [string]$FailureMessage
  )

  Push-Location $PackageDirectory
  try {
    & npm @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) {
      throw $FailureMessage
    }
  }
  finally {
    Pop-Location
  }
}

# Load package metadata so the script can validate inputs and compute the output archive name.
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

# Refresh dependencies before building so local file-based packages stay aligned after folder moves or edits.
Write-Host "Installing dependencies for $packageName in '$PackageDirectory'..."
Invoke-NpmCommand -Arguments @('install') -FailureMessage "npm install failed for '$packageName'."

# Build the package before packing so the generated archive contains the latest compiled output.
Write-Host "Building $packageName $packageVersion..."
Invoke-NpmCommand -Arguments @('run', 'build') -FailureMessage "npm run build failed for '$packageName'."

# Ensure the shared folder exists before writing the package archive into it.
if (-not (Test-Path -LiteralPath $SharedFolder)) {
  Write-Host "Creating shared folder '$SharedFolder'..."
  New-Item -ItemType Directory -Path $SharedFolder | Out-Null
}

# Derive the final npm archive name from package name and version.
$archiveName = (($packageName -replace '^@', '') -replace '/', '-') + "-$packageVersion.tgz"
$archivePath = Join-Path $SharedFolder $archiveName

# Pack the already-built package and store the resulting tarball in the shared folder.
Write-Host "Packing $packageName into '$SharedFolder'..."
Push-Location $PackageDirectory
try {
  & npm pack --ignore-scripts --pack-destination $SharedFolder | Out-Host
  if ($LASTEXITCODE -ne 0) {
    throw "npm pack failed for '$packageName'."
  }
}
finally {
  Pop-Location
}

if (-not (Test-Path -LiteralPath $archivePath)) {
  throw "npm pack completed for '$packageName', but '$archivePath' was not found."
}

# Print the final archive path so consumers know exactly what was produced.
Write-Host ''
Write-Host "Package published to shared folder successfully: $archivePath"
