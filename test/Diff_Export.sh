#!/bin/bash
# Schreibt alle Aenderungen in eine Datei zum Kopieren.
# Aufruf: ./Diff_Export.sh        Vergleich mit dem Index (Stand von git add -A)
#         ./Diff_Export.sh head   Vergleich mit dem letzten Commit
cd "$(dirname "$0")/.." || { echo "Projektordner: nicht gefunden"; exit 1; }

ziel="$HOME/PoGoExchange_diff.txt"
if [ "$1" = "head" ]; then
  basis="HEAD"
  basisText="letzter Commit"
else
  basis=""
  basisText="Index (Stand von git add -A)"
fi

melde() { [ -z "$2" ] && echo "$1: keine" || echo "$2"; }

{
  echo "=== Export ==="
  echo "Erstellt: $(date '+%Y-%m-%d %H:%M:%S')"
  echo "Vergleich mit: $basisText"
  echo "Letzter Commit: $(git log -1 --format='%h %s')"

  echo "=== Geaenderte Dateien ==="
  melde "Geaenderte Dateien" "$(git diff $basis --stat)"

  echo "=== Neue Dateien, Git noch unbekannt ==="
  melde "Neue Dateien" "$(git ls-files --others --exclude-standard)"

  echo "=== Aenderungen ausser Leerraum ==="
  melde "Aenderungen ausser Leerraum" "$(git diff $basis -w --ignore-blank-lines)"

  echo "=== Voller Diff ==="
  melde "Voller Diff" "$(git diff $basis)"
} > "$ziel"

echo "=== Diff-Export ==="
echo "Datei: $ziel"
echo "Zeilen: $(wc -l < "$ziel")"
