Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Get-RepositoryRoot { [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')) }
function Find-Dotnet {
 $candidates=@((Get-Command dotnet -ErrorAction SilentlyContinue).Source,(Join-Path $env:USERPROFILE 'scoop\apps\dotnet-sdk\current\dotnet.exe')) | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -Unique
 foreach ($candidate in $candidates) { if (@(& $candidate --list-sdks) -match '^10\.0\.') { return $candidate } }
 throw '.NET 10 SDK not found.'
}
function Get-ReleaseVersion {
 $value = (Get-Content -LiteralPath (Join-Path (Get-RepositoryRoot) 'VERSION') -Raw).Trim()
 if ($value -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION must contain major.minor.patch.' }
 return $value
}
function Assert-ChildPath([string]$Path,[string]$Root) {
 $target = [IO.Path]::GetFullPath($Path)
 $parent = [IO.Path]::GetFullPath($Root).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
 if (-not $target.StartsWith($parent,[StringComparison]::OrdinalIgnoreCase)) { throw "Path outside build root: $target" }
}
function Find-WindowsSdkTool([string]$Name) {
 if ($env:WINDOWS_SDK_BIN -and (Test-Path -LiteralPath (Join-Path $env:WINDOWS_SDK_BIN $Name))) { return Join-Path $env:WINDOWS_SDK_BIN $Name }
 $sdkRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
 if (Test-Path -LiteralPath $sdkRoot) {
  $match = Get-ChildItem -LiteralPath $sdkRoot -Directory | Sort-Object Name -Descending | ForEach-Object { Join-Path $_.FullName "x64\$Name" } | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
  if ($match) { return $match }
 }
 throw "Windows SDK tool $Name not found. Set WINDOWS_SDK_BIN to an installed SDK x64 directory."
}
