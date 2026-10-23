Jun Link Pet 1.0.0 - Jun on your desktop (optional companion app for Jun Link)
==============================================================================
Made by Original Flavour (github.com/orgnlflvr). A fan project, not made or supported by Incontinent Cell.

A small see-through window that shows Jun, drawn live by the game: drag her around, click her, talk to her.
It is a separate Windows program, not a game mod. It only works together with:
  - the game running (it can be minimized) with your save loaded, and
  - the Jun Link mod 1.1 or newer installed (it sends the pictures, on http://127.0.0.1:8765), and
  - for talking: your AI server running, same as for Jun Link's chat.
Jun has to be awake in your save (Advanced AI, all body parts), like for Jun Link itself.

INSTALL
  Unzip JunLinkPet.exe anywhere (for example your Documents folder) and double-click it. Nothing else to install.
  Windows may warn "Windows protected your PC" because the exe is not signed (signing costs money):
  click "More info" > "Run anyway". The source is in the JunLink repo (pet folder) if you want to check or build it yourself.

USE
  1. Start the game with Jun Link, load your save, wait until you are in the room.
  2. Run JunLinkPet.exe. She appears at the bottom right of your screen. There is also an icon in the tray (next to the clock).
  - Drag her with the left mouse button.            - Click her head or body: she reacts.
  - Double-click her, or right-click > Talk to Jun: type a message and press Enter (Esc = cancel).
  - Right-click her (or the tray icon) for the menu: size, always on top, pause, start with Windows, start the game with the pet, quit.

START WITH WINDOWS / THE GAME
  - Right-click > "Start with Windows" makes the pet start when you log in.
  - Right-click > "Choose the game's .exe..." then tick "Start the game with the pet": the pet starts the game minimized if it is not
    already running. The game opens at its main menu, so you click "continue" once; she appears as soon as the save is loaded.
  - To stop it: right-click > Pause (the game then stops drawing her) or Quit. Untick the two options to stop the autorun.

IF SOMETHING LOOKS WRONG
  - Dark or bright outline around her: right-click > toggle "Fix dark edges".
  - She does not appear: hover the tray icon, its tooltip says what it is waiting for ("start the game with Jun Link",
    "load your save in the game").
  - Heavy on the PC: pick a smaller size, lower Pet > Fps and Pet > Height in Jun Link's settings page (F8 in the game), or Pause
    when you do not need her.
  Settings are saved in  %APPDATA%\JunLinkPet\settings.json   (settings of the older MdrgPet are picked up once)

UNINSTALL
  Right-click > untick "Start with Windows", Quit, delete JunLinkPet.exe and the %APPDATA%\JunLinkPet folder.

BUILD FROM SOURCE
  Double-click build_pet.bat (needs the .NET 8 SDK). Result: publish\JunLinkPet.exe, one file.

Fonts: Montserrat (SIL Open Font License, see fonts\OFL.txt) is packed inside the exe, so nothing is downloaded.
