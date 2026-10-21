@echo off
REM Jun Link (AI chat with Jun) - PUBLIC build, no money/stat editor. This is the one you share.
REM Uses the game folder from Local.props (repo root), or pass it: build_public.bat "C:\path\to\game"
if "%~1"=="" (dotnet build -c Release -o out\public) else (dotnet build -c Release -p:GameDir="%~1" -o out\public)
if errorlevel 1 (echo Build failed. & pause & exit /b 1)
echo.
echo Done: out\public\JunLink.dll  -  copy it into the Mods folder of your game
pause
