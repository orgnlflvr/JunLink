@echo off
rem Builds JunLinkPet.exe (one file, nothing else needs to be installed to run it)
rem and packs the release zip JunLinkPet_1.0.0.zip (the exe + README.txt) next to this file.
cd /d "%~dp0"
dotnet publish -c Release -o publish || goto :err
copy /y README.txt publish\README.txt >nul
powershell -NoProfile -Command "Compress-Archive -Force -Path 'publish\JunLinkPet.exe','publish\README.txt' -DestinationPath 'JunLinkPet_1.0.0.zip'" || goto :err
echo.
echo Done:
echo   %~dp0publish\JunLinkPet.exe       (run this to test)
echo   %~dp0JunLinkPet_1.0.0.zip         (upload this to the GitHub release)
pause
exit /b 0
:err
echo Build failed.
pause
exit /b 1
