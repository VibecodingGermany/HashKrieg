#!/usr/bin/env bash
#
# Baut den macOS-Player, signiert ihn mit Developer ID, notarisiert ihn und
# packt ein DMG zum Verschicken.
#
#   tools/packaging/build-mac.sh              # voll: bauen, signieren, notarisieren, DMG
#   tools/packaging/build-mac.sh --fast       # nur bauen, ad-hoc signiert, kein DMG
#   tools/packaging/build-mac.sh --skip-build # vorhandenen Build nur neu verpacken
#   tools/packaging/build-mac.sh --open       # nach dem Bauen starten
#   tools/packaging/build-mac.sh --allow-dirty # trotz unsauberem Baum (Stempel '-dirty')
#   tools/packaging/build-mac.sh --check-only # nur Quellstand pruefen, kein Unity
#
# Voll- und --skip-build-Laeufe brechen bei unsauberem Arbeitsbaum ab (#132).
# --fast ist fuer die eigene Iteration und darf unsauber sein.
#
# WARUM DAS EIN SKRIPT IST UND KEINE ANLEITUNG:
# Der Relay sperrt Matches zwischen ungleichen Builds (Sprint 12, A4 —
# Fingerprint-Sperre). Jede Aenderung an der Simulation heisst also: beide
# Seiten brauchen ein neues Paket. Das passiert oft genug, dass es reproduzierbar
# sein muss — und der Commit-Hash im Dateinamen und im Info.plist ist die
# Antwort auf "welchen Build hast du eigentlich?", bevor ihr vierzig Minuten in
# einen Desync lauft.
#
# Signiert wird hier selbst, nicht ueber `notarize.sh --sign`: dessen
# Suchausdruck fasst bei Unity-Apps auch `PlugIns/*.bundle/Contents` an, ein
# nacktes Verzeichnis, an dem `codesign` mit "bundle format unrecognized"
# abbricht. Reihenfolge unten ist deshalb von Hand gesetzt: erst die losen
# Dylibs, dann die Plugin-Bundles als Einheit, das Hauptbundle zuletzt.
set -euo pipefail

FAST=0 SKIP_BUILD=0 OPEN_AFTER=0 ALLOW_DIRTY=0 CHECK_ONLY=0
while [ $# -gt 0 ]; do
  case "$1" in
    --fast)       FAST=1; shift ;;
    --skip-build) SKIP_BUILD=1; shift ;;
    --open)       OPEN_AFTER=1; shift ;;
    --allow-dirty) ALLOW_DIRTY=1; shift ;;
    --check-only) CHECK_ONLY=1; shift ;;
    -h|--help)    sed -n '2,24p' "${BASH_SOURCE[0]}"; exit 0 ;;
    *)            echo "FEHLER: unbekannte Option: $1" >&2; exit 1 ;;
  esac
done

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ENTITLEMENTS="$REPO/tools/packaging/Nova.entitlements"
BUILD_DIR="$REPO/Builds/MacOSArm64"
APP="$BUILD_DIR/ProjectNova.app"
SIDECAR="$BUILD_DIR/.build-commit"
DIST="$REPO/Builds/dist"
NOTARIZE="$HOME/.claude/skills/apple-upload/scripts/notarize.sh"

step() { printf '\n\033[1m==> %s\033[0m\n' "$1"; }
die()  { printf '\033[31mFEHLER: %s\033[0m\n' "$1" >&2; exit 1; }

# --- Preflight --------------------------------------------------------------
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
  if [ "$ALLOW_DIRTY" -eq 1 ] || [ "$FAST" -eq 1 ]; then
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
UNITY="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"
[ -x "$UNITY" ] || die "Unity $UNITY_VERSION nicht gefunden unter $UNITY"
echo "    Unity:  $UNITY_VERSION"

# Ein laufender Editor haelt eine Sperre auf Library/ — der Batchmode-Lauf
# scheitert dann mit einer irrefuehrenden Meldung ueber ein gesperrtes Projekt.
if [ "$SKIP_BUILD" -eq 0 ] && [ -f "$REPO/Library/EditorInstance.json" ]; then
  die "Der Unity-Editor hat das Projekt offen. Erst schliessen, dann bauen."
fi

if [ "$FAST" -eq 0 ]; then
  IDENTITY="$(security find-identity -v -p codesigning 2>/dev/null \
    | grep 'Developer ID Application' | head -1 | sed -E 's/.*"(.+)".*/\1/' || true)"
  [ -n "$IDENTITY" ] || die "kein 'Developer ID Application'-Zertifikat im Keychain."
  [ -x "$NOTARIZE" ] || die "notarize.sh fehlt: $NOTARIZE"
  # Nur ein echter Aufruf ist belastbar — das Profil kann in "Local Items"
  # statt in login.keychain liegen, eine Keychain-Suche meldet dann falsch.
  xcrun notarytool history --keychain-profile "apple-notary" >/dev/null 2>&1 \
    || die "notarytool-Profil 'apple-notary' nicht nutzbar (siehe apple-upload-Skill)."
  echo "    Signatur: $IDENTITY"
  echo "    Notary:   apple-notary (geprueft)"
fi

