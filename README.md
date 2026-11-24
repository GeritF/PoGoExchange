# PoGoExchange

Verwaltung von Tausch- und Sammellisten fuer Pokemon GO, mit besonderem
Fokus auf Shiny-Tausche (Glueckstausch). Laeuft unter Linux, Windows und
Android aus einer gemeinsamen Codebasis (C#, .NET, Avalonia UI).

Hinweis zur Schreibweise: Dieses Dokument ist bewusst in reinem ASCII
gehalten (ae, oe, ue, ss statt Umlauten), damit das Encoding
editorunabhaengig bleibt. Das Programm selbst und seine Datendateien
verwenden echte Umlaute (UTF-8).

Die Einrichtung der Entwicklungsumgebung ist in
`install/Entwicklungsumgebung_Linux.md` beschrieben.

## Inhalt

1. Funktionen fuer Anwender
2. Datenstrukturen
3. Technische Details und Begruendungen
4. Hinweise zur Entwicklung

---

# 1. Funktionen fuer Anwender

## Grundidee

Das Programm trennt zwei Arten von Daten:

- **Nutzdaten**: Welche Pokemon und welche Formen gibt es im Spiel?
  Diese Daten werden mit dem Programm ausgeliefert und per Update gepflegt.
- **Persoenliche Listen**: Was habe ich, was suche ich, was kann ich
  abgeben? Diese Daten gehoeren jedem Spieler selbst.

## Formen und Merkmale

Ein Pokemon kann in verschiedenen **Formen** vorkommen:

- **Form**, z.B. "Rot" bei Flabebe, "Q" bei Icognito, "XL" bei Irrbiss
- **Region** fuer Regionalformen, z.B. "Alola" bei Vulpix

Fuer jedes Pokemon gibt es mindestens eine Form. Meist ist das die Grundform
(ohne Form-Text). Manche Pokemon haben nur benannte Formen, z.B. Flabebe in
verschiedenen Farben.

Daneben hat jedes einzelne Exemplar **Merkmale**:

- **Geschlecht**: maennlich oder weiblich
- **Shiny**
- **Dynamax**: keins, Dynamax oder Gigadynamax
- **Kostuem**: keins, oder ein Kostuem mit oder ohne Beschreibung
- **Hintergrund**: keiner, oder ein Hintergrund mit oder ohne Beschreibung
- **Glueck**
- **100er** (Werte 15/15/15)

Bei Eintraegen in "Habe ich" beschreiben die Merkmale das vorhandene
Exemplar. Bei Eintraegen in "Suche ich" sind sie Anforderungen: Ein nicht
gesetztes Merkmal bedeutet "egal". Wer z.B. "Pikachu" ohne Haken bei Shiny
sucht, nimmt auch ein Shiny. Kostuem oder Hintergrund ohne Beschreibung
bedeuten "beliebiges Kostuem" bzw. "beliebiger Hintergrund".

Dynamax und Gigadynamax sind getrennte Ziele: Ein Gigadynamax-Exemplar
erfuellt die Anforderung "Dynamax" nicht und umgekehrt.

Auch die Form kann in "Suche ich" offen bleiben: **Form beliebig**. Wer
"Vulpix 100er, Form beliebig" sucht, ist mit einem Kanto-Vulpix ebenso
zufrieden wie mit einem Alola-Vulpix. "Form beliebig" gibt es nur in
"Suche ich". In "Habe ich" und "Habe ich ueber" hat jedes Exemplar eine
konkrete Form.

Eigene Formen (z.B. eine neue Form, die es in den Nutzdaten noch nicht gibt)
kann jeder Spieler selbst anlegen. Eine Form, die es fuer dieses Pokemon
schon gibt, wird dabei abgelehnt.

## Persoenliche Listen

Jeder Spieler fuehrt drei Listen:

- **Habe ich**: Pokemon, die man besitzt, mit allen Merkmalen.
- **Suche ich**: Pokemon, die man noch haben moechte. Eine Form mit denselben
  Anforderungen kann mehrfach eingetragen werden (z.B. 3x Glumanda Shiny).
  Zusaetzlich:
  - "1:1 moeglich": Man wuerde dasselbe Pokemon im Tausch zurueckgeben.
- **Habe ich ueber**: Pokemon, die man abgeben kann, mit ihren Merkmalen
  (ohne Glueck und 100er).

Regeln:

- Gluecks-Pokemon wurden bereits getauscht und koennen nicht erneut
  getauscht werden. Sie lassen sich daher nicht nach "Habe ich ueber"
  verschieben.
- Mit "Von 'Suche ich' nach 'Habe ich' verschieben" wird ein erfolgreicher
  Tausch eingetragen. Der Sucheintrag wird dabei als erledigt markiert, in
  "Habe ich" entsteht ein neues Exemplar. Die Merkmale des neuen Exemplars
  koennen dabei angepasst werden (z.B. ist es ein 100er geworden). War die
  Suche "Form beliebig", muss fuer das neue Exemplar die tatsaechliche Form
  angegeben werden. Der Sucheintrag selbst bleibt unveraendert.
- Wird ein Exemplar entwickelt, geaendert oder geloescht, schlaegt das
  Programm vor, welche erledigten Sucheintraege angepasst werden sollen. Es
  aendert nichts selbststaendig (siehe "Pokemon entwickeln" und "Exemplare
  aendern oder loeschen").

## Pokemon zu "Suche ich" hinzufuegen

Die Auswahl zeigt drei getrennte Listen:

1. **Verfuegbar**: Formen, die es im Spiel gibt.
2. **Hast du schon**: Formen, zu denen es bereits Exemplare in "Habe ich"
   gibt.
3. **Nicht verfuegbar**: Formen, deren Shiny es noch nicht gibt, wenn Shiny
   gesucht wird (z.B. ein Shiny, das erst zum naechsten Community Day
   erscheint). Bei "Form beliebig" gilt das Shiny als verfuegbar, sobald es
   fuer mindestens eine Form des Pokemon verfuegbar ist.

Alle drei Listen sind auswaehlbar.

Gibt es in "Habe ich" bereits Exemplare, die die Anforderungen erfuellen,
bietet das Programm an, entsprechend viele der neuen Sucheintraege sofort als
erledigt zu markieren.

## Sammel-Suche

Mit einer Sammel-Suche werden viele Sucheintraege mit denselben
Anforderungen auf einmal angelegt, z.B. "Von jedem ein 100er". Ein Schalter
legt fest, wie:

- **Einer pro Form** (voreingestellt): Vulpix bekommt zwei Eintraege (Kanto
  und Alola), Flabebe einen pro Farbe.
- **Einer pro Pokemon**: jeweils ein Eintrag mit "Form beliebig".

Eintraege, fuer die es in "Habe ich" bereits ein passendes Exemplar gibt,
werden sofort als erledigt markiert. Da eine Sammel-Suche sehr viele
Eintraege erzeugt, schlaegt das Programm vorher vor, eine Sicherung
anzulegen.

## Pokemon entwickeln

In "Habe ich" gibt es die Funktion "Entwickeln". Da das Programm keine
Entwicklungsketten kennt, wird die Zielform von Hand ausgewaehlt. Dabei zaehlt
die Form, nicht nur das Pokemon: Aus Alola-Vulpix wird Alola-Vulnona.

Ablauf:

1. In "Habe ich" das Exemplar auswaehlen und "Entwickeln" waehlen.
2. Die Zielform auswaehlen, z.B. Glurak.
3. Das Exemplar bekommt die neue Form. Alle Merkmale (Shiny, Glueck, 100er,
   Hintergrund usw.) bleiben erhalten und koennen danach bei Bedarf angepasst
   werden.
4. Das Programm zeigt alle Zeilen aus "Suche ich" mit erledigten Eintraegen
   derselben Form, deren Anforderungen das Exemplar erfuellt.
5. Man hakt an, welche Zeilen "mit hochgezogen" werden sollen. Pro
   angehakter Zeile wird genau ein erledigter Eintrag auf die neue Form
   umgestellt und bleibt erledigt. Nicht angehakte Zeilen bleiben unveraendert.

**Beispiel 1: Einfache Entwicklung**

Vorher:

- Habe ich: 3x Glumanda Shiny
- Suche ich: 3x Glumanda Shiny, alle erledigt (3/3)

Ein Glumanda wird zu Glurak entwickelt. Das Programm zeigt:

- [ ] Glumanda - Shiny (3 erledigt)

Mit Haken nachher:

- Habe ich: 2x Glumanda Shiny, 1x Glurak Shiny
- Suche ich: Glumanda - Shiny (2/2), Glurak - Shiny (1/1)

Ohne Haken nachher:

- Habe ich: 2x Glumanda Shiny, 1x Glurak Shiny
- Suche ich: Glumanda - Shiny (3/3)

Im zweiten Fall gibt es in "Habe ich" nur noch 2 passende Glumanda fuer 3
erledigte Eintraege. Die Plausibilitaetspruefung weist darauf hin (siehe
unten).

**Beispiel 2: Mehrere Zeilen**

Vorher:

- Habe ich: 3x Glumanda Shiny, eines davon ist ein 100er
- Suche ich: 3x Glumanda Shiny (3/3), 1x Glumanda Shiny [100er] (1/1)

Das 100er-Glumanda wird zu Glurak entwickelt. Das Programm zeigt:

- [ ] Glumanda - Shiny (3 erledigt)
- [ ] Glumanda - Shiny [100er] (1 erledigt)

Sind beide Zeilen angehakt, nachher:

- Habe ich: 2x Glumanda Shiny, 1x Glurak Shiny 100er
- Suche ich: Glumanda - Shiny (2/2), Glurak - Shiny (1/1),
  Glurak - Shiny [100er] (1/1)

Wird stattdessen ein Glumanda ohne 100er entwickelt, erscheint nur die erste
Zeile, denn dieses Exemplar erfuellt die Anforderung "100er" nicht.

**Beispiel 3: Keine passende Suche**

Gibt es zu dem Exemplar keine erledigten Sucheintraege (z.B. weil es nie
gesucht wurde), wird nur das Exemplar umgestellt. Es erscheint keine Auswahl.

## Exemplare aendern oder loeschen

Wird ein Exemplar in "Habe ich" geloescht oder so geaendert, dass es
Anforderungen nicht mehr erfuellt, zeigt das Programm die betroffenen Zeilen
aus "Suche ich" mit erledigten Eintraegen. Man hakt an, in welchen Zeilen ein
Eintrag wieder geoeffnet werden soll. Pro angehakter Zeile wird genau ein
Eintrag wieder offen. Das Programm aendert nichts selbststaendig.

**Beispiel 4: Exemplar geloescht**

Vorher:

- Habe ich: 3x Glumanda Shiny
- Suche ich: Glumanda - Shiny (3/3)

Ein Glumanda wurde versehentlich eingetragen und wird geloescht. Das
Programm zeigt:

- [ ] Glumanda - Shiny (3 erledigt): einen Eintrag wieder oeffnen

Mit Haken nachher:

- Habe ich: 2x Glumanda Shiny
- Suche ich: Glumanda - Shiny (2/3), erscheint wieder unter "fehlt"

**Beispiel 5: Merkmal geaendert**

Vorher:

- Habe ich: 1x Glumanda Shiny mit Glueck
- Suche ich: Glumanda - Shiny [Glueck] (1/1), Glumanda - Shiny (1/1)

Der Glueck-Haken war ein Versehen und wird entfernt. Das Exemplar erfuellt
"Shiny" weiterhin, "Shiny mit Glueck" aber nicht mehr. Das Programm zeigt
daher nur:

- [ ] Glumanda - Shiny [Glueck] (1 erledigt): einen Eintrag wieder oeffnen

Mit Haken nachher:

- Suche ich: Glumanda - Shiny [Glueck] (0/1), Glumanda - Shiny (1/1)

## Plausibilitaetspruefung

Das Programm vergleicht pro Zeile in "Suche ich" die Anzahl der erledigten
Eintraege mit der Anzahl passender Exemplare in "Habe ich". Gibt es mehr
erledigte Eintraege als passende Exemplare, erscheint ein Hinweis, z.B.
"Glumanda - Shiny: 3 erledigt, aber nur 2 passende Exemplare vorhanden". Die
Pruefung korrigiert nichts. Man kann den Hinweis bestaetigen oder einen
Eintrag wieder oeffnen.

## Tauschliste erstellen

Das Programm erzeugt eine Tauschliste als Text, gruppiert nach Region und
sortiert nach Dex-Nummer. Die Liste ist fuer Menschen lesbar und kann von
anderen Spielern in ihr Programm eingelesen werden:

```
Kanto 1:1:
- #4 Glumanda - Shiny (1/3)
- #6 Glurak - Shiny

Kanto fehlt:
- #1 Bisasam - Shiny
- #4 Glumanda - Shiny [100er]
- #150 Mewtu - Shiny

Kanto abzugeben:
- #2 Bisaknosp - Shiny
```

Die Angabe "(1/3)" bedeutet: 1 erledigt, 3 als Ziel. Bei einem einzelnen,
noch offenen Sucheintrag entfaellt die Angabe. Angaben in eckigen Klammern
wie "[Glueck]" oder "[100er]" zeigen, welche Anforderung nur der eigene
Zaehler beruecksichtigt. Fuer den Tauschpartner sind sie ohne Bedeutung, da
Glueck und Werte erst durch den Tausch entstehen.

Vollstaendig erledigte Suchen erscheinen nicht in der Tauschliste.

Pokemon, deren Dex-Nummer in keiner Region liegt (z.B. Meltan und Melmetal),
stehen in einer eigenen Gruppe "Unbekannt" am Ende der Liste.

Eintraege, die die Konsistenzpruefung nicht bestehen (z.B. wegen
beschaedigter Daten), erscheinen nicht in der Liste, sondern werden als
Fehler gemeldet.

## Tauschlisten anderer Spieler abgleichen

Eine fremde Tauschliste wird eingefuegt und einmalig mit den eigenen Listen
verglichen. Sie wird nicht gespeichert. Es darf die ganze Nachricht
eingefuegt werden: Text ausserhalb der Liste (z.B. eine Begruessung) wird
ignoriert.

Das Ergebnis besteht aus drei Listen:

1. **1:1 passt exakt**: Wir suchen beide dasselbe Pokemon 1:1 und koennen es
   uns gegenseitig geben.
2. **Er hat, was ich suche**: Seine Eintraege unter "abzugeben" und "1:1"
   gegen meine offenen Sucheintraege. Wer etwas 1:1 sucht, hat es auch und
   kann es abgeben.
3. **Er sucht, was ich uebrig habe**: Seine Eintraege unter "fehlt" und "1:1"
   gegen mein "Habe ich ueber" und meine eigenen 1:1-Eintraege.

Ist an einem Treffer in Liste 2 oder 3 ein 1:1-Eintrag beteiligt, wird er mit
"1:1 gewuenscht" gekennzeichnet. Ob das streng gemeint ist ("gebe ich nur
gegen dasselbe ab") oder nur ein Angebot ("kann ich auch zurueckgeben"),
klaeren die Spieler untereinander.

Ein Pokemon, das bereits in Liste 1 einen Treffer hat, erscheint nicht
zusaetzlich in Liste 2 oder 3. Der 1:1-Tausch ist der Idealfall.

Jeder Treffer ist entweder **sicher** oder mit **pruefen** markiert, z.B.
"Pruefe Quabbel: Geschlecht beim Angebot nicht angegeben", wenn ich
"Quabbel weiblich" suche und er "Quabbel" ohne Angabe des Geschlechts
anbietet.

Sortierung innerhalb jeder Liste:

1. Treffer mit "1:1 gewuenscht" zuerst,
2. dann sichere Treffer vor denen mit "pruefen",
3. dann nach Dex-Nummer.

Zusaetzlich werden Zeilen der fremden Liste angezeigt, die keiner bekannten
Form zugeordnet werden konnten, jeweils mit Zeilennummer und Grund.

## Abgleich zwischen Geraeten (z.B. Handy und PC)

Es gibt zwei Wege, die eigenen Daten zwischen Geraeten zu uebertragen:

1. **Ueber das WLAN**: Auf beiden Geraeten das Abgleich-Fenster oeffnen. Die
   Geraete finden sich selbst. Beide zeigen einen 6-stelligen Code an, der
   auf beiden Seiten bestaetigt werden muss. Das Programm empfiehlt die
   Richtung anhand der juengsten Aenderung. Beide Seiten muessen die Richtung
   bestaetigen, sie kann auch geaendert werden.
2. **Ueber eine Datei**: Export als ZIP-Datei, die auf das andere Geraet
   kopiert und dort importiert wird.

Wichtig:

- Ein Abgleich ist immer vollstaendig. Die Daten des Zielgeraets werden
  ueberschrieben. Mit dem Haken "Vorher Backup anlegen" (standardmaessig
  aus) wird vorher eine Sicherung erstellt.
- Wurde seit dem letzten Abgleich auf **beiden** Geraeten etwas geaendert,
  warnt das Programm. Die Aenderungen auf der ueberschriebenen Seite gehen
  verloren.
- Geloeschte Eintraege werden 30 Tage aufbewahrt, bevor sie endgueltig
  entfernt werden. Wer laenger als 30 Tage keinen Abgleich macht, sollte die
  Richtung bewusst pruefen.
- Waehrend des WLAN-Abgleichs bleibt der Bildschirm eingeschaltet. Das
  Fenster sollte bis zum Ende geoeffnet bleiben.

## Updates der Nutzdaten

Mit neuen Versionen des Programms koennen neue Pokemon und Formen
hinzukommen. Eigene Formen, die das Update ebenfalls enthaelt, werden
automatisch durch die offizielle Form ersetzt. Formen, die das Update
entfernt, aber noch in den eigenen Listen verwendet werden, bleiben erhalten
und werden als "ungueltig" angezeigt, mit der Aufforderung, eine andere Form
auszuwaehlen.

## Sicherungen

Sicherungen entstehen:

- automatisch vor jedem Update der Nutzdaten,
- manuell ueber die Funktion "Sicherung erstellen" (das Programm schlaegt
  dies auch vor Massenaenderungen wie einer Sammel-Suche vor),
- vor einem Abgleich, wenn der Haken "Vorher Backup anlegen" gesetzt ist.

Damit die Datenmenge begrenzt bleibt, raeumt das Programm alte Sicherungen
nach drei Regeln auf:

1. Pro Tag bleibt nur die neueste Sicherung erhalten.
2. Pro Monat bleiben nur die Sicherungen der neuesten 3 Tage erhalten.
3. Insgesamt bleiben hoechstens die 10 neuesten Sicherungen erhalten.

Beispiel: Wird ein Jahr lang jede Stunde zur halben Stunde eine Sicherung
erstellt, sind am 31.12.2026 um 20:00 Uhr noch vorhanden:

- 31.12.2026 19:30 Uhr, 30.12.2026 23:30 Uhr, 29.12.2026 23:30 Uhr
- 30.11.2026, 29.11.2026, 28.11.2026, jeweils 23:30 Uhr
- 31.10.2026, 30.10.2026, 29.10.2026, jeweils 23:30 Uhr
- 30.09.2026 23:30 Uhr

---

# 2. Datenstrukturen

Alle Daten liegen als CSV-Dateien vor und koennen z.B. mit Excel oder
LibreOffice geoeffnet werden.

## Allgemeine Festlegungen

| Thema          | Festlegung                                                        |
|----------------|-------------------------------------------------------------------|
| Kodierung      | UTF-8 mit BOM                                                     |
| Trennzeichen   | Semikolon `;`                                                     |
| Erste Zeile    | Spaltenueberschriften                                             |
| GUID           | 36 Zeichen, Kleinbuchstaben, mit Bindestrichen                    |
| Wahrheitswerte | `1` = ja, `0` = nein                                              |
| Zeitstempel    | ISO 8601 in UTC, z.B. `2026-11-23T18:30:00Z`                      |
| Leere Felder   | Bedeuten "nicht gesetzt" bzw. "egal" (siehe jeweilige Spalte)     |

## Nutzdaten

### pokemon.csv

Eine Zeile pro Pokemon-Art.

| Spalte    | Inhalt                                                                    |
|-----------|---------------------------------------------------------------------------|
| Guid      | Eindeutige Kennung                                                        |
| DexNr     | Nationale Pokedex-Nummer                                                  |
| Name      | Deutscher Name                                                            |
| Kategorie | Leer, `Legendaer`, `Mysterioes` oder `Ultrabestie` (wird nicht angezeigt) |

### formen.csv

Eine Zeile pro Form.

| Spalte          | Inhalt                                                 |
|-----------------|--------------------------------------------------------|
| Guid            | Eindeutige Kennung                                     |
| PokemonGuid     | Verweis auf `pokemon.csv`                              |
| Form            | Freier Text, leer = Grundform                          |
| Region          | Leer oder Regionsname (siehe `regionen.csv`)           |
| ShinyVerfuegbar | `0` / `1` (gibt es das Shiny bereits im Spiel?)        |
| ArchiviertAm    | Leer oder Zeitstempel der Archivierung                 |

Einschraenkungen:

- `Form` darf keine doppelten Anfuehrungszeichen, keine Zeilenumbrueche und
  nicht die Zeichenfolge ` - ` enthalten.
- Alle Textfelder werden beim Speichern getrimmt: Am Anfang und Ende werden
  alle nicht druckbaren Zeichen entfernt.
- Pro Pokemon darf es keine zwei Formen mit gleicher Kombination aus `Form`
  und `Region` geben (Vergleich in Kleinbuchstaben, siehe Abschnitt 3).

### regionen.csv

Liste der Regionen. Dient der Gruppierung nach Dex-Nummer und als Auswahl
fuer die Spalte `Region` in `formen.csv`.

| Spalte | Inhalt                                          |
|--------|-------------------------------------------------|
| Region | Name der Region, z.B. `Kanto`                   |
| DexVon | Erste Dex-Nummer (einschliesslich), oder leer   |
| DexBis | Letzte Dex-Nummer (einschliesslich), oder leer  |

Eine Region ohne `DexVon` und `DexBis` ist die **Auffang-Region**. Sie nimmt
alle Dex-Nummern auf, die in keinem Bereich liegen, z.B. `Unbekannt` fuer
Meltan (808) und Melmetal (809), wenn Alola bei 807 endet.

Einschraenkungen:

- `DexVon` und `DexBis` sind entweder beide gesetzt oder beide leer.
- Bereiche ueberschneiden sich nicht. Luecken sind erlaubt.
- Es gibt hoechstens eine Auffang-Region. Fehlt sie, gilt der Name
  `Unbekannt`.

### nutzdaten_info.csv

Schluessel-Wert-Paare zu den Nutzdaten.

| Schluessel | Inhalt                              |
|------------|-------------------------------------|
| Version    | Fortlaufende Versionsnummer         |
| Stand      | Zeitstempel der Erstellung          |

## Eigene Daten des Spielers

### eigene_formen.csv

Aufbau wie `formen.csv`. Enthaelt vom Spieler angelegte Formen und Formen,
die durch ein Update aus den Nutzdaten entfernt wurden, aber noch verwendet
werden (dann ist `ArchiviertAm` gesetzt).

### Gemeinsame Verweis-Spalten

Jeder Eintrag der drei persoenlichen Listen verweist auf ein Pokemon und
eine Form:

| Spalte      | Inhalt                                                          |
|-------------|-----------------------------------------------------------------|
| PokemonGuid | Verweis auf `pokemon.csv`, immer gesetzt                        |
| FormGuid    | Verweis auf eine Form; leer = "Form beliebig" (nur in "Suche ich") |

Regeln (Konsistenzpruefung):

- `PokemonGuid` muss auf ein bekanntes Pokemon verweisen.
- In "Habe ich" und "Habe ich ueber" muss `FormGuid` gesetzt sein.
- Ist `FormGuid` gesetzt, muss die Form bekannt sein und zum Pokemon aus
  `PokemonGuid` gehoeren.

### Gemeinsame Merkmal-Spalten

Die drei persoenlichen Listen verwenden dieselben Spalten fuer die Merkmale.
In "Habe ich" und "Habe ich ueber" beschreiben sie das Exemplar, in
"Suche ich" die Anforderung.

| Spalte           | Exemplar                              | Anforderung                         |
|------------------|---------------------------------------|-------------------------------------|
| Geschlecht       | `M`, `W` oder leer (nicht angegeben)  | `M`, `W` oder leer (egal)           |
| Shiny            | `0` / `1`                             | `1` = muss Shiny sein, `0` = egal   |
| Dynamax          | Leer, `Dynamax` oder `Gigadynamax`    | Leer = egal, sonst exakt dieser Wert |
| KostuemAktiv     | `1` = traegt ein Kostuem              | `1` = muss Kostuem tragen, `0` = egal |
| KostuemText      | Beschreibung, leer = nicht angegeben  | Beschreibung, leer = beliebig       |
| HintergrundAktiv | `1` = hat einen Hintergrund           | `1` = muss Hintergrund haben, `0` = egal |
| HintergrundText  | Beschreibung, leer = nicht angegeben  | Beschreibung, leer = beliebig       |

`KostuemText` und `HintergrundText` duerfen keine doppelten
Anfuehrungszeichen und keine Zeilenumbrueche enthalten und werden beim
Speichern getrimmt.

### habe_ich.csv

| Spalte        | Inhalt                                       |
|---------------|----------------------------------------------|
| Guid          | Eindeutige Kennung des Eintrags              |
| PokemonGuid   | Verweis auf ein Pokemon (siehe oben)         |
| FormGuid      | Verweis auf eine Form (siehe oben)           |
| (Merkmale)    | Gemeinsame Merkmal-Spalten (siehe oben)      |
| Glueck        | `0` / `1`                                    |
| Hundert       | `0` / `1` (100er, 15/15/15)                  |
| GeaendertAm   | Zeitstempel der letzten Aenderung            |
| GeloeschtAm   | Leer oder Zeitstempel der Loeschung          |

### suche_ich.csv

| Spalte        | Inhalt                                       |
|---------------|----------------------------------------------|
| Guid          | Eindeutige Kennung des Eintrags              |
| PokemonGuid   | Verweis auf ein Pokemon (siehe oben)         |
| FormGuid      | Verweis auf eine Form (siehe oben)           |
| (Merkmale)    | Gemeinsame Merkmal-Spalten (siehe oben)      |
| Glueck        | `1` = muss Glueck haben, `0` = egal          |
| Hundert       | `1` = muss 100er sein, `0` = egal            |
| EinsZuEins    | `0` / `1` (1:1-Tausch moeglich)              |
| ErledigtAm    | Leer = offen, sonst Zeitstempel der Erledigung |
| GeaendertAm   | Zeitstempel der letzten Aenderung            |
| GeloeschtAm   | Leer oder Zeitstempel der Loeschung          |

### habe_ich_ueber.csv

| Spalte        | Inhalt                                       |
|---------------|----------------------------------------------|
| Guid          | Eindeutige Kennung des Eintrags              |
| PokemonGuid   | Verweis auf ein Pokemon (siehe oben)         |
| FormGuid      | Verweis auf eine Form (siehe oben)           |
| (Merkmale)    | Gemeinsame Merkmal-Spalten (siehe oben)      |
| GeaendertAm   | Zeitstempel der letzten Aenderung            |
| GeloeschtAm   | Leer oder Zeitstempel der Loeschung          |

### abgleich_info.csv

Schluessel-Wert-Paare fuer den Abgleich zwischen Geraeten.

| Schluessel      | Inhalt                                     |
|-----------------|--------------------------------------------|
| Geraetename     | Anzeigename dieses Geraets                 |
| LetzterAbgleich | Zeitstempel des letzten Abgleichs          |

## Exportpaket (ZIP)

Fuer Abgleich und Datei-Export werden alle oben genannten Dateien
(Nutzdaten und eigene Daten) in eine ZIP-Datei gepackt. Der WLAN-Abgleich
uebertraegt dasselbe Paket. Sicherungen verwenden dasselbe Format.

## Eintragstext

Aus Form und Merkmalen eines Eintrags wird ein Text erzeugt, der in
Tauschlisten und beim Abgleich verwendet wird:

```
Name Form - Geschlecht - Shiny - Region - Dynamax - Kostuem - Hintergrund
```

- Die Form folgt ohne Trennstrich direkt auf den Namen.
- Bei "Form beliebig" folgt auf den Namen der Bestandteil `Form (beliebig)`,
  eine Region entfaellt dann.
- Alle weiteren Bestandteile werden mit ` - ` getrennt.
- Nicht gesetzte Bestandteile entfallen vollstaendig.
- Kostuem und Hintergrund mit Beschreibung erscheinen als
  `Kostuem: "Partyhut"`, ohne Beschreibung als `Kostuem (beliebig)`.

Beispiele (im Programm mit echten Umlauten):

```
Icognito Q - Shiny - Hintergrund (beliebig)
Flabebe Rot - Maennlich - Shiny
Vulpix - Shiny - Alola - Hintergrund: "New York 2026"
Vulpix - Form (beliebig) - Shiny
Glumanda
```

## Tauschliste (Textformat)

```
<Region> 1:1:
- #<DexNr> <Eintragstext> [<Zaehlbedingungen>] (<Erledigt>/<Ziel>)

<Region> fehlt:
- #<DexNr> <Eintragstext> [<Zaehlbedingungen>] (<Erledigt>/<Ziel>)

<Region> abzugeben:
- #<DexNr> <Eintragstext>
```

- "1:1" enthaelt gesuchte Pokemon, fuer die man dasselbe Pokemon im Tausch
  zurueckgeben wuerde (`EinsZuEins` = `1`).
- "fehlt" enthaelt die uebrigen gesuchten Pokemon.
- "abzugeben" enthaelt die Eintraege aus "Habe ich ueber".
- Sucheintraege mit gleicher Form, gleichen Merkmalen sowie gleichen Werten
  fuer `Glueck`, `Hundert` und `EinsZuEins` bilden eine Zeile. Die Zeile
  erscheint nur, wenn mindestens ein Eintrag offen ist.
- `[<Zaehlbedingungen>]` enthaelt `Glueck` und/oder `100er`, durch Komma
  getrennt, und entfaellt, wenn keine gesetzt ist.
- `(Erledigt/Ziel)` entfaellt bei `(0/1)`.
- Gleiche Eintraege aus "Habe ich ueber" erscheinen nur einmal.
- Die Region der Ueberschrift richtet sich nach der Dex-Nummer
  (`regionen.csv`), nicht nach der Spalte `Region` der Form.
- Reihenfolge der Regionen: Regionen mit Bereich nach Dex-Nummer, die
  Auffang-Region zuletzt. Leere Abschnitte und Regionen entfallen.
- Innerhalb eines Abschnitts wird nach Dex-Nummer und dann nach
  Eintragstext sortiert.
- Zeilenende ist immer ein einfacher Zeilenvorschub, unabhaengig vom
  Betriebssystem.

---

# 3. Technische Details und Begruendungen

## Warum Nutzdaten und eigene Daten getrennt sind

Die Nutzdaten (alle Pokemon und Formen) werden zentral gepflegt. Die eigenen
Listen enthalten nur Verweise per GUID. So bleiben die eigenen Listen klein,
und ein Update der Nutzdaten ueberschreibt keine persoenlichen Daten.

## Warum Formen und Merkmale getrennt sind

Nur Form und Region machen ein Pokemon zu einem anderen Eintrag im Pokedex.
Geschlecht, Shiny, Dynamax, Kostuem, Hintergrund, Glueck und Werte sind
Eigenschaften eines einzelnen Exemplars.

Wuerden diese Merkmale als eigene Variationen in den Nutzdaten gefuehrt,
muessten alle Kombinationen vorab angelegt werden (z.B. 3 Geschlechter x
Shiny ja/nein x 3 Dynamax-Stufen pro Form). Ausserdem bedeutet "nicht
gesetzt" in einer Suche "egal", bei einem Exemplar aber "hat es nicht". Das
laesst sich mit festen Variationen nicht abbilden. Deshalb stehen die
Merkmale direkt an den Eintraegen der persoenlichen Listen.

## Warum "Form beliebig" kein Eintrag in den Nutzdaten ist

Ein leerer Form-Text bedeutet in `formen.csv` bereits "Grundform" (z.B. das
Kanto-Vulpix). Er kann deshalb nicht zusaetzlich "egal" bedeuten. Eine
eigene Form "beliebig" in den Nutzdaten waere eine Form, die es im Spiel
nicht gibt, und Exemplare koennten ihr faelschlich zugeordnet werden.

"Form beliebig" ist daher wie "Geschlecht egal" eine Anforderung des
Sucheintrags: `FormGuid` bleibt leer, `PokemonGuid` gibt das Pokemon an.
Damit jeder Eintrag sein Pokemon auch ohne Form kennt, fuehren alle drei
Listen `PokemonGuid`. Bei Eintraegen mit Form steht das Pokemon damit doppelt
in den Daten; die Konsistenzpruefung stellt sicher, dass beide
Angaben zusammenpassen.

Ein Exemplar erfuellt eine Suche mit "Form beliebig", wenn es dasselbe
Pokemon ist, gleich welche Form. Ueberschneidungen wie "Vulpix 100er (Form
beliebig)" neben "Vulpix Alola 100er" sind erlaubt: Ein Alola-100er erfuellt
beide Zeilen, so wie ein Exemplar auch sonst mehrere Zeilen erfuellen darf.

Beim Entwickeln wird eine Zeile mit "Form beliebig" auf das neue Pokemon
hochgezogen und bleibt "Form beliebig".

## Warum GUIDs statt Dex-Nummer oder Name

Dex-Nummer und Name reichen nicht, um eine Form eindeutig zu bezeichnen, und
Namen koennen sich (z.B. bei Uebersetzungen) aendern. Eine GUID bleibt
dauerhaft gleich. Einmal vergebene GUIDs werden nie neu erzeugt.

## Warum es keine Region-Spalte bei den Pokemon gibt

Die Region ergibt sich aus der Dex-Nummer. Regionalformen wie Alola-Vulpix
werden wie im Spiel als Form des Ursprungs-Pokemons gefuehrt (Vulpix,
Dex-Nummer 37, Kanto). Die Spalte `Region` der Form beschreibt daher die
Form, nicht die Gruppierung in der Liste. Die Bereiche stehen in
`regionen.csv`, damit sie ohne Programmaenderung angepasst werden koennen.

Die Auffang-Region ohne Bereich gibt es, weil Pokemon GO einzelne Pokemon
keiner Region zuordnet (z.B. Meltan und Melmetal). Statt Ausnahmen im
Programm zu pflegen, laesst man im Bereich der Regionen eine Luecke. Ebenso
landen neue Pokemon dort, bevor `regionen.csv` erweitert wurde.

## Warum es keine Angabe zur Tauschbarkeit gibt

Ob ein Pokemon tauschbar ist, haengt teils von der Art, teils von einzelnen
Kostuemen oder Ereignissen ab. Das laesst sich weder auf Ebene der Art noch
der Form zuverlaessig abbilden. Die Angabe entfaellt daher.

## Warum die Kategorie gefuehrt wird

Die Kategorie (legendaer, mysterioes usw.) wird fuer moegliche spaetere
Funktionen mitgefuehrt. Sie wird noch nirgends angezeigt.

## Glueck und 100er

Glueck und Werte (IV) entstehen beim Tausch neu.

- In "Habe ich" beschreiben sie das vorhandene Exemplar.
- In "Suche ich" legen sie fest, welche Exemplare die Suche erfuellen. Fuer
  den Tauschpartner sind sie ohne Bedeutung, da er sie nicht beeinflussen
  kann. In der Tauschliste stehen sie deshalb nur in eckigen Klammern als
  Hinweis zum Zaehler und werden beim Einlesen ignoriert.
- In "Habe ich ueber" gibt es sie nicht: Gluecks-Pokemon sind nicht erneut
  tauschbar, und die Werte aendern sich beim Tausch.

## Erledigt statt Loeschen bei "Suche ich"

Ein erfolgreich gesuchtes Pokemon wird nicht aus "Suche ich" entfernt,
sondern mit `ErledigtAm` markiert. Nur so bleibt der Zaehler auch bei
gemischten Anforderungen eindeutig.

Beispiel: Gesucht werden 5x Glumanda Shiny, davon 1x mit `Hundert` = `1`.
Kommt zuerst der 100er, wird dieser Eintrag erledigt. Wuerde er stattdessen
geloescht, wuerde der 100er in "Habe ich" anschliessend auch fuer die vier
Eintraege ohne Anforderung zaehlen, und der Zaehler zeigte (1/5) statt (0/4).

Zaehler einer Zeile:

- Ziel: Anzahl aller Sucheintraege der Zeile (offen und erledigt).
- Erledigt: Anzahl der erledigten Sucheintraege der Zeile.

Ein Exemplar darf mehrere Sucheintraege erfuellen, wenn diese zu
verschiedenen Zeilen gehoeren. Beispiel: Ein Shiny-Gluecks-100er erfuellt
"Shiny mit Glueck" und "100er". Innerhalb einer Zeile duerfen nicht mehr
Eintraege automatisch als erledigt markiert werden, als es passende
Exemplare in "Habe ich" gibt.

## Warum es keine Verknuepfung zwischen Sucheintrag und Exemplar gibt

Ein erledigter Sucheintrag speichert nicht, welches Exemplar ihn erledigt
hat. Das ist nicht noetig: Alle Eintraege einer Zeile haben dieselbe Form
und dieselben Anforderungen und sind damit untereinander austauschbar. Es ist
also gleichgueltig, welcher von ihnen beim Entwickeln umgestellt oder beim
Loeschen wieder geoeffnet wird.

Welche Zeilen betroffen sein koennen, ergibt sich aus dem Exemplar selbst:
Es sind die Zeilen mit derselben Form, deren Anforderungen das Exemplar vor
der Aenderung erfuellt hat. Welche davon tatsaechlich angepasst werden,
entscheidet der Anwender, weil nur er weiss, fuer welches Ziel ein Exemplar
gedacht war. Deshalb schlaegt das Programm Anpassungen nur vor und fuehrt sie
nie selbststaendig aus.

Ohne Verknuepfung kann es vorkommen, dass eine Zeile mehr erledigte
Eintraege hat als passende Exemplare, z.B. wenn beim Entwickeln eine Zeile
nicht angehakt wurde. Die Plausibilitaetspruefung macht solche Faelle
sichtbar, ohne sie automatisch zu korrigieren.

Entwicklungsketten sind bewusst nicht enthalten, um den Pflegeaufwand der
Nutzdaten gering zu halten. Die Zielform beim Entwickeln wird deshalb von
Hand gewaehlt.

## Warum "Habe ich ueber" keine Anzahl hat

Die Tauschliste soll zeigen, was grundsaetzlich abgegeben werden kann. Die
genaue Menge ist fuer den einzelnen Tausch irrelevant.

## Eindeutigkeit von Formen

Zwei Formen desselben Pokemons gelten als gleich, wenn `Form` und `Region`
(in Kleinbuchstaben) gleich sind. Diese Regel wird an drei Stellen genutzt:

1. Beim Anlegen wird eine bereits vorhandene Form abgelehnt.
2. Beim Einlesen einer Tauschliste wird die Form ueber Dex-Nummer, Form und
   Region gefunden.
3. Beim Update der Nutzdaten werden eigene Formen erkannt, die nun
   offiziell enthalten sind. Verweise werden auf die offizielle GUID
   umgestellt, die eigene Form wird entfernt.

Verglichen wird immer nur innerhalb derselben Dex-Nummer. Leerzeichen
innerhalb des Textes bleiben erhalten, nur am Anfang und Ende wird getrimmt.

## Einlesen von Tauschlisten

Grundregeln:

- Als Eintrag gilt jede Zeile der Form `#<DexNr> ...`, optional mit
  Aufzaehlungszeichen `-` oder `*` davor. Alle anderen Zeilen (z.B. eine
  Begruessung) werden ignoriert.
- Eine Zeile, die mit `:` endet, ist eine Ueberschrift. Das letzte Wort davor
  bestimmt den Abschnitt (`1:1`, `fehlt` oder `abzugeben`). Eine
  Ueberschrift mit unbekanntem Abschnitt beendet den aktuellen Abschnitt,
  damit nachfolgende Eintraege nicht falsch zugeordnet werden.
- Die Region in der Ueberschrift wird nicht ausgewertet, massgeblich ist die
  Dex-Nummer.
- Gross- und Kleinschreibung spielt keine Rolle, Windows-Zeilenenden sind
  erlaubt.

Ablauf pro Eintrag:

1. Abschnitt aus der letzten Ueberschrift uebernehmen.
2. Aufzaehlungszeichen und `#<DexNr>` abtrennen.
3. Zaehler `(a/b)` und Zaehlbedingungen `[...]` am Ende abtrennen und
   ignorieren.
4. Den Rest an ` - ` ausserhalb von Anfuehrungszeichen zerlegen.
5. Im ersten Teil den bekannten Namen zu dieser Dex-Nummer abtrennen, der
   Rest ist die Form.
6. Die uebrigen Teile als Merkmale erkennen: `Form (beliebig)`, Geschlecht,
   `Shiny`, Regionsname, `Dynamax`/`Gigadynamax`, Kostuem, Hintergrund.
7. Bei `Form (beliebig)` bleibt die Form leer. Steht zusaetzlich eine Form
   oder Region dabei, ist das ein Fehler.
8. Sonst die Form ueber Dex-Nummer, Form und Region suchen.

Eintraege, die sich nicht zuordnen lassen, landen mit Zeilennummer und Grund
in einer Fehlerliste, z.B. "Dex-Nummer 999 unbekannt", "Name passt nicht zu
#4 (Glumanda)", "Unbekannter Bestandteil: Glitzer" oder "Form nicht
gefunden". Eintraege vor der ersten gueltigen Ueberschrift werden ebenfalls
gemeldet. Freie Texte stehen in Anfuehrungszeichen, deshalb duerfen sie
selbst keine enthalten. Trennstriche innerhalb von Anfuehrungszeichen
stoeren nicht.

## Abgleich der Merkmale mit fremden Listen

Beim Abgleich steht immer ein **Angebot** einer **Anforderung** gegenueber.
Angebote sind Eintraege unter "abzugeben", Eintraege aus "Habe ich ueber"
sowie 1:1-Eintraege (wer 1:1 sucht, hat das Pokemon auch). Anforderungen
sind Eintraege unter "fehlt" und "1:1" bzw. die eigenen offenen
Sucheintraege.

Ein Angebot beschreibt ein Exemplar: Ein nicht gesetztes Merkmal bedeutet
"hat es nicht" (z.B. kein Shiny), beim Geschlecht "nicht angegeben". Eine
Anforderung behandelt nicht gesetzte Merkmale als "egal".

Jedes Merkmal liefert eines von drei Ergebnissen:

| Merkmal            | passt                                         | pruefen                                             | passt nicht                         |
|--------------------|-----------------------------------------------|-----------------------------------------------------|-------------------------------------|
| Pokemon            | gleich                                        | -                                                   | anders                              |
| Form               | gleich, oder Anforderung "Form beliebig"      | Angebot "Form beliebig", Anforderung bestimmte Form | anders                              |
| Geschlecht         | Anforderung egal oder gleich                  | Anforderung M/W, Angebot ohne Angabe                | anderes Geschlecht                  |
| Shiny              | nicht gefordert, oder Angebot ist Shiny       | -                                                   | gefordert, Angebot nicht Shiny      |
| Dynamax            | egal oder exakt gleich                        | -                                                   | anders                              |
| Kostuem/Hintergrund | nicht gefordert, beliebig, oder gleicher Text | bestimmter Text gefordert, Angebot ohne Beschreibung | gefordert, aber keins; anderer Text |

Ist ein Merkmal "passt nicht", passt der Eintrag nicht. Ist mindestens eines
"pruefen", ist es ein Treffer mit Hinweis, sonst ein sicherer Treffer.
Glueck und 100er werden nicht verglichen, da sie erst durch den Tausch
entstehen.

Beispiele:

- Ich suche "Zubat" (Geschlecht egal), angeboten wird "Zubat weiblich":
  sicherer Treffer.
- Ich suche "Quabbel weiblich", angeboten wird "Quabbel" ohne
  Geschlechtsangabe: Treffer mit Hinweis, weil unklar ist, was der Partner
  wirklich hat.
- Ich suche ein bestimmtes Kostuem, angeboten wird ein Kostuem ohne
  Beschreibung: Treffer mit Hinweis. Fuer Hintergruende gilt dasselbe.

Ein **1:1-Treffer** (Liste 1) liegt vor, wenn sein 1:1-Eintrag als Angebot
meine 1:1-Anforderung erfuellt und mein 1:1-Eintrag als Angebot seine. Gilt
das, erscheint das Paar nur in Liste 1.

Die Regel fuer die eigenen Listen ("erfuellt ein Exemplar aus 'Habe ich'
einen Sucheintrag?") kennt dagegen nur ja oder nein: Dort sind alle Angaben
bekannt, ein "pruefen" ist nicht noetig.

## Archivieren statt Loeschen

Formen in den Nutzdaten werden nie geloescht, sondern archiviert. Verweise
aus den Listen der Spieler bleiben dadurch gueltig. Entfernt ein Update eine
Form, die ein Spieler noch verwendet, wird sie mit gleicher GUID nach
`eigene_formen.csv` uebernommen und archiviert. Archivierte Formen koennen
nicht neu ausgewaehlt werden.

Eigene Formen koennen nur geloescht werden, solange keine Liste sie
verwendet.

## Loeschen mit Aufbewahrungsfrist

Eintraege in den eigenen Listen werden beim Loeschen nur mit `GeloeschtAm`
markiert und nach 30 Tagen endgueltig entfernt. Dadurch hat auch eine
Loeschung einen Zeitstempel, und die Empfehlung beim Abgleich ("juengste
Aenderung") beruecksichtigt sie.

## Sicherungen

Sicherungen werden als Exportpaket (ZIP) mit Zeitstempel im Namen abgelegt.
Die Aufraeumregeln werden nach jeder neuen Sicherung in dieser Reihenfolge
angewendet:

1. Pro Kalendertag nur die neueste Sicherung behalten.
2. Pro Kalendermonat nur die Sicherungen der neuesten 3 Tage behalten.
3. Insgesamt nur die 10 neuesten Sicherungen behalten.

Vor einem Abgleich wird standardmaessig keine Sicherung erstellt, da
haeufige Abgleiche sonst viele Sicherungen erzeugen wuerden. Vor
Massenaenderungen (z.B. Sammel-Suche) schlaegt das Programm eine manuelle
Sicherung vor.

## Abgleich zwischen Geraeten

Ein Abgleich uebertraegt immer das vollstaendige Exportpaket, einschliesslich
der Nutzdaten. Ein teilweiser Abgleich wuerde bei unterschiedlichen
Nutzdaten-Versionen zu Verweisen auf unbekannte GUIDs fuehren.

Empfehlung der Richtung: Jede Seite ermittelt den juengsten Zeitstempel aus
`GeaendertAm` und `GeloeschtAm` ihrer Listen. Die Seite mit dem juengeren
Wert wird als Quelle empfohlen. Liegen auf beiden Seiten Aenderungen nach
`LetzterAbgleich`, wird zusaetzlich gewarnt.

### Ports

| Port  | Protokoll | Zweck                               |
|-------|-----------|-------------------------------------|
| 28211 | UDP       | Suche nach Gegenstellen (Broadcast) |
| 28212 | TCP       | Uebertragung                        |

Feste Ports erlauben eine gezielte Firewall-Regel. Beide Ports liegen in
einem bei der IANA nicht zugewiesenen Bereich und unterhalb der Bereiche, die
Linux und Windows fuer ausgehende Verbindungen nutzen.

### Ablauf

1. Beide Geraete oeffnen das Abgleich-Fenster und lauschen auf Port 28211.
2. Jedes Geraet wartet 2000 ms plus eine Zufallszeit von 100 bis 1000 ms
   (Zufallszahl 1 bis 10, mal 100 ms) und sendet dann einen Broadcast.
3. Empfaengt ein Geraet einen Broadcast, startet es den Server auf Port 28212,
   antwortet "Kommunikation ueber Port 28212" und beendet seinen eigenen
   Broadcast.
4. Erhaelt ein Geraet eine Antwort auf seinen Broadcast, beendet es seinen
   Lauscher und verbindet sich als Client.
5. Brechen beide Seiten gleichzeitig ab, beginnt der Vorgang von vorn. Durch
   die Zufallszeit hat spaetestens im zweiten Durchlauf ein Geraet den
   Vorrang. Nach einer Gesamtzeit ohne Erfolg wird der Vorgang abgebrochen.
6. Der Server erzeugt einen 6-stelligen Zahlencode. Beide Geraete zeigen ihn
   an, beide Nutzer bestaetigen.
7. Beide Seiten tauschen ihre Zeitstempel aus. Das Programm empfiehlt die
   Richtung, beide Nutzer bestaetigen oder aendern sie.
8. Ist "Vorher Backup anlegen" gesetzt, erstellt das Zielgeraet eine
   Sicherung. Danach wird das Paket uebertragen.
9. Beide Geraete setzen `LetzterAbgleich` und beenden die Verbindung.

Der Code verhindert, dass sich versehentlich ein fremdes Geraet verbindet.
Die Uebertragung selbst ist nicht verschluesselt. Fuer Tauschlisten im
Heimnetz wird das als ausreichend angesehen.

## Dateiformat

- **UTF-8 mit BOM**: Pokemon-Namen enthalten Sonderzeichen (z.B. Flabebe mit
  Akzent, Nidoran mit Geschlechtssymbol). Ohne BOM oeffnet Excel die Datei
  nicht korrekt.
- **Semikolon**: Standard-Trennzeichen eines deutschen Excel.
- **Keine Anfuehrungszeichen in freien Texten**: Im Export begrenzen
  Anfuehrungszeichen die freien Texte, deshalb duerfen diese selbst keine
  enthalten.

## Sprache

Namen werden nur auf Deutsch gefuehrt. Weitere Sprachen koennen spaeter
ueber eine Datei pro Sprache (GUID und Name) ergaenzt werden. Tauschlisten
enthalten immer die Dex-Nummer, damit sie auch bei unterschiedlichen Namen
zugeordnet werden koennen.

## Plattformen

- Desktop (Linux, Windows): Das Programm wird regulaer beendet.
- Android: "Beenden" schliesst die App und entfernt sie aus der Liste der
  zuletzt genutzten Apps. Den Prozess selbst darf Android noch eine Weile im
  Speicher halten. Da Android eine App jederzeit beenden kann, werden alle
  Aenderungen sofort gespeichert und nicht erst beim Beenden.
- Android speichert die Daten in einem privaten Bereich. Der Zugriff von
  aussen erfolgt ueber Export und Import (Exportpaket).

---

# 4. Hinweise zur Entwicklung

- Die Einrichtung der Entwicklungsumgebung steht in
  `install/Entwicklungsumgebung_Linux.md`.
- Die Fachlogik (Ordner `Models` und `Services`) ist unabhaengig von der
  Oberflaeche und wird im Projekt `PoGoExchange.Tests` mit xUnit getestet.
- `test/Test_XUnit.sh` fuehrt alle Tests aus und zeigt eine Auswertung pro
  Testklasse. Mit `ALLE_TESTS=1` im Skript wird jeder einzelne Test mit
  seinen Testdaten aufgelistet.
- Die mitgelieferten Nutzdaten liegen in `src/PoGoExchange/PoGoExchange/Nutzdaten`
  und werden als eingebettete Ressource in das Programm uebernommen. Die
  Tests in `NutzdatenTests` pruefen ihre innere Stimmigkeit (Verweise,
  Eindeutigkeit, Regionsbereiche).
- Quelltext wird in reinem ASCII geschrieben. Sonderzeichen in Anzeigetexten
  stehen als `\u`-Escapes im Quelltext (z.B. `"Kost\u00fcm"`).
- Der Code-Stil ist in `src/PoGoExchange/.editorconfig` festgelegt:
  Einrueckung mit 2 Leerzeichen, geschweifte Klammern auch bei einzeiligen
  `if`-Bloecken, Zeilen bis etwa 100 Zeichen.
- Die Konsistenz der persoenlichen Listen (Verweise auf Pokemon und Formen)
  prueft `Services/Konsistenz.cs`; die Namen der Listen fuer Meldungen
  stehen in `ListenNamen`.
- Klassennamen weichen an zwei Stellen von den Dateinamen ab, weil eine
  C#-Klasse keine Eigenschaft mit ihrem eigenen Namen haben darf:
  `PokemonForm` (Datei `formen.csv`, Spalte `Form`) und `RegionBereich`
  (Datei `regionen.csv`, Spalte `Region`).
