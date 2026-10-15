$ErrorActionPreference='Stop'
$shell=(Get-Process -Id $PID).Path
$records=[System.Collections.Generic.List[object]]::new()
$manifest=Join-Path $PSScriptRoot 'WindowsFinalSessions.json'
foreach($model in 0..5) {
    $run=[Guid]::NewGuid().ToString('N')
    foreach($mode in @('seed','continue')) {
        & $shell -NoProfile -File (Join-Path $PSScriptRoot 'RunRosterWindows.ps1') -Mode $mode -Model $model -Run $run
        $code=$LASTEXITCODE
        $records.Add([pscustomobject]@{model=$model;mode=$mode;run=$run;exitCode=$code;endedUtc=[DateTime]::UtcNow.ToString('o')})
        $records | ConvertTo-Json -Depth 5 | Out-File -LiteralPath $manifest -Encoding utf8
        if($code -ne 0) { throw "Final Windows acceptance failed: model=$model mode=$mode run=$run exit=$code" }
    }
}
Write-Output 'PASS: all six final-build seed/quit/relaunch/continue pairs.'
