[CmdletBinding()]
param(
    [string]$Python = "py",
    [string]$Branch = "development"
)

$repo = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$script = Join-Path $repo "tools\weekly_squash.py"
if (-not (Test-Path -LiteralPath $script)) {
    throw "Cannot find $script"
}

# The task starts in the repository so Git resolves the intended checkout.
$action = New-ScheduledTaskAction -Execute $Python -Argument ('"{0}" --branch "{1}" --apply --push' -f $script, $Branch) -WorkingDirectory $repo
$trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Saturday -At 12:00PM
$description = "Squash commits made since the previous Cumulative commit on $Branch."

Register-ScheduledTask -TaskName "ChillSharp weekly squash ($Branch)" `
    -Action $action -Trigger $trigger -Description $description -Force

Write-Host "Created scheduled task: ChillSharp weekly squash ($Branch)"
