$ErrorActionPreference='Stop'
Add-Type -Path 'Assets/TruckSnowmanBuilding/Rules/SnowmanRules.cs','Assets/TruckSnowmanBuilding/Rules/SnowmanDeliveryRoute.cs'
$used=0.0
$r=[LWS.TruckSnowmanBuilding.SnowmanRules]::Grow(.75,0,10,.25,10,3,[ref]$used)
if($r -ne .75 -or $used -ne 0){throw 'Idle growth'}
$r=[LWS.TruckSnowmanBuilding.SnowmanRules]::Grow(1,1,1,.25,.1,3,[ref]$used)
if($r -le 1 -or [Math]::Abs($used-.1) -gt .00001){throw 'Finite physical rolling volume'}
$rules=[LWS.TruckSnowmanBuilding.SnowmanRules]::new()
if($rules.Place(1,$true) -or $rules.Place(0,$false)){throw 'Invalid physical placement'}
foreach($role in @(0,1,2)){if(!$rules.Place($role,$true)){throw 'Ordered physical placement'}}
foreach($family in @(0,1,2,3,4,5)){if(!$rules.Decorate($family,$true)){throw 'Decoration'}}
if($rules.Finish($false,10,2.4,1.7,1.2,1,300)){throw 'Unstable completion'}
if(!$rules.Finish($true,10,2.4,1.7,1.2,1,300) -or $rules.Score -le 200 -or $rules.Finish($true,10,2.4,1.7,1.2,1,10)){throw 'Atomic measured completion'}
Write-Output "SNOWMAN_PURE_PASS grade=$($rules.Score); actual engine-free C# only, native physics pending."

if(![LWS.TruckSnowmanBuilding.SnowmanDeliveryRoute]::Arrived(1.234,0)){throw 'Observed stop-radius deadlock'}
if([Math]::Abs([LWS.TruckSnowmanBuilding.SnowmanDeliveryRoute]::InterceptX(-11.07913,-19.04857,-.20043,2.53755)+11.95182) -gt .0001){throw 'Observed lateral trajectory'}
Write-Output 'SNOWMAN_ROUTE_OBSERVED_REGRESSION_PASS'
