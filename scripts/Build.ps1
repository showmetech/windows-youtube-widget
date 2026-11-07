param([ValidateSet('x64','x86','arm64')][string]$Architecture='x64',[string]$CacheRoot,[switch]$SkipFixedRuntime)
. "$PSScriptRoot\Common.ps1"
$root=Get-RepositoryRoot
$buildRoot=Join-Path $root 'build'
$target=Join-Path $buildRoot "win-$Architecture"
Assert-ChildPath $target $buildRoot
if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
$sdk=Find-Dotnet
& $sdk publish (Join-Path $root 'src\YouTubeWidget.csproj') -c Release -r "win-$Architecture" --self-contained true -o $target
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
$launcher=Join-Path $buildRoot "startup-$Architecture"
& $sdk publish (Join-Path $root 'startup\StartupLauncher.csproj') -c Release -r "win-$Architecture" --self-contained true -o $launcher
if ($LASTEXITCODE -ne 0) { throw 'Startup launcher build failed.' }
Get-ChildItem -LiteralPath $launcher -Filter 'YouTubeWidget.Startup.*' | Where-Object Extension -ne '.pdb' | Copy-Item -Destination $target
if (-not $SkipFixedRuntime) {
 $runtime=Get-Content -LiteralPath (Join-Path $root 'packaging\webview2-runtime.json') -Raw | ConvertFrom-Json
 $item=$runtime.artifacts.$Architecture
 if (-not $CacheRoot) { $CacheRoot=Join-Path $buildRoot 'cache' }
 New-Item -ItemType Directory -Path $CacheRoot -Force | Out-Null
 $cab=Join-Path $CacheRoot "Microsoft.WebView2.FixedVersionRuntime.$($runtime.version).$Architecture.cab"
 if (-not (Test-Path -LiteralPath $cab)) {
  $uri=[uri]$item.url
  if ($uri.Scheme -ne 'https' -or -not $uri.Host.EndsWith('.microsoft.com')) { throw 'Runtime URL must use an official Microsoft HTTPS host.' }
  $ProgressPreference='SilentlyContinue'
  Invoke-WebRequest -Uri $uri -OutFile $cab
 }
 if ((Get-FileHash -LiteralPath $cab -Algorithm SHA256).Hash -ne $item.sha256) { throw 'WebView2 archive hash mismatch.' }
 $extract=Join-Path $buildRoot "runtime-$Architecture"
 Assert-ChildPath $extract $buildRoot
 if (Test-Path -LiteralPath $extract) { Remove-Item -LiteralPath $extract -Recurse -Force }
 New-Item -ItemType Directory -Path $extract -Force | Out-Null
 & expand.exe $cab '-F:*' $extract | Out-Null
 if ($LASTEXITCODE -ne 0) { throw 'WebView2 extraction failed.' }
 $runtimeExe=Get-ChildItem -LiteralPath $extract -Filter msedgewebview2.exe -Recurse | Select-Object -First 1
 if (-not $runtimeExe) { throw 'WebView2 executable not found in archive.' }
 $signature=Get-AuthenticodeSignature -LiteralPath $runtimeExe.FullName
 if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') { throw 'WebView2 Microsoft signature could not be verified.' }
 Copy-Item -LiteralPath $runtimeExe.DirectoryName -Destination (Join-Path $target 'WebView2Runtime') -Recurse
}
Copy-Item -LiteralPath (Join-Path $root 'extension') -Destination (Join-Path $target 'extension') -Recurse
Copy-Item -LiteralPath (Join-Path $root 'docs\CONECTAR-YOUTUBE.md') -Destination (Join-Path $target 'CONECTAR-YOUTUBE.md')
Copy-Item -LiteralPath (Join-Path $target 'Assets\App.ico') -Destination (Join-Path $target "Assets\App-$(Get-ReleaseVersion).ico")
Get-ChildItem -LiteralPath $target -Filter '*.pdb' -Recurse | Remove-Item -Force
$metadata=[ordered]@{version=(Get-ReleaseVersion);architecture=$Architecture;framework='self-contained .NET 10';webView2Bundled=(-not $SkipFixedRuntime);minimumWindowsBuild=19041}
$metadata | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $target 'distribution.json') -Encoding utf8
Write-Output "Built $target"
