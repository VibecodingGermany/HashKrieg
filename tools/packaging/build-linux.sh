#!/usr/bin/env bash
#
# Baut und verpackt den Linux-x64-Player reproduzierbar fuer Netztests.
#
#   tools/packaging/build-linux.sh
#   tools/packaging/build-linux.sh --skip-build
#   tools/packaging/build-linux.sh --help
#
# Ergebnis: Builds/dist/ProjectNova-linux-x64-<commit>.tar.gz
# Der Commit steht zusaetzlich in ProjectNova_Data/NovaBuildCommit.txt.
set -euo pipefail

SKIP_BUILD=0
while [ $# -gt 0 ]; do
  case "$1" in
    --skip-build) SKIP_BUILD=1; shift ;;
    -h|--help) sed -n '2,10p' "${BASH_SOURCE[0]}"; exit 0 ;;
    *) echo "FEHLER: unbekannte Option: $1" >&2; exit 1 ;;
  esac
done

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BUILD_DIR="$REPO/Builds/Linux64"
PLAYER="$BUILD_DIR/ProjectNova.x86_64"
DATA_DIR="$BUILD_DIR/ProjectNova_Data"
STAMP="$DATA_DIR/NovaBuildCommit.txt"
DIST="$REPO/Builds/dist"

step() { printf '\n\033[1m==> %s\033[0m\n' "$1"; }
die()  { printf '\033[31mFEHLER: %s\033[0m\n' "$1" >&2; exit 1; }

step "Preflight"
UNITY_VERSION="$(sed -n 's/^m_EditorVersion: //p' "$REPO/ProjectSettings/ProjectVersion.txt")"
UNITY_ROOT="/Applications/Unity/Hub/Editor/$UNITY_VERSION"
UNITY="$UNITY_ROOT/Unity.app/Contents/MacOS/Unity"
LINUX_SUPPORT="$UNITY_ROOT/PlaybackEngines/LinuxStandaloneSupport"

COMMIT="$(git -C "$REPO" rev-parse --short HEAD)"
if [ -n "$(git -C "$REPO" status --porcelain)" ]; then
  COMMIT="$COMMIT-dirty"
  echo "    WARNUNG: Arbeitsbaum nicht sauber — Paket heisst '$COMMIT'."
  echo "             Ein '-dirty'-Build laesst sich nicht rekonstruieren."
fi
echo "    Commit: $COMMIT"

if [ "$SKIP_BUILD" -eq 0 ]; then
  [ -x "$UNITY" ] || die "Unity $UNITY_VERSION nicht gefunden unter $UNITY"
  [ -d "$LINUX_SUPPORT" ] \
    || die "Unity-Linux-Build-Modul fehlt unter $LINUX_SUPPORT (nicht automatisch installiert)."
  [ ! -f "$REPO/Library/EditorInstance.json" ] \
    || die "Der Unity-Editor hat das Projekt offen. Erst schliessen, dann bauen."
  echo "    Unity:  $UNITY_VERSION"
  echo "    Modul:  $LINUX_SUPPORT"
fi

if [ "$SKIP_BUILD" -eq 0 ]; then
  step "Linux-x64-Player bauen"
  LOG="$REPO/Builds/build-linux.log"
  mkdir -p "$REPO/Builds"
  if ! "$UNITY" -batchmode -nographics -projectPath "$REPO" \
        -executeMethod Nova.Editor.BuildScript.BuildLinux64 \
        -quit -logFile "$LOG"; then
    echo "--- letzte 40 Zeilen aus $LOG ---" >&2
    tail -40 "$LOG" >&2
    die "Unity-Linux-Build fehlgeschlagen."
  fi
  echo "    Log: $LOG"
fi

[ -x "$PLAYER" ] || die "kein ausfuehrbarer Linux-Player unter $PLAYER"
[ -d "$DATA_DIR" ] || die "Player-Daten fehlen unter $DATA_DIR"

step "Commit stempeln"
printf '%s\n' "$COMMIT" > "$STAMP"
[ "$(tr -d '\r\n' < "$STAMP")" = "$COMMIT" ] \
  || die "Commit-Stempel konnte nicht verifiziert werden."
echo "    $STAMP = $COMMIT"

step "tar.gz bauen"
mkdir -p "$DIST"
ARCHIVE="$DIST/ProjectNova-linux-x64-$COMMIT.tar.gz"
rm -f "$ARCHIVE"
tar -C "$BUILD_DIR" \
  --exclude='*_BurstDebugInformation_DoNotShip' \
  --exclude='.DS_Store' \
  -czf "$ARCHIVE" .

ARCHIVED_STAMP="$(tar -xOf "$ARCHIVE" ./ProjectNova_Data/NovaBuildCommit.txt | tr -d '\r\n')"
[ "$ARCHIVED_STAMP" = "$COMMIT" ] \
  || die "Commit-Stempel im Archiv stimmt nicht: '$ARCHIVED_STAMP'"

step "Fertig"
echo "    $ARCHIVE"
echo "    Commit-Stempel im Archiv: $ARCHIVED_STAMP"
