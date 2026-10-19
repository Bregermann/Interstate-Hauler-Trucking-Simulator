param([string[]]$Modes=@('insane','mixed','calibration','show'))
$ErrorActionPreference='Stop'
$sportsRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location -LiteralPath $sportsRoot
$driver=Join-Path $sportsRoot 'Logs/TruckSports/EssentialRelease/Dance/native-driver.txt'
[IO.File]::WriteAllText($driver,('START '+[DateTime]::UtcNow.ToString('o')+[Environment]::NewLine))
foreach($mode in $Modes) {
 $began=[DateTime]::UtcNow
 & (Join-Path $PSScriptRoot 'RequestEssentialJob.ps1') ('dance-play '+$mode)
 [IO.File]::AppendAllText($driver,('REQUEST '+$mode+' '+$began.ToString('o')+[Environment]::NewLine))
 $result=Join-Path $sportsRoot ('Logs/TruckSports/EssentialRelease/Dance/Native/'+$mode+'/result.txt')
 $timeout=if($mode -eq 'show'){310}else{125}
 $complete=$false
 while(([DateTime]::UtcNow-$began).TotalSeconds -lt $timeout) {
  if(Test-Path -LiteralPath $result) {
   $item=Get-Item -LiteralPath $result
   if($item.LastWriteTimeUtc -ge $began) {
    $text=[IO.File]::ReadAllText($result)
    if($text -match 'CHECKS=\d+ ERRORS=(\d+)') {
     [IO.File]::AppendAllText($driver,('FINISHED '+$mode+' '+$Matches[0]+[Environment]::NewLine))
     if([int]$Matches[1] -ne 0){throw ('Acceptance failed: '+$mode+'; preserve result for investigation.')}
     $complete=$true;break
    }
   }
  }
  Start-Sleep -Milliseconds 500
 }
 if(-not $complete){[IO.File]::AppendAllText($driver,('TIMEOUT '+$mode+[Environment]::NewLine));throw ('Acceptance did not finish: '+$mode)}
}
[IO.File]::AppendAllText($driver,('PASS all requested Dance native modes '+[DateTime]::UtcNow.ToString('o')+[Environment]::NewLine))
