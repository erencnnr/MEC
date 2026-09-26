param(
    [Parameter(Mandatory)][string]$PublishPath,
    [Parameter(Mandatory)][string]$DotnetPath
)
$ErrorActionPreference = 'Stop'
try {
    $publishDirectory = (Resolve-Path -LiteralPath $PublishPath).Path
    Set-Location -LiteralPath $publishDirectory
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    $env:AppSettings__Environment = 'Production'
    $logDirectory = Join-Path $publishDirectory 'logs\annual-leave'
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    $logPath = Join-Path $logDirectory ('job-{0}.log' -f (Get-Date -Format 'yyyy-MM-dd'))
    & $DotnetPath (Join-Path $publishDirectory 'MEC.Portal.dll') --annual-leave-job *>> $logPath
    exit $LASTEXITCODE
}
catch {
    Write-Error $_
    exit 1
}
