[CmdletBinding()]
param()

# Fail fast on script errors and undefined-variable mistakes.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Capture the workspace roots used by the menu actions.
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Split-Path -Parent $scriptDirectory
$defaultSharedFolder = 'C:\source\npm-shared'
$defaultNugetSharedFolder = 'C:\source\nuget-shared'
$uiTemplatePath = Join-Path $scriptDirectory 'ui\chill-sharp-ui-template'
$apiTemplatePath = Join-Path $repositoryRoot 'ChillSharp.Template'
$script:NuGetSharedFolder = $defaultNugetSharedFolder

# Track publish settings per publishable package during the current script session.
$script:Packages = @(
  [pscustomobject]@{
    Key = 'ts-client'
    Label = 'chill-sharp-ts-client'
    PublishScript = Join-Path $scriptDirectory 'chill-sharp-ts-client\publish-to-shared-folder.ps1'
    Mode = 'shared-folder'
    SharedFolder = $defaultSharedFolder
  }
  [pscustomobject]@{
    Key = 'ng-client'
    Label = 'chill-sharp-ng-client'
    PublishScript = Join-Path $scriptDirectory 'chill-sharp-ng-client\publish-to-shared-folder.ps1'
    Mode = 'shared-folder'
    SharedFolder = $defaultSharedFolder
  }
  [pscustomobject]@{
    Key = 'react-client'
    Label = 'chill-sharp-react-client'
    PublishScript = Join-Path $scriptDirectory 'chill-sharp-react-client\publish-to-shared-folder.ps1'
    Mode = 'shared-folder'
    SharedFolder = $defaultSharedFolder
  }
  [pscustomobject]@{
    Key = 'vue-client'
    Label = 'chill-sharp-vue-client'
    PublishScript = Join-Path $scriptDirectory 'chill-sharp-vue-client\publish-to-shared-folder.ps1'
    Mode = 'shared-folder'
    SharedFolder = $defaultSharedFolder
  }
  [pscustomobject]@{
    Key = 'ui-core'
    Label = '@chill-sharp/ui-core'
    PublishScript = Join-Path $scriptDirectory 'ui\chill-sharp-ui-core\publish-to-shared-folder.ps1'
    Mode = 'shared-folder'
    SharedFolder = $defaultSharedFolder
  }
)

# Track the packable ChillSharp NuGet packages exposed by this repository.
$script:NuGetPackages = @(
  [pscustomobject]@{
    Label = 'ChillSharp'
    ProjectPath = Join-Path $repositoryRoot 'ChillSharp\ChillSharp.csproj'
  }
  [pscustomobject]@{
    Label = 'ChillSharp.Client'
    ProjectPath = Join-Path $repositoryRoot 'ChillSharp.Client\ChillSharp.Client.csproj'
  }
)

function Pause-ForUser {
  Write-Host ''
  [void](Read-Host 'Press Enter to continue')
}

function Get-ModeLabel {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Mode,

    [string]$SharedFolder
  )

  switch ($Mode) {
    'shared-folder' { return "Shared npm folder ($SharedFolder)" }
    'private-registry' { return 'Private npm registry (FUTURE IMPLEMENTATION)' }
    'public-npm' { return 'Public npm (FUTURE IMPLEMENTATION)' }
    default { return $Mode }
  }
}

function Read-MenuChoice {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Prompt,

    [Parameter(Mandatory = $true)]
    [string[]]$ValidChoices
  )

  while ($true) {
    $choice = [string](Read-Host $Prompt)
    if ($ValidChoices -contains $choice) {
      return $choice
    }

    Write-Warning "Invalid choice. Valid options: $($ValidChoices -join ', ')"
  }
}

function Show-PackageTable {
  Write-Host ''
  Write-Host 'Publishable packages:'

  for ($index = 0; $index -lt $script:Packages.Count; $index++) {
    $package = $script:Packages[$index]
    $modeLabel = Get-ModeLabel -Mode $package.Mode -SharedFolder $package.SharedFolder
    Write-Host ("{0}. {1} [{2}]" -f ($index + 1), $package.Label, $modeLabel)
  }

  Write-Host 'A. All packages'
}

function Select-Packages {
  Show-PackageTable
  Write-Host ''

  $validChoices = @('A')
  for ($index = 1; $index -le $script:Packages.Count; $index++) {
    $validChoices += [string]$index
  }

  $choice = Read-MenuChoice -Prompt 'Select package number or A for all' -ValidChoices $validChoices
  if ($choice -eq 'A') {
    return $script:Packages
  }

  return @($script:Packages[[int]$choice - 1])
}

