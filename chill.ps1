[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishScriptPath = Join-Path $scriptPath 'extra\publish.ps1'
$coreTestProjectPath = Join-Path $scriptPath 'ChillSharp.Test\ChillSharp.Test.csproj'
$uiCorePath = Join-Path $scriptPath 'extra\chill-sharp-ui-core'

function Pause-ForUser { Write-Host ''; [void](Read-Host 'Press Enter to continue') }

function Read-MenuChoice([string]$Prompt, [string[]]$ValidChoices) {
  while ($true) {
    $choice = [string](Read-Host $Prompt)
    if ($ValidChoices -contains $choice) { return $choice }
    Write-Warning "Invalid choice. Valid options: $($ValidChoices -join ', ')"
  }
}

function Show-TestMenu {
  while ($true) {
    Clear-Host
    Write-Host 'Test Menu'
    Write-Host '========='
    Write-Host ''
    Write-Host '1. Test ChillSharp core (C#)'
    Write-Host '2. Test ChillSharp Ui Core (Angular)'
    Write-Host '0. Back'
    Write-Host ''
    switch (Read-MenuChoice -Prompt 'Select an option' -ValidChoices @('1', '2', '0')) {
      '1' { & dotnet test $coreTestProjectPath; if ($LASTEXITCODE -ne 0) { throw 'ChillSharp core tests failed.' }; Pause-ForUser }
      '2' { Push-Location $uiCorePath; try { & npm test; if ($LASTEXITCODE -ne 0) { throw 'ChillSharp UI Core tests failed.' } } finally { Pop-Location }; Pause-ForUser }
      '0' { return }
    }
  }
}

while ($true) {
  Clear-Host
  Write-Host 'ChillSharp Menu'
  Write-Host '==============='
  Write-Host ''
  Write-Host '1. Publish'
  Write-Host '2. Test'
  Write-Host '0. Exit'
  Write-Host ''
  switch (Read-MenuChoice -Prompt 'Select an option' -ValidChoices @('1', '2', '0')) {
    '1' { & $publishScriptPath }
    '2' { Show-TestMenu }
    '0' { return }
  }
}
