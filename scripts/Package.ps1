param([ValidateSet('x64','x86','arm64')][string]$Architecture='x64',[string]$InnoCompiler)
. "$PSScriptRoot\Common.ps1"
$root=Get-RepositoryRoot
if(-not $InnoCompiler){$InnoCompiler=Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'}
if(-not(Test-Path -LiteralPath $InnoCompiler)){throw 'Install Inno Setup 6 or pass -InnoCompiler.'}
$version=Get-ReleaseVersion
$build=Join-Path $root "build\win-$Architecture"
if(-not (Test-Path -LiteralPath (Join-Path $build 'WebView2Runtime\msedgewebview2.exe'))){throw 'Public distributions require the bundled, verified WebView2 runtime.'}
& $InnoCompiler "/DAppVersion=$version" "/DTargetArchitecture=$Architecture" "/DBuildRoot=$build" (Join-Path $root 'packaging\installer.iss')
if($LASTEXITCODE -ne 0){throw 'Installer build failed.'}
$out=Join-Path $root 'artifacts'
Compress-Archive -LiteralPath $build -DestinationPath (Join-Path $out "YouTubeWidget-$version-$Architecture-portable.zip") -Force
$store=Get-Content -LiteralPath (Join-Path $root 'packaging\store-config.json') -Raw | ConvertFrom-Json
& "$PSScriptRoot\Build-Msix.ps1" -Architecture $Architecture -Development:(-not $store.identityName)
Write-Output "Packages prepared for $Architecture"
