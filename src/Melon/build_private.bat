@echo off
REM Jun Link - PRIVATE build (keeps the money/stat editor). For you only: never share JunLink.Private.dll.
REM Uses the game folder from Local.props (repo root), or pass it: build_private.bat "C:\path\to\game"
if "%~1"=="" (dotnet build -c Release -p:Private=true -o out\private) else (dotnet build -c Release -p:GameDir="%~1" -p:Private=true -o out\private)
if errorlevel 1 (echo Build failed. & pause & exit /b 1)
echo.
echo Done: out\private\JunLink.Private.dll  -  copy it into the Mods folder of your game
echo Keep only ONE Jun Link DLL in Mods (remove JunLink.dll and the old MdrgPanelML.dll).
pause
