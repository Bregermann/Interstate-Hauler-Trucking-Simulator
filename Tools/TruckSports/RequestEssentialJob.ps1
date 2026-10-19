param([Parameter(Mandatory=$true)][string]$Command)
$ErrorActionPreference = 'Stop'
$sportsRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$sportsJobs = [IO.Path]::GetFullPath((Join-Path $sportsRoot 'Logs/TruckSports/EssentialRelease'))
if (-not $sportsJobs.StartsWith($sportsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Job directory escaped the project.' }
[IO.Directory]::CreateDirectory($sportsJobs) | Out-Null
$request = Join-Path $sportsJobs 'kickball.request'
if (Test-Path -LiteralPath $request) { throw 'An explicit job is already pending. Preserve it.' }
$temp = Join-Path $sportsJobs ('job-' + [Guid]::NewGuid().ToString('N') + '.tmp')
[IO.File]::WriteAllText($temp, $Command, [Text.UTF8Encoding]::new($false))
[IO.File]::Move($temp, $request)
