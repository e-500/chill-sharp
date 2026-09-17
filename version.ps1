[CmdletBinding()]
param(
    [ValidateSet('major', 'minor', 'build')]
    [string]$Part
)

$propsPath = Join-Path $PSScriptRoot 'Directory.Build.props'
$content = Get-Content -LiteralPath $propsPath -Raw
$match = [regex]::Match($content, '<ChillSharpVersion>(?<version>\d+)\.(?<minor>\d+)\.(?<build>\d+)</ChillSharpVersion>')

if (-not $match.Success) {
    throw "Could not find a three-part ChillSharpVersion in $propsPath."
}

if (-not $Part) {
    $Part = Read-Host 'Increase which version part? (major, minor, build)'
    $Part = $Part.Trim().ToLowerInvariant()
}

if ($Part -notin @('major', 'minor', 'build')) {
    throw 'Choose major, minor, or build.'
}

$major = [int]$match.Groups['version'].Value
$minor = [int]$match.Groups['minor'].Value
$build = [int]$match.Groups['build'].Value

switch ($Part) {
    'major' { $major++; $minor = 0; $build = 0 }
    'minor' { $minor++; $build = 0 }
    'build' { $build++ }
}

$oldVersion = $match.Value -replace '</?ChillSharpVersion>', ''
$newVersion = "$major.$minor.$build"
$updatedContent = [regex]::Replace(
    $content,
    '<ChillSharpVersion>\d+\.\d+\.\d+</ChillSharpVersion>',
    "<ChillSharpVersion>$newVersion</ChillSharpVersion>",
    1)

[System.IO.File]::WriteAllText($propsPath, $updatedContent, [System.Text.UTF8Encoding]::new($false))

$packageJsonPaths = @(
    'extra/chill-sharp-ts-client/package.json',
    'extra/chill-sharp-ng-client/package.json',
    'extra/chill-sharp-react-client/package.json',
    'extra/chill-sharp-vue-client/package.json',
    'extra/chill-sharp-ui-core/package.json',
    'extra/chill-sharp-ui-template/package.json'
)

foreach ($relativePath in $packageJsonPaths) {
    $path = Join-Path $PSScriptRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        continue
    }

    $manifest = Get-Content -LiteralPath $path -Raw
    $manifest = [regex]::Replace($manifest, '("version"\s*:\s*")\d+\.\d+\.\d+("\s*,?)', {
        param($match)
        "$($match.Groups[1].Value)$newVersion$($match.Groups[2].Value)"
    }, 1)
    $manifest = [regex]::Replace($manifest, '("@chill-sharp/(?:ts-client|ng-client|ui-core)"\s*:\s*"\^)\d+\.\d+\.\d+', {
        param($match)
        "$($match.Groups[1].Value)$newVersion"
    })
    $manifest = [regex]::Replace($manifest, '(file:\./packages/chill-sharp-(?:ts-client|ng-client|ui-core)-)\d+\.\d+\.\d+(\.tgz)', {
        param($match)
        "$($match.Groups[1].Value)$newVersion$($match.Groups[2].Value)"
    })
    [System.IO.File]::WriteAllText($path, $manifest, [System.Text.UTF8Encoding]::new($false))
}

$packageLockPaths = @(
    'extra/chill-sharp-ts-client/package-lock.json',
    'extra/chill-sharp-ng-client/package-lock.json',
    'extra/chill-sharp-react-client/package-lock.json',
    'extra/chill-sharp-vue-client/package-lock.json',
    'extra/chill-sharp-ui-template/package-lock.json'
)

foreach ($relativePath in $packageLockPaths) {
    $path = Join-Path $PSScriptRoot $relativePath
    if (Test-Path -LiteralPath $path) {
        $lockFile = Get-Content -LiteralPath $path -Raw
        $lockVersionMatch = [regex]::Match($lockFile, '(?s)^\s*\{.*?"version"\s*:\s*"(?<version>\d+\.\d+\.\d+)"')
        if ($lockVersionMatch.Success) {
            $lockFile = $lockFile.Replace($lockVersionMatch.Groups['version'].Value, $newVersion)
        }
        [System.IO.File]::WriteAllText($path, $lockFile, [System.Text.UTF8Encoding]::new($false))
    }
}

$pythonProjectPath = Join-Path $PSScriptRoot 'extra/chill-sharp-py-client/pyproject.toml'
if (Test-Path -LiteralPath $pythonProjectPath) {
    $pythonProject = Get-Content -LiteralPath $pythonProjectPath -Raw
    $pythonProject = [regex]::Replace($pythonProject, '(?m)^(version\s*=\s*")\d+\.\d+\.\d+(")', {
        param($match)
        "$($match.Groups[1].Value)$newVersion$($match.Groups[2].Value)"
    }, 1)
    [System.IO.File]::WriteAllText($pythonProjectPath, $pythonProject, [System.Text.UTF8Encoding]::new($false))
}

Write-Host "ChillSharp version: $oldVersion -> $newVersion"
Write-Host 'Updated .NET projects, JavaScript package manifests and locks, and the Python package manifest.'
Write-Host 'Run: dotnet test .\ChillSharp.Test\ChillSharp.Test.csproj'

$createCommit = (Read-Host "Create Git commit 'Switched to $newVersion'? (y/N)").Trim().ToLowerInvariant()
if ($createCommit -in @('y', 'yes')) {
    & git -C $PSScriptRoot diff --cached --quiet
    if ($LASTEXITCODE -eq 1) {
        Write-Warning 'The Git index already contains staged changes. No commit was created.'
        exit 0
    }
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not inspect the Git index.'
    }

    $releaseFiles = @('Directory.Build.props') + $packageJsonPaths + $packageLockPaths + @('extra/chill-sharp-py-client/pyproject.toml')
    $releaseFiles = $releaseFiles | Where-Object { Test-Path -LiteralPath (Join-Path $PSScriptRoot $_) }
    & git -C $PSScriptRoot add -- $releaseFiles
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not stage release version files.'
    }

    & git -C $PSScriptRoot diff --cached --quiet
    if ($LASTEXITCODE -eq 1) {
        & git -C $PSScriptRoot commit -m "Switched to $newVersion"
        if ($LASTEXITCODE -ne 0) {
            throw 'Git commit failed.'
        }
    }
    elseif ($LASTEXITCODE -ne 0) {
        throw 'Could not inspect staged release version files.'
    }
    else {
        Write-Host 'No version file changes were available to commit.'
    }
}
