# Claude Usage Monitor Installer
# Run as Administrator (optional, for startup registration)

param(
    [switch]$AddToStartup,
    [switch]$AddDesktopShortcut,
    [switch]$Uninstall
)

$AppName = "Claude Usage Monitor"
$InstallPath = "$env:LOCALAPPDATA\ClaudeUsageMonitor"
$ExeName = "ClaudeUsageMonitor.exe"
$StartupKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"

function Create-Shortcut {
    param($ShortcutPath, $TargetPath, $Description)
    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = $TargetPath
    $Shortcut.Description = $Description
    $Shortcut.WorkingDirectory = Split-Path $TargetPath
    $Shortcut.Save()
}

if ($Uninstall) {
    Write-Host "Uninstalling $AppName..." -ForegroundColor Yellow
    
    # Remove startup entry
    if (Get-ItemProperty -Path $StartupKey -Name $AppName -ErrorAction SilentlyContinue) {
        Remove-ItemProperty -Path $StartupKey -Name $AppName
        Write-Host "  Removed startup entry" -ForegroundColor Green
    }
    
    # Remove desktop shortcut
    $DesktopShortcut = "$env:USERPROFILE\Desktop\$AppName.lnk"
    if (Test-Path $DesktopShortcut) {
        Remove-Item $DesktopShortcut -Force
        Write-Host "  Removed desktop shortcut" -ForegroundColor Green
    }
    
    # Remove start menu shortcut
    $StartMenuShortcut = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\$AppName.lnk"
    if (Test-Path $StartMenuShortcut) {
        Remove-Item $StartMenuShortcut -Force
        Write-Host "  Removed start menu shortcut" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Note: Application files in $InstallPath were not removed." -ForegroundColor Cyan
    Write-Host "Delete manually if needed." -ForegroundColor Cyan
    exit 0
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "  $AppName Installer" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Check if running from extracted folder
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SourceExe = Join-Path $ScriptDir $ExeName

if (-not (Test-Path $SourceExe)) {
    Write-Host "Error: $ExeName not found in script directory." -ForegroundColor Red
    Write-Host "Please run this script from the extracted folder." -ForegroundColor Red
    exit 1
}

# Create install directory
if (-not (Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    Write-Host "Created install directory: $InstallPath" -ForegroundColor Green
}

# Copy files
Write-Host "Copying files..." -ForegroundColor Yellow
$FilesToCopy = Get-ChildItem -Path $ScriptDir -Recurse
$TotalFiles = ($FilesToCopy | Where-Object { -not $_.PSIsContainer }).Count
$CurrentFile = 0

foreach ($Item in $FilesToCopy) {
    $RelativePath = $Item.FullName.Substring($ScriptDir.Length + 1)
    $DestPath = Join-Path $InstallPath $RelativePath
    
    if ($Item.PSIsContainer) {
        if (-not (Test-Path $DestPath)) {
            New-Item -ItemType Directory -Path $DestPath -Force | Out-Null
        }
    } else {
        $CurrentFile++
        $DestDir = Split-Path $DestPath
        if (-not (Test-Path $DestDir)) {
            New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
        }
        Copy-Item -Path $Item.FullName -Destination $DestPath -Force
        Write-Progress -Activity "Copying files" -Status "$CurrentFile / $TotalFiles" -PercentComplete (($CurrentFile / $TotalFiles) * 100)
    }
}
Write-Progress -Activity "Copying files" -Completed
Write-Host "  Copied $TotalFiles files" -ForegroundColor Green

# Create Start Menu shortcut
$StartMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs"
$StartMenuShortcut = "$StartMenuPath\$AppName.lnk"
Create-Shortcut -ShortcutPath $StartMenuShortcut -TargetPath "$InstallPath\$ExeName" -Description $AppName
Write-Host "  Created Start Menu shortcut" -ForegroundColor Green

# Desktop shortcut (optional)
if ($AddDesktopShortcut) {
    $DesktopShortcut = "$env:USERPROFILE\Desktop\$AppName.lnk"
    Create-Shortcut -ShortcutPath $DesktopShortcut -TargetPath "$InstallPath\$ExeName" -Description $AppName
    Write-Host "  Created Desktop shortcut" -ForegroundColor Green
}

# Add to startup (optional)
if ($AddToStartup) {
    Set-ItemProperty -Path $StartupKey -Name $AppName -Value "$InstallPath\$ExeName"
    Write-Host "  Added to Windows startup" -ForegroundColor Green
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "  Installation Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Installed to: $InstallPath" -ForegroundColor White
Write-Host ""
Write-Host "Usage:" -ForegroundColor Yellow
Write-Host "  - Run from Start Menu: '$AppName'" -ForegroundColor White
Write-Host "  - Or run: $InstallPath\$ExeName" -ForegroundColor White
Write-Host ""

if (-not $AddToStartup) {
    Write-Host "Tip: To start automatically on login, run:" -ForegroundColor Cyan
    Write-Host "  .\install.ps1 -AddToStartup" -ForegroundColor White
    Write-Host ""
}

# Ask to launch
$Launch = Read-Host "Launch $AppName now? (Y/n)"
if ($Launch -ne 'n' -and $Launch -ne 'N') {
    Start-Process "$InstallPath\$ExeName"
    Write-Host "Launched $AppName" -ForegroundColor Green
}
