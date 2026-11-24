#!/bin/bash
# Entwicklerwerkzeug: persoenliche Daten vom PC in die Debug-App auf dem Handy kopieren.
# Funktioniert nur mit Debug-Versionen (run-as). Ueberschreibt die Daten auf dem Handy.

adb="$HOME/Android/Sdk/platform-tools/adb"
paket="com.GeritF.PoGoExchange"
quelle="$HOME/.local/share/PoGoExchange"
ziel="files/PoGoExchange"

echo "=== Geraet ==="
geraet=$("$adb" devices | tail -n +2 | grep -w device | cut -f1)
echo "Verbundenes Geraet: ${geraet:-nichts gefunden}"
if [ -z "$geraet" ]; then
  exit 1
fi

echo "=== App beenden ==="
if "$adb" shell am force-stop "$paket"; then
  echo "App beendet: True"
else
  echo "App beendet: False"
fi

echo "=== Dateien uebertragen ==="
for datei in habe_ich.csv suche_ich.csv habe_ich_ueber.csv eigene_formen.csv; do
  if [ ! -f "$quelle/$datei" ]; then
    echo "Uebertragen $datei: Quelldatei nicht gefunden"
    continue
  fi

  if ! "$adb" push "$quelle/$datei" "/data/local/tmp/$datei" > /dev/null 2>&1; then
    echo "Uebertragen $datei: Fehler beim Push"
    continue
  fi

  ausgabe=$("$adb" shell run-as "$paket" cp "/data/local/tmp/$datei" "$ziel/$datei" 2>&1)
  "$adb" shell rm -f "/data/local/tmp/$datei"
  if [ -z "$ausgabe" ]; then
    echo "Uebertragen $datei: OK"
  else
    echo "Uebertragen $datei: Fehler: $ausgabe"
  fi
done

echo "=== Dateien in der App ==="
liste=$("$adb" shell run-as "$paket" ls -l "$ziel" 2>&1)
if [ -z "$liste" ]; then
  echo "App-Dateien: nichts gefunden"
else
  echo "$liste" | sed 's/^/App-Datei: /'
fi
