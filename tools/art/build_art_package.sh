#!/usr/bin/env bash
#
# Baut das Art-Asset-Paket, das laut docs/assets/AssetPackage.md ausserhalb des
# Repositories verteilt wird.
#
# Der Paketinhalt wird nicht von Hand gepflegt, sondern aus .gitignore
# abgeleitet: alles, was git im Art-Baum ausschliesst, gehoert ins Paket, und
# nur das. Damit koennen Repo-Ausschluss und Paketinhalt nicht auseinanderlaufen
# — wer eine .gitignore-Regel aendert, aendert automatisch das Paket mit.
#
# Ausgabe: output/art-package/Hashkrieg_Art_MS1_<datum>_<uhrzeit>.zip
#          plus die README.txt daneben. Eigener Unterordner, weil beide Dateien
#          zusammen in den geteilten Ordner gehoeren (AssetPackage.md §3) — so
#          laesst sich der Ordner am Stueck hochladen, statt sie zwischen den
#          uebrigen Tool-Ausgaben in output/ herauszusuchen.
#
# Der Zeitstempel traegt Datum UND Uhrzeit, weil an manchen Tagen mehrfach
# nachgeliefert wird: zwei Pakete vom selben Tag waeren sonst nicht
# unterscheidbar, und im geteilten Ordner wuerde eines das andere still
# ueberschreiben. Der Name sagt damit auf den Blick, welcher Stand das ist.
#
# Aufruf:  tools/art/build_art_package.sh [YYYY-MM-DD_HHMM]
#          ohne Argument werden Datum und Uhrzeit des Laufs verwendet.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

STAMP="${1:-$(date +%Y-%m-%d_%H%M)}"
OUT_DIR="$REPO_ROOT/output/art-package"
ZIP_NAME="Hashkrieg_Art_MS1_${STAMP}.zip"
ZIP_PATH="$OUT_DIR/$ZIP_NAME"
LIST_FILE="$(mktemp)"
trap 'rm -f "$LIST_FILE"' EXIT

mkdir -p "$OUT_DIR"

# ── 1. Inhalt bestimmen ─────────────────────────────────────────────────────
# --others --ignored --exclude-standard = die von .gitignore erfassten Dateien.
# Sortiert, damit die Zip-Reihenfolge zwischen zwei Laeufen stabil bleibt.
git ls-files --others --ignored --exclude-standard -- Assets/_Project/Art \
  | LC_ALL=C sort > "$LIST_FILE"

FILE_COUNT=$(wc -l < "$LIST_FILE" | tr -d ' ')
if [ "$FILE_COUNT" -eq 0 ]; then
  echo "FEHLER: keine Art-Dateien gefunden. Liegt der Art-Ordner am Platz?" >&2
  exit 1
fi

# ── 2. Plausibilitaet: jedes Asset braucht sein .meta ───────────────────────
# Ohne .meta vergibt Unity beim Import neue GUIDs und alle Material-, Prefab-
# und Registry-Referenzen brechen bei jedem Entwickler anders (AssetPackage.md §3).
MISSING_META=0
while IFS= read -r f; do
  case "$f" in
    *.meta) continue ;;
  esac
  if ! grep -qxF "${f}.meta" "$LIST_FILE"; then
    echo "WARNUNG: .meta fehlt fuer $f" >&2
    MISSING_META=$((MISSING_META + 1))
  fi
done < "$LIST_FILE"
if [ "$MISSING_META" -gt 0 ]; then
  echo "FEHLER: $MISSING_META Asset(s) ohne .meta — Paket waere GUID-instabil." >&2
  exit 1
fi

# ── 3. Packen ───────────────────────────────────────────────────────────────
# -X laesst macOS-Extraattribute und Resource-Forks weg (sonst __MACOSX-Muell
# beim Entpacken unter Windows/Linux). -@ liest die Dateiliste von stdin.
rm -f "$ZIP_PATH"
zip -q -X -9 "$ZIP_PATH" -@ < "$LIST_FILE"

SHA256=$(shasum -a 256 "$ZIP_PATH" | cut -d' ' -f1)
SIZE_MB=$(awk -v b="$(stat -f%z "$ZIP_PATH")" 'BEGIN{printf "%.0f", b/1048576}')

# ── 4. Zugehoeriger Repo-Stand ──────────────────────────────────────────────
# Ein Paket ist ohne den Code-Stand, zu dem es passt, nur halb bestimmt: die
# .meta-GUIDs muessen zu den Prefab- und Registry-Referenzen im Repo passen.
# Der Commit ist deshalb Teil der Paketkennung, nicht Beiwerk.
HEAD_SHORT=$(git rev-parse --short HEAD)
HEAD_BRANCH=$(git rev-parse --abbrev-ref HEAD)
if [ -n "$(git status --porcelain)" ]; then
  TREE_STATE="mit uncommitteten Aenderungen"
else
  TREE_STATE="sauber"
fi

# ── 5. Kennzahlen fuer AssetPackage.md §3 ───────────────────────────────────
count_ext() { grep -c "\.$1\$" "$LIST_FILE" || true; }

cat <<EOF

Paket gebaut: $ZIP_PATH

  Dateien    $FILE_COUNT ($(count_ext fbx)x .fbx, $(count_ext png)x .png, $(count_ext mat)x .mat, $(count_ext prefab)x .prefab, $(count_ext meta)x .meta)
  Groesse    rund $SIZE_MB MB
  SHA-256    $SHA256
  Repo-Stand $HEAD_SHORT ($HEAD_BRANCH, Arbeitsbaum $TREE_STATE)

Naechste Schritte:
  1. Werte in docs/assets/AssetPackage.md §3 fortschreiben.
  2. Zip UND README.txt in den geteilten Ordner laden (beide, immer zusammen).
EOF
