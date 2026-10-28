$ErrorActionPreference='Stop'
$rules=@('Assets/TruckTruckGoose/Scripts/Rules/GooseRules.cs','Assets/TruckGuessThat/Scripts/Rules/GuessRules.cs')
Add-Type -Path $rules
$r=[LWS.TruckTruckGoose.GooseRules]::new(8,1)
if(!$r.Select(1,0,0,$true)){throw 'selection'}
for($i=1;$i -le 36;$i++){$r.Observe(0,[LWS.TruckTruckGoose.GooseRules]::Wrap($i*10),$true)}
if($r.Claim(0,$true,5,$true)){throw 'unsafe arrival'}
if(!$r.Claim(0,$true,2,$true) -or $r.Scores[0] -ne 1){throw 'legal claim'}
if($r.Claim(1,$true,0,$true)){throw 'double award'}
$roster=[LWS.TruckGuessThat.GuessIdentity[]]::new(24)
for($i=0;$i -lt 24;$i++){$id=[LWS.TruckGuessThat.GuessIdentity]::new();$id.id='id.'+$i;$id.name='Truck '+$i;$id.body='body.'+[Math]::Floor($i/4);$id.hat='hat.'+($i%4);$id.eyes='eyes.'+($i%2);$id.mouth='mouth.'+($i%3);$id.mirrors='mirror.'+($i%3);$id.wheels='wheel.'+($i%3);$roster[$i]=$id}
foreach($count in @(2,3,4)){
 $g=[LWS.TruckGuessThat.GuessRules]::new($roster,$count,511)
 for($step=0;$step -lt 100 -and $g.Winner -lt 0;$step++){
  $choices=$g.PublicCandidates($g.Turn);$allowed=[Func[int,bool]]{param($q) $g.CanAsk($g.Turn,$q)}
  $q=[LWS.TruckGuessThat.GuessDeduction]::BestQuestion($g.Identities,$g.Questions,$choices,$allowed)
  if($choices.Length -eq 1 -or $q -lt 0){$correct=$false;if(!$g.Guess($g.Turn,$choices[0],[ref]$correct)){throw 'guess refused'}}else{$answer=[Activator]::CreateInstance([LWS.TruckGuessThat.GuessAnswer]);if(!$g.Ask($g.Turn,$q,[ref]$answer)){throw 'question refused'}}
  $g.Next()
 }
 if($g.Winner -lt 0 -or $g.Turns -ge 32){throw 'deduction did not finish'}
 Write-Output "GUESS_PURE players=$count turns=$($g.Turns) winner=$($g.Winner)"
}
Write-Output 'PURE_RULE_CHECKS_PASS actual C#; no Unity, scene or physics execution.'

