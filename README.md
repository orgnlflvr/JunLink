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
| Jun Link | 1.3.0 |
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
- **She has feelings of her own.** How much she likes you, how much she misses you and her desire come straight from the game's own brain state and colour how she talks.
- **Diary.** At the end of every in-game day, Jun writes a short diary entry. Read it at `http://127.0.0.1:8765/diary`.
- **Personality from her diary** *(new in 1.2)*. One button on the diary page: your AI writes her lines for everyday moments (greeting, headpat, cuddles, kisses, shower) from her diary and how she feels, and saves them as a normal game personality mod. Restart the game, buy **Jun's Heart (Jun Link)** in the Lady Parts shop and equip it. While she misses you a lot, her greeting and cuddle lines switch to "I missed you" ones by themselves.
- **Update notice** *(new in 1.2)*. When the game starts, Jun Link asks GitHub once whether a newer version is out and tells you in the log and on its pages. It never downloads or installs anything by itself. Turn it off with Panel > CheckUpdates.
- **Stream mode** *(new in 1.3)*. Jun goes live as a VTuber on **streamhub.ic**, a made-up stream site on Anon's PC, with live chat, donations, a fishing game, Mod Duty and more. See [Stream mode](#stream-mode).
- **A more human Jun** *(new in 1.3)*. How robotic she sounds follows your bond in the game: cold and literal early on, warm and natural once she loves you, with her android habits only slipping out now and then.
- **Settings page.** Every option on one page (F8), no file editing.

### Personality moments

The moments it writes are listed in `UserData\JunLink\personality_triggers.txt`, one per line: `TriggerName | what happens`. Add any trigger from the game's personality trigger list. Thanks to the MDRG modding community for the idea and the trigger list.

> [!NOTE]
> **Adult moments are optional**: a checkbox on the diary page, off by default, 18+, consensual scenes only. Non-consent triggers are never written and keep the game's own lines. The lines are written on your PC by your own AI and stay there; nothing explicit is part of this mod or this repo.

### Stream mode

Open the PC in the room. The desktop has a new **streamhub.ic** shortcut that opens the site in the game's own browser: pick a title and a description (or press **Random**), check the **stream rules** and press **Go Live**. You go back to the room and the studio opens.

> [!NOTE]
> streamhub.ic is a **made-up, SFW** site with made-up viewers and chat. The **donations are paid out to you as in-game money** when the stream ends: once per in-game day, after a 50% platform cut, up to a daily cap (15% of your weekly rent by default). Turn it off or tune it in the settings (Stream > Payout). Stream mode is separate from the game's own stream and doesn't touch it. Jun has to be **politely dressed** to go live: a top and a bottom (a dress counts as both).

- **Scenes** (buttons under the video): **Chatting**, **Close-up**, **Fishing** (a small fishing game with her facecam in the corner), **Mod Duty** (delete spam and rude messages before they stay up, without hitting nice viewers) and **BRB**.
- **Live chat.** Your AI writes viewer messages and Jun answers them, plus donations, superchats, new subscribers and gifted subs. **Click any message** and she answers that viewer next. Type in the in-game chat (F2) to talk to her on stream yourself.
- **Visitors.** People she knows from the city sometimes drop into chat (Lady Annalie, Melissa and the Landlord; Big Daddy and Shanice are in `stream_guests.txt`, switched off so they don't spoil anything, remove the `#` once you've met them), and two very famous tech CEOs might stop by with a big superchat.
- **Backgrounds.** Drawn ones, a snapshot of your apartment, or your own pictures (**+ My BG** opens the folder).
- **Channel.** Likes, subscribers (kept between streams), a stats menu (top left), and an end-of-stream summary.
- **She remembers.** After each stream the facts go into her memory and she writes a short note about it. Ask her "how was the stream?" later. The diary page has a **Her streams** section.

Files in `UserData\JunLink` you can edit:

| File / folder | What it is |
| --- | --- |
| `stream_backgrounds\` | Your own background pictures (.png / .jpg) |
| `stream_guests.txt` | Who can drop into chat and how they talk (made after your first stream) |
| `stream_channel.txt` | Starting subscriber count for a save that never streamed (the real count is kept in your save) |
| `jun_streams.jsonl` | Her past streams and notes |

### Desktop pet (optional)

**Jun Link Pet** is a small Windows app that shows Jun on your desktop, drawn live by the game: drag her around, click her, talk to her. It needs the game running with Jun Link and your save loaded. Download `JunLinkPet_1.0.0.zip` from [Releases](../../releases), unzip `JunLinkPet.exe` anywhere and run it. Windows may say "Windows protected your PC" because the exe isn't signed: **More info > Run anyway**. Source and build script: [`pet/`](pet).

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
4. Download `JunLink_1.3.0.zip` from [Releases](../../releases) and copy its `Mods` and `UserData` folders into the game folder.
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
| `http://127.0.0.1:8765/diary` | Jun's diary and her streams |

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
| Chat > BrainState | true | Her own feelings from the game (sympathy, longing, desire) shape her replies |
| Diary > Enabled | true | Jun writes a diary entry at the end of each day |
| Stream > Enabled | true | The streamhub.ic icon on Anon's PC and stream mode |
| Stream > Payout | true | Stream donations become in-game money (once per in-game day) |
| Stream > PayoutPercent | 50 | Your share after the platform's cut, in % |
| Stream > PayoutDailyCap | 0 | Most one day of streaming can pay, in $ (0 = 15% of your weekly rent) |
| Requirements | see above | When Jun Link switches on |
| Panel > CheckUpdates | true | Ask GitHub once per game start whether a newer version is out (only tells you) |
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
