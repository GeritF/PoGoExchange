#!/bin/bash
# Testlauf mit Auswertung pro Testklasse
# ALLE_TESTS=1 listet zusaetzlich jeden einzelnen Test mit seinen Testdaten
ALLE_TESTS=0

cd ~/DEV/3_PoGoExchange/src/PoGoExchange || exit 1
log=/tmp/pogo_test.log

# Nicht-ASCII-Zeichen als \uXXXX ausgeben, damit die Ausgabe reines ASCII bleibt
ascii() {
  perl -CS -pe 's/([^\x00-\x7F])/sprintf("\\u%04x", ord($1))/ge'
}

echo "=== Testlauf ==="
if dotnet test PoGoExchange.Tests -nologo --logger "console;verbosity=normal" > "$log" 2>&1; then
  echo "Tests erfolgreich: True"
else
  echo "Tests erfolgreich: False"
fi

echo "=== Build-Fehler ==="
b=$(grep -E 'error (CS|NU|MSB)[0-9]+' "$log" | sort -u | head -n 10)
if [ -z "$b" ]; then echo "Build-Fehler: keine"; else echo "$b" | ascii | sed 's/^/Build-Fehler: /'; fi

# Ergebniszeilen der einzelnen Tests, vereinheitlicht zu "OK <Name>" bzw. "FEHLER <Name>"
ergebnisse=$(grep -E '^[[:space:]]+(Bestanden|Passed|Fehler|Failed) PoGoExchange\.Tests\.' "$log" \
  | sed -E 's/^[[:space:]]+(Bestanden|Passed) /OK /; s/^[[:space:]]+(Fehler|Failed) /FEHLER /' \
  | sed -E 's/ \[[^]]*\]$//')

echo "=== Zusammenfassung ==="
if [ -z "$ergebnisse" ]; then
  echo "Zusammenfassung: nichts gefunden"
else
  gesamt=$(echo "$ergebnisse" | wc -l)
  ok=$(echo "$ergebnisse" | grep -c '^OK ')
  fehler=$(echo "$ergebnisse" | grep -c '^FEHLER ')
  echo "Tests gesamt: $gesamt"
  echo "Tests bestanden: $ok"
  echo "Tests fehlgeschlagen: $fehler"
fi

echo "=== Tests pro Klasse ==="
if [ -z "$ergebnisse" ]; then
  echo "Tests pro Klasse: nichts gefunden"
else
  echo "$ergebnisse" | awk '
    { split($2, teile, "."); klasse = teile[3]; anzahl[klasse]++; if ($1 == "FEHLER") fehler[klasse]++ }
    END { for (k in anzahl) printf "Klasse %s: %d Tests, %d Fehler\n", k, anzahl[k], fehler[k] + 0 }
  ' | sort
fi

echo "=== Fehlgeschlagene Tests ==="
f=$(grep -A 8 -E '^[[:space:]]+(Fehler|Failed) PoGoExchange\.Tests\.' "$log" | head -n 60)
if [ -z "$f" ]; then echo "Fehlerdetails: keine"; else echo "$f" | ascii | sed 's/^/Fehlerdetail: /'; fi

if [ "$ALLE_TESTS" = "1" ]; then
  echo "=== Alle Tests ==="
  if [ -z "$ergebnisse" ]; then
    echo "Einzeltests: nichts gefunden"
  else
    echo "$ergebnisse" \
      | sed -E 's/PoGoExchange\.Tests\.//' \
      | sort -k2 \
      | ascii \
      | cut -c1-160 \
      | sed 's/^/Einzeltest: /'
  fi
fi
