#!/usr/bin/env bash
#
# Baut und verpackt den Linux-x64-Player reproduzierbar fuer Netztests.
#
#   tools/packaging/build-linux.sh
#   tools/packaging/build-linux.sh --skip-build
#   tools/packaging/build-linux.sh --allow-dirty   # trotz unsauberem Baum (Stempel '-dirty')
#   tools/packaging/build-linux.sh --check-only    # nur Quellstand pruefen, kein Unity
#   tools/packaging/build-linux.sh --help
#
# Ergebnis: Builds/dist/ProjectNova-linux-x64-<commit>.tar.gz
# Der Commit steht zusaetzlich in ProjectNova_Data/NovaBuildCommit.txt.
# Bei unsauberem Arbeitsbaum brechen Bauen und --skip-build ab (#132).
set -euo pipefail

SKIP_BUILD=0 ALLOW_DIRTY=0 CHECK_ONLY=0
while [ $# -gt 0 ]; do
  case "$1" in
    --skip-build) SKIP_BUILD=1; shift ;;
    --allow-dirty) ALLOW_DIRTY=1; shift ;;
    --check-only) CHECK_ONLY=1; shift ;;
    -h|--help) sed -n '2,12p' "${BASH_SOURCE[0]}"; exit 0 ;;
    *) echo "FEHLER: unbekannte Option: $1" >&2; exit 1 ;;
  esac
done

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BUILD_DIR="$REPO/Builds/Linux64"
PLAYER="$BUILD_DIR/ProjectNova.x86_64"
DATA_DIR="$BUILD_DIR/ProjectNova_Data"
STAMP="$DATA_DIR/NovaBuildCommit.txt"
SIDECAR="$BUILD_DIR/.build-commit"
DIST="$REPO/Builds/dist"

step() { printf '\n\033[1m==> %s\033[0m\n' "$1"; }
die()  { printf '\033[31mFEHLER: %s\033[0m\n' "$1" >&2; exit 1; }

# --- Quellstand pruefen (#132, D-124) -----------------------------------------
# Ein Paket, das die Testpartie spielt, muss aus einem Commit fallen, den ein
# frischer Klon reproduzieren kann. Deshalb steht diese Pruefung VOR jedem
# Unity-Aufruf: ein unsauberer Baum (auch untrackte Dateien — Unity kompiliert
# alles unter Assets/ mit) bricht ab. Gleiche Definition wie BuildCommitStamp.cs:
# `git status --porcelain` ueber den ganzen Baum.
step "Quellstand"

HEAD_FULL="$(git -C "$REPO" rev-parse HEAD)"
COMMIT="$(git -C "$REPO" rev-parse --short HEAD)"
TREE_STATE=clean
DIRTY_LIST="$(git -C "$REPO" status --porcelain)"
if [ -n "$DIRTY_LIST" ]; then
  TREE_STATE=dirty
  if [ "$ALLOW_DIRTY" -eq 1 ]; then
    COMMIT="$COMMIT-dirty"
    echo "    WARNUNG: Arbeitsbaum nicht sauber — Paket heisst '$COMMIT'."
    echo "             Ein '-dirty'-Build laesst sich nicht rekonstruieren."
  else
    {
      echo "Geaenderte oder untrackte Dateien (git status --porcelain, erste 20):"
      printf '%s\n' "$DIRTY_LIST" | head -20 | sed 's/^/    /'
    } >&2
    die "Arbeitsbaum nicht sauber. Beta-Pakete entstehen aus einem sauberen Commit, am besten aus einem frischen Worktree von origin/main (siehe tools/packaging/README.md). Ausweg fuer lokale Pakete: --allow-dirty (Stempel behaelt '-dirty')."
  fi
fi
echo "    Commit: $COMMIT"

# --skip-build verpackt einen vorhandenen Build neu. Ohne Gegenprobe bekaeme ein
# alter Build das Etikett des aktuellen Commits. Der Sidecar entsteht nach einem
# erfolgreichen Unity-Build und haelt fest, aus welchem HEAD er stammt.
if [ "$SKIP_BUILD" -eq 1 ]; then
  [ -f "$SIDECAR" ] \
    || die "Kein $SIDECAR — woher der vorhandene Build stammt, ist unbekannt. Erst ohne --skip-build bauen."
  SC_HEAD="$(sed -n 's/^head=//p' "$SIDECAR")"
  SC_STATE="$(sed -n 's/^state=//p' "$SIDECAR")"
  [ "$SC_HEAD" = "$HEAD_FULL" ] \
    || die "Der vorhandene Build stammt aus ${SC_HEAD:-unbekannt}, HEAD ist $HEAD_FULL. Neu bauen statt --skip-build."
  [ "$SC_STATE" = "$TREE_STATE" ] \
    || die "Der vorhandene Build entstand aus einem Baum mit Zustand '${SC_STATE:-unbekannt}', jetzt ist er '$TREE_STATE'. Neu bauen statt --skip-build."
  echo "    Build-Sidecar: passt zu HEAD ($SC_STATE)"
fi

# Nur eine Warnung: ein Beta-Paket soll normalerweise main sein, ob ein Branch-
# Paket (etwa fuer einen Test) trotzdem rausgehen darf, entscheidet der Inhaber.
# Offline darf das nie abbrechen — daher `if !` unter `set -e`.
if ! GIT_TERMINAL_PROMPT=0 git -C "$REPO" fetch --quiet origin main >/dev/null 2>&1; then
  echo "    HINWEIS: git fetch origin main fehlgeschlagen (offline oder kein 'origin') — Vergleich nur mit dem Stand des letzten Fetch."
fi
ORIGIN_MAIN="$(git -C "$REPO" rev-parse --verify --quiet origin/main || true)"
if [ -z "$ORIGIN_MAIN" ]; then
  echo "    WARNUNG: origin/main unbekannt — HEAD kann nicht gegen main verglichen werden."
elif [ "$ORIGIN_MAIN" != "$HEAD_FULL" ]; then
  echo "    WARNUNG: HEAD ist nicht origin/main (${ORIGIN_MAIN:0:7}). Ein Beta-Paket soll aus main fallen."
else
  echo "    HEAD = origin/main"
fi

if [ "$CHECK_ONLY" -eq 1 ]; then
  echo "    --check-only: Quellstand geprueft, Abbruch vor Unity."
  exit 0
fi

step "Preflight"
UNITY_VERSION="$(sed -n 's/^m_EditorVersion: //p' "$REPO/ProjectSettings/ProjectVersion.txt")"
UNITY_ROOT="/Applications/Unity/Hub/Editor/$UNITY_VERSION"
UNITY="$UNITY_ROOT/Unity.app/Contents/MacOS/Unity"
LINUX_SUPPORT="$UNITY_ROOT/PlaybackEngines/LinuxStandaloneSupport"

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

  # Sidecar fuer --skip-build: aus welchem HEAD und Baumzustand dieser Build kam.
  printf 'head=%s\nstate=%s\n' "$HEAD_FULL" "$TREE_STATE" > "$SIDECAR"
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
  --exclude='.build-commit' \
  -czf "$ARCHIVE" .

ARCHIVED_STAMP="$(tar -xOf "$ARCHIVE" ./ProjectNova_Data/NovaBuildCommit.txt | tr -d '\r\n')"
[ "$ARCHIVED_STAMP" = "$COMMIT" ] \
  || die "Commit-Stempel im Archiv stimmt nicht: '$ARCHIVED_STAMP'"

step "Fertig"
echo "    $ARCHIVE"
echo "    Commit-Stempel im Archiv: $ARCHIVED_STAMP"
