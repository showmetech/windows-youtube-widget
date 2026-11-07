param([ValidateSet('x64','x86','arm64')][string]$Architecture='x64',[switch]$Development)
. "$PSScriptRoot\Common.ps1"
$root=Get-RepositoryRoot
$config=Get-Content -LiteralPath (Join-Path $root 'packaging\store-config.json') -Raw | ConvertFrom-Json
if (-not $Development -and (-not $config.identityName -or -not $config.publisher)) { throw 'The existing Partner Center product identity must be configured before creating a Store submission package.' }
$identity=if($Development){'Showmetech.YouTubeWidget.Development'}else{$config.identityName}
$publisher=if($Development){'CN=Unassigned-Development-Publisher'}else{$config.publisher}
$stage=Join-Path $root "build\msix-$Architecture"
Assert-ChildPath $stage (Join-Path $root 'build')
if(Test-Path -LiteralPath $stage){Remove-Item -LiteralPath $stage -Recurse -Force}
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root "build\win-$Architecture") -Destination (Join-Path $stage 'app') -Recurse
Copy-Item -LiteralPath (Join-Path $root 'packaging\Assets') -Destination (Join-Path $stage 'Assets') -Recurse
$manifest=Get-Content -LiteralPath (Join-Path $root 'packaging\AppxManifest.template.xml') -Raw
$replacements=@{'@IDENTITY@'=$identity;'@PUBLISHER@'=$publisher;'@VERSION@'=(Get-ReleaseVersion);'@ARCH@'=$Architecture;'@PUBLISHER_DISPLAY@'=$config.publisherDisplayName}
foreach($key in $replacements.Keys){$manifest=$manifest.Replace($key,[Security.SecurityElement]::Escape([string]$replacements[$key]))}
$manifest | Set-Content -LiteralPath (Join-Path $stage 'AppxManifest.xml') -Encoding utf8
$out=Join-Path $root 'artifacts';New-Item -ItemType Directory -Path $out -Force | Out-Null
$suffix=if($Development){'-development'}else{''}
$package=Join-Path $out "YouTubeWidget-$(Get-ReleaseVersion)-$Architecture$suffix.msix"
$log=Join-Path $out "msix-$Architecture.log"
& (Find-WindowsSdkTool 'makeappx.exe') pack /d $stage /p $package /o *> $log
if($LASTEXITCODE -ne 0){Get-Content -LiteralPath $log -Tail 20;throw 'MSIX packaging/schema validation failed.'}
Write-Output $package