# --- Bauen ------------------------------------------------------------------
if [ "$SKIP_BUILD" -eq 0 ]; then
  step "Player bauen (Universal — laeuft auf Intel und Apple Silicon)"
  LOG="$REPO/Builds/build-mac.log"
  mkdir -p "$REPO/Builds"
  if ! "$UNITY" -batchmode -projectPath "$REPO" \
        -executeMethod Nova.Editor.BuildScript.BuildMacOSArm64 \
        -quit -logFile "$LOG"; then
    echo "--- letzte 40 Zeilen aus $LOG ---" >&2
    tail -40 "$LOG" >&2
    die "Unity-Build fehlgeschlagen."
  fi
  echo "    Log: $LOG"

  # Sidecar fuer --skip-build: aus welchem HEAD und Baumzustand dieser Build kam.
  printf 'head=%s\nstate=%s\n' "$HEAD_FULL" "$TREE_STATE" > "$SIDECAR"
fi
[ -d "$APP" ] || die "kein Build unter $APP"

# --- Aufraeumen und stempeln ------------------------------------------------
step "Aufraeumen und Commit stempeln"
# Der Name sagt es: dieser Ordner darf nicht mit ausgeliefert werden.
rm -rf "$BUILD_DIR"/*_BurstDebugInformation_DoNotShip
find "$BUILD_DIR" -name '.DS_Store' -delete 2>/dev/null || true

# Muss VOR dem Signieren passieren — jede spaetere Aenderung bricht das Siegel.
/usr/libexec/PlistBuddy -c "Delete :NovaBuildCommit" "$APP/Contents/Info.plist" 2>/dev/null || true
/usr/libexec/PlistBuddy -c "Add :NovaBuildCommit string $COMMIT" "$APP/Contents/Info.plist"
echo "    Info.plist: NovaBuildCommit = $COMMIT"

if [ "$FAST" -eq 1 ]; then
  step "Fertig (--fast, unsigniert)"
  echo "    $APP"
  if [ "$OPEN_AFTER" -eq 1 ]; then open "$APP"; fi
  exit 0
fi

# --- Signieren --------------------------------------------------------------
step "Signieren mit Hardened Runtime"
SIGN=(codesign --force --timestamp --options runtime
      --sign "$IDENTITY" --entitlements "$ENTITLEMENTS")

# Von innen nach aussen. Erst lose Dylibs (auch die in Plugin-Bundles), dann
# die Bundles als Einheit, zuletzt die App — jede andere Reihenfolge bricht
# das gerade gesetzte Siegel der Huelle wieder auf.
while IFS= read -r -d '' item; do
  "${SIGN[@]}" "$item" 2>&1 | sed 's/^/    /'
done < <(find "$APP/Contents" \( -name '*.dylib' -o -name '*.so' \) -print0)

while IFS= read -r -d '' item; do
  "${SIGN[@]}" "$item" 2>&1 | sed 's/^/    /'
done < <(find "$APP/Contents" -maxdepth 2 \( -name '*.bundle' -o -name '*.framework' \) -print0)

"${SIGN[@]}" "$APP" 2>&1 | sed 's/^/    /'
codesign --verify --deep --strict "$APP" && echo "    Signatur haelt."

# --- App notarisieren und stapeln -------------------------------------------
# Erst die .app stapeln, dann das DMG daraus bauen: so traegt auch eine aus
# dem DMG herausgezogene App ihr Ticket und startet ohne Online-Pruefung.
step "App notarisieren (dauert ein paar Minuten)"
"$NOTARIZE" "$APP"

# --- DMG bauen --------------------------------------------------------------
step "DMG bauen"
mkdir -p "$DIST"
DMG="$DIST/ProjectNova-$COMMIT.dmg"
STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT

cp -R "$APP" "$STAGE/"
ln -s /Applications "$STAGE/Applications"
cat > "$STAGE/LIESMICH.txt" <<EOF
Project Nova — Testbuild
Commit:  $COMMIT
Gebaut:  $(date '+%Y-%m-%d %H:%M')

Installation: ProjectNova.app in den Ordner "Applications" ziehen, fertig.

Wichtig: Beide Spieler brauchen GENAU diesen Commit ($COMMIT). Der Server
lehnt ein Match zwischen ungleichen Builds ab — absichtlich, damit ihr nicht
nach vierzig Minuten in einem unerklaerlichen Desync sitzt.
EOF

rm -f "$DMG"
hdiutil create -volname "Project Nova $COMMIT" -srcfolder "$STAGE" \
  -ov -format UDZO "$DMG" | sed 's/^/    /'

codesign --force --timestamp --sign "$IDENTITY" "$DMG"

step "DMG notarisieren (dauert nochmal ein paar Minuten)"
"$NOTARIZE" "$DMG"

# --- Gegenprobe -------------------------------------------------------------
step "Gegenprobe — wie Gatekeeper beim Empfaenger urteilt"
spctl -a -t open --context context:primary-signature -v "$DMG" 2>&1 | sed 's/^/    /'
xcrun stapler validate "$DMG" 2>&1 | sed 's/^/    /'

step "Fertig"
echo "    $DMG"
echo "    $(du -h "$DMG" | cut -f1) — verschickbar, oeffnet beim Empfaenger ohne Warnung."
if [ "$OPEN_AFTER" -eq 1 ]; then open "$DMG"; fi
exit 0
