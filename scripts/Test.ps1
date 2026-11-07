param([ValidateSet('x64','x86','arm64')][string]$Architecture='x64',[switch]$Ui)
. "$PSScriptRoot\Common.ps1"
$root=Get-RepositoryRoot
Push-Location $root
try {
 New-Item -ItemType Directory -Path artifacts -Force | Out-Null
 Get-ChildItem -LiteralPath tests -Filter '*.cjs' | ForEach-Object { & node $_.FullName; if ($LASTEXITCODE -ne 0) { throw "Test failed: $($_.Name)" } }
 if ($Architecture -eq 'arm64' -and $env:PROCESSOR_ARCHITECTURE -ne 'ARM64') { Write-Output 'ARM64 compiled; execution requires ARM64 hardware.'; return }
 $exe=Join-Path $root "build\win-$Architecture\WindowsYouTubeWidget.exe"
 $process=Start-Process -FilePath $exe -ArgumentList '--test' -WindowStyle Hidden -Wait -PassThru
 if ($process.ExitCode -ne 0) { throw 'Native parser tests failed.' }
 $report=Get-Content -LiteralPath (Join-Path (Split-Path $exe) 'tests.json') -Raw | ConvertFrom-Json
 if ($report.passed -lt 12) { throw 'Native test report missing checks.' }
 Copy-Item -LiteralPath (Join-Path (Split-Path $exe) 'tests.json') -Destination "artifacts\native-tests-$Architecture.json"
 if ($Ui) {
  $profile=Join-Path $root "build\test-profile-$Architecture"
  $process=Start-Process -FilePath $exe -ArgumentList @('--suite-test','--data-root',('"'+$profile+'"')) -WindowStyle Hidden -PassThru
  if (-not $process.WaitForExit(180000)) { $process.Kill(); throw 'Isolated UI suite timed out.' }
  $report=Get-Content -LiteralPath (Join-Path (Split-Path $exe) 'suite-verification.json') -Raw | ConvertFrom-Json
  if (-not $report.Passed) { throw 'UI suite failed.' }
  Copy-Item -LiteralPath (Join-Path (Split-Path $exe) 'suite-verification.json') -Destination "artifacts\ui-tests-$Architecture.json"
 }
 # Test reports and profiles must never enter public installation packages.
 Get-ChildItem -LiteralPath (Split-Path $exe) -File | Where-Object { $_.Name -match '^tests\.json$|verification\.json$|\.log$' } | Remove-Item -Force
} finally { Pop-Location }