function Set-PublishMode {
  # Let the user change publish settings for either npm packages or the ChillSharp NuGet package output.
  Write-Host ''
  Write-Host 'Publish targets:'
  Write-Host '1. extra npm packages'
  Write-Host '2. ChillSharp NuGet packages'
  Write-Host ''

  $targetChoice = Read-MenuChoice -Prompt 'Select publish target' -ValidChoices @('1', '2')
  switch ($targetChoice) {
    '1' {
      $selectedPackages = Select-Packages

      Write-Host ''
      Write-Host 'Publish modes:'
      Write-Host '1. Shared npm folder'
      Write-Host '2. Private npm registry (FUTURE IMPLEMENTATION)'
      Write-Host '3. Public npm (FUTURE IMPLEMENTATION)'
      Write-Host ''

      $modeChoice = Read-MenuChoice -Prompt 'Select publish mode' -ValidChoices @('1', '2', '3')
      switch ($modeChoice) {
        '1' {
          $currentFolder = $selectedPackages[0].SharedFolder
          $sharedFolder = [string](Read-Host "Shared folder path [$currentFolder]")
          if ([string]::IsNullOrWhiteSpace($sharedFolder)) {
            $sharedFolder = $currentFolder
          }

          foreach ($package in $selectedPackages) {
            $package.Mode = 'shared-folder'
            $package.SharedFolder = $sharedFolder
          }
        }
        '2' {
          foreach ($package in $selectedPackages) {
            $package.Mode = 'private-registry'
          }
        }
        '3' {
          foreach ($package in $selectedPackages) {
            $package.Mode = 'public-npm'
          }
        }
      }

      Write-Host ''
      Write-Host 'Updated package configuration:'
      foreach ($package in $selectedPackages) {
        Write-Host "- $($package.Label): $(Get-ModeLabel -Mode $package.Mode -SharedFolder $package.SharedFolder)"
      }
    }
    '2' {
      $nugetFolder = [string](Read-Host "NuGet shared folder path [$script:NuGetSharedFolder]")
      if ([string]::IsNullOrWhiteSpace($nugetFolder)) {
        $nugetFolder = $script:NuGetSharedFolder
      }

      $script:NuGetSharedFolder = $nugetFolder

      Write-Host ''
      Write-Host "Updated NuGet package output folder: $script:NuGetSharedFolder"
    }
  }
}

function Publish-Package {
  param(
    [Parameter(Mandatory = $true)]
    [pscustomobject]$Package
  )

  # Dispatch publishing according to the configured mode for the selected package.
  switch ($Package.Mode) {
    'shared-folder' {
      if (-not (Test-Path -LiteralPath $Package.PublishScript)) {
        throw "Could not find publish script at '$($Package.PublishScript)'."
      }

      Write-Host ''
      Write-Host "Publishing $($Package.Label) to shared folder '$($Package.SharedFolder)'..."
      & $Package.PublishScript -SharedFolder $Package.SharedFolder
      if ($LASTEXITCODE -ne 0) {
        throw "Publish script failed for '$($Package.Label)'."
      }
    }
    'private-registry' {
      Write-Warning "Publishing $($Package.Label) to a private npm registry is FUTURE IMPLEMENTATION."
    }
    'public-npm' {
      Write-Warning "Publishing $($Package.Label) to public npm is FUTURE IMPLEMENTATION."
    }
    default {
      throw "Unsupported publish mode '$($Package.Mode)' for '$($Package.Label)'."
    }
  }
}

function Publish-Packages {
  # Publish either extra npm packages or the ChillSharp NuGet package set.
  Write-Host ''
  Write-Host 'Publish targets:'
  Write-Host '1. extra npm packages'
  Write-Host '2. ChillSharp NuGet packages'
  Write-Host ''

  $targetChoice = Read-MenuChoice -Prompt 'Select publish target' -ValidChoices @('1', '2')
  switch ($targetChoice) {
    '1' {
      $selectedPackages = Select-Packages

      foreach ($package in $selectedPackages) {
        Publish-Package -Package $package
      }
    }
    '2' {
      Publish-NuGetPackages
    }
  }

  Write-Host ''
  Write-Host 'Publish action completed.'
}

function Show-NuGetPackageTable {
  Write-Host ''
  Write-Host 'Publishable ChillSharp NuGet packages:'

  for ($index = 0; $index -lt $script:NuGetPackages.Count; $index++) {
    $package = $script:NuGetPackages[$index]
    Write-Host ("{0}. {1}" -f ($index + 1), $package.Label)
  }

  Write-Host 'A. All NuGet packages'
}

