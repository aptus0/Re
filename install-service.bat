@echo off
REM Re.Api Windows Service Installation Script
REM This script installs Re.Api as a Windows Service
REM Run as Administrator

setlocal enabledelayedexpansion

REM Set the path to Re.Api.exe
set "API_EXE=%~dp0src\Re.Api\bin\Release\net10.0\Re.Api.exe"
set "SERVICE_NAME=Re.Api"
set "SERVICE_DISPLAY_NAME=Re ERP API Service"
set "SERVICE_DESCRIPTION=Re ERP API - Backend service for inventory, sales, accounts, and finance management"

echo.
echo ================== Re.Api Windows Service Installer ==================
echo.

REM Check if running as Administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
	echo ERROR: This script must be run as Administrator!
	echo Please right-click this batch file and select "Run as administrator"
	pause
	exit /b 1
)

REM Check if Re.Api.exe exists
if not exist "%API_EXE%" (
	echo ERROR: Re.Api.exe not found at: %API_EXE%
	echo Please ensure the Release build is complete.
	pause
	exit /b 1
)

echo Service executable: %API_EXE%
echo.

REM Check if service already exists
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% equ 0 (
	echo Service already exists. Stopping and removing old service...
	net stop "%SERVICE_NAME%" >nul 2>&1
	sc delete "%SERVICE_NAME%" >nul 2>&1
	echo Old service removed.
	echo.
)

REM Create the service
echo Creating service "%SERVICE_NAME%"...
sc create "%SERVICE_NAME%" binPath= "%API_EXE%" start= auto DisplayName= "%SERVICE_DISPLAY_NAME%"

if %errorLevel% neq 0 (
	echo ERROR: Failed to create service!
	pause
	exit /b 1
)

REM Set service description
sc description "%SERVICE_NAME%" "%SERVICE_DESCRIPTION%"

REM Set service to auto-start
sc config "%SERVICE_NAME%" start= auto >nul 2>&1

REM Set service recovery settings (restart on failure)
sc failure "%SERVICE_NAME%" reset= 30 actions= restart/5000/restart/5000/restart/5000 >nul 2>&1

echo.
echo ==================== Service Installation Complete ====================
echo.
echo Service Name: %SERVICE_NAME%
echo Display Name: %SERVICE_DISPLAY_NAME%
echo Start Type: Auto (starts with system)
echo.
echo Next steps:
echo 1. Type the following command to start the service:
echo    net start "%SERVICE_NAME%"
echo.
echo 2. To check service status:
echo    sc query "%SERVICE_NAME%"
echo.
echo 3. To view service logs (Event Viewer):
echo    eventvwr.msc
echo.
echo 4. To stop the service:
echo    net stop "%SERVICE_NAME%"
echo.
echo 5. To remove the service:
echo    sc delete "%SERVICE_NAME%"
echo.

pause
