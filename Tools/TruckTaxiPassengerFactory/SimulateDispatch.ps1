param([string]$OutputDirectory = 'Documentation/TruckTaxi/DispatchAudit')
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location $root
try {
    $inputData = (& python "$PSScriptRoot/audit_dispatch_roster.py") | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'Roster YAML audit failed.' }
    # Load exact production policy source into this process, never Unity or a project build.
    $source = Get-Content 'Assets/LWS/TruckTaxi/Scripts/TruckTaxiPassengerDispatchPolicy.cs' -Raw
    $area = Get-Content 'Assets/LWS/TruckTaxi/Scripts/TruckTaxiRideWorkArea.cs' -Raw
    $start = $area.IndexOf('    // Completed fares,')
    $end = $area.IndexOf('    public readonly struct TruckTaxiFareModifiers')
    if ($start -lt 0 -or $end -le $start) { throw 'Intercity policy source markers changed.' }
    Add-Type -TypeDefinition ($source + "`nnamespace LWS.TruckTaxi {`n" + $area.Substring($start, $end - $start) + "`n}")
    $rows = @($inputData.roster)
    foreach ($row in $rows) {
        $gender = [LWS.TruckTaxi.TruckTaxiPassengerEligibilityPolicy]::ResolveGender($row.gender, $row.castingGender)
        $category = [LWS.TruckTaxi.TruckTaxiPassengerEligibilityPolicy]::BroadArchetype($row.dispatchArchetype, $row.id, $row.archetype, [string[]]$row.traits)
        $row | Add-Member -NotePropertyName resolvedGender -NotePropertyValue "$gender"
        $row | Add-Member -NotePropertyName category -NotePropertyValue $category
        $row | Add-Member -NotePropertyName appreciationEligible -NotePropertyValue ([LWS.TruckTaxi.TruckTaxiPassengerEligibilityPolicy]::IsAppreciationEligible($gender, $row.explicitlyAdult, $row.minimumAge, $row.optIn))
    }
    $contexts = @(
        @{ name='ordinary-daytime'; hour=12; district='Downtown'; race=$false },
        @{ name='night'; hour=23; district='Downtown'; race=$false },
        @{ name='neutral-town'; hour=12; district='Neutral'; race=$false },
        @{ name='race-event'; hour=12; district='Speedway'; race=$true }
    )
    $summary = [System.Collections.Generic.List[object]]::new()
    $audit = [System.Collections.Generic.List[object]]::new()
    foreach ($context in $contexts) {
        $random = [Random]::new(20260930)
        $policy = [LWS.TruckTaxi.TruckTaxiPassengerDispatchPolicy]::new()
        $choices = [System.Collections.Generic.List[LWS.TruckTaxi.TruckTaxiPassengerCandidate]]::new()
        $eligible = @{}
        foreach ($row in $rows) {
            $reason = if ($row.activeCount -eq 0) {'Not in active database'} elseif (!$row.available) {'Unavailable'} elseif ($row.weight -le 0) {'Nonpositive spawn weight'} elseif ($row.districts.Count -gt 0 -and $context.district -notin $row.districts) {'District restriction'} else {''}
            $weight = [double]$row.weight * @($inputData.config.commonWeight, $inputData.config.uncommonWeight, $inputData.config.rareWeight, $inputData.config.legendaryWeight)[$row.rarity]
            if ($weight -le 0) { $reason='Nonpositive rarity/context weight' }
            # Scenario context weights only; C# policy is the production selection authority.
            if ($context.hour -eq 23) {
                $tags = ($row.archetype + ' ' + ($row.traits -join ' ')).ToLowerInvariant()
                if ($row.rarity -ge 2) { $weight *= $inputData.config.lateNightRareWeight }
                if ($tags -match 'weird|strange|eccentric|alien|robot') { $weight *= $inputData.config.lateNightWeirdWeight }
                if ($tags -match 'nightlife|party|club|bartender|entertainer') { $weight *= $inputData.config.lateNightNightlifeWeight }
                if ($row.chaosAffinity -gt 0 -or $tags -match 'chaotic|chaos') { $weight *= $inputData.config.lateNightChaoticWeight }
                if ($row.flirtatious -or $tags -match 'flirtatious') { $weight *= $inputData.config.lateNightFlirtatiousWeight }
            }
            if ($row.category -eq 'racing' -and $context.race) { $weight *= 1.5 }
            if (!$reason) {
                $eligible[$row.id] = $true
                $choices.Add([LWS.TruckTaxi.TruckTaxiPassengerCandidate]::new($row.id, $row.category, $weight))
            }
            $audit.Add([pscustomobject]@{context=$context.name; id=$row.id; name=$row.name; eligible=(!$reason); reason=$reason; category=$row.category; spawnWeight=$row.weight; rarity=$row.rarity; districts=($row.districts -join ','); gender=$row.resolvedGender; offers=0; accepted=0; completed=0; path=$row.path})
        }
        $previousId = ''; $previousCategory = ''; $idRun=0; $categoryRun=0; $maxIdRun=0; $maxCategoryRun=0; $races=0; $firstThirdCoverage=100.0
        for ($i=0; $i -lt 1000; $i++) {
            $id = $policy.Select($choices, $random, $context.race, $previousId)
            $category = ($choices | Where-Object Id -EQ $id | Select-Object -First 1).Archetype
            $policy.RecordOffered($id, $category, $i*10)
            $policy.RecordAccepted($id); $policy.RecordCompleted($id)
            $idRun = if ($id -eq $previousId) {$idRun+1} else {1}
            $categoryRun = if ($category -eq $previousCategory) {$categoryRun+1} else {1}
            $maxIdRun=[Math]::Max($maxIdRun,$idRun); $maxCategoryRun=[Math]::Max($maxCategoryRun,$categoryRun)
            if ($category -eq 'racing') {$races++}
            if ($policy.Exposure($id).timesOffered -eq 3) {
                $coverage = 100.0 * $policy.CaptureState().passengers.Count / $eligible.Count
                $firstThirdCoverage=[Math]::Min($firstThirdCoverage,$coverage)
            }
            $previousId=$id; $previousCategory=$category
        }
        $snapshot=$policy.CaptureState()
        foreach ($entry in $audit | Where-Object context -EQ $context.name) {
            $counts=$policy.Exposure($entry.id); $entry.offers=$counts.timesOffered; $entry.accepted=$counts.timesAccepted; $entry.completed=$counts.timesCompleted
        }
        $summary.Add([pscustomobject]@{context=$context.name; offers=1000; eligible=$eligible.Count; unique=$snapshot.passengers.Count; never=@($eligible.Keys | Where-Object {$policy.Exposure($_).timesOffered -eq 0}); maxOffers=($snapshot.passengers.timesOffered | Measure-Object -Maximum).Maximum; largestPassengerStreak=$maxIdRun; largestArchetypeStreak=$maxCategoryRun; racingPercent=$races/10.0; coverageAtThirdPercent=$firstThirdCoverage})
    }
    $random=[Random]::new(330033); $offered=0
    for($i=0;$i -lt 10000;$i++) {
        $eligible=[LWS.TruckTaxi.TruckTaxiPassengerEligibilityPolicy]::IsAppreciationEligible([LWS.TruckTaxi.TruckTaxiPassengerGender]::Female,$true,21,$true)
        if([LWS.TruckTaxi.TruckTaxiPassengerEligibilityPolicy]::ShouldOfferAppreciation($eligible,$random)) {$offered++}
    }
    $intercityResults=@()
    foreach($chance in @(.02, [double]$inputData.config.intercityRideChance)) {
        $policy=[LWS.TruckTaxi.TruckTaxiIntercityPolicy]::new($chance,2,3); $random=[Random]::new(130013)
        $intercity=0; $maxLocal=0; $guaranteed=$false
        for($i=0;$i -lt 10000;$i++) {
            $across=$policy.WantsIntercity($random,[ref]$guaranteed)
            $policy.CompleteRide($across)
            if($across) {$intercity++}
            $maxLocal=[Math]::Max($maxLocal,$policy.CompletedLocalStreak)
        }
        $intercityResults += [pscustomobject]@{conditionalChance=$chance; completed=10000; intercityPercent=$intercity/100.0; maxCompletedLocalStreak=$maxLocal; routeScope='Policy only: every opportunity assumed routable. Unity graph-gated tests supplied separately.'}
    }
    $result=[pscustomobject]@{scope='Real production C# selection/eligibility/intercity policies; static authored roster and scenario metadata. No live Unity, continuity movement, or path execution.'; contexts=$summary; male=@($rows | Where-Object resolvedGender -EQ Male).Count; female=@($rows | Where-Object resolvedGender -EQ Female).Count; unassigned=@($rows | Where-Object resolvedGender -EQ Unassigned).Count; adultFemaleEligible=@($rows | Where-Object appreciationEligible).Count; maleEligible=@($rows | Where-Object {$_.resolvedGender -eq 'Male' -and $_.appreciationEligible}).Count; unresolved=@($rows | Where-Object resolvedGender -EQ Unassigned | Select-Object id,name,castingGender); captain=@($rows | Where-Object id -EQ 'space-hero'); appreciationTrials=10000; appreciationOffered=$offered; appreciationPercent=$offered/100.0; intercity=$intercityResults}
    $null=New-Item -ItemType Directory -Path $OutputDirectory -Force
    $result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath "$OutputDirectory/dispatch-simulation.json" -Encoding utf8
    $audit | Export-Csv -LiteralPath "$OutputDirectory/roster-eligibility.csv" -NoTypeInformation -Encoding utf8
    $result | Select-Object scope,contexts,male,female,unassigned,adultFemaleEligible,maleEligible,appreciationTrials,appreciationOffered,appreciationPercent,intercity | ConvertTo-Json -Depth 8
} finally { Pop-Location }
