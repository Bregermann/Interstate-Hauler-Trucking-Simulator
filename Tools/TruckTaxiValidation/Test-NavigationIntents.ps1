param([string]$ProjectRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$ErrorActionPreference = 'Stop'
$source = Join-Path $ProjectRoot 'Assets/LWS/TruckTaxi/Scripts/TruckTaxiNavigationIntents.cs'
Add-Type -TypeDefinition (Get-Content -LiteralPath $source -Raw)
$layer = [LWS.TruckTaxi.TruckTaxiNavigationLayer]
function Assert-Equal($Expected, $Actual, [string]$Message) {
    if ($Expected -ne $Actual) { throw "$Message Expected '$Expected', got '$Actual'." }
}
$stack = [LWS.TruckTaxi.TruckTaxiNavigationIntents[string]]::new()
$stack.Set($layer::RideFinal, 'ride', 'foundry', 'final') | Out-Null
$stack.Set($layer::PassengerDiversion, 'passenger', 'skyline', 'scenic') | Out-Null
$restroom = $stack.Set($layer::PlayerService, 'player-service', 'restroom', 'restroom')
Assert-Equal 'restroom' $stack.Active.TargetId 'Service priority.'
$stack.Release($restroom.Layer, $restroom.OwnerId, $restroom.TargetId, $restroom.Token) | Out-Null
Assert-Equal 'skyline' $stack.Active.TargetId 'Suspended scenic restore.'
$stack.Release($layer::PassengerDiversion, 'passenger', 'skyline', 0) | Out-Null
Assert-Equal 'foundry' $stack.Active.TargetId 'Final restore.'
$old = $stack.Set($layer::PlayerService, 'player-service', 'gas', 'old')
$new = $stack.Set($layer::PlayerService, 'player-service', 'gas', 'new')
Assert-Equal $false ($stack.Release($old.Layer, $old.OwnerId, $old.TargetId, $old.Token)) 'Stale token.'
Assert-Equal $false ($stack.Release($new.Layer, 'wrong-owner', $new.TargetId, 0)) 'Wrong owner.'
$emergency = $stack.Set($layer::Emergency, 'recovery', 'tow', 'tow')
$stack.Set($layer::PlayerService, 'player-service', 'sleep', 'sleep') | Out-Null
Assert-Equal 'tow' $stack.Active.TargetId 'Emergency priority.'
$stack.Release($emergency.Layer, $emergency.OwnerId, $emergency.TargetId, $emergency.Token) | Out-Null
Assert-Equal 'sleep' $stack.Active.TargetId 'Replaced suspended service restores.'
$stack.Release($layer::RideFinal, 'ride', 'foundry', 0) | Out-Null
Assert-Equal 'sleep' $stack.Active.TargetId 'Ride release leaves player service.'
$stack.Clear()
Assert-Equal $null $stack.Active 'Clear all.'

# Compare production C# against an independent four-slot reference through 10,000 mutations.
$random = [Random]::new(2302732)
$reference = [object[]]::new(4)
for ($i = 0; $i -lt 10000; $i++) {
    $slot = $random.Next(4)
    $owner = "owner-$slot"
    if ($random.Next(3) -ne 0) {
        $id = "target-$i"
        $reference[$slot] = $stack.Set([LWS.TruckTaxi.TruckTaxiNavigationLayer]$slot, $owner, $id, $id)
    } elseif ($null -ne $reference[$slot]) {
        $lease = $reference[$slot]
        Assert-Equal $true ($stack.Release($lease.Layer, $owner, $lease.TargetId, $lease.Token)) 'Release existing.'
        $reference[$slot] = $null
    }
    $expected = $null
    for ($s = 3; $s -ge 0; $s--) { if ($null -ne $reference[$s]) { $expected = $reference[$s].TargetId; break } }
    Assert-Equal $expected $stack.Active.TargetId "Random mutation $i."
}
'PASS: 9 focused ownership assertions and 10,000 randomized production-C# ownership mutations.'
