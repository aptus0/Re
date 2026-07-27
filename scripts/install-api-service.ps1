# PowerShell Script to install Re.Api as a Windows Service

$serviceName = "Re.Api"
$binPath = "$PSScriptRoot\..\src\Re.Api\bin\Debug\net10.0\Re.Api.exe"
$resolvedPath = [System.IO.Path]::GetFullPath($binPath)

Write-Host "Re.Api Windows Servis Kurulumu" -ForegroundColor Cyan
Write-Host "Exe Yolu: $resolvedPath" -ForegroundColor Yellow

if (-not (Test-Path $resolvedPath)) {
    Write-Host "HATA: Executable bulunamadı. Lütfen önce projeyi derleyin (dotnet build)." -ForegroundColor Red
    exit 1
}

# Check if service exists
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-Host "Servis zaten mevcut. Durduruluyor..." -ForegroundColor Yellow
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $serviceName
    Start-Sleep -Seconds 2
}

Write-Host "Yeni Windows Servisi oluşturuluyor ($serviceName)..." -ForegroundColor Green
sc.exe create $serviceName binPath= "`"$resolvedPath`"" start= auto

Write-Host "Servis başlatılıyor..." -ForegroundColor Green
Start-Service -Name $serviceName

Write-Host "Re.Api başarıyla Windows Servisi olarak kuruldu ve başlatıldı!" -ForegroundColor Green
