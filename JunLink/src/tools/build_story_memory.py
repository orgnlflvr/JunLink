"""
Builds Jun's story memory from YOUR OWN save and YOUR OWN game files.
Runs only on your PC. Nothing is uploaded anywhere.

What it does:
  1. Reads the list of text lines your save says you have already SEEN.
  2. Looks those lines up in the game's English text tables.
  3. Writes BepInEx\\jun_story_import.txt, which the MDRG Panel plugin
     loads so Jun can recall scenes you really experienced.

Setup (once):   pip install UnityPy
Run:
  python build_story_memory.py --save "C:\\path\\to\\your_save.mdrg" ^
      --bundles "I:\\...\\factorial-omega-win-64\\<GameName>_Data\\StreamingAssets\\aa\\StandaloneWindows64" ^
      --out "I:\\...\\factorial-omega-win-64\\BepInEx\\jun_story_import.txt" ^
      --bot Jun --player Anon
Add --flags to also include the story-event timeline (from the same save).
"""
import argparse, base64, glob, json, os, re, struct, sys, zlib

def seen_ids(save):
    raw = zlib.decompress(base64.b64decode(save["storyTextIds_Comp"]), -15)
    return set(struct.unpack("<%dq" % (len(raw) // 8), raw))

def load_tables(folder):
    import UnityPy
    text, keys = {}, {}
    for f in sorted(glob.glob(os.path.join(folder, "*.bundle"))):
        size = os.path.getsize(f)
        if size > 3_000_000:      # skip audio bundles
            continue
        try:
            env = UnityPy.load(f)
        except Exception:
            continue
        for o in env.objects:
            if o.type.name != "MonoBehaviour":
                continue
            try:
                t = o.read_typetree()
            except Exception:
                continue
            if "m_TableData" in t and t.get("m_LocaleId", {}).get("m_Code") == "en":
                table = t["m_Name"].rsplit("_", 1)[0]
                for e in t["m_TableData"]:
                    text[e["m_Id"]] = (table, e["m_Localized"])
            elif "m_Entries" in t and "m_TableCollectionName" in t:
                for e in t["m_Entries"]:
                    keys[e["m_Id"]] = e["m_Key"]
    return text, keys

def clean(s, bot, player):
    s = s.replace("{f_botName}", bot).replace("{f_playerName}", player)
    s = re.sub(r"\{\{[^}]*\}\}", "", s)
    s = re.sub(r"\{[a-z]+_[A-Za-z]+\}", "", s)
    s = re.sub(r"<[^>]+>", "", s)
    s = s.replace("\r", " ").replace("\n", " / ")
    return re.sub(r"\s+", " ", s).strip()

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--save", required=True)
    ap.add_argument("--bundles", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--bot", default="Jun")
    ap.add_argument("--player", default="Anon")
    ap.add_argument("--flags", action="store_true")
    a = ap.parse_args()

    save = json.load(open(a.save, encoding="utf-8"))
    ids = seen_ids(save)
    print("lines your save says you have seen:", len(ids))
    text, keys = load_tables(a.bundles)
    print("text lines found in the game files:", len(text))

    print("of those, found with text:", sum(1 for i in ids if i in text))
    blocks = {}
    for i in ids:
        if i not in text:
            continue
        table, line = text[i]
        key = keys.get(i, "")
        m = re.match(r"(.*)\+(\d+)", key)
        block, idx = (m.group(1), int(m.group(2))) if m else (key or table, 0)
        blocks.setdefault((table, block), []).append((idx, clean(line, a.bot, a.player)))
    out = []
    for (table, block), lines in sorted(blocks.items()):
        lines.sort()
        name = re.sub(r"_\d+_[A-Za-z]*$", "", block.split("/")[-1])
        chunk, size = [], 0
        for _, t in lines:
            if not t:
                continue
            chunk.append(t); size += len(t)
            if size > 600:
                out.append("[seen scene %s] %s" % (name, " / ".join(chunk))); chunk, size = [], 0
        if chunk:
            out.append("[seen scene %s] %s" % (name, " / ".join(chunk)))
    if a.flags:
        best = None
        for s in save.get("saves", []) + save.get("autoSaves", []):
            try:
                g = json.loads(s["savedata"])
            except Exception:
                continue
            if best is None or g.get("time", 0) > best.get("time", 0):
                best = g
        if best:
            for f in sorted(best["flags"], key=lambda f: f["firstTimeAdded"]):
                if re.match(r"^DAY \d+", f["name"]):
                    continue
                out.append("Day %d [save history] Story event happened: %s" % (f["firstTimeAdded"] // 1440, f["name"]))
    with open(a.out, "w", encoding="utf-8") as fh:
        fh.write("\n".join(out) + "\n")
    print("wrote", len(out), "memory lines to", a.out)

if __name__ == "__main__":
    main()
