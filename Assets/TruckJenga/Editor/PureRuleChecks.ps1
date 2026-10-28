$ErrorActionPreference='Stop'
Add-Type -Path 'Assets/TruckJenga/Scripts/Rules/JengaRules.cs'
foreach($players in @(2,3,4)){
 $r=[LWS.TruckJenga.JengaRules]::new($players,6)
 if($r.Collapse($true)){throw 'Unattributed foundation collapse cannot invent a loser.'}
 $r.FoundationReady()
 for($turn=0;$turn -lt 4;$turn++){
  $piece=[LWS.TruckJenga.JengaSelection]::Careful($r)
  if($piece -lt 0 -or !$r.BeginPull($r.Owner,$piece)){throw 'Legal selection'}
  if($r.Extracted($piece,$false)){throw 'Extraction without physical clearance'}
  if(!$r.Extracted($piece,$true)){throw 'Physical extraction'}
  if($r.Restacked($piece,$false)){throw 'Score before physical release/rest'}
  if(!$r.Restacked($piece,$true)){throw 'Physical restack'}
  if($r.Restacked($piece,$true)){throw 'Duplicate score'}
  if($r.NextTurn($false)){throw 'Turn before settling'}
  if(!$r.NextTurn($true)){throw 'Settled turn'}
 }
 $last=$r.Responsible
 if(!$r.Collapse($true) -or $r.Loser -ne $last -or $r.Collapse($true)){throw 'Last-puller collapse attribution/atomicity'}
 if(($r.Stacks|Measure-Object -Sum).Sum -ne 4){throw 'Stack total'}
 Write-Output "JENGA_PURE players=$players stacks=4 loser=$($r.Loser) PASS"
}
Write-Output 'Engine-free actual C# rule checks only; native tower/winch/crane physics remain unexecuted.'
