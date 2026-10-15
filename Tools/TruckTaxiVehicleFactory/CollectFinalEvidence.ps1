$ErrorActionPreference='Stop'
$project='F:/Unity Projects/Interstate-Hauler-Trucking-Simulator'
$evidence=Join-Path $project 'Tools/TruckTaxiVehicleFactory/Evidence'
$final=Join-Path $evidence 'FinalWindows'
$review=Join-Path $project 'Tools/TruckTaxiVehicleFactory/ReviewRenders'
New-Item -ItemType Directory -Path $final,$review -Force | Out-Null
$sessions=@(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'WindowsFinalSessions.json') -Raw | ConvertFrom-Json)
if($sessions.Count -ne 12 -or @($sessions | Where-Object exitCode -ne 0).Count -ne 0){throw 'Final acceptance manifest is not twelve successful sessions'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'WindowsFinalSessions.json') -Destination (Join-Path $evidence 'WindowsFinalSessions.json')
$performance=[System.Collections.Generic.List[object]]::new()
foreach($record in $sessions) {
    $log=Join-Path $PSScriptRoot "windows-roster-$($record.model)-$($record.run)-$($record.mode).log"
    $lines=@(Get-Content -LiteralPath $log)
    if(!($lines | Where-Object {$_ -match '^TRUCK TAXI RESTART PASS mode='})){throw "Missing actual player PASS: $log"}
    $errors=@($lines | Where-Object {$_ -match '^(NullReferenceException|InvalidOperationException|ArgumentException|TRUCK TAXI RESTART FAIL)'})
    if($errors.Count -gt 0){throw "Runtime error in $log"}
    $lines | Where-Object {$_ -match '^TRUCK TAXI RESTART'} | Out-File -LiteralPath (Join-Path $final "$($record.model)-$($record.mode).txt") -Encoding utf8
    $root=Join-Path 'C:/Users/Jeremy/AppData/Local/Temp/Jeremy Bregermann/Interstate Hauler/TruckTaxiRestartTests' $record.run
    $names=@("roster-$($record.model)-$($record.mode).png","roster-$($record.model)-$($record.mode).txt")
    if($record.mode -eq 'seed'){$names+=@("dealer-$($record.model).png","cockpit-$($record.model).png","performance-$($record.model).txt")}
    foreach($name in $names){Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $final $name)}
    if($record.mode -eq 'seed'){
        $sample=Get-Content -LiteralPath (Join-Path $root "performance-$($record.model).txt") -Raw
        $m=[regex]::Match($sample,'Samples=(\d+) medianMs=([\d.]+) p95Ms=([\d.]+)')
        if(!$m.Success){throw 'Missing frame sample'}
        $performance.Add([pscustomobject]@{model=$record.model;samples=[int]$m.Groups[1].Value;medianMs=[double]$m.Groups[2].Value;p95Ms=[double]$m.Groups[3].Value;context='Stationary live Demo City, weather/NPCs on, shared GTX1660Ti; not isolated/driving benchmark'})
    }
}
$performance | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $evidence 'WindowsFinalPerformance.json') -Encoding utf8
$renders=Join-Path $PSScriptRoot 'Renders'
if(Test-Path -LiteralPath $renders){Get-ChildItem -LiteralPath $renders -Filter '*.png' -File | Copy-Item -Destination $review}
foreach($model in @('highwayking','nightrunner','roadpalace')){
    $name="Sleeper_taxi.truck.$model.png"
    Copy-Item -LiteralPath (Join-Path $project "Builds/TruckTaxiDemo/Validation/$name") -Destination (Join-Path $evidence $name)
}
$artifact=[pscustomobject]@{
    buildResult=(Get-Content -LiteralPath (Join-Path $project 'Tools/TruckTaxiVehicleFactory/Build-result.txt') -Raw).Trim()
    executable=(Join-Path $project 'Builds/TruckTaxiVehicleFactory/TruckTaxi.exe')
    runtimeHash=(Get-FileHash -LiteralPath (Join-Path $project 'Builds/TruckTaxiVehicleFactory/TruckTaxi_Data/Managed/LWS.TruckTaxi.Runtime.dll') -Algorithm SHA256).Hash
    integrationCommit=(& git -C $project rev-parse HEAD)
    sessionCount=$sessions.Count
    errorsFound=0
    capturedUtc=[DateTime]::UtcNow.ToString('o')
}
$artifact | ConvertTo-Json -Depth 4 | Out-File -LiteralPath (Join-Path $evidence 'FinalBuildArtifact.json') -Encoding utf8
$performance | Format-Table model,samples,medianMs,p95Ms
Write-Output 'Preserved twelve final player results, 24 Windows images, three Unity sleeper views and 27 Blender review renders.'
