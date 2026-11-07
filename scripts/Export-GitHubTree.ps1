. "$PSScriptRoot\Common.ps1"
$root=Get-RepositoryRoot
$textExtensions=@('.cs','.csproj','.props','.manifest','.xml','.json','.js','.cjs','.md','.txt','.html','.ps1','.iss','.yml','.yaml')
$roots=@('src','startup','extension','packaging','scripts','tests','docs','.github')
$files=@()
foreach($folder in $roots){$files+=Get-ChildItem -LiteralPath (Join-Path $root $folder) -File -Recurse | Where-Object FullName -NotMatch '\\(bin|obj|Browser|TestProfile|SuiteTestProfile)\\'}
foreach($file in @('VERSION','Directory.Build.props','global.json','.gitignore','AGENTS.md','README.md','index.html','privacy.html','.nojekyll')){if(Test-Path -LiteralPath (Join-Path $root $file)){$files+=Get-Item -LiteralPath (Join-Path $root $file)}}
$entries=foreach($file in $files | Sort-Object FullName -Unique){
 $relative=[IO.Path]::GetRelativePath($root,$file.FullName).Replace('\','/')
 if($file.Extension -in @('.exe','.dll','.cab','.pfx','.p12','.key') -or $file.Name -match '^\.env|^settings\.json$|credentials|bridge-') {throw "Private/build file refused: $relative"}
 $isText=$file.Extension -in $textExtensions -or $file.Name -in @('VERSION','.gitignore','.nojekyll')
 $content=if($isText){[IO.File]::ReadAllText($file.FullName)}else{[Convert]::ToBase64String([IO.File]::ReadAllBytes($file.FullName))}
 if($isText -and ($content -match '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|github_pat_[A-Za-z0-9_]{30,}|ghp_[A-Za-z0-9]{30,}')) {throw "Potential secret refused: $relative"}
 [ordered]@{path=$relative;mode='100644';type='blob';encoding=if($isText){'utf-8'}else{'base64'};content=$content}
}
$entries | ConvertTo-Json -Depth 4 -Compress
