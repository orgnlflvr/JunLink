"""
Saves some of the game's own background pictures (bowling alley, karaoke, ...) from YOUR OWN install
into jun_backgrounds, so a story can use them with  @background bowling.
Runs only on your PC. Nothing is uploaded. Keep the pictures private (they are the game's art, do not share them).

Setup (once):   pip install UnityPy
Run it from the game folder (where the game .exe is), with Jun Link installed:
  python "UserData\\StoryCreator\\tools\\export_backgrounds.py"
It finds <GameName>_Data and saves into UserData\\StoryCreator\\jun_backgrounds by itself.
Or name the game folder:  python export_backgrounds.py "C:\\path\\to\\game"
Or say everything:        python export_backgrounds.py --data "<game>\\<GameName>_Data" --out "<jun_backgrounds folder>"
(resources.assets AND resources.assets.resS must both be in the _Data folder.)
Add --list to only print every big picture name in resources.assets, or --names bowling,karaoke to save only those (any name from --list works).
In a story the name is the lowercase file name:  @background bowling   (letters and digits only; spaces and _ are dropped).
"""
import argparse, glob, os, re, sys

# the game's place pictures (1920x1080 or close). Phone ads, PC screens, sky layers and scene art are left out on purpose.
DEFAULT = [
    "bowling", "karaoke", "cafe_night", "Restaurant", "Restaurant2", "plaza", "city", "Theatre", "Lobby", "Reception",
    "ArtGallery", "ArtGallery3", "AquariumInside", "AquariumNightLight", "AquariumCircusShow",
    "amusement park1", "amusement park2", "amusement park3", "planetarium1", "planetarium2",
    "star gazing1", "star gazing2", "star gazing3", "SunnyBeach", "GreenField", "temple", "Ruin", "frozen",
    "Casino Background", "cazino_slot", "cazino_blackjack", "black_market", "Store", "shop_shelves",
    "richpeopleshop", "richpeopleshop inside", "Underground Street", "Tunnel Dark", "Tunnel Populated", "staircase",
    "industrial area 1", "Industrial area 2", "factory", "destroyed lab", "polluted_lake", "trashed_river",
    "microplastic coast", "riots", "riots2", "dystopia", "dystopia2", "dystopia3", "dystopia4", "premium_farm", "galaxy",
]

def key(name):
    return re.sub(r"[^a-z0-9]", "", name.lower())

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("game", nargs="?", help="the game folder (where the game .exe is). Optional when run from inside it")
    ap.add_argument("--data", help="the <GameName>_Data folder (found by itself if left out)")
    ap.add_argument("--out", help="the jun_backgrounds folder (default: UserData\\StoryCreator\\jun_backgrounds in the game folder)")
    ap.add_argument("--names", default=",".join(DEFAULT), help="comma list of picture names (default: %s)" % ",".join(DEFAULT))
    ap.add_argument("--list", action="store_true", help="print the names of every big picture and stop")
    a = ap.parse_args()
    here = os.path.dirname(os.path.abspath(__file__))
    game = a.game
    if not game:
        # <game>\UserData\StoryCreator\tools\export_backgrounds.py -> <game>; else the current folder
        up = os.path.abspath(os.path.join(here, "..", "..", ".."))
        game = up if os.path.basename(os.path.dirname(os.path.dirname(here))).lower() == "userdata" else os.getcwd()
    if not a.data:
        cands = [d for d in glob.glob(os.path.join(game, "*_Data")) if os.path.isfile(os.path.join(d, "resources.assets"))]
        if not cands:
            sys.exit("could not find the <GameName>_Data folder in " + game + "\nRun this from the game folder, or give it:  python export_backgrounds.py \"C:\\path\\to\\game\"")
        a.data = cands[0]
    if not a.out and not a.list:
        if os.path.basename(here).lower() == "tools" and os.path.isdir(os.path.dirname(here)):
            a.out = os.path.join(os.path.dirname(here), "jun_backgrounds")          # next to this tools folder: UserData\StoryCreator
        else:
            a.out = os.path.join(game, "UserData", "StoryCreator", "jun_backgrounds")
        print("saving into", a.out)
    import UnityPy
    src = os.path.join(a.data, "resources.assets")
    if not os.path.isfile(src):
        sys.exit("not found: " + src)
    if not os.path.isfile(src + ".resS"):
        sys.exit("not found: " + src + ".resS  (the pixels are stored there; it must sit next to resources.assets)")
    env = UnityPy.load(src)
    want = {key(n): n for n in a.names.split(",") if n.strip()}
    done = set()
    for o in env.objects:
        if o.type.name != "Texture2D":
            continue
        try:
            t = o.read()
        except Exception:
            continue
        if t.m_Width < 1200 or t.m_Height < 600:
            continue
        if a.list:
            print("%-32s %dx%d" % (t.m_Name, t.m_Width, t.m_Height))
            continue
        k = key(t.m_Name)
        if k not in want or k in done:
            continue
        if not a.out:
            sys.exit("give --out <jun_backgrounds folder>")
        os.makedirs(a.out, exist_ok=True)
        path = os.path.join(a.out, k + ".png")
        t.image.convert("RGB").save(path)
        done.add(k)
        print("saved", path, "%dx%d" % (t.m_Width, t.m_Height))
    if not a.list:
        for k, n in want.items():
            if k not in done:
                print("not found:", n)
        if done and a.out:
            with open(os.path.join(a.out, "BACKGROUNDS.txt"), "w", encoding="utf-8") as f:
                f.write("Pictures saved from the game. Use them in a story as  @background <name>\n\n")
                for k in sorted(done):
                    f.write("@background %s\n" % k)
            print("\n%d pictures saved. The list is in %s" % (len(done), os.path.join(a.out, "BACKGROUNDS.txt")))

if __name__ == "__main__":
    main()
