param([string]$ProjectRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$ErrorActionPreference = 'Stop'
# Execute only the production pure-C# state and ownership helpers in memory. No Unity/build/assembly output.
foreach ($name in @('TruckTaxiPassengerBathroomState', 'TruckTaxiNavigationIntents')) {
    Add-Type -TypeDefinition (Get-Content -LiteralPath (Join-Path $ProjectRoot "Assets/LWS/TruckTaxi/Scripts/$name.cs") -Raw)
}
$script:assertions = 0
function Assert-True([bool]$Value, [string]$Message) {
    if (!$Value) { throw $Message }
    $script:assertions++
}
function New-Policy {
    $policy = [LWS.TruckTaxi.TruckTaxiPassengerBathroomPolicy]::new()
    $policy.rideChance = 1
    $policy.initialBladder = .7
    $policy.gainPerGameHour = .2
    return $policy
}
$stateType = [LWS.TruckTaxi.TruckTaxiPassengerBathroomState]
$policy = New-Policy
$state = $stateType::new($policy, 0, $false, 1)
$state.ObserveClock(600, $true)
Assert-True (!$state.CanOffer) 'Short rides must not offer even with a full bladder.'
$state.ObserveClock(2700, $true)
Assert-True $state.CanOffer 'A 100% authored chance must include sample=1.'
$bladder = $state.Bladder
$state.ObserveClock(10000, $false)
Assert-True ($state.Bladder -eq $bladder -and $state.RideMinutes -eq 45) 'Pause must update baseline without accumulating need.'
Assert-True ($state.Offer()) 'Offer ready request.'
Assert-True (!$state.Offer()) 'Do not offer twice.'
Assert-True (!$state.CompleteAfterReboard()) 'Offered request cannot complete.'
Assert-True ($state.Accept()) 'Accept request.'
Assert-True (!$state.Decline()) 'Accepted request cannot be declined as a pending modal.'
Assert-True (!$state.CompleteAfterReboard()) 'Navigation arrival alone cannot relieve need.'
Assert-True ($state.BeginPresentation()) 'Begin parked presentation.'
$state.ObserveClock(10180, $true)
Assert-True ($state.RideMinutes -eq 45) 'Own relief time skip must not accumulate need.'
Assert-True ($state.CompleteAfterReboard()) 'Reboard acknowledgement completes once.'
Assert-True ($state.Bladder -eq 0) 'Successful restroom break resets bladder.'
Assert-True (!$state.CompleteAfterReboard()) 'Duplicate completion must be inert.'
$state.ObserveClock(20000, $true)
Assert-True (!$state.CanOffer) 'Default one request per ride must not nag again.'

$policy.maximumRequestsPerRide = 2
$state = $stateType::new($policy, 0, $false, 0)
$state.ObserveClock(3600, $true)
$state.Offer() | Out-Null
$state.Decline() | Out-Null
Assert-True ($state.Bladder -gt 0 -and $state.ResolvedRequests -eq 1) 'Decline must not relieve need.'
Assert-True (!$state.CanOffer) 'Decline cooldown applies.'
$state.ObserveClock(7200, $true)
Assert-True $state.CanOffer 'Authored repeat can occur after game-clock cooldown.'
Assert-True ($stateType::DeclinePatienceLoss(.1) -eq 0) 'Decline must not exhaust low patience.'
Assert-True ([Math]::Abs(.16 - $stateType::DeclinePatienceLoss(.16) - .15) -lt .0001) 'Decline has a nonfatal patience floor.'
$state.Offer() | Out-Null
$state.Accept() | Out-Null
$state.Withdraw()
Assert-True ($state.ResolvedRequests -eq 1 -and !$state.CanOffer) 'Unavailable service withdrawal must not consume quota or spam.'
$state.ObserveClock(7500, $true)
Assert-True $state.CanOffer 'Withdrawal is retryable after a bounded delay.'

$local = $stateType::new($policy, 0, $false, 0)
$intercity = $stateType::new($policy, 0, $true, 0)
$local.ObserveClock(1800, $true)
$intercity.ObserveClock(1800, $true)
Assert-True ($intercity.Bladder -gt $local.Bladder) 'Authored intercity gain applies.'
$policy.rideChance = 0
$disabled = $stateType::new($policy, 0, $true, 0)
$disabled.ObserveClock(86400, $true)
Assert-True (!$disabled.CanOffer) 'Disabled chance must never offer.'
$policy.reliefMinutes = [float]::NaN
$policy.minimumRideMinutes = -10
$normalized = $policy.Normalized()
Assert-True ($normalized.reliefMinutes -eq 3 -and $normalized.minimumRideMinutes -eq 1 -and $policy.minimumRideMinutes -eq -10) 'Normalize copied authoring only.'
$local.ObserveClock([double]::NaN, $true)
$local.ObserveClock(-10, $true)
Assert-True ($local.RideMinutes -eq 30) 'Invalid or backward clocks cannot add need.'

$layer = [LWS.TruckTaxi.TruckTaxiNavigationLayer]
$stack = [LWS.TruckTaxi.TruckTaxiNavigationIntents[string]]::new()
$stack.Set($layer::RideFinal, 'ride', 'fare', 'fare') | Out-Null
$bathroom = $stack.Set($layer::PassengerDiversion, 'passenger-bathroom', 'restroom', 'restroom')
$service = $stack.Set($layer::PlayerService, 'player-service', 'food', 'food')
$emergency = $stack.Set($layer::Emergency, 'recovery', 'tow', 'tow')
Assert-True ($stack.Active.TargetId -eq 'tow') 'Emergency suspends bathroom and service.'
$stack.Release($emergency.Layer, $emergency.OwnerId, $emergency.TargetId, $emergency.Token) | Out-Null
Assert-True ($stack.Active.TargetId -eq 'food') 'Emergency pop restores player service.'
$stack.Release($service.Layer, $service.OwnerId, $service.TargetId, $service.Token) | Out-Null
Assert-True ($stack.Active.TargetId -eq 'restroom') 'Player service pop restores bathroom.'
$stack.Release($bathroom.Layer, $bathroom.OwnerId, $bathroom.TargetId, $bathroom.Token) | Out-Null
Assert-True ($stack.Active.TargetId -eq 'fare') 'Bathroom completion restores fare.'
$stack.Set($layer::PassengerDiversion, 'passenger', 'scenic', 'scenic') | Out-Null
Assert-True (!$stack.Release($bathroom.Layer, $bathroom.OwnerId, $bathroom.TargetId, $bathroom.Token)) 'Stale bathroom completion cannot remove a newer diversion.'

$random = [Random]::new(100923)
$policy = New-Policy
$fuzz = $stateType::new($policy, 0, $true, 0)
$seconds = 0.0
for ($i = 0; $i -lt 10000; $i++) {
    $seconds += $random.Next(-50, 300)
    $fuzz.ObserveClock($seconds, $random.Next(2) -eq 0)
    switch ($random.Next(6)) {
        0 { $fuzz.Offer() | Out-Null }
        1 { $fuzz.Accept() | Out-Null }
        2 { $fuzz.Decline() | Out-Null }
        3 { $fuzz.BeginPresentation() | Out-Null }
        4 { $fuzz.CompleteAfterReboard() | Out-Null }
        5 { $fuzz.Withdraw() }
    }
    if ($fuzz.Bladder -lt 0 -or $fuzz.Bladder -gt 1 -or [double]::IsNaN($fuzz.Bladder) -or $fuzz.ResolvedRequests -gt 1) {
        throw "State invariant failed at mutation $i."
    }
}
"PASS: $script:assertions focused production-C# assertions and 10,000 randomized bathroom clock/lifecycle mutations."