function Select-NuGetPackages {
  Show-NuGetPackageTable
  Write-Host ''

  $validChoices = @('A')
  for ($index = 1; $index -le $script:NuGetPackages.Count; $index++) {
    $validChoices += [string]$index
  }

  $choice = Read-MenuChoice -Prompt 'Select package number or A for all' -ValidChoices $validChoices
  if ($choice -eq 'A') {
    return $script:NuGetPackages
  }

  return @($script:NuGetPackages[[int]$choice - 1])
}

function Publish-NuGetPackages {
  # Pack one or more ChillSharp .NET packages into a shared NuGet folder.
  $selectedPackages = Select-NuGetPackages

  $outputFolder = $script:NuGetSharedFolder

  if (-not (Test-Path -LiteralPath $outputFolder)) {
    New-Item -ItemType Directory -Path $outputFolder | Out-Null
  }

  foreach ($package in $selectedPackages) {
    if (-not (Test-Path -LiteralPath $package.ProjectPath)) {
      throw "Could not find project file at '$($package.ProjectPath)'."
    }

    Write-Host ''
    Write-Host "Packing $($package.Label) into '$outputFolder'..."
    & dotnet pack $package.ProjectPath -c Release -o $outputFolder
    if ($LASTEXITCODE -ne 0) {
      throw "dotnet pack failed for '$($package.Label)'."
    }
  }

  Write-Host ''
  Write-Host "NuGet package publication completed to '$outputFolder'."
}

function Copy-FilteredItem {
  param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePath,

    [Parameter(Mandatory = $true)]
    [string]$DestinationParentPath,

    [Parameter(Mandatory = $true)]
    [string[]]$ExcludedNames
  )

  $item = Get-Item -LiteralPath $SourcePath -Force
  if ($ExcludedNames -contains $item.Name) {
    return
  }

  $destinationPath = Join-Path $DestinationParentPath $item.Name
  if ($item.PSIsContainer) {
    if (-not (Test-Path -LiteralPath $destinationPath)) {
      New-Item -ItemType Directory -Path $destinationPath | Out-Null
    }

    foreach ($child in Get-ChildItem -LiteralPath $item.FullName -Force) {
      Copy-FilteredItem -SourcePath $child.FullName -DestinationParentPath $destinationPath -ExcludedNames $ExcludedNames
    }

    return
  }

  Copy-Item -LiteralPath $item.FullName -Destination $destinationPath -Force
}

function Copy-TemplateProject {
  param(
    [Parameter(Mandatory = $true)]
    [string]$TemplatePath,

    [Parameter(Mandatory = $true)]
    [string]$DestinationPrompt,

    [Parameter(Mandatory = $true)]
    [string]$TemplateLabel,

    [Parameter(Mandatory = $true)]
    [string[]]$ExcludedNames
  )

  if (-not (Test-Path -LiteralPath $TemplatePath)) {
    throw "Could not find $TemplateLabel template at '$TemplatePath'."
  }

  Write-Host ''
  $destinationInput = [string](Read-Host $DestinationPrompt)
  if ([string]::IsNullOrWhiteSpace($destinationInput)) {
    throw 'Destination folder is required.'
  }

  $destinationPath = [System.IO.Path]::GetFullPath($destinationInput)
  if (Test-Path -LiteralPath $destinationPath) {
    $existingEntries = Get-ChildItem -LiteralPath $destinationPath -Force -ErrorAction SilentlyContinue
    if ($existingEntries.Count -gt 0) {
      throw "Destination folder '$destinationPath' already exists and is not empty."
    }
  }
  else {
    New-Item -ItemType Directory -Path $destinationPath | Out-Null
  }

  foreach ($item in Get-ChildItem -LiteralPath $TemplatePath -Force) {
    Copy-FilteredItem -SourcePath $item.FullName -DestinationParentPath $destinationPath -ExcludedNames $ExcludedNames
  }

  Write-Host ''
  Write-Host "$TemplateLabel template copied to '$destinationPath'."
}

function Create-UiFromTemplate {
  # Copy the UI template into a new destination folder without transient build artifacts.
  Copy-TemplateProject `
    -TemplatePath $uiTemplatePath `
    -DestinationPrompt 'Destination folder for the new UI project' `
    -TemplateLabel 'UI' `
    -ExcludedNames @('node_modules', 'dist', '.angular')
}

function Create-ApiFromTemplate {
  # Copy the API template into a new destination folder without transient build artifacts.
  Copy-TemplateProject `
    -TemplatePath $apiTemplatePath `
    -DestinationPrompt 'Destination folder for the new API project' `
    -TemplateLabel 'API' `
    -ExcludedNames @('bin', 'obj', '.vs')
}

