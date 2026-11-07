param([Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,[switch]$AllowPendingStore)
. "$PSScriptRoot\Common.ps1"
if([Environment]::OSVersion.Version -lt [version]'10.0.19041'){throw 'Windows 10 build 19041 or later is required.'}
$repo='showmetech/windows-youtube-widget'
$api="https://api.github.com/repos/$repo/releases/tags/v$Version"
$release=Invoke-RestMethod -Uri $api -Headers @{'User-Agent'='YouTubeWidget-Release'}
$receiptAsset=$release.assets | Where-Object name -eq 'store-submission.json' | Select-Object -First 1
if(-not $receiptAsset){throw 'Store publication receipt is missing; desktop update is gated.'}
$receipt=Invoke-RestMethod -Uri $receiptAsset.browser_download_url
if($receipt.version -ne $Version){throw 'Store publication receipt version mismatch.'}
if($receipt.state -notin @('submitted','published') -and -not $AllowPendingStore){throw "Store publication is $($receipt.state). Finish the existing Store onboarding before updating the desktop."}
$arch=[Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
if($arch -notin @('x64','x86','arm64')){throw "Unsupported Windows architecture: $arch"}
$name="Windows-YouTube-Widget-$Version-$arch-Setup.exe"
$asset=$release.assets | Where-Object name -eq $name | Select-Object -First 1
$sums=$release.assets | Where-Object name -eq 'SHA256SUMS.txt' | Select-Object -First 1
if(-not $asset -or -not $sums){throw 'Release installer or checksums are missing.'}
$work=Join-Path ([IO.Path]::GetTempPath()) ('YouTubeWidgetRelease-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$installer=Join-Path $work $name
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $installer
$sumText=(Invoke-WebRequest -Uri $sums.browser_download_url).Content
$line=@($sumText -split "`n" | Where-Object { $_.TrimEnd().EndsWith('  '+$name) })
if($line.Count -ne 1){throw 'The installer must have exactly one checksum entry.'}
$expected=($line[0] -split '\s+')[0]
if((Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash -ne $expected){throw 'Release installer checksum mismatch.'}
$process=Start-Process -FilePath $installer -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw "Installer exited with $($process.ExitCode)."}
$installed=Join-Path $env:LOCALAPPDATA 'Programs\WindowsYouTubeWidget\WindowsYouTubeWidget.exe'
$actual=(Get-Item -LiteralPath ([IO.Path]::ChangeExtension($installed,'.dll'))).VersionInfo.ProductVersion
if($actual -notlike "$Version*"){throw 'Installed app version does not match the GitHub release.'}
Start-Process -FilePath $installed -ArgumentList '--startup' -WindowStyle Hidden
Write-Output "Installed GitHub release $Version; Store state: $($receipt.state)."
