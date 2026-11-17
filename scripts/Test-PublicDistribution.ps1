. "$PSScriptRoot\Common.ps1"
$fixture=Join-Path (Get-RepositoryRoot) ('build\distribution-audit-'+[guid]::NewGuid().ToString('N'))
Assert-ChildPath $fixture (Join-Path (Get-RepositoryRoot) 'build')
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
try {
 Set-Content -LiteralPath (Join-Path $fixture 'distribution.json') -Value '{"version":"1.0.0"}'
 & "$PSScriptRoot\Assert-PublicDistribution.ps1" -Directory $fixture | Out-Null
 $cases=@('settings.json','settings.json.tmp','recommendation.json','bridge-health.json','runtime-health.json','native-host.json.update-backup','tests.json','capture-verification.json','private.pfx','errors.log')
 foreach($name in $cases){
  $file=Join-Path $fixture $name;Set-Content -LiteralPath $file -Value '{}'
  $rejected=$false
  try{& "$PSScriptRoot\Assert-PublicDistribution.ps1" -Directory $fixture | Out-Null}catch{$rejected=$true}
  Remove-Item -LiteralPath $file -Force
  if(-not $rejected){throw "Distribution privacy guard missed $name"}
 }
 $credentialFile=Join-Path $fixture 'example.txt';Set-Content -LiteralPath $credentialFile -Value ('ghp_'+('x'*40))
 $rejected=$false;try{& "$PSScriptRoot\Assert-PublicDistribution.ps1" -Directory $fixture | Out-Null}catch{$rejected=$true}
 if(-not $rejected){throw 'Distribution privacy guard missed a credential marker.'}
 Remove-Item -LiteralPath $credentialFile -Force
 foreach($encoding in @([Text.Encoding]::UTF8,[Text.Encoding]::Unicode)){
  $binary=Join-Path $fixture 'WindowsYouTubeWidget.dll'
  [IO.File]::WriteAllBytes($binary,$encoding.GetBytes('C:\Users\ExampleDeveloper\project\app.pdb'))
  $rejected=$false;try{& "$PSScriptRoot\Assert-PublicDistribution.ps1" -Directory $fixture | Out-Null}catch{$rejected=$true}
  Remove-Item -LiteralPath $binary -Force
  if(-not $rejected){throw 'Distribution privacy guard missed a binary developer path.'}
 }
 Write-Output 'PASS: clean distribution accepted; 10 private/test artifact types, a synthetic credential and UTF-8/UTF-16 developer paths rejected.'
} finally {
 Assert-ChildPath $fixture (Join-Path (Get-RepositoryRoot) 'build')
 Remove-Item -LiteralPath $fixture -Recurse -Force
}
