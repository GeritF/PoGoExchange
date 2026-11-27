#!/bin/bash
# Listet Abweichungen von den Formatierungsregeln (nur lesend)
cd "$(dirname "$0")/../src/PoGoExchange" || { echo "Quellordner: nicht gefunden"; exit 1; }

dateien=$(find . \( -name obj -o -name bin \) -prune -o -type f \
  \( -name '*.cs' -o -name '*.axaml' -o -name '*.csproj' -o -name '*.props' \
  -o -name '*.slnx' -o -name '.editorconfig' \) -print | sort)

melde() { [ -z "$2" ] && echo "$1: keine" || echo "$2"; }

echo "=== Zu lange Zeilen (ueber 100 Zeichen) ==="
t=$(for f in $dateien; do
  awk -v f="$f" 'length($0) > 100 {
    print "Zu lange Zeile: " f ":" FNR " (" length($0) " Zeichen)" }' "$f"
done)
melde "Zu lange Zeilen" "$t"

echo "=== if/else/for/foreach/while ohne Klammern (nur .cs) ==="
t=$(for f in $dateien; do
  case "$f" in *.cs) ;; *) continue ;; esac
  awk -v f="$f" '
    offen != "" {
      if ($0 !~ /^[ \t]*\{/) print "if-Klammern: " f ":" offen
      offen = ""
    }
    /^[ \t]*(if|else if|for|foreach|while) *\(.*\)[ \t]*$/ || /^[ \t]*else[ \t]*$/ {
      offen = FNR; next
    }
    /^[ \t]*(if|else if|for|foreach|while) *\(.*\)[ \t]*[^ \t{].*;[ \t]*$/ {
      print "if-Klammern: " f ":" FNR " (Einzeiler)"
    }' "$f"
done)
melde "if-Klammern" "$t"

echo "=== Dateien mit BOM ==="
t=$(for f in $dateien; do
  [ "$(head -c3 "$f" | od -An -tx1 | tr -d ' \n')" = "efbbbf" ] && echo "BOM: $f"
done)
melde "BOM" "$t"

echo "=== Einrueckung (Tabulatoren oder kleinste Einrueckung ueber 2) ==="
t=$(for f in $dateien; do
  awk -v f="$f" '
    /^\t/ { tab = 1 }
    match($0, /^ +[^ ]/) { n = RLENGTH - 1; if (min == 0 || n < min) min = n }
    END {
      if (tab) print "Einrueckung: " f " (Tabulator)"
      if (min > 2) print "Einrueckung: " f " (kleinste Einrueckung " min ")"
    }' "$f"
done)
melde "Einrueckung" "$t"

echo "=== Nicht-ASCII-Zeichen (ohne BOM) ==="
t=$(for f in $dateien; do
  LC_ALL=C awk -v f="$f" '{ z = $0; if (FNR == 1) sub(/^\xef\xbb\xbf/, "", z) }
    z ~ /[\x80-\xff]/ { print "Nicht-ASCII: " f ":" FNR }' "$f"
done)
melde "Nicht-ASCII" "$t"

echo "=== Ausgerichtete Folgezeilen nach Operator (nur .cs) ==="
t=$(for f in $dateien; do
  case "$f" in *.cs) ;; *) continue ;; esac
  awk -v f="$f" '
    {
      match($0, /^ */); ein = RLENGTH
      folge = (vorher ~ /(\+|&&|\|\||\?\?)[ \t]*$/) || ($0 ~ /^ *(\+|&&|\|\||\?\?) /)
      if (folge && $0 !~ /^ *$/ && ein > vorherEin + 2) {
        print "Folgezeile: " f ":" FNR " (Einrueckung " ein ", Zeile davor " vorherEin ")"
      }
      if ($0 !~ /^ *$/) { vorher = $0; vorherEin = ein }
    }' "$f"
done)
melde "Ausgerichtete Folgezeilen" "$t"
