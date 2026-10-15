param([ValidateSet('seed','continue')][string]$Mode='seed',
    [ValidateRange(0,5)][int]$Model=0,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-f0-9]{32}$')][string]$Run)
$ErrorActionPreference='Stop'
$exe='F:\Unity Projects\Interstate-Hauler-Trucking-Simulator\Builds\TruckTaxiVehicleFactory\TruckTaxi.exe'
$log=Join-Path $PSScriptRoot "windows-roster-$Model-$Run-$Mode.log"
if(!(Test-Path -LiteralPath $exe)){throw 'Fresh vehicle-factory Windows build is missing.'}
$arguments=@('-screen-fullscreen','0','-screen-width','1600','-screen-height','900',
    '-truck-taxi-restart-test',$Mode,'-truck-taxi-test-run',$Run,'-truck-taxi-test-roster',$Model,
    '-logFile',('"'+$log+'"'))
$process=Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory (Split-Path $exe) -PassThru -WindowStyle Normal
"Started isolated vehicle acceptance PID=$($process.Id) model=$Model mode=$Mode run=$Run log=$log"
if(!$process.WaitForExit(900000)){throw "Test remains active after 15 minutes; inspect only PID $($process.Id), do not close unrelated players."}
[pscustomobject]@{pid=$process.Id;model=$Model;mode=$Mode;run=$Run;exitCode=$process.ExitCode;endedUtc=[DateTime]::UtcNow.ToString('o')} |
    ConvertTo-Json | Out-File -LiteralPath (Join-Path $PSScriptRoot "exit-$Model-$Run-$Mode.json") -Encoding utf8
Get-Content -LiteralPath $log -Tail 25
exit $process.ExitCode
