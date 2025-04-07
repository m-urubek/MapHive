@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0find_excluded_files.ps1"
echo.
echo Press any key to exit...
pause > nul 