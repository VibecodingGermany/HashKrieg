#!/usr/bin/env python3
"""Generator v2 - Stilplatte statt Objektreferenz.

Der Unterschied zu v1: Als Referenzbild dient eine Materialtafel ohne
erkennbare Silhouette. Damit uebernimmt das Modell Palette, Oberflaeche und
Leuchtlinien-Anmutung, hat aber keine Form zum Abmalen. Die Rahmung kommt
vollstaendig aus dem Text.

Aufruf:
  python3 generate_v2.py <asset-id> [<asset-id> ...]
  python3 generate_v2.py --alle-fehlenden
Der Key steht nur in einer curl-Konfigurationsdatei mit Rechten 600,
niemals in der Prozessliste.
"""

import base64
import json
import os
import pathlib
import re
import subprocess
import sys
import tempfile
from concurrent.futures import ThreadPoolExecutor

ROOT = pathlib.Path(__file__).parent
IMG = ROOT / "img"
ENV = pathlib.Path("/Volumes/2TB_CodingProjekte/Coding_Projekte/B-RollMaster6000Puls/.env")
ENDPOINT = "https://api.openai.com/v1/images/edits"
WORKERS = 3
MAX_TRIES = 3

PLATE = {
    "alliance": ROOT / "style" / "styleplate_alliance.png",
    "legion": ROOT / "style" / "styleplate_legion.png",
}
GLOW = {"alliance": "cyan", "legion": "orange"}

# 1. Die Referenz ist Material, KEINE Form. Das muss unmissverstaendlich sein.
PLATE_RULE = (
    "The reference image is a material and lighting swatch sheet only. It contains no "
    "object and no silhouette. Use it exclusively for surface treatment, colour palette, "
    "material feel, and the way the {glow} emissive light lines are painted along panel "
    "seams and vents. Do not copy any shape, outline, layout or composition from it. "
    "Invent the subject's form entirely from the description below."
)

# 2. Rahmung kommt komplett aus dem Text, weil die Platte keine Kamera vorgibt.
FRAMING_RULE = (
    "Render the subject exactly as a straight-on front elevation, the way an object is "
    "drawn face-on in an elevation drawing: all vertical edges perfectly vertical and "
    "parallel, all horizontal edges perfectly horizontal and parallel, no vanishing point, "
    "no perspective convergence, no three-quarter rotation, no visible side faces, no "
    "top-down view of any upper surface. The camera is exactly level with the middle of "
    "the subject. The subject is centred and fills about 72 percent of the frame height "
    "with clear empty margin on all four sides, nothing touching the frame edge. It floats "
    "isolated on a flat very dark blue-black background #0B1017, no ground, no floor, no "
    "terrain, no base, no plinth, no cast shadow, nothing it stands on. "
    "Lighting: cool key light from upper front left, warm rim light from behind right "
    "separating the silhouette, soft fill. "
    "Painterly stylized military science fiction concept art, Tempest Rising and Command "
    "and Conquer 3 aesthetic, high contrast, strong readable silhouette, not "
    "photorealistic, not cartoon, not anime, no blueprint, no text, no logos, no watermark, "
    "no user interface."
)

ROOF_RULE = (
    "Keep upper surfaces shallow, stepped or rounded so no roof plane becomes visible from "
    "straight ahead."
)
LEGION_RULE = (
    "No spikes, no horns, no blades, no skulls, no faces, no demonic or orc or warhammer "
    "motifs of any kind. This is salvaged consumer computing hardware bolted onto military "
    "structures: circuit boards, fan banks, heat sinks, cable looms, riveted patch plates."
)
INFANTRY_RULE = (
    "CRITICAL: the subject is one single human soldier, a person standing upright on two "
    "legs, facing the camera, wearing full combat armour with the face hidden behind a "
    "visor. Human body proportions, roughly two metres tall, head, torso, two arms, two "
    "legs. It is absolutely not a vehicle, not a tank, not a mech, not a walker, and has "
    "no tracks and no wheels."
)


def load_key() -> str:
    for line in ENV.read_text(encoding="utf-8").splitlines():
        if line.startswith("OPENAI_API_KEY="):
            return line.split("=", 1)[1].strip().strip('"').strip("'")
    sys.exit("Kein OPENAI_API_KEY gefunden")


def build_prompt(asset: dict) -> str:
    faction, domain, role = asset["faction"], asset["domain"], asset["role"]
    body = re.split(r"\s*Framing:", asset["prompt"])[0].strip()
    parts = [PLATE_RULE.format(glow=GLOW[faction])]
    if "Infantry" in role:
        parts.append(INFANTRY_RULE)
    parts.append(body)
    if domain == "building":
        parts.append(ROOF_RULE)
    if faction == "legion":
        parts.append(LEGION_RULE)
    parts.append(FRAMING_RULE)
    return " ".join(parts)


def generate(asset: dict, config: str, outdir: pathlib.Path) -> tuple[str, str]:
    out = outdir / asset["filename"]
    ref = PLATE[asset["faction"]]
    prompt = build_prompt(asset)
    for attempt in range(1, MAX_TRIES + 1):
        proc = subprocess.run(
            ["curl", "-sS", "--config", config, "-X", "POST", ENDPOINT,
             "-F", "model=gpt-image-1", "-F", f"image[]=@{ref}",
             "-F", f"prompt={prompt}", "-F", "size=1024x1024",
             "-F", "quality=high", "-F", "n=1"],
            capture_output=True, text=True, timeout=600,
        )
        try:
            payload = json.loads(proc.stdout)
        except json.JSONDecodeError:
            if attempt == MAX_TRIES:
                return asset["id"], "FEHLER: ungueltige Antwort"
            continue
        if "error" in payload:
            if attempt == MAX_TRIES:
                return asset["id"], f"FEHLER: {payload['error'].get('message','')[:110]}"
            continue
        out.write_bytes(base64.b64decode(payload["data"][0]["b64_json"]))
        return asset["id"], f"OK {out.name} ({out.stat().st_size//1024} KB)"
    return asset["id"], "FEHLER: Versuche erschoepft"


def main() -> None:
    catalog = json.loads((ROOT / "prompts" / "prompts.json").read_text(encoding="utf-8"))
    by_id = {a["id"]: a for a in catalog["assets"]}
    args = sys.argv[1:]
    if not args:
        sys.exit("Bitte Asset-IDs angeben oder --alle-fehlenden")
    outdir = IMG
    if args[0] == "--test":
        outdir = IMG / "v2test"
        outdir.mkdir(exist_ok=True)
        args = args[1:]
    todo = [by_id[i] for i in args if i in by_id]
    unknown = [i for i in args if i not in by_id]
    if unknown:
        print("Unbekannte IDs:", unknown, flush=True)
    print(f"Erzeuge {len(todo)} Bild(er) nach {outdir}", flush=True)

    fd, config = tempfile.mkstemp(suffix=".curlrc")
    os.close(fd)
    os.chmod(config, 0o600)
    try:
        with open(config, "w", encoding="utf-8") as fh:
            fh.write(f'header = "Authorization: Bearer {load_key()}"\n')
        with ThreadPoolExecutor(max_workers=WORKERS) as pool:
            for aid, status in pool.map(lambda a: generate(a, config, outdir), todo):
                print(f"  {aid}: {status}", flush=True)
    finally:
        os.remove(config)


if __name__ == "__main__":
    main()
