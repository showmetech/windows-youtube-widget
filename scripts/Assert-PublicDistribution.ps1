param([Parameter(Mandatory)][string]$Directory)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $Directory).Path
$files=@(Get-ChildItem -LiteralPath $root -File -Recurse -Force)
foreach($file in $files){
 $relative=[IO.Path]::GetRelativePath($root,$file.FullName)
 if($relative -match '(^|[\\/])(Browser|TestProfile|SuiteTestProfile|User Data|\.git)([\\/]|$)' -or
    $file.Name -match '^(\.env($|\.)|(settings|recommendation(-refresh)?|runtime-health|broker-last|handoff.*|bridge-.*|native-host|tests|.*verification)\.json($|\.)|Cookies$|Login Data$|History$|Web Data$|Preferences$|Local State$)' -or
    $file.Extension -in @('.pfx','.p12','.pdb','.log')){throw "Private or test artifact refused: $relative"}
 if($file.Extension -in @('.json','.js','.html','.txt','.md','.xml','.config')){
  $text=[IO.File]::ReadAllText($file.FullName)
  if($text -match '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|github_pat_[A-Za-z0-9_]{30,}|ghp_[A-Za-z0-9]{30,}|AIza[0-9A-Za-z_-]{35}|ya29\.[A-Za-z0-9_-]{30,}|C:[\\/]+Users[\\/]+bruno[\\/]'){
   throw "Potential credential or developer profile path refused: $relative"
  }
 }
 if($file.Name -in @('WindowsYouTubeWidget.dll','WindowsYouTubeWidget.exe','YouTubeWidget.Startup.dll','YouTubeWidget.Startup.exe')){
  $bytes=[IO.File]::ReadAllBytes($file.FullName)
  foreach($encoding in @([Text.Encoding]::UTF8,[Text.Encoding]::Unicode)){
   if($encoding.GetString($bytes) -match '[A-Za-z]:[\\/]+Users[\\/]+[^\\/:\r\n\x00]+[\\/]'){
    throw "Developer profile path in application binary refused: $relative"
   }
  }
 }
}
Write-Output "Public distribution audit passed: $($files.Count) files; no private/test artifacts found."
