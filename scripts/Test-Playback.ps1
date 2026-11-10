param([ValidateSet('x64','x86')][string]$Architecture='x64',[ValidatePattern('^[A-Za-z0-9_-]{11}$')][string]$Video='M7lc1UVf-VE',[ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Case='playback',[switch]$DropFirstPlay)
. "$PSScriptRoot\Common.ps1"
$root=Get-RepositoryRoot
$exe=Join-Path $root "build\win-$Architecture\WindowsYouTubeWidget.exe"
$testData=Join-Path $root ('build\playback-'+$Case+'-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testData | Out-Null
$arguments=@('--probe','--audible-play-test','--playback-video',$Video,'--data-root',('"'+$testData+'"'))
if($DropFirstPlay){$arguments+='--drop-first-play'}
$process=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit(90000)){$process.Kill();throw 'Isolated audible playback test timed out.'}
$path=Join-Path $testData 'audible-playback-verification.json'
$report=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
New-Item -ItemType Directory -Path (Join-Path $root 'artifacts') -Force | Out-Null
Copy-Item -LiteralPath $path -Destination (Join-Path $root "artifacts\audible-$Case-$Architecture.json")
$report | ConvertTo-Json -Depth 6
if(-not $report.Passed){throw 'Audible playback verification failed.'}
