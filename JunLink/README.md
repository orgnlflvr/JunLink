# Jun Link

An AI companion mod for **!Ω Factorial Omega: My Dystopian Robot Girlfriend** by Incontinent Cell.
Talk to Jun through an AI model running on your own PC, let her greet you and keep a diary, and play custom stories (with bowling and other timing minigames) in the game's own dialogue box.

> [!IMPORTANT]
> This is a **fan-made mod**. It is not made, checked or supported by Incontinent Cell.
> It does not unlock or touch any paid or patron content, and the public build has no money or stat editor.
> Use it on a **copy** of your game folder.

## Version notice

| | Version |
| --- | --- |
| Jun Link | 1.0.0 |
| Game | 0.97.12 |
| MelonLoader | **0.7.3** (stable) |
| OS | Windows |

> [!WARNING]
> Use MelonLoader **0.7.3**. Do **not** enable "nightly builds" in the MelonLoader installer.
> Other game versions may work, but are untested. If something breaks after a game update, open an issue with your `MelonLoader\Latest.log`.

## Overview

- **Chat with Jun** in the game (F2) or in your browser. Replies come from your own local AI server (KoboldCpp, llama.cpp, LM Studio or Ollama). Nothing is sent to the internet.
- **She remembers.** Jun keeps notes of what happened in your save and your chats, and uses them when you talk.
- **Greeting.** Jun greets you once when you load a save.
- **Diary.** At the end of every in-game day, Jun writes a short diary entry. Read it at `http://127.0.0.1:8765/diary`.
- **Custom stories.** Write and play your own scenes with the real portraits, backgrounds, faces, outfits and music (F3). There's an editor in your browser with a checker.
- **Minigames in stories.** Stories can use the game's own timing bar: bowling matches, repairs, rescues.
- **Story packs.** Share stories as one `.storypack` file.
- **Settings page.** Every option is in one page (F8), no file editing needed.

### Jun has to be awake first

The AI side (chat, greeting, diary, stories) only switches on when Jun is awake in **your** save:

- the prologue is finished
- the **Advanced AI** is unlocked: the scene where she tells you the "Advanced AI setup" has finished (story stage 3). Before that she runs the emergency AI and can't really talk
- she has all her body parts (both arms and both legs)

Until then, F2 and F3 show a checklist of what is still missing. Keep playing; it unlocks by itself. The limits are in Settings > Requirements.

## Installation

1. Make a copy of your game folder. Work only in the copy.
2. Run the [MelonLoader](https://github.com/LavaGang/MelonLoader) installer, pick the game's `.exe` from the copy, choose **0.7.3**, and install.
3. Start the game once and let MelonLoader finish setting up (it can take a few minutes). Close it at the menu.
4. Download `JunLink_1.0.0.zip` from [Releases](../../releases) and copy its `Mods` and `UserData` folders into the game folder (next to the game `.exe`).
5. Start your AI server. Easiest: [KoboldCpp](https://github.com/LostRuins/koboldcpp). Open `koboldcpp.exe`, pick a `.gguf` model, press **Launch**, and leave it running. It listens on `http://127.0.0.1:5001`, the mod's default.
6. Start the game and load your save.

> [!NOTE]
> Avoid non-Latin characters in the game's folder path.

### Keys and pages

| Key / page | What it does |
| --- | --- |
| F2 | Chat with Jun in the game |
| F3 | Play a custom story (press again to stop) |
| F4 | Zoom in / out |
| F8 | Open the settings page |
| `http://127.0.0.1:8765` | Panel: chat, status, links |
| `http://127.0.0.1:8765/stories` | Story editor |
| `http://127.0.0.1:8765/packs` | Install and make story packs |

## Updating / Uninstalling

**From MDRG Panel (the old name):** delete `Mods\MdrgPanelML.dll`. Jun Link stays off while it is there. On the first start, your stories, memory, diary and settings are copied from `UserData\MdrgPanel` to `UserData\JunLink`; the old folder stays as a backup.

**Updating Jun Link:** replace `Mods\JunLink.dll` with the new one. Your files in `UserData\JunLink` stay.

**Uninstalling:** delete `Mods\JunLink.dll`, the `UserData\JunLink` folder and `UserData\JunLink.cfg`. Back up the folder first if you want to keep your stories and Jun's memory.

## Backgrounds for stories

Some included stories use the game's own places (bowling alley, karaoke bar, beach). Those pictures are **not** in this repo or the release. Make them from your own copy of the game:

```
pip install UnityPy
python "UserData\JunLink\tools\export_backgrounds.py" "C:\path\to\your\game"
```

> [!CAUTION]
> The exported pictures belong to the game. Keep them on your PC. Never upload them, put them in a story pack, or open a pull request with them.

## Writing your own stories

Stories are plain `.story` text files in `UserData\JunLink\jun_stories`. A tiny one:

```
title: Hello
# start
@background room
{bot} [happy]: You wrote a story. I am proud of you.
? Thanks -> end
? Again! -> start
```

A bowling throw with the game's timing bar:

```
@score reset
@minigame skillcheck 1
{bot} [lecture]: {pins} pins. Total: {total}.
@total win>=8 lose
```

The full command list, every character's faces, and a step-by-step bowling match are in the story-writing guide. The editor's help panel at `/stories` has them too.

## Building from source

Requirements: the [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0), and a game folder with MelonLoader 0.7.3 that has been started once (so `MelonLoader\Il2CppAssemblies` exists).

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
| Requirements | see above | When the AI side switches on |
| Panel > Port | 8765 | Address of the web pages |

## Reporting problems

Open an [issue](../../issues) and attach `MelonLoader\Latest.log`. Say your game version and what you did right before it went wrong.

## Credits

- Made by **Original Flavour** ([@orgnlflvr](https://github.com/orgnlflvr)).
- **!Ω Factorial Omega: My Dystopian Robot Girlfriend** and all its characters, art and music belong to Incontinent Cell. This mod is unofficial.
- [MelonLoader](https://github.com/LavaGang/MelonLoader) by LavaGang.
- [Montserrat](https://github.com/JulietaUla/Montserrat) font (SIL Open Font License), built into the web pages.
- Thanks to the MDRG modding community.

## License

The mod's own code and story files are under the [MIT License](LICENSE.txt).
This does not cover the game or anything from it.
