@echo off
REM Story Creator (custom stories, minigames, story packs). Public, share this one.
REM Uses the game folder from Local.props (repo root), or pass it: build_story.bat "C:\path\to\game"
if "%~1"=="" (dotnet build -c Release -p:Mod=Story -o out\story) else (dotnet build -c Release -p:GameDir="%~1" -p:Mod=Story -o out\story)
if errorlevel 1 (echo Build failed. & pause & exit /b 1)
echo.
echo Done: out\story\StoryCreator.dll  -  copy it into the Mods folder of your game
pause
