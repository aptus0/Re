# Re.Api Windows Service Kurulum Scripti
# PowerShell'i Administrator olarak çalıştırın!
# Kullanım: Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force; .\kurulum-servis.ps1

$serviceName = "Re.Api"
$serviceDisplayName = "Re ERP API Servis"
$serviceDescription = "Re ERP API - Envanzo, Satış, Muhasebe ve Finansman Yönetimi"
$exePath = "C:\Users\ttsy\source\repos\Envanzo\src\Re.Api\bin\Release\net10.0\Re.Api.exe"

Write-Host "========== Re.Api Windows Servis Kurulum Aracı ==========" -ForegroundColor Cyan
Write-Host ""

# Administrator yetkisi kontrol et
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
	Write-Host "[HATA] Bu script Administrator olarak çalışması gerekir!" -ForegroundColor Red
	Write-Host "Lütfen PowerShell'i sağ tıklayıp 'Yönetici olarak çalıştır' seçin." -ForegroundColor Yellow
	Read-Host "Devam etmek için Enter tuşuna basın"
	exit 1
}

# Dosyanın varlığı kontrol et
if (-not (Test-Path $exePath)) {
	Write-Host "[HATA] Re.Api.exe bulunamadı: $exePath" -ForegroundColor Red
	Write-Host "Lütfen Release build işlemini tamamladığından emin olun." -ForegroundColor Yellow
	Read-Host "Devam etmek için Enter tuşuna basın"
	exit 1
}

Write-Host "Servis yürütülebilir: $exePath" -ForegroundColor Green
Write-Host ""

# Eğer servis varsa sil
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {
	Write-Host "[*] Servis zaten var. Durduruluyor..." -ForegroundColor Yellow
	Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
	Start-Sleep -Seconds 2
	Write-Host "[*] Eski servis kaldırılıyor..." -ForegroundColor Yellow
	sc.exe delete $serviceName | Out-Null
	Start-Sleep -Seconds 2
}

# Servisi oluştur
Write-Host "[*] Servis oluşturuluyor..." -ForegroundColor Yellow
$result = sc.exe create $serviceName binPath= $exePath start= auto DisplayName= $serviceDisplayName
if ($LASTEXITCODE -ne 0) {
	Write-Host "[HATA] Servis oluşturulamadı!" -ForegroundColor Red
	Write-Host $result -ForegroundColor Red
	Read-Host "Devam etmek için Enter tuşuna basın"
	exit 1
}

Write-Host "[✓] Servis oluşturuldu." -ForegroundColor Green

# Servis açıklaması ayarla
sc.exe description $serviceName $serviceDescription | Out-Null

# Otomatik başlatma ayarı
sc.exe config $serviceName start= auto | Out-Null

# Hata durumunda yeniden başlatma ayarı (30 sn reset, failures)
sc.exe failure $serviceName reset= 30 actions= restart/5000/restart/5000/restart/5000 | Out-Null

Write-Host ""
Write-Host "================= Kurulum Tamamlandı =================" -ForegroundColor Green
Write-Host ""
Write-Host "Servis Adı: $serviceName" -ForegroundColor Cyan
Write-Host "Yapı Adı: $serviceDisplayName" -ForegroundColor Cyan
Write-Host "Başlatma Türü: Otomatik (sistem başlangıcı ile)" -ForegroundColor Cyan
Write-Host ""
Write-Host "Sonraki Adımlar:" -ForegroundColor Yellow
Write-Host "1. Servisi başlatmak için:" -ForegroundColor White
Write-Host "   net start $serviceName" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. Servis durumunu kontrol etmek için:" -ForegroundColor White
Write-Host "   Get-Service $serviceName" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. Servis loglarını görmek için (Event Viewer):" -ForegroundColor White
Write-Host "   eventvwr.msc" -ForegroundColor Cyan
Write-Host ""
Write-Host "4. Servisi durdurmak için:" -ForegroundColor White
Write-Host "   net stop $serviceName" -ForegroundColor Cyan
Write-Host ""
Write-Host "5. Servisi kaldırmak için:" -ForegroundColor White
Write-Host "   sc.exe delete $serviceName" -ForegroundColor Cyan
Write-Host ""
Write-Host "=====================================================" -ForegroundColor Green
Write-Host ""

Read-Host "Tamamlamak için Enter tuşuna basın"
