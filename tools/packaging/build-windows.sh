#!/usr/bin/env bash
#
# Baut und verpackt den Windows-x64-Player reproduzierbar fuer Testrunden.
#
#   tools/packaging/build-windows.sh
#   tools/packaging/build-windows.sh --skip-build
#   tools/packaging/build-windows.sh --help
#
# Ergebnis: Builds/dist/ProjectNova-win-x64-<commit>.zip
# Der Commit steht zusaetzlich in ProjectNova_Data/NovaBuildCommit.txt.
#
# WARUM ZIP UND NICHT TAR.GZ: Der Empfaenger sitzt an Windows und packt mit
# dem Explorer aus. tar.gz kann Windows 11 zwar, aber nicht jeder weiss das —
# und ein Testpaket, das am Auspacken scheitert, hat nichts getestet.
set -euo pipefail

SKIP_BUILD=0
while [ $# -gt 0 ]; do
  case "$1" in
    --skip-build) SKIP_BUILD=1; shift ;;
    -h|--help) sed -n '2,14p' "${BASH_SOURCE[0]}"; exit 0 ;;
    *) echo "FEHLER: unbekannte Option: $1" >&2; exit 1 ;;
  esac
done

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BUILD_DIR="$REPO/Builds/Windows64"
PLAYER="$BUILD_DIR/ProjectNova.exe"
DATA_DIR="$BUILD_DIR/ProjectNova_Data"
STAMP="$DATA_DIR/NovaBuildCommit.txt"
DIST="$REPO/Builds/dist"

step() { printf '\n\033[1m==> %s\033[0m\n' "$1"; }
die()  { printf '\033[31mFEHLER: %s\033[0m\n' "$1" >&2; exit 1; }

step "Preflight"
UNITY_VERSION="$(sed -n 's/^m_EditorVersion: //p' "$REPO/ProjectSettings/ProjectVersion.txt")"
UNITY_ROOT="/Applications/Unity/Hub/Editor/$UNITY_VERSION"
UNITY="$UNITY_ROOT/Unity.app/Contents/MacOS/Unity"
# Zwei Orte, und der Hub benutzt beide: das Windows-Modul landet je nach
# Installationsweg neben dem Editor ODER im App-Bundle. Ein Check auf nur einen
# Pfad meldet ein vorhandenes Modul als fehlend.
WIN_SUPPORT=""
for candidate in \
  "$UNITY_ROOT/PlaybackEngines/WindowsStandaloneSupport" \
  "$UNITY_ROOT/Unity.app/Contents/PlaybackEngines/WindowsStandaloneSupport"
do
  if [ -d "$candidate/Variations/win64_player_nondevelopment_mono" ]; then
    WIN_SUPPORT="$candidate"
    break
  fi
done

COMMIT="$(git -C "$REPO" rev-parse --short HEAD)"
if [ -n "$(git -C "$REPO" status --porcelain)" ]; then
  COMMIT="$COMMIT-dirty"
  echo "    WARNUNG: Arbeitsbaum nicht sauber — Paket heisst '$COMMIT'."
  echo "             Ein '-dirty'-Build laesst sich nicht rekonstruieren."
fi
echo "    Commit: $COMMIT"

if [ "$SKIP_BUILD" -eq 0 ]; then
  [ -x "$UNITY" ] || die "Unity $UNITY_VERSION nicht gefunden unter $UNITY"
  if [ -z "$WIN_SUPPORT" ]; then
    printf '\033[31mFEHLER: Unity-Windows-Build-Modul nicht gefunden.\033[0m\n' >&2
    cat >&2 <<EOF
Gesucht in:
  $UNITY_ROOT/PlaybackEngines/WindowsStandaloneSupport
  $UNITY_ROOT/Unity.app/Contents/PlaybackEngines/WindowsStandaloneSupport

Das Modul wird nicht automatisch installiert. Einmalig nachholen (Editor zu):

  "/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless \\
    install-modules --version $UNITY_VERSION --module windows-mono --childModules

Meldet der Hub "already installed", liegt das Modul im App-Bundle statt
daneben — derselbe Befehl zieht es an die erwartete Stelle nach.
EOF
    exit 1
  fi
  [ ! -f "$REPO/Library/EditorInstance.json" ] \
    || die "Der Unity-Editor hat das Projekt offen. Erst schliessen, dann bauen."
  echo "    Unity:  $UNITY_VERSION"
  echo "    Modul:  $WIN_SUPPORT"
fi

if [ "$SKIP_BUILD" -eq 0 ]; then
  step "Windows-x64-Player bauen (Mono, unsigniert)"
  LOG="$REPO/Builds/build-windows.log"
  mkdir -p "$REPO/Builds"
  if ! "$UNITY" -batchmode -nographics -projectPath "$REPO" \
        -executeMethod Nova.Editor.BuildScript.BuildWindows64 \
        -quit -logFile "$LOG"; then
    echo "--- letzte 40 Zeilen aus $LOG ---" >&2
    tail -40 "$LOG" >&2
    die "Unity-Windows-Build fehlgeschlagen."
  fi
  echo "    Log: $LOG"
fi

[ -f "$PLAYER" ] || die "kein Windows-Player unter $PLAYER"
[ -d "$DATA_DIR" ] || die "Player-Daten fehlen unter $DATA_DIR"

step "Commit stempeln"
printf '%s\n' "$COMMIT" > "$STAMP"
[ "$(tr -d '\r\n' < "$STAMP")" = "$COMMIT" ] \
  || die "Commit-Stempel konnte nicht verifiziert werden."
echo "    $STAMP = $COMMIT"

step "ZIP bauen"
mkdir -p "$DIST"
ARCHIVE="$DIST/ProjectNova-win-x64-$COMMIT.zip"
FOLDER="ProjectNova-win-x64-$COMMIT"
STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT

# Ueber eine Stage statt direkt aus Builds/: der Empfaenger soll einen Ordner
# auspacken und nicht fuenfzehn lose Dateien in seinem Download-Verzeichnis.
mkdir -p "$STAGE/$FOLDER"
cp -R "$BUILD_DIR"/. "$STAGE/$FOLDER/"
rm -rf "$STAGE/$FOLDER"/*_BurstDebugInformation_DoNotShip
find "$STAGE/$FOLDER" -name '.DS_Store' -delete 2>/dev/null || true

cat > "$STAGE/$FOLDER/LIESMICH.txt" <<EOF
Hashkrieg (Project Nova) — Testbuild fuer Windows
Commit:  $COMMIT
Gebaut:  $(date '+%Y-%m-%d %H:%M')

STARTEN
  Diesen Ordner komplett auspacken (nicht aus dem ZIP heraus starten),
  dann ProjectNova.exe doppelklicken.

  Windows zeigt beim ersten Start "Der Computer wurde durch Windows
  geschuetzt". Das ist erwartet: der Build ist nicht signiert.
  -> "Weitere Informationen" -> "Trotzdem ausfuehren".

WELCHEN BUILD HAST DU?
  type ProjectNova_Data\\NovaBuildCommit.txt
  Antwort ist $COMMIT.

  Beide Spieler brauchen fuer eine Netzpartie GENAU diesen Commit. Der Server
  lehnt ein Match zwischen ungleichen Builds ab — absichtlich, damit ihr nicht
  nach vierzig Minuten in einem unerklaerlichen Desync sitzt.

WENN ETWAS ABSTUERZT
  Das Logfile liegt unter
  %USERPROFILE%\\AppData\\LocalLow\\Project Nova\\Project Nova\\Player.log
  Bitte mitschicken.

Der Build ist zum Testen ueberlassen, nicht zur Weitergabe (PolyForm
Noncommercial 1.0.0).
EOF

rm -f "$ARCHIVE"
( cd "$STAGE" && zip -r -q -X "$ARCHIVE" "$FOLDER" )

ARCHIVED_STAMP="$(unzip -p "$ARCHIVE" "$FOLDER/ProjectNova_Data/NovaBuildCommit.txt" | tr -d '\r\n')"
[ "$ARCHIVED_STAMP" = "$COMMIT" ] \
  || die "Commit-Stempel im Archiv stimmt nicht: '$ARCHIVED_STAMP'"

step "Fertig"
echo "    $ARCHIVE"
echo "    $(du -h "$ARCHIVE" | cut -f1) — Commit-Stempel im Archiv: $ARCHIVED_STAMP"
echo
echo "    Hinweis: unsigniert. SmartScreen warnt beim Empfaenger einmalig;"
echo "    der Weg darum herum steht in der LIESMICH.txt im Paket."