function Test-IsWithinRoot {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [string]$RootPath
  )

  $normalizedPath = [System.IO.Path]::GetFullPath($Path).TrimEnd('\')
  $normalizedRoot = [System.IO.Path]::GetFullPath($RootPath).TrimEnd('\')

  return $normalizedPath.StartsWith($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-IsSameOrChildPath {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [string]$CandidateParentPath
  )

  $normalizedPath = [System.IO.Path]::GetFullPath($Path).TrimEnd('\')
  $normalizedParent = [System.IO.Path]::GetFullPath($CandidateParentPath).TrimEnd('\')

  if ($normalizedPath.Equals($normalizedParent, [System.StringComparison]::OrdinalIgnoreCase)) {
    return $true
  }

  return $normalizedPath.StartsWith($normalizedParent + '\', [System.StringComparison]::OrdinalIgnoreCase)
}

function Cleanup-Workspace {
  # Remove dependency folders and common temporary build artifacts from the extra workspace.
  $cleanupTargets = New-Object System.Collections.Generic.List[string]

  $workspacePatterns = @('node_modules', '.angular', '__pycache__')
  foreach ($pattern in $workspacePatterns) {
    $matches = Get-ChildItem -LiteralPath $scriptDirectory -Directory -Recurse -Force -ErrorAction SilentlyContinue |
      Where-Object { $_.Name -eq $pattern }

    foreach ($match in $matches) {
      [void]$cleanupTargets.Add($match.FullName)
    }
  }

  $pythonTempMatches = Get-ChildItem -LiteralPath (Join-Path $scriptDirectory 'chill-sharp-py-client') -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.PSIsContainer -and ($_.Name -eq 'build' -or $_.Name -like '*.egg-info') }
  foreach ($match in $pythonTempMatches) {
    [void]$cleanupTargets.Add($match.FullName)
  }

  $repoTempFolder = Join-Path $repositoryRoot '.tmp-npm-shared'
  if (Test-Path -LiteralPath $repoTempFolder) {
    [void]$cleanupTargets.Add($repoTempFolder)
  }

  # Remove duplicate paths and collapse nested children when a parent folder is already scheduled for deletion.
  $uniqueTargets = $cleanupTargets |
    Sort-Object -Unique |
    Where-Object { Test-IsWithinRoot -Path $_ -RootPath $repositoryRoot }
  $filteredTargets = New-Object System.Collections.Generic.List[string]

  foreach ($target in ($uniqueTargets | Sort-Object { $_.Length }, $_)) {
    $isCoveredByParent = $false

    foreach ($existingTarget in $filteredTargets) {
      if (Test-IsSameOrChildPath -Path $target -CandidateParentPath $existingTarget) {
        $isCoveredByParent = $true
        break
      }
    }

    if (-not $isCoveredByParent) {
      [void]$filteredTargets.Add($target)
    }
  }

  if ($filteredTargets.Count -eq 0) {
    Write-Host ''
    Write-Host 'No cleanup targets were found.'
    return
  }

  Write-Host ''
  Write-Host 'Cleanup will remove:'
  foreach ($target in $filteredTargets) {
    Write-Host "- $target"
  }

  Write-Host ''
  $confirmation = [string](Read-Host 'Type YES to continue')
  if ($confirmation -cne 'YES') {
    Write-Host 'Cleanup cancelled.'
    return
  }

  foreach ($target in $filteredTargets) {
    if (Test-Path -LiteralPath $target) {
      Remove-Item -LiteralPath $target -Recurse -Force
    }
  }

  Write-Host ''
  Write-Host 'Cleanup completed.'
}

function Show-MainMenu {
  # Drive the interactive text menu until the user chooses to exit.
  while ($true) {
    Clear-Host
    Write-Host 'Extra Publish Menu'
    Write-Host '=================='
    Write-Host ''
    Write-Host '1. Select publish mode'
    Write-Host '2. Publish package'
    Write-Host '3. Create UI from template'
    Write-Host '4. Create API from template'
    Write-Host '5. Cleanup'
    Write-Host '0. Exit'
    Write-Host ''

    $choice = Read-MenuChoice -Prompt 'Select an option' -ValidChoices @('1', '2', '3', '4', '5', '0')

    try {
      switch ($choice) {
        '1' { Set-PublishMode; Pause-ForUser }
        '2' { Publish-Packages; Pause-ForUser }
        '3' { Create-UiFromTemplate; Pause-ForUser }
        '4' { Create-ApiFromTemplate; Pause-ForUser }
        '5' { Cleanup-Workspace; Pause-ForUser }
        '0' { return }
      }
    }
    catch {
      Write-Host ''
      Write-Error $_
      Pause-ForUser
    }
  }
}

Show-MainMenu
