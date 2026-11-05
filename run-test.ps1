param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

function Format-CoveragePercent([double]$Rate) {
    $value = ([Math]::Floor($Rate * 10000) / 100).ToString("0.##", [System.Globalization.CultureInfo]::InvariantCulture)
    return "$value%"
}

$repositoryRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$testProject = Join-Path $repositoryRoot "ChillSharp.Test\ChillSharp.Test.csproj"
$resultsDirectory = Join-Path $repositoryRoot "TestResults"
$coveragePrefix = Join-Path $resultsDirectory "coverage"

New-Item -ItemType Directory -Force -Path $resultsDirectory | Out-Null

dotnet test $testProject `
    --configuration $Configuration `
    --results-directory $resultsDirectory `
    --logger "trx;LogFileName=tests.trx" `
    "-p:CollectCoverage=true" `
    "-p:CoverletOutput=$coveragePrefix" `
    "-p:CoverletOutputFormat=json%2ccobertura"

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$coverageReport = "$coveragePrefix.cobertura.xml"
$coverageJsonReport = "$coveragePrefix.json"
if (-not (Test-Path $coverageReport)) {
    throw "Coverage report was not generated: $coverageReport"
}
if (-not (Test-Path $coverageJsonReport)) {
    throw "Coverage report was not generated: $coverageJsonReport"
}

$coverage = [xml](Get-Content -Raw -Path $coverageReport)
$coverageJson = Get-Content -Raw -Path $coverageJsonReport | ConvertFrom-Json
$rows = @(
    foreach ($package in $coverage.coverage.packages.package) {
        $methods = @($package.SelectNodes(".//methods/method"))
        $coveredMethods = @($methods | Where-Object { [double]$_.GetAttribute("line-rate") -gt 0 }).Count
        $methodRate = if ($methods.Count -eq 0) { 1.0 } else { $coveredMethods / $methods.Count }
        $moduleData = $coverageJson.PSObject.Properties["$($package.GetAttribute('name')).dll"].Value
        $lineHits = @{}
        $branchCount = 0
        $coveredBranches = 0

        foreach ($file in $moduleData.PSObject.Properties) {
            foreach ($class in $file.Value.PSObject.Properties) {
                foreach ($method in $class.Value.PSObject.Properties) {
                    foreach ($line in $method.Value.Lines.PSObject.Properties) {
                        $lineKey = "$($file.Name)|$($line.Name)"
                        if (-not $lineHits.ContainsKey($lineKey) -or [int]$line.Value -gt $lineHits[$lineKey]) {
                            $lineHits[$lineKey] = [int]$line.Value
                        }
                    }

                    foreach ($branch in $method.Value.Branches) {
                        $branchCount++
                        if ([int]$branch.Hits -gt 0) {
                            $coveredBranches++
                        }
                    }
                }
            }
        }

        $lineCount = $lineHits.Count
        $coveredLines = @($lineHits.Values | Where-Object { $_ -gt 0 }).Count

        [pscustomobject]@{
            Module = $package.GetAttribute("name")
            Line = if ($lineCount -eq 0) { 1.0 } else { $coveredLines / $lineCount }
            Branch = if ($branchCount -eq 0) { 1.0 } else { $coveredBranches / $branchCount }
            Method = $methodRate
            Methods = [pscustomobject]@{ Covered = $coveredMethods; Count = $methods.Count }
            LineCount = $lineCount
            CoveredLines = $coveredLines
            BranchCount = $branchCount
            CoveredBranches = $coveredBranches
        }
    }
)

$allMethods = @($rows | ForEach-Object { $_.Methods })
$totalMethodRate = if ($allMethods.Count -eq 0) { 1.0 } else {
    ($allMethods | Measure-Object -Property Covered -Sum).Sum / ($allMethods | Measure-Object -Property Count -Sum).Sum
}
$averageLine = (($rows | ForEach-Object { [Math]::Floor($_.Line * 10000) / 10000 }) | Measure-Object -Average).Average
$averageBranch = (($rows | ForEach-Object { [Math]::Floor($_.Branch * 10000) / 10000 }) | Measure-Object -Average).Average
$averageMethod = (($rows | ForEach-Object { [Math]::Floor($_.Method * 10000) / 10000 }) | Measure-Object -Average).Average
$totalLineCount = ($rows | Measure-Object -Property LineCount -Sum).Sum
$totalCoveredLines = ($rows | Measure-Object -Property CoveredLines -Sum).Sum
$totalBranchCount = ($rows | Measure-Object -Property BranchCount -Sum).Sum
$totalCoveredBranches = ($rows | Measure-Object -Property CoveredBranches -Sum).Sum
$totalLineRate = if ($totalLineCount -eq 0) { 1.0 } else { $totalCoveredLines / $totalLineCount }
$totalBranchRate = if ($totalBranchCount -eq 0) { 1.0 } else { $totalCoveredBranches / $totalBranchCount }
$lineWidth = [Math]::Max(6, ($rows | ForEach-Object { (Format-CoveragePercent $_.Line).Length } | Measure-Object -Maximum).Maximum)
$branchWidth = [Math]::Max(6, ($rows | ForEach-Object { (Format-CoveragePercent $_.Branch).Length } | Measure-Object -Maximum).Maximum)
$methodWidth = [Math]::Max(6, ($rows | ForEach-Object { (Format-CoveragePercent $_.Method).Length } | Measure-Object -Maximum).Maximum)
$moduleWidth = [Math]::Max(6, ($rows | ForEach-Object { $_.Module.Length } | Measure-Object -Maximum).Maximum)
$border = "+-{0}-+-{1}-+-{2}-+-{3}-+" -f ("-" * $moduleWidth), ("-" * $lineWidth), ("-" * $branchWidth), ("-" * $methodWidth)

Write-Host ""
Write-Host $border
Write-Host ("| {0,-$moduleWidth} | {1,$lineWidth} | {2,$branchWidth} | {3,$methodWidth} |" -f "Module", "Line", "Branch", "Method")
Write-Host $border
foreach ($row in $rows) {
    Write-Host ("| {0,-$moduleWidth} | {1,$lineWidth} | {2,$branchWidth} | {3,$methodWidth} |" -f `
        $row.Module, (Format-CoveragePercent $row.Line), (Format-CoveragePercent $row.Branch), (Format-CoveragePercent $row.Method))
    Write-Host $border
}
Write-Host ""
Write-Host ("| {0,-$moduleWidth} | {1,$lineWidth} | {2,$branchWidth} | {3,$methodWidth} |" -f "", "Line", "Branch", "Method")
Write-Host $border
Write-Host ("| {0,-$moduleWidth} | {1,$lineWidth} | {2,$branchWidth} | {3,$methodWidth} |" -f `
    "Total", (Format-CoveragePercent $totalLineRate), `
    (Format-CoveragePercent $totalBranchRate), `
    (Format-CoveragePercent $totalMethodRate))
Write-Host $border
Write-Host ("| {0,-$moduleWidth} | {1,$lineWidth} | {2,$branchWidth} | {3,$methodWidth} |" -f `
    "Average", (Format-CoveragePercent $averageLine), (Format-CoveragePercent $averageBranch), (Format-CoveragePercent $averageMethod))
Write-Host $border
Write-Host ""
Write-Host "Test results: $resultsDirectory\tests.trx"
Write-Host "Coverage reports: $coveragePrefix.json and $coveragePrefix.cobertura.xml"
