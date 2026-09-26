[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$PublishPath,
    [Parameter(Mandatory)][string]$TaskUser,
    [string]$TaskName = 'MEC-AnnualLeave-0700'
)
$ErrorActionPreference = 'Stop'
$publishDirectory = (Resolve-Path -LiteralPath $PublishPath).Path
$portalDll = Join-Path $publishDirectory 'MEC.Portal.dll'
$runnerPath = Join-Path $publishDirectory 'Scripts\Run-AnnualLeaveJob.ps1'
if (!(Test-Path -LiteralPath $portalDll) -or !(Test-Path -LiteralPath $runnerPath)) {
    throw 'PublishPath must contain MEC.Portal.dll and Scripts\Run-AnnualLeaveJob.ps1.'
}
if ((Get-TimeZone).Id -ne 'Turkey Standard Time') {
    throw 'Set the target server time zone to Turkey Standard Time before installing the 07:00 task.'
}
$dotnetPath = (Get-Command dotnet.exe -ErrorAction Stop).Source
$powershellPath = Join-Path $PSHOME 'powershell.exe'
if (!(Test-Path -LiteralPath $powershellPath)) { $powershellPath = (Get-Command powershell.exe).Source }
$arguments = '-NoProfile -NonInteractive -File "{0}" -PublishPath "{1}" -DotnetPath "{2}"' -f $runnerPath, $publishDirectory, $dotnetPath
$action = New-ScheduledTaskAction -Execute $powershellPath -Argument $arguments -WorkingDirectory $publishDirectory
$trigger = New-ScheduledTaskTrigger -Daily -At '07:00'
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew `
    -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 10) -ExecutionTimeLimit (New-TimeSpan -Hours 2)
if ($PSCmdlet.ShouldProcess($TaskName, 'Register daily 07:00 annual leave job')) {
    $credential = Get-Credential -UserName $TaskUser -Message 'Task account credentials on the target server'
    if (!$credential) { throw 'Credentials are required.' }
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings `
        -User $credential.UserName -Password $credential.GetNetworkCredential().Password `
        -Description 'MEC annual leave anniversary accrual, daily at 07:00 Turkey time' -Force
}
