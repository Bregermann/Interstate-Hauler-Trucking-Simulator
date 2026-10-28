$ErrorActionPreference='Stop'
Add-Type -Path 'Assets/TruckArchaeology/Scripts/Rules/ArchaeologyRules.cs'
foreach($count in 1..4){
 $rules=[LWS.TruckArchaeology.ArchaeologyRules]::new($count,$false)
 for($a=0;$a -lt $count*3;$a++){
  $owner=[int][Math]::Floor($a/3)
  if($rules.Scan($a,$owner,$false) -or !$rules.Scan($a,$owner,$true)){throw 'Actual scanner range'}
  if($rules.Expose($a,$false) -or !$rules.Expose($a,$true)){throw 'Actual terrain collider clearance'}
  if(!$rules.Lift($a,$owner,$true) -or $rules.Load($a,$owner,$false) -or !$rules.Load($a,$owner,$true)){throw 'Actual physical rack rest'}
  $rules.Damage($a,[LWS.TruckArchaeology.ArchaeologyRules]::ToolLoss([LWS.TruckArchaeology.ArchaeologyTool]::Brush))
  if($rules.Deliver($a,$owner,$false,100) -ne -1 -or $rules.Deliver($a,$owner,$true,100) -ne 100 -or $rules.Deliver($a,$owner,$true,100) -ne -1){throw 'Actual reception once'}
 }
 if(!$rules.Complete -or $rules.Delivered -ne $count*3 -or ($rules.Scores|Measure-Object -Sum).Sum -ne $count*300){throw 'Original complete exhibit'}
 "ARCHAEOLOGY_PURE players=$count delivered=$($rules.Delivered) PASS; actual engine-free C# only, no vendor/native terrain acceptance."
}

$drop=[LWS.TruckArchaeology.ArchaeologyRules]::new(1,$false)
$null=$drop.Scan(0,0,$true);$null=$drop.Expose(0,$true);$null=$drop.Lift(0,0,$true);$null=$drop.Load(0,0,$true)
if($drop.DropCargo(0,0,$false) -or !$drop.DropCargo(0,0,$true) -or $drop.Owners[0] -ne -1 -or $drop.Scores[0] -ne 0 -or !$drop.Lift(0,0,$true)){throw 'Actual lost-joint cargo recovery'}
'ARCHAEOLOGY_PURE actual-joint-loss semantics PASS'
