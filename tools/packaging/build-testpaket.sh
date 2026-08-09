#!/usr/bin/env bash
#
# Schnuert aus dem macOS- und dem Windows-Paket EIN Testpaket zum Verschicken,
# inklusive Anleitung und ChatGPT-Prompt fuer die Rueckmeldung.
#
#   tools/packaging/build-testpaket.sh
#   tools/packaging/build-testpaket.sh --help
#
# Ergebnis: Builds/dist/Hashkrieg-Test-<commit>.zip
#
# Vorher laufen lassen:
#   tools/packaging/build-mac.sh
#   tools/packaging/build-windows.sh
#
# WARUM DAS EIN SKRIPT IST: Beide Haelften muessen aus DEMSELBEN Commit
# stammen. Von Hand zusammengezogen faellt genau das irgendwann hinten runter
# — und dann sitzt ein Testender auf einem Stand, den es nie gab. Das Skript
# prueft den Stempel in beiden Haelften, bevor es packt, und zwar im DMG
# selbst und nicht nur im Dateinamen.
set -euo pipefail

while [ $# -gt 0 ]; do
  case "$1" in
    -h|--help) sed -n '2,17p' "${BASH_SOURCE[0]}"; exit 0 ;;
    *) echo "FEHLER: unbekannte Option: $1" >&2; exit 1 ;;
  esac
done

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SRC="$REPO/tools/packaging/testpaket"
WIN_DIR="$REPO/Builds/Windows64"
DIST="$REPO/Builds/dist"

step() { printf '\n\033[1m==> %s\033[0m\n' "$1"; }
die()  { printf '\033[31mFEHLER: %s\033[0m\n' "$1" >&2; exit 1; }

step "Preflight"
# Der Commit kommt aus dem ARTEFAKT, nicht aus HEAD. Sonst verlangt eine
# geaenderte Textdatei einen Neubau des ganzen Spiels — und was zaehlt, ist
# ohnehin nur, dass beide Haelften aus demselben Stand stammen.
[ -f "$WIN_DIR/ProjectNova.exe" ] || die "kein Windows-Build unter $WIN_DIR
       Erst 'tools/packaging/build-windows.sh' laufen lassen."
COMMIT="$(tr -d '\r\n' < "$WIN_DIR/ProjectNova_Data/NovaBuildCommit.txt" 2>/dev/null || true)"
[ -n "$COMMIT" ] || die "Windows-Build hat keinen Commit-Stempel. Neu bauen."
echo "    Windows: $COMMIT"

case "$COMMIT" in
  *-dirty) die "Der Build stammt aus einem unsauberen Arbeitsbaum ($COMMIT).
       Er laesst sich nicht rekonstruieren und wird nicht verschickt." ;;
esac

DMG="$DIST/ProjectNova-$COMMIT.dmg"
[ -f "$DMG" ] || die "kein macOS-Paket fuer $COMMIT: $DMG
       Beide Haelften muessen aus demselben Commit stammen — erst
       'tools/packaging/build-mac.sh' laufen lassen."

HEAD_SHORT="$(git -C "$REPO" rev-parse --short HEAD)"
if [ "$COMMIT" != "$HEAD_SHORT" ]; then
  printf '    \033[33mHinweis: HEAD steht auf %s, das Paket stammt aus %s.\033[0m\n' \
    "$HEAD_SHORT" "$COMMIT"
  echo "    In Ordnung, solange seither nur Doku und Packaging geaendert wurden."
  echo "    Jede Simulationsaenderung verlangt einen neuen Build."
fi

# Der Dateiname des DMG ist eine Behauptung. Der Stempel im Info.plist ist der
# Beleg — und nur er faellt auf, wenn jemand ein DMG umbenannt hat.
step "macOS-Stempel im DMG pruefen"
MOUNT="$(mktemp -d)"
hdiutil attach -nobrowse -readonly -mountpoint "$MOUNT" "$DMG" >/dev/null
DMG_STAMP="$(/usr/libexec/PlistBuddy -c 'Print :NovaBuildCommit' \
  "$MOUNT/ProjectNova.app/Contents/Info.plist" 2>/dev/null || true)"
hdiutil detach "$MOUNT" -quiet || true
rmdir "$MOUNT" 2>/dev/null || true
[ "$DMG_STAMP" = "$COMMIT" ] \
  || die "DMG traegt intern '$DMG_STAMP', erwartet '$COMMIT'. Neu bauen."
echo "    macOS:   $DMG_STAMP"

step "Testpaket schnueren"
FOLDER="Hashkrieg-Test-$COMMIT"
STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
ROOT="$STAGE/$FOLDER"
mkdir -p "$ROOT/macOS" "$ROOT/Windows"

# Die Anleitung wandert nach oben und traegt "ZUERST LESEN" im Namen: sie ist
# das Einzige, was der Empfaenger sicher zuerst sehen muss.
cp "$SRC/ANLEITUNG.txt"            "$ROOT/ANLEITUNG - ZUERST LESEN.txt"
cp "$SRC/PROMPT-FUER-CHATGPT.txt"  "$ROOT/PROMPT FUER CHATGPT.txt"
cp "$DMG"                          "$ROOT/macOS/"
cp -R "$WIN_DIR"/.                 "$ROOT/Windows/"
rm -rf "$ROOT/Windows"/*_BurstDebugInformation_DoNotShip
find "$ROOT" -name '.DS_Store' -delete 2>/dev/null || true

cat > "$ROOT/BUILD.txt" <<EOF
Hashkrieg — Testpaket

Build:   $COMMIT
Gepackt: $(date '+%Y-%m-%d %H:%M')

Diese Kennung bitte am Anfang der ersten Sprachnachricht nennen. Ohne sie
laesst sich eine Beobachtung keinem Stand zuordnen.

macOS:   macOS/$(basename "$DMG")
Windows: Windows/ProjectNova.exe
Anleitung: "ANLEITUNG - ZUERST LESEN.txt"
EOF

step "ZIP bauen"
mkdir -p "$DIST"
ARCHIVE="$DIST/$FOLDER.zip"
rm -f "$ARCHIVE"
( cd "$STAGE" && zip -r -q -X "$ARCHIVE" "$FOLDER" )

step "Gegenprobe"
for expected in \
  "$FOLDER/ANLEITUNG - ZUERST LESEN.txt" \
  "$FOLDER/PROMPT FUER CHATGPT.txt" \
  "$FOLDER/BUILD.txt" \
  "$FOLDER/macOS/$(basename "$DMG")" \
  "$FOLDER/Windows/ProjectNova.exe"
do
  unzip -l "$ARCHIVE" "$expected" >/dev/null 2>&1 \
    || die "fehlt im Archiv: $expected"
  echo "    ok: $expected"
done

ZIP_STAMP="$(unzip -p "$ARCHIVE" "$FOLDER/Windows/ProjectNova_Data/NovaBuildCommit.txt" | tr -d '\r\n')"
[ "$ZIP_STAMP" = "$COMMIT" ] || die "Stempel im Archiv stimmt nicht: '$ZIP_STAMP'"
echo "    ok: Stempel im Archiv = $ZIP_STAMP"

step "Fertig"
echo "    $ARCHIVE"
echo "    $(du -h "$ARCHIVE" | cut -f1) — beide Plattformen, ein Commit, Anleitung dabei."
