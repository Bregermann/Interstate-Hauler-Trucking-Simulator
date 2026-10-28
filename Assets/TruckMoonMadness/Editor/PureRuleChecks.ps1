$ErrorActionPreference='Stop'
Add-Type -Path 'Assets/TruckMoonMadness/Rules/MoonRules.cs'
$rules=[LWS.TruckMoonMadness.MoonRules]::new(5,3)
if($rules.Collect(0,0,$false)){throw 'No physical contact'}
if(!$rules.Collect(0,0,$true) -or $rules.Collect(0,1,$true) -or $rules.Collect(1,0,$true)){throw 'Exclusive physical owner'}
if($rules.Deliver(0,1,$true) -or $rules.Deliver(0,0,$false)){throw 'Wrong owner/nonphysical station'}
if(!$rules.Drop(0) -or $rules.Drop(0)){throw 'Physical drop once'}
if(($rules.Scores|Measure-Object -Sum).Sum -ne 0){throw 'No drop score'}
foreach($n in 0..2){
 if(!$rules.Collect(0,6,$true) -or !$rules.Deliver(0,6,$true) -or $rules.Deliver(0,6,$true)){throw 'Actual delivery once'}
 if($n -lt 2 -and !$rules.Respawn(0)){throw 'Delivered flag respawn'}
}
if(!$rules.Complete -or $rules.Scores[6] -ne 3 -or $rules.Deliveries -ne 3 -or $rules.Leaders()[0] -ne 6){throw 'Actual target champion'}
if($rules.Collect(1,1,$true) -or $rules.Respawn(0)){throw 'Result lock'}
$fresh=[LWS.TruckMoonMadness.MoonRules]::new(5,3)
if(($fresh.Scores|Measure-Object -Sum).Sum -ne 0 -or $fresh.Complete -or @($fresh.Owners|Where-Object {$_ -ne -1}).Count -ne 0){throw 'Fresh match'}
'MOON_PURE actual compiled C# ownership/contact/drop/rest/atomic-delivery/target-lock/rematch PASS; not native physics validation.'
