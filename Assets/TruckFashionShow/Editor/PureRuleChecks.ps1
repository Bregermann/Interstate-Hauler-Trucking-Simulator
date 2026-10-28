$ErrorActionPreference='Stop'
Add-Type -Path 'Assets/TruckFashionShow/Rules/FashionRules.cs','Assets/TruckDanceOff/Rules/DanceRules.cs'
$items=[LWS.TruckFashionShow.FashionItem[]]@([LWS.TruckFashionShow.FashionItem]::new('hat.crown','hat',[LWS.TruckFashionShow.FashionRules]::Tags('hat.crown')),[LWS.TruckFashionShow.FashionItem]::new('mirrors.gold','mirrors',[LWS.TruckFashionShow.FashionRules]::Tags('mirrors.gold')),[LWS.TruckFashionShow.FashionItem]::new('wheels.gold','wheels',[LWS.TruckFashionShow.FashionRules]::Tags('wheels.gold')))
$grade=[LWS.TruckFashionShow.FashionRules]::Judge([LWS.TruckFashionShow.FashionTheme]::Royalty,$items,10,12,10000,12000,$true)
if($grade.theme -ne 120 -or $grade.coordination -ne 60 -or $grade.qte -ne 500){throw 'Actual outfit/performance components'}
$show=[LWS.TruckFashionShow.FashionCompetition]::new(4)
foreach($owner in 0..3){if(!$show.Lock($owner,$grade) -or $show.Lock($owner,$grade)){throw 'Once-only actual turns'}}
$notes=[LWS.TruckDanceOff.DanceNote[]]@([LWS.TruckDanceOff.DanceNote]::new(1,1,0),[LWS.TruckDanceOff.DanceNote]::new(2,4,.3))
$timing=[LWS.TruckDanceOff.DanceRules]::new($notes,.12,.28)
$timing.Step(1.2,1,1);$timing.Step(1.5,2,2);$timing.Step(2,4,4);$timing.Step(2.3,0,4)
if($timing.Resolved -ne 2 -or $timing.Score -ne 1410 -or $timing.EmptyPresses -ne 1){throw 'Actual reused Dance timing/hold/penalty'}
"FASHION_PURE_PASS originalDanceScore=$($timing.Score) outfitTheme=$($grade.theme) coordination=$($grade.coordination) actual4turnTotal=$($show.Scores[0]); no native/visual claim."
