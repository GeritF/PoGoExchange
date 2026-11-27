#!/bin/bash
# Baut Desktop- und Android-Projekt und meldet Erfolg und Fehler.
cd "$(dirname "$0")/../src/PoGoExchange" || { echo "Quellordner: nicht gefunden"; exit 1; }

melde() { [ -z "$2" ] && echo "$1: keine" || echo "$2"; }

for projekt in \
  PoGoExchange.Desktop/PoGoExchange.Desktop.csproj \
  PoGoExchange.Android/PoGoExchange.Android.csproj
do
  echo "=== Build $projekt ==="
  ausgabe=$(dotnet build "$projekt" -nologo -v q 2>&1)
  rc=$?
  if [ $rc -eq 0 ]; then
    echo "Build erfolgreich: True"
  else
    echo "Build erfolgreich: False"
  fi
  melde "Build-Fehler" "$(echo "$ausgabe" | grep -E ': error ' | sort -u)"
done
