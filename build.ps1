$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$src = Join-Path $root 'src'
$srcHotkey = Join-Path $root 'src-hotkey'
$dist = Join-Path $root 'dist'
$icon = Join-Path $src 'app.ico'

if (-not (Test-Path $icon)) {
    & (Join-Path $root 'build_icon.ps1')
}

New-Item -ItemType Directory -Force -Path $dist | Out-Null

$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

# 1. Compile Main BrightKeys.exe
$files = @(Get-ChildItem $src -Filter '*.cs' | ForEach-Object { $_.FullName })
if ($files.Count -eq 0) { throw 'No source files found.' }

$argsMain = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/codepage:65001',
    "/win32icon:$icon",
    "/resource:$icon,app.ico",
    "/out:$dist\BrightKeys.exe",
    '/r:System.dll',
    '/r:System.Drawing.dll',
    '/r:System.Windows.Forms.dll',
    '/r:System.Core.dll'
) + $files

Write-Host "Compiling BrightKeys.exe..."
& $csc @argsMain
if ($LASTEXITCODE -ne 0) { throw "Compile BrightKeys.exe failed with exit code $LASTEXITCODE" }

# 2. Compile Lightweight Hotkey Launcher
$hotkeyFile = Join-Path $srcHotkey 'Hotkey.cs'
$hotkeyExe = Join-Path $dist 'BrightKeysHotkey.exe'
$argsHotkey = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/codepage:65001',
    "/win32icon:$icon",
    "/out:$hotkeyExe",
    '/r:System.dll'
) + @($hotkeyFile)

Write-Host "Compiling Hotkey launcher..."
& $csc @argsHotkey
if ($LASTEXITCODE -ne 0) { throw "Compile Hotkey launcher failed with exit code $LASTEXITCODE" }

Copy-Item $hotkeyExe (Join-Path $dist 'BrightKeysDown.exe') -Force
Copy-Item $hotkeyExe (Join-Path $dist 'BrightKeysUp.exe') -Force
Copy-Item $hotkeyExe (Join-Path $dist 'BrightKeysNext.exe') -Force
Copy-Item $hotkeyExe (Join-Path $dist 'BrightKeysNightLight.exe') -Force
Remove-Item $hotkeyExe -Force

Copy-Item (Join-Path $root 'README.txt') (Join-Path $dist 'README.txt') -Force

Write-Host 'Build OK:'
Get-ChildItem $dist | Select-Object Name, Length | Format-Table -AutoSize
