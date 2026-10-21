# Jun Link

Chat with Jun in **!Ω Factorial Omega: My Dystopian Robot Girlfriend** by Incontinent Cell, through an AI model running on your own PC. She remembers what happens in your save, greets you when you load it, and keeps a diary.

> [!IMPORTANT]
> This is a **fan-made mod**. It is not made, checked or supported by Incontinent Cell.
> It does not unlock or touch any paid or patron content, and the public build has no money or stat editor.
> Use it on a **copy** of your game folder.

> [!NOTE]
> **Looking for custom stories?** Since 1.1 they're their own mod: [Story Creator](https://github.com/orgnlflvr/StoryCreator). Jun Link is only the AI chat now. Install both if you want both.

## Version notice

| | Version |
| --- | --- |
| Jun Link | 1.1.0 |
| Game | 0.97.12 |
| MelonLoader | **0.7.3** (stable) |
| OS | Windows |

> [!WARNING]
> Use MelonLoader **0.7.3**. Do **not** enable "nightly builds" in the MelonLoader installer.

## Features

- **Chat with Jun** in the game (F2) or in your browser. She answers in her own voice with expressions and gestures. Replies come from your own local AI server (KoboldCpp, llama.cpp, LM Studio or Ollama). Nothing is sent to the internet.
- **She remembers.** Jun keeps notes of what happened in your save and your chats, and uses them when you talk.
- **She knows how you're doing.** Your health, stamina, mental health, hunger, mood and the time of day are part of what she sees.
- **Greeting.** Jun greets you once when you load a save.
- **Diary.** At the end of every in-game day, Jun writes a short diary entry. Read it at `http://127.0.0.1:8765/diary`.
- **Settings page.** Every option on one page (F8), no file editing.

### Jun has to be awake first

Jun Link switches on only when Jun is awake in **your** save:

- the prologue is finished
- the **Advanced AI** is unlocked: the scene where she tells you the "Advanced AI setup" has finished (story stage 3). Before that she runs the emergency AI and can't really talk
- she has all her body parts (both arms and both legs)

Until then, F2 shows a checklist of what is still missing. Keep playing; it unlocks by itself.

## Installation

1. Make a copy of your game folder. Work only in the copy.
2. Run the [MelonLoader](https://github.com/LavaGang/MelonLoader) installer, pick the game's `.exe` from the copy, choose **0.7.3**, and install.
3. Start the game once and let MelonLoader finish setting up. Close it at the menu.
4. Download `JunLink_1.1.0.zip` from [Releases](../../releases) and copy its `Mods` folder into the game folder.
5. Start your AI server. Easiest: [KoboldCpp](https://github.com/LostRuins/koboldcpp). Open `koboldcpp.exe`, pick a `.gguf` model, press **Launch**, and leave it running. It listens on `http://127.0.0.1:5001`, the mod's default.
6. Start the game and load your save.

> [!NOTE]
> **Updating:** replace `Mods\JunLink.dll`. **From MDRG Panel:** also delete `Mods\MdrgPanelML.dll` (Jun Link stays off while it is there); your files are copied to `UserData\JunLink` on the first start.

### Keys and pages

| Key / page | What it does |
| --- | --- |
| F2 | Chat with Jun in the game |
| F4 | Zoom in / out |
| F8 | Open the settings page |
| `http://127.0.0.1:8765` | Panel: chat, status |
| `http://127.0.0.1:8765/diary` | Jun's diary |

## Building from source

Jun Link and Story Creator are built from the same source. Requirements: the [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0), and a game folder with MelonLoader 0.7.3 that has been started once.

1. Copy `Local.props.example` to `Local.props` and put your game folder in it.
2. In `src\Melon`, run `build_public.bat`. The DLL lands in `src\Melon\out\public\JunLink.dll`.

Or by hand: `dotnet build -c Release -p:GameDir="C:\path\to\game"`.

> [!NOTE]
> A plain build is always the **public** build. There is a private build switch for the author's own testing; its DLL is never released.

## Configuration

Everything is on the settings page (F8 or `http://127.0.0.1:8765/settings`) and saved in `UserData\JunLink.cfg`. The main ones:

| Setting | Default | What it does |
| --- | --- | --- |
| AI server > Url | `http://127.0.0.1:5001` | Your AI server. llama-server `:8080`, LM Studio `:1234`, Ollama `:11434` |
| AI server > Model | empty | KoboldCpp and llama-server use the loaded model; Ollama and LM Studio need its name |
| Chat > GreetOnLoad | true | Jun greets you when a save loads |
| Diary > Enabled | true | Jun writes a diary entry at the end of each day |
| Requirements | see above | When Jun Link switches on |
| Panel > Port | 8765 | Address of the web pages |

## Reporting problems

Open an [issue](../../issues) and attach `MelonLoader\Latest.log`. Say your game version, your AI server and model, and what you did right before it went wrong.

## Credits

- Made by **Original Flavour** ([@orgnlflvr](https://github.com/orgnlflvr)).
- **!Ω Factorial Omega: My Dystopian Robot Girlfriend** and all its characters, art and music belong to Incontinent Cell. This mod is unofficial.
- [MelonLoader](https://github.com/LavaGang/MelonLoader) by LavaGang.
- [Montserrat](https://github.com/JulietaUla/Montserrat) font (SIL Open Font License), built into the web pages.
- Thanks to Ikari and the MDRG modding community.

## License

The mod's own code is under the [MIT License](LICENSE.txt).
This does not cover the game or anything from it.
