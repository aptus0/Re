@echo off
chcp 65001 >nul
REM Re.Api Windows Service Kurulum Scripti
REM Bu scripti Administrator olarak çalıştırın!

setlocal enabledelayedexpansion

REM Servinin kurulması için gereken bilgiler
set "API_EXE=%~dp0src\Re.Api\bin\Release\net10.0\Re.Api.exe"
set "SERVICE_NAME=Re.Api"
set "SERVICE_DISPLAY_NAME=Re ERP API Servis"
set "SERVICE_DESCRIPTION=Re ERP API - Envanzo, Satış, Muhasebe ve Finansman Yönetimi"

echo.
echo ========== Re.Api Windows Servis Kurulum Aracı ==========
echo.

REM Administrator yetkisi kontrol et
net session >nul 2>&1
if %errorLevel% neq 0 (
	echo [HATA] Bu script Administrator olarak çalışması gerekir!
	echo Lütfen bu dosyaya sağ tıklayıp "Yönetici olarak çalıştır" seçin.
	pause
	exit /b 1
)

REM Re.Api.exe dosyasının varlığı kontrol et
if not exist "%API_EXE%" (
	echo [HATA] Re.Api.exe bulunamadı: %API_EXE%
	echo Lütfen Release build işlemini tamamladığından emin olun.
	pause
	exit /b 1
)

echo Servis yürütülebilir: %API_EXE%
echo.

REM Eğer servis zaten varsa, sil
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% equ 0 (
	echo [*] Servis zaten var. Durduruluyor ve kaldırılıyor...
	net stop "%SERVICE_NAME%" >nul 2>&1
	timeout /t 2 /nobreak >nul
	sc delete "%SERVICE_NAME%" >nul 2>&1
	echo [✓] Eski servis kaldırıldı.
	echo.
)

REM Servisi oluştur
echo [*] Servis oluşturuluyor...
sc create "%SERVICE_NAME%" binPath= "%API_EXE%" start= auto DisplayName= "%SERVICE_DISPLAY_NAME%"

if %errorLevel% neq 0 (
	echo [HATA] Servis oluşturulamadı!
	pause
	exit /b 1
)

echo [✓] Servis oluşturuldu.

REM Servis açıklaması ayarla
sc description "%SERVICE_NAME%" "%SERVICE_DESCRIPTION%"

REM Otomatik başlatma ayarı
sc config "%SERVICE_NAME%" start= auto >nul 2>&1

REM Hata durumunda yeniden başlatma ayarı
sc failure "%SERVICE_NAME%" reset= 30 actions= restart/5000/restart/5000/restart/5000 >nul 2>&1

echo.
echo ================= Kurulum Tamamlandı =================
echo.
echo Servis Adı: %SERVICE_NAME%
echo Yapı Adı: %SERVICE_DISPLAY_NAME%
echo Başlatma Türü: Otomatik (sistem başlangıcı ile)
echo.
echo Sonraki Adımlar:
echo 1. Servisi başlatmak için aşağıdaki komutu yazın:
echo    net start "%SERVICE_NAME%"
echo.
echo 2. Servis durumunu kontrol etmek için:
echo    sc query "%SERVICE_NAME%"
echo.
echo 3. Servis loglarını görmek için (Event Viewer):
echo    eventvwr.msc
echo.
echo 4. Servisi durdurmak için:
echo    net stop "%SERVICE_NAME%"
echo.
echo 5. Servisi kaldırmak için:
echo    sc delete "%SERVICE_NAME%"
echo.
echo ====================================================
echo.
pause
