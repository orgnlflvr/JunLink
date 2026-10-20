Jun Link (formerly MDRG Panel) - MelonLoader build

There are two builds of the same mod. Keep only ONE of them in the game's Mods folder.

  PUBLIC   JunLink.dll           the one you share. No money/stat editor.
  PRIVATE  JunLink.Private.dll   for you only. Keeps the money/stat editor. Never share it.

Build (in this Melon folder). Put your game folder in Local.props at the repo root once (copy Local.props.example), then:
  build_public.bat      -> out\public\JunLink.dll        (or build_public.bat "C:\path\to\game")
  build_private.bat     -> out\private\JunLink.Private.dll  (or build_private.bat "C:\path\to\game")
or by hand:
  dotnet build -c Release -p:GameDir="C:\path\to\game"                      (public)
  dotnet build -c Release -p:GameDir="C:\path\to\game" -p:Private=true      (private)
A plain build is always the public one, so nobody gets the editor by accident.

Safety switches
- If the old MdrgPanelML.dll is still in Mods, Jun Link switches itself off and says so in MelonLoader\Latest.log. Remove the old DLL.
- If both JunLink.dll and JunLink.Private.dll are in Mods, the public one switches itself off.

Where things go
- Settings:   <game>\UserData\JunLink.cfg
- Mod files:  <game>\UserData\JunLink\     (jun_stories, jun_backgrounds, jun_memory.txt, chat_log.txt, jun_diary.jsonl, ...)
- On the first start after the rename, your old UserData\MdrgPanel folder and MdrgPanel.cfg are COPIED over (the old ones stay as a backup).
- Web pages:  http://127.0.0.1:8765
- Log:        MelonLoader\Latest.log
