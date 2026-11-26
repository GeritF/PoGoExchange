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

## Aufbau der Oberflaeche

Die Hauptseite fuehrt ueber Kacheln zu den Bereichen:

- **Tauschlisten**: die drei persoenlichen Listen als Registerkarten
  "Suche ich", "Habe ich" und "Habe ich ueber"
- **Import/Export**: eigene Tauschliste erzeugen und kopieren, fremde Listen
  einfuegen und abgleichen, eigene Listen als Datei exportieren und
  importieren (Registerkarte "Eigene Listen")
- **Verwaltung**: Datenordner, Anzahl der Zeilen (bei "Habe ich" auch der
  Exemplare), Zeitpunkt der letzten Aenderung und Lademeldungen, mit einem
  Button zum Kopieren in die Zwischenablage; dazu die Buttons "Sicherungen"
  und "Eigene Formen"
- **Beenden**

In allen Listen gilt:

- Oben steht ein Suchfeld. Eine Zahl (auch mit `#`, z.B. `#37`) filtert nach
  Dex-Nummer, sonst wird im Namen gesucht.
- Die Eintraege sind nach Region gruppiert und nach Dex-Nummer sortiert.
- Eintraege, die nicht zu den Nutzdaten passen (z.B. unbekanntes Pokemon),
  stehen in einer eigenen Gruppe "Fehlerhafte Eintraege" am Ende.
- Gab es beim Laden Meldungen, weist eine Hinweisleiste darauf hin.
- In "Habe ich" steht vor Zeilen mit mehreren gleichen Exemplaren die
  Anzahl, z.B. `3x #4 Glumanda - Shiny [Glueck]`.

In allen drei Listen gibt es ausserdem:

- oben den Button "+ Neu" und darunter eine Rueckmeldezeile, z.B. "1 von 3
  bereits erledigt", "Zeile geloescht" oder "Entwickelt zu #6 Glurak"
- das Oeffnen einer Zeile durch Antippen

Ist die Datei einer Liste gesperrt (siehe "Pruefung beim Laden"), fehlen
dort "+ Neu" und das Oeffnen; die Hinweisleiste nennt den Grund.

In "Suche ich" zusaetzlich:

- Neben "+ Neu" gibt es "+ Mehrere" (siehe "Mehrere auf einmal suchen").
- Jede Zeile zeigt ihre Bedingungen in eckigen Klammern (Glueck, 100er, 1:1)
  und den Fortschritt, z.B. `#4 Glumanda - Shiny [Glueck, 1:1] (1/3)`.
- Vollstaendig erledigte Zeilen sind ausgeblendet. Der Button "Erledigte
  anzeigen" blendet sie nach einer Rueckfrage ein; sie erscheinen dann blass
  und durchgestrichen. Ein Hinweis zeigt jeweils, ob erledigte Zeilen
  angezeigt werden.

Navigation:

- Jede Unterseite hat oben einen sichtbaren Zurueck-Button, auf allen
  Plattformen.
- Die Zurueck-Taste bzw. -Geste von Android wirkt wie der Zurueck-Button. Auf
  der Hauptseite wird sie abgefangen, damit die App nicht versehentlich
  verlassen wird; dafuer gibt es den Button "Beenden".

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
kann jeder Spieler selbst anlegen (siehe "Eigene Formen").

Ob es ein Kostuem oder einen Hintergrund auch als Shiny gibt, prueft das
Programm nicht. Die Shiny-Verfuegbarkeit wird nur pro Form gefuehrt; sie mit
Kostuemen und Hintergruenden zu verknuepfen, waere kaum zu pflegen. Darauf
achten die Spieler selbst.

## Persoenliche Listen

Jeder Spieler fuehrt drei Listen:

- **Habe ich** ist die "Fertig"-Liste: Exemplare, die man hat (auch bereits
  getauschte), mit allen Merkmalen. Gleiche Exemplare stehen in einer Zeile
  mit Anzahl, z.B. "3x Glumanda Shiny [Glueck]". Die Liste zeigt den
  Fortschritt der Suche und ist keine vollstaendige Inventur aller Pokemon.
- **Suche ich** enthaelt Zeilen mit einer Anzahl, z.B. "3x Glumanda Shiny
  [Glueck]". Jede Zeile ist ein eigenstaendiges Ziel. Zusaetzlich gibt es
  "1:1 moeglich": Man wuerde dasselbe Pokemon im Tausch zurueckgeben.
- **Habe ich ueber** enthaelt tauschbare Pokemon (noch nicht getauscht, kein
  Crypto usw.), die man abgeben kann, mit ihren Merkmalen (ohne Glueck und
  100er). Diese Liste wird getrennt von "Habe ich" gepflegt; es gibt keine
  Verknuepfung zwischen beiden. Sie hat keine Anzahl (siehe "Warum 'Habe ich
  ueber' keine Anzahl hat").

Fortschritt in "Suche ich":

- Der Fortschritt wird nicht gespeichert, sondern aus "Habe ich" berechnet:
  Summe der Anzahlen aller passenden Zeilen, hoechstens die gesuchte Anzahl.
  Die gesuchte Anzahl selbst aendert sich dadurch nicht. Bei 7
  passenden Exemplaren und 5 gesuchten zeigt die Zeile `(5/5)`.
- Ein Exemplar zaehlt in **jeder** Zeile, deren Anforderungen es erfuellt.
  Beispiel: Gesucht werden "5x Glumanda Shiny" und "1x Glumanda Shiny
  [100er]". Ein Shiny-100er ergibt `(1/5)` und `(1/1)`.
- Wer "5 Shinys, davon einer als 100er" sucht, traegt deshalb "5x Shiny" und
  "1x Shiny [100er]" ein, nicht "4x Shiny" und "1x Shiny [100er]".

Regeln:

- Gluecks-Pokemon wurden bereits getauscht und koennen nicht erneut
  getauscht werden. In "Habe ich ueber" gibt es daher keinen Glueck-Haken.
- Mit "Erhalten" wird ein erfolgreicher Tausch eingetragen (siehe
  "Erhalten"). Die Zeile in "Suche ich" bleibt unveraendert, ihr Zaehler
  steigt.
- Wird eine Zeile in "Habe ich" geaendert oder geloescht, werden die Zaehler
  einfach neu berechnet. Beim Entwickeln bietet das Programm zusaetzlich an,
  die Suche anzupassen (siehe "Pokemon entwickeln").
- Geloeschte Zeilen werden sofort aus der Datei entfernt.

## Pokemon zu "Suche ich" hinzufuegen

Die Seite "Neuer Sucheintrag" (Button "+ Neu" in "Suche ich") ist in zwei
Teile gegliedert:

1. **Anforderungen** oben: Geschlecht, Shiny, Dynamax, Kostuem, Hintergrund,
   Glueck, 100er, 1:1 und die Anzahl (voreingestellt 1).
2. **Auswahl des Pokemon** unten: Suchfeld und darunter drei Abschnitte. Jedes
   Ziel ist eine bestimmte Form (z.B. "Vulpix - Alola") oder ein Pokemon mit
   "Form beliebig" (z.B. "Vulpix - Form (beliebig)"):
   - **Verfuegbar**: alle uebrigen Ziele.
   - **Hast du schon**: Ziele, fuer die es in "Habe ich" bereits ein Exemplar
     gibt, das die Anforderungen erfuellt.
   - **Nicht verfuegbar**: Shiny ist gefordert, aber fuer dieses Ziel noch
     nicht im Spiel verfuegbar (z.B. ein Shiny, das erst zum naechsten
     Community Day erscheint). Bei "Form beliebig" gilt das Shiny als
     verfuegbar, sobald es fuer mindestens eine Form verfuegbar ist.

Alle drei Abschnitte sind auswaehlbar. Sie aktualisieren sich, sobald die
Anforderungen geaendert werden; die gewaehlte Form bleibt dabei gewaehlt.
"Form beliebig" wird nur bei Pokemon mit mehreren Formen angeboten;
archivierte Formen sind nicht waehlbar. Je Abschnitt werden hoechstens 100
Ziele angezeigt, damit die Seite auch auf dem Handy fluessig bleibt. Gibt es
mehr, weist ein Hinweis auf das Suchfeld hin.

Nach dem Speichern zeigt das Programm, wie viele davon bereits erledigt sind,
z.B. "1 von 3 bereits erledigt". Gibt es dieselbe Zeile schon, wird ihre
Anzahl erhoeht, z.B. "Diese Zeile gab es bereits: 1/2, zusammen jetzt: 1/5",
und die Zeile wird zum Nachkorrigieren geoeffnet.

Beschreibungen fuer Kostuem und Hintergrund duerfen keine Anfuehrungszeichen
und keine Zeilenumbrueche enthalten.

## Zeile in "Suche ich" bearbeiten

Antippen einer Zeile oeffnet sie:

- **Anforderungen aendern** gilt fuer die Zeile. Pokemon und Form lassen sich
  nicht aendern; dafuer wird eine neue Zeile angelegt und die alte auf
  Anzahl 0 gesetzt.
- Ergeben die geaenderten Anforderungen eine Zeile, die es bereits gibt,
  fragt das Programm mit Aufschluesselung nach, z.B. "Diese Zeile: 2/4,
  gefundene Zeile: 1/3, zusammen: 3/7. Zusammenfuehren?". Bei "Ja" werden
  beide Zeilen zusammengefuehrt (die Anzahlen addiert) und die zusammengefuehrte
  Zeile geoeffnet; bei "Nein" bleibt man in der Bearbeitung.
- **Anzahl aendern** ueberschreibt die Anzahl ohne Rueckfrage, solange sie
  mindestens 1 ist.
- **Anzahl 0** entfernt die Zeile, aber erst nach der Rueckfrage "Anzahl 0
  entfernt die Zeile vollstaendig. Wirklich loeschen?". Ein versehentlicher
  Klick auf "Pfeil runter" loescht so nichts. Bei "Nein" springt das
  Eingabefeld auf die gespeicherte Anzahl zurueck.
- **Loeschen**: Der Button zeigt nur den Hinweis "Setze die Anzahl auf 0, um
  die Zeile zu loeschen". So ist eindeutig, dass die ganze Zeile entfernt
  wird und nicht nur ein Stueck.

## Erhalten

Auf der Seite "Zeile bearbeiten" in "Suche ich" traegt der Button "Erhalten"
einen erfolgreichen Tausch ein. Er oeffnet die Seite "Erhalten":

- Die Merkmale sind aus den Anforderungen der Zeile vorbelegt (z.B. Shiny,
  Glueck, 100er, Kostuem). Was die Zeile offen laesst (z.B. Geschlecht
  "egal"), steht auf "unbekannt" und kann ergaenzt werden.
- Hat die Zeile eine bestimmte Form, ist diese bereits ausgewaehlt; das
  Suchfeld entfaellt.
- Bei "Form beliebig" stehen nur die Formen dieses Pokemon zur Auswahl; die
  tatsaechliche Form muss gewaehlt werden.
- Nach dem Speichern steigt in "Habe ich" die passende Zeile um die Anzahl
  oder es entsteht eine neue Zeile. Man landet wieder auf der Zeilenseite und
  sieht den neuen Stand, z.B. "Erhalten. Fortschritt: 2/3".

## Exemplar zu "Habe ich" hinzufuegen

Die Seite "Neues Exemplar" (Button "+ Neu" in "Habe ich") ist fuer Exemplare
gedacht, die man schon vor dem Erfassen hatte oder ausserhalb eines
gesuchten Tauschs bekommt:

1. **Merkmale** oben. Sie beschreiben das Exemplar, deshalb lauten die
   Auswahlwerte anders als bei Anforderungen: Geschlecht "unbekannt /
   maennlich / weiblich", Dynamax "keins / Dynamax / Gigadynamax"; ein
   leerer Kostuem- oder Hintergrundtext bedeutet "nicht beschrieben".
2. **Anzahl** gleicher Exemplare (voreingestellt 1).
3. **Pokemon und Form** ueber Suchfeld und Liste (hoechstens 100 Eintraege,
   archivierte Formen sind nicht waehlbar). "Form beliebig" gibt es hier
   nicht.

Gibt es dieselbe Zeile schon, wird ihre Anzahl erhoeht, z.B. "Diese Zeile
gab es bereits: 2x, jetzt 3x".

## "Habe ich ueber" bearbeiten

- **"+ Neu"** wie bei "Habe ich", aber ohne Glueck, 100er und Anzahl. Gibt es
  denselben Eintrag schon, wird nichts doppelt angelegt ("Diesen Eintrag gibt
  es bereits").
- **Antippen** oeffnet "Eintrag bearbeiten": Merkmale aendern (waere der
  Eintrag danach gleich einem anderen, fragt das Programm, ob beide
  zusammengefuehrt werden sollen) und **Loeschen** nach Rueckfrage.

## Mehrere auf einmal suchen ("+ Mehrere")

Mit "+ Mehrere" werden viele Suchzeilen mit denselben Anforderungen auf
einmal angelegt, z.B. "Von jedem Kanto-Pokemon ein Shiny" oder "Von jedem ein
100er". Die Seite bietet:

- **Region**: "Alle Regionen" oder eine bestimmte Region (nach Dex-Nummer).
- **Zeilen**: "Eine Zeile pro Form" (voreingestellt; Vulpix bekommt zwei
  Zeilen, Kanto und Alola, Flabebe eine pro Farbe) oder "Eine Zeile pro
  Pokemon (Form beliebig)". Archivierte Formen werden nicht beruecksichtigt.
- **Anforderungen** wie bei "Neuer Sucheintrag".
- Eine **Vorschau**, die sich bei jeder Aenderung neu berechnet, z.B. "Neue
  Zeilen: 5 . uebersprungen (gibt es schon): 0 . Shiny nicht verfuegbar: 1 .
  davon bereits erledigt: 0".
- Den Haken **"Vorher Sicherung anlegen"** (voreingestellt an).

Regeln:

- Gibt es dieselbe Zeile (gleiches Pokemon, gleiche Form, gleiche
  Anforderungen) bereits, wird sie uebersprungen. Wer "+ Mehrere" versehentlich
  zweimal startet, bekommt keine doppelten Zeilen.
- Ist Shiny gefordert, werden Formen ohne verfuegbares Shiny uebersprungen
  (bei "Form beliebig": wenn es fuer keine Form verfuegbar ist). So landen
  keine Shinys in der Tauschliste, die es im Spiel noch nicht gibt. Wird
  dieselbe Suche spaeter erneut gestartet, kommen inzwischen verfuegbare
  Shinys hinzu.
- Neue Zeilen, fuer die es in "Habe ich" schon ein passendes Exemplar gibt,
  zaehlen sofort als erledigt.
- Gibt es nichts Neues, wird nichts angelegt und auch keine Sicherung
  erstellt.

Eine Funktion, um mehrere Zeilen auf einmal wieder zu entfernen, gibt es
bewusst nicht; dafuer dient die Sicherung vorher.

## Eigene Formen

Unter Verwaltung > Eigene Formen stehen alle selbst angelegten Formen,
sortiert nach Dex-Nummer, z.B. "#201 Icognito ?". Formen, die ein Update der
Nutzdaten archiviert hat, tragen den Zusatz "(archiviert)".

- **"+ Neu"**: Pokemon ueber Suchfeld und Liste waehlen, dann Form-Text (leer
  = Grundform), Region (Auswahl aus den bekannten Regionen, fuer
  Regionalformen wie Alola) und "Shiny verfuegbar".
- **Antippen** oeffnet die Form: Form-Text, Region und "Shiny verfuegbar"
  lassen sich aendern. **Loeschen** geht nach einer Rueckfrage, aber nur,
  solange keine Liste die Form verwendet; sonst nennt das Programm die
  Listen.
- Eine Form, die es fuer dieses Pokemon schon gibt (gleicher Form-Text und
  gleiche Region, ohne Gross-/Kleinschreibung), wird beim Anlegen und Aendern
  abgelehnt.
- Im Form-Text nicht erlaubt sind Anfuehrungszeichen, eckige und runde
  Klammern sowie " - " (Bindestrich mit Leerzeichen), weil sie die
  Tauschliste durcheinanderbringen wuerden. Statt "Toll (und gross)" also z.B.
  "Toll, und gross".
- Die Region ist eine Auswahl statt freiem Text, weil das Einlesen fremder
  Listen nur bekannte Regionsnamen erkennt.

Auf "Neues Exemplar", "Neuer Sucheintrag" und "Erhalten" (bei "Form
beliebig") gibt es den Link **"Form fehlt? Eigene Form anlegen"**. Beim
Erhalten ist das Pokemon vorgegeben. Danach ist die neue Form gleich
ausgewaehlt.

Ganz neue Pokemon (eine Dex-Nummer, die in den Nutzdaten fehlt) lassen sich
nicht anlegen; sie kommen mit einem Update der Nutzdaten.

## Pokemon entwickeln

In "Habe ich" gibt es die Funktion "Entwickeln". Da das Programm keine
Entwicklungsketten kennt, wird die Zielform von Hand ausgewaehlt. Dabei zaehlt
die Form, nicht nur das Pokemon: Aus Alola-Vulpix wird Alola-Vulnona.

Ablauf:

1. In "Habe ich" die Zeile oeffnen und "Entwickeln" waehlen. Hat die Zeile
   mehrere Exemplare, weist die Seite darauf hin, dass genau eines
   entwickelt wird.
2. Die Zielform ueber Suchfeld und Liste auswaehlen, z.B. Glurak. Die
   aktuelle Form steht nicht zur Auswahl.
3. Es wird immer genau ein Exemplar entwickelt: Die Zeile in "Habe ich"
   sinkt um 1 (beim letzten Exemplar wird sie entfernt), und eine Zeile mit
   denselben Merkmalen (Shiny, Glueck, 100er, Hintergrund usw.) fuer die neue
   Form steigt um 1 oder wird neu angelegt. Die Merkmale koennen danach
   angepasst werden.
4. Sobald eine Zielform gewaehlt ist, zeigt die Seite die Zeilen aus "Suche
   ich", deren Anforderungen das Exemplar vor der Entwicklung erfuellt hat,
   mit Bedingungen und Zaehler. Alle sind angehakt.
5. Fuer jede angehakte Zeile wird die Suche "mit hochgezogen": Die Anzahl der
   Zeile sinkt um 1 (bei 0 ist die Zeile geloescht), und eine Zeile mit
   denselben Anforderungen fuer die neue Form steigt um 1 oder wird mit
   Anzahl 1 neu angelegt. Bei "Form beliebig" wechselt nur das Pokemon, die
   Zeile bleibt "Form beliebig".
6. Beide Listen werden gespeichert, und man landet wieder in der Liste "Habe
   ich" (die Zeile kann beim letzten Exemplar verschwunden sein). Die
   Rueckmeldung lautet z.B. "Entwickelt zu #6 Glurak. Suche angepasst: 1
   Zeile(n)".

**Beispiel 1: Einfache Entwicklung**

Vorher:

- Habe ich: 3x Glumanda Shiny
- Suche ich: Glumanda - Shiny (3/3)

Ein Glumanda wird zu Glurak entwickelt. Das Programm zeigt:

- [x] Glumanda - Shiny (3/3)

Mit Haken nachher:

- Habe ich: 2x Glumanda Shiny, 1x Glurak Shiny
- Suche ich: Glumanda - Shiny (2/2), Glurak - Shiny (1/1)

Ohne Haken nachher:

- Habe ich: 2x Glumanda Shiny, 1x Glurak Shiny
- Suche ich: Glumanda - Shiny (2/3)

**Beispiel 2: Mehrere Zeilen**

Vorher:

- Habe ich: 2x Glumanda Shiny, eines davon ist ein 100er
- Suche ich: Glumanda - Shiny (2/2), Glumanda - Shiny [100er] (1/1)

Wird das 100er-Glumanda entwickelt, bietet das Programm beide Zeilen an. Wird
das andere Glumanda entwickelt, nur die erste, denn dieses Exemplar erfuellt
die Anforderung "100er" nicht.

**Beispiel 3: Zielzeile gibt es schon**

Gibt es in "Suche ich" bereits "Glurak - Shiny (0/1)", wird beim Hochziehen
deren Anzahl erhoeht: "Glurak - Shiny (1/2)". Es entsteht keine zweite Zeile.

**Beispiel 4: Keine passende Suche**

Erfuellt das Exemplar keine Zeile in "Suche ich", wird nur das Exemplar
umgestellt. Es erscheint keine Auswahl.

## Zeilen in "Habe ich" aendern oder loeschen

Antippen einer Zeile in "Habe ich" oeffnet "Exemplar bearbeiten":

- **Merkmale aendern** gilt fuer die ganze Zeile. Soll nur eines von drei
  Exemplaren geaendert werden (z.B. eines ist ein 100er), wird die Anzahl um 1
  verringert und eine neue Zeile mit den geaenderten Merkmalen angelegt.
- Ergeben die geaenderten Merkmale eine Zeile, die es schon gibt, fragt das
  Programm nach, z.B. "Diese Zeile: 2x, gefundene Zeile: 3x, zusammen: 5x.
  Zusammenfuehren?".
- **Anzahl aendern** und **Loeschen** funktionieren wie in "Suche ich":
  Anzahl 0 entfernt die Zeile nach Rueckfrage.
- Die Zaehler in "Suche ich" werden einfach neu berechnet; Vorschlaege sind
  nicht noetig. Beispiel: Wird beim Glumanda der Glueck-Haken entfernt, zaehlt
  es fuer "Glumanda - Shiny [Glueck]" nicht mehr, fuer "Glumanda - Shiny"
  weiterhin.

- **Entwickeln** siehe "Pokemon entwickeln".

## Tauschliste erstellen

Unter "Import/Export", Registerkarte "Meine Tauschliste", erzeugt das
Programm beim Oeffnen eine Tauschliste als Text. Der Button "Kopieren" legt
sie in die Zwischenablage, z.B. zum Einfuegen in einen Messenger. Ist die
Liste leer, ist der Button gesperrt und ein Hinweis erklaert den Grund.

Gesuchte Pokemon ("1:1" und "fehlt") sind nach Region gegliedert. Was man
abgeben kann, steht gesammelt am Ende unter einer Trennlinie, ohne
Gliederung nach Region. Innerhalb jedes Abschnitts wird nach Dex-Nummer
sortiert. Die Liste ist fuer Menschen lesbar und kann von anderen Spielern in
ihr Programm eingelesen werden:

```
Kanto 1:1:
- #4 Glumanda - Shiny (1/3)
- #6 Glurak - Shiny

Kanto fehlt:
- #1 Bisasam - Shiny
- #4 Glumanda - Shiny [100er]
- #150 Mewtu - Shiny

Johto fehlt:
- #155 Feurigel - Shiny

--------------------

Abzugeben:
- #2 Bisaknosp - Shiny
- #158 Karnimani
```

Die Angabe "(1/3)" bedeutet: 1 passendes Exemplar in "Habe ich", 3 gesucht.
Bei einer einzelnen, noch offenen Suche entfaellt die Angabe. Angaben in eckigen Klammern
wie "[Glueck]" oder "[100er]" zeigen, welche Anforderung nur der eigene
Zaehler beruecksichtigt. Fuer den Tauschpartner sind sie ohne Bedeutung, da
Glueck und Werte erst durch den Tausch entstehen.

Vollstaendig erledigte Suchen erscheinen nicht in der Tauschliste.

Gesuchte Pokemon, deren Dex-Nummer in keiner Region liegt (z.B. Meltan und
Melmetal), stehen in einer eigenen Gruppe "Unbekannt" nach den uebrigen
Regionen.

Eintraege, die die Konsistenzpruefung nicht bestehen (z.B. wegen
beschaedigter Daten), erscheinen nicht in der Liste, sondern werden als
Fehler gemeldet.

## Tauschlisten anderer Spieler abgleichen

Unter "Import/Export", Registerkarte "Fremde Liste abgleichen", wird eine
fremde Tauschliste in das Textfeld eingefuegt (Button "Einfuegen" aus der
Zwischenablage oder direkt ins Feld) und mit "Abgleichen" einmalig mit den
eigenen Listen verglichen. Sie wird nicht gespeichert, weil sie beim
naechsten Tausch oft schon veraltet ist. Es darf die ganze Nachricht
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

Die Ergebnisseite zeigt nur Listen mit Treffern, jeweils mit Anzahl in der
Ueberschrift. Jeder Treffer besteht aus bis zu drei Zeilen:

```
Er:  - #4 Glumanda - Shiny
Ich: #4 Glumanda - Shiny [Glueck] (1/3)
     pruefen: Geschlecht beim Angebot nicht angegeben . 1:1 gewuenscht
```

"Er" ist seine Zeile genau so, wie sie in seiner Liste stand; so findet man
sie in seiner Nachricht wieder. "Ich" ist der eigene Eintrag in der
gewohnten Darstellung. Die dritte Zeile erscheint nur bei "pruefen" oder
"1:1 gewuenscht"; Treffer mit "pruefen" sind zusaetzlich am Rand markiert.

Zeilen der fremden Liste, die keiner bekannten Form zugeordnet werden
konnten, stehen am Ende in einem aufklappbaren Bereich "Nicht erkannte
Zeilen", jeweils mit Zeilennummer, Text und Grund. Erkannt werden derzeit nur
Zeilen mit Dex-Nummer (`#4 ...`).

## Abgleich zwischen Geraeten (z.B. Handy und PC)

Es gibt zwei Wege, die eigenen Daten zwischen Geraeten zu uebertragen:

1. **Ueber das WLAN**: Auf beiden Geraeten das Abgleich-Fenster oeffnen. Die
   Geraete finden sich selbst. Beide zeigen einen 6-stelligen Code an, der
   auf beiden Seiten bestaetigt werden muss. Das Programm empfiehlt die
   Richtung anhand der juengsten Aenderung. Beide Seiten muessen die Richtung
   bestaetigen, sie kann auch geaendert werden.
2. **Ueber eine Datei** (umgesetzt): unter Import/Export > "Eigene Listen"
   exportieren, die Datei auf das andere Geraet bringen und dort importieren
   (siehe "Eigene Listen exportieren und importieren").

Wichtig:

- Ein Abgleich ist immer vollstaendig. Die Daten des Zielgeraets werden
  ueberschrieben. Mit dem Haken "Vorher Backup anlegen" (standardmaessig
  aus) wird vorher eine Sicherung erstellt.
- Wurde seit dem letzten Abgleich auf **beiden** Geraeten etwas geaendert,
  warnt das Programm. Die Aenderungen auf der ueberschriebenen Seite gehen
  verloren.
- Waehrend des WLAN-Abgleichs bleibt der Bildschirm eingeschaltet. Das
  Fenster sollte bis zum Ende geoeffnet bleiben.

## Eigene Listen exportieren und importieren

Unter Import/Export, Registerkarte "Eigene Listen":

- **"Eigene Listen als Datei exportieren"** speichert "Habe ich", "Suche ich",
  "Habe ich ueber", die eigenen Formen und `abgleich_info.csv` in einer
  ZIP-Datei. Die Dateiauswahl des Systems schlaegt einen Namen wie
  `PoGoExchange_2026-11-26_15-30.zip` vor; Speicherort frei waehlbar, auf dem
  Handy z.B. "Downloads" oder ein Cloud-Speicher. Damit lassen sich die Daten
  auf ein anderes Geraet uebertragen und ausserhalb des Geraets sichern.
- **"Eigene Listen aus Datei importieren"** ersetzt alle eigenen Listen durch
  die der Datei. Vorher prueft das Programm, ohne etwas zu veraendern:
  - ob es ein Paket dieses Programms ist,
  - ob es aus derselben oder einer aelteren Programmversion stammt (Version
    der Nutzdaten); Pakete aus einer neueren Version werden abgelehnt mit dem
    Hinweis, zuerst das Programm zu aktualisieren,
  - ob sich alle Dateien fehlerfrei laden lassen.
  Danach folgt eine Rueckfrage mit Zusammenfassung (Datum, Anzahl der Zeilen).
  Bei "Ja" wird der aktuelle Stand gesichert, die Dateien ersetzt und alles
  neu geladen. Aeltere Pakete durchlaufen dabei die ueblichen Schritte beim
  Laden, z.B. die Uebernahme eigener Formen, die es inzwischen offiziell gibt.
- "Letzte Aenderung" wird aus dem Paket uebernommen, damit beide Geraete
  denselben Stand und denselben Zeitstempel haben.

Die Nutzdaten sind nicht enthalten; sie gehoeren zum Programm.

## Updates der Nutzdaten

Mit neuen Versionen des Programms koennen neue Pokemon und Formen
hinzukommen. Eigene Formen, die das Update ebenfalls enthaelt (gleiches
Pokemon, gleicher Form-Text und gleiche Region), werden beim Start
automatisch durch die offizielle Form ersetzt:

- Alle Eintraege in "Habe ich", "Suche ich" und "Habe ich ueber" werden auf
  die offizielle Form umgestellt. Entstehen dabei doppelte Zeilen, werden sie
  zusammengefuehrt.
- Die eigene Form wird entfernt.
- Vorher wird eine Sicherung angelegt, aber nur, wenn tatsaechlich etwas
  uebernommen wird.
- Ein Hinweis nennt jede uebernommene Form, z.B. "Die eigene Form 'Icognito
  ?' gibt es jetzt offiziell".
- Ist eine der Listen wegen Fehlern gesperrt, wird nichts uebernommen, bis
  sie fehlerfrei geladen wird.

Dass ein Update eine offizielle Form entfernt, ist bisher nicht vorgekommen
und noch nicht behandelt (vorgesehen: siehe "Archivieren statt Loeschen").

## Regeln fuer Formen

Damit Formen einheitlich bleiben, gelten fuer die Nutzdaten und sinngemaess
fuer eigene Formen diese Regeln:

- **Eine Form** ist, was dauerhaft am Exemplar haengt und kein Merkmal ist:
  Regionalformen, Icognito, Flabebe-Familie, Vivillon-Familie, Burmy, Formeo,
  Deoxys, Rotom, Giratina, Kyurem, Coiffwaff, Irrbis, Wolwerock usw.
- **Keine Form** sind Mega-Entwicklungen, Protomorphosen, Gigadynamax,
  Kostueme (auch Pikachu mit Muetzen), Geschlechtsunterschiede (z.B.
  Quabbel) und reine Kampf- oder Zeitformen (z.B. Zen-Modus, Durengard).
- **Benennung:** offizieller deutscher Formtext ohne Pokemon-Namen, z.B.
  "Rotbluetler", "Groesse S", "Westliches Meer". Leer bleibt der Formtext nur,
  wenn die Standardform offiziell "Normalform" heisst oder keinen Namen hat;
  benannte Standardformen behalten ihren Namen (z.B. Giratina "Wandelform").
- **Regionalformen:** Region in der Spalte `Region`, Formtext leer.
- **Shiny verfuegbar** nur bei sicherem Beleg; Kostuem-Shinys zaehlen nicht.
- Formen werden nur fuer Pokemon angelegt, die es in Pokemon GO gibt; andere
  bekommen nur eine leere Grundform.

## Wo die Daten liegen

Die persoenlichen Daten liegen im Benutzerordner der jeweiligen Plattform:

| Plattform | Datenordner                                   |
|-----------|-----------------------------------------------|
| Linux     | `~/.local/share/PoGoExchange`                 |
| Windows   | `%LOCALAPPDATA%\PoGoExchange`                 |
| Android   | `/data/user/0/com.GeritF.PoGoExchange/files/PoGoExchange` (privater Bereich der App, von aussen nicht sichtbar) |

Beim ersten Start legt das Programm dort leere Dateien mit Kopfzeile an
(`habe_ich.csv`, `suche_ich.csv`, `habe_ich_ueber.csv`, `eigene_formen.csv`).
Jede Aenderung wird sofort gespeichert.

Unter Android erfolgt der Zugriff von aussen ueber Export und Import (siehe
"Abgleich zwischen Geraeten").

### Daten mit Excel oder LibreOffice bearbeiten

Die Dateien koennen mit einer Tabellenkalkulation bearbeitet werden, solange
das Programm geschlossen ist. Dabei ist zu beachten:

- Beim Oeffnen alle Spalten als **Text** importieren. Sonst kann z.B.
  LibreOffice Zeitstempel als Datum erkennen und beim Speichern in einem
  anderen Format zurueckschreiben.
- Beim Speichern das Format "Text CSV" mit Zeichensatz **UTF-8** und
  **Semikolon** als Trennzeichen waehlen. Das ist der haeufigste Fehler: Wird
  mit Komma gespeichert, erkennt das Programm keine einzige Spalte. Es sperrt
  die Datei dann und meldet den Fehler, statt sie zu veraendern. Nach dem
  Korrigieren (z.B. erneut mit Semikolon speichern) laedt sie wieder.
- Eigene Spalten (z.B. "Notiz") werden vom Programm nicht uebernommen
  (siehe unten).

### Pruefung beim Laden

Beim Start prueft das Programm alle persoenlichen Dateien und zeigt
gefundene Probleme als Meldung an:

- **Fehlende Spalten** werden ergaenzt, **unbekannte Spalten** entfernt
  (z.B. nach einem Versionswechsel oder einer eigenen Spalte). Die Datei wird
  vorher gesichert, z.B. als `habe_ich.csv.bak_1`. Die Meldung nennt den
  Namen der Sicherung.
- **Keine einzige bekannte Spalte** (meist Komma statt Semikolon): Die Datei
  wird gesperrt und nicht veraendert, die Meldung weist auf das Trennzeichen
  hin.
- **Fehlende Kennungen** (GUID) werden neu erzeugt.
- **Doppelte Zeilen** in `habe_ich.csv` und `suche_ich.csv` (gleiches
  Pokemon, gleiche Form, gleiche Merkmale bzw. Anforderungen) werden zu einer
  Zeile zusammengefuehrt, die Anzahlen addiert. Auch hier wird die Datei
  vorher gesichert.
- **Ungueltige Werte** (z.B. `x` in einer Ja/Nein-Spalte) werden mit
  Zeilennummer gemeldet. Die betroffene Datei wird dann **gesperrt**: Die
  lesbaren Eintraege werden angezeigt, die Datei wird aber nicht veraendert,
  bis der Fehler behoben ist. So gehen keine Daten verloren.
- **Verweise**, die nicht zu den Nutzdaten passen (z.B. unbekanntes Pokemon),
  werden als Hinweis gemeldet. Die Datei bleibt bearbeitbar.

## Sicherungen

Sicherungen enthalten die persoenlichen Listen, die eigenen Formen und
`abgleich_info.csv`. Sie entstehen:

- manuell mit "Jetzt sichern" unter Verwaltung > Sicherungen,
- vor "+ Mehrere", wenn der Haken "Vorher Sicherung anlegen" gesetzt ist,
- automatisch vor jedem Wiederherstellen (mit dem aktuellen Stand),
- spaeter automatisch vor jedem Update der Nutzdaten und auf Wunsch vor einem
  Abgleich zwischen Geraeten.

Unter Verwaltung > Sicherungen stehen alle Sicherungen mit Datum und Uhrzeit,
die neueste zuerst. "Wiederherstellen" ersetzt nach einer Rueckfrage die
persoenlichen Dateien durch den Stand der Sicherung, laedt alle Daten neu und
zeigt die Hauptseite. Weil vorher der aktuelle Stand gesichert wird, laesst
sich auch eine Wiederherstellung rueckgaengig machen.

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

Folge der Regel 1: Wird eine Sicherung vom selben Tag wiederhergestellt,
ist sie danach verschwunden, weil die neuere Sicherung des aktuellen Stands
sie als "neueste des Tages" ersetzt. Das ist bewusst so; der wiederhergestellte
Stand ist ja jetzt der aktuelle.

Auf dem Handy liegen die Sicherungen im privaten Bereich der App. Sie
schuetzen vor Fehlbedienung, nicht vor dem Verlust des Geraets; dafuer ist
spaeter das Exportpaket gedacht.

---

# 2. Datenstrukturen

Alle Daten liegen als CSV-Dateien vor und koennen z.B. mit Excel oder
LibreOffice geoeffnet werden.

## Allgemeine Festlegungen

| Thema          | Festlegung                                                        |
|----------------|-------------------------------------------------------------------|
| Kodierung      | UTF-8 mit BOM                                                     |
| Trennzeichen   | Semikolon `;`                                                     |
| Zeilenumbruch  | Schreiben mit Windows-Zeilenumbruch (CRLF), Lesen mit CRLF oder LF |
| Erste Zeile    | Spaltenueberschriften                                             |
| GUID           | 36 Zeichen, Kleinbuchstaben, mit Bindestrichen                    |
| Wahrheitswerte | `1` = ja, `0` = nein                                              |
| Zeitstempel    | ISO 8601 in UTC, z.B. `2026-11-23T18:30:00Z`                      |
| Leere Felder   | Bedeuten "nicht gesetzt" bzw. "egal" (siehe jeweilige Spalte)     |

## Nutzdaten

### pokemon.csv

Eine Zeile pro Pokemon-Art.

| Spalte         | Inhalt                                                                              |
|----------------|-------------------------------------------------------------------------------------|
| Guid           | Eindeutige Kennung                                                                  |
| DexNr          | Nationale Pokedex-Nummer                                                            |
| Name           | Deutscher Name                                                                      |
| Kategorie      | Leer, `Legendaer`, `Mysterioes`, `Ultrabestie` oder `Paradox` (wird nicht angezeigt) |
| InGoVerfuegbar | `1` = sicher belegt, dass es in Pokemon GO erhaeltlich ist, sonst `0`; optional    |

`InGoVerfuegbar` wird gelesen, aber noch nicht genutzt (spaeter
voraussichtlich bei "+ Mehrere"). Fehlt die Spalte, gilt `0`. Die Kategorie ist
freier Text; neue Kategorien brauchen keine Programmaenderung.

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
werden (dann ist `ArchiviertAm` gesetzt). `Region` ist leer oder einer der
Namen aus `regionen.csv`.

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
| Anzahl        | Anzahl gleicher Exemplare, ab 1; leer = 1    |

Gleiche Exemplare (gleiches Pokemon, gleiche Form, gleiche Merkmale) stehen
in genau einem Datensatz.

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
| Anzahl        | Gesuchte Anzahl, ganze Zahl ab 1; leer = 1   |

Pro Zeile in "Suche ich" gibt es genau einen Datensatz. Der Fortschritt
wird nicht gespeichert, sondern aus `habe_ich.csv` berechnet.

### habe_ich_ueber.csv

| Spalte        | Inhalt                                       |
|---------------|----------------------------------------------|
| Guid          | Eindeutige Kennung des Eintrags              |
| PokemonGuid   | Verweis auf ein Pokemon (siehe oben)         |
| FormGuid      | Verweis auf eine Form (siehe oben)           |
| (Merkmale)    | Gemeinsame Merkmal-Spalten (siehe oben)      |

Geloeschte Eintraege werden in allen drei Listen sofort aus der Datei
entfernt. Eine Anzahl von 0 kommt in den Dateien nicht vor.

### abgleich_info.csv

Schluessel-Wert-Paare fuer den Abgleich zwischen Geraeten.

| Schluessel      | Inhalt                                     |
|-----------------|--------------------------------------------|
| LetzteAenderung | Zeitstempel des letzten Speicherns         |
| Geraetename     | Anzeigename dieses Geraets (folgt)         |
| LetzterAbgleich | Zeitstempel des letzten Abgleichs (folgt)  |

`LetzteAenderung` wird bei jedem erfolgreichen Speichern einer Liste oder der
eigenen Formen gesetzt, auch beim Loeschen. Fehlt die Datei, wird sie beim
Start leer mit Kopfzeile angelegt.

## Exportpaket (ZIP)

Das Exportpaket enthaelt dieselben persoenlichen Dateien wie eine Sicherung
(`habe_ich.csv`, `suche_ich.csv`, `habe_ich_ueber.csv`, `eigene_formen.csv`,
`abgleich_info.csv`) und zusaetzlich `paket_info.csv`:

| Schluessel       | Wert                                               |
|------------------|----------------------------------------------------|
| Format           | Version des Paketformats, derzeit `1`              |
| Erstellt         | Zeitpunkt der Erstellung (ISO 8601, UTC)           |
| NutzdatenVersion | `Version` aus `nutzdaten_info.csv` beim Erstellen  |

Die Nutzdaten sind nicht enthalten. Eingespielt wird nur, wenn die
`NutzdatenVersion` des Pakets gleich oder aelter ist als die des Programms
(als Zahl verglichen; nicht als Zahl lesbare Versionen muessen gleich sein).
Der spaetere WLAN-Abgleich soll dasselbe Paket uebertragen.

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

--------------------

Abzugeben:
- #<DexNr> <Eintragstext>
```

- "1:1" enthaelt gesuchte Pokemon, fuer die man dasselbe Pokemon im Tausch
  zurueckgeben wuerde (`EinsZuEins` = `1`).
- "fehlt" enthaelt die uebrigen gesuchten Pokemon.
- "abzugeben" enthaelt die Eintraege aus "Habe ich ueber".
- Jede Zeile aus "Suche ich" erscheint, solange sie nicht vollstaendig
  erledigt ist.
- `Erledigt` ist die Anzahl passender Exemplare in "Habe ich", hoechstens
  `Ziel`; `Ziel` ist die gesuchte Anzahl.
- `[<Zaehlbedingungen>]` enthaelt `Glueck` und/oder `100er`, durch Komma
  getrennt, und entfaellt, wenn keine gesetzt ist.
- `(Erledigt/Ziel)` entfaellt bei `(0/1)`.
- Gleiche Eintraege aus "Habe ich ueber" erscheinen nur einmal.
- Die Region einer Ueberschrift richtet sich nach der Dex-Nummer
  (`regionen.csv`), nicht nach der Spalte `Region` der Form.
- "1:1" und "fehlt" sind nach Region gegliedert. Reihenfolge der Regionen:
  Regionen mit Bereich nach Dex-Nummer, die Auffang-Region zuletzt. Leere
  Abschnitte und Regionen entfallen.
- "Abzugeben" steht ohne Region am Ende, getrennt durch eine Trennlinie aus
  20 Bindestrichen. Fehlen "1:1" und "fehlt", entfaellt die Trennlinie.
- Beim Einlesen gilt das letzte Wort einer Ueberschrift als Abschnitt; eine
  Region ist nicht noetig. Aeltere Listen mit "<Region> abzugeben:" werden
  deshalb weiterhin erkannt. Die Trennlinie wird wie anderer Text ignoriert.
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

## Zeilen in "Suche ich" und ihre Zaehler

Jede Zeile in "Suche ich" ist ein eigenstaendiges Ziel mit einer Anzahl.
Gespeichert wird nur die Anzahl. Der Fortschritt wird bei jeder Anzeige aus
"Habe ich" berechnet:

- Ziel = gesuchte Anzahl
- Erledigt = Anzahl der passenden Exemplare, hoechstens das Ziel

Ein Exemplar zaehlt in jeder Zeile, deren Anforderungen es erfuellt. Damit
sind die Zaehler bei unabhaengigen Zielen korrekt, z.B. bei "Von jedem ein
Shiny mit Glueck" und zusaetzlich "Von jedem ein 100er": Ein
Shiny-Gluecks-100er erfuellt beide Ziele. Eine neue Zeile veraendert keine
bestehende, niemand muss Ueberschneidungen von Hand herausrechnen.

Ein Ziel, das man in Teile zerlegen moechte ("5 Shinys, davon einer als
100er"), wird als zwei unabhaengige Ziele formuliert: "5x Shiny" und "1x Shiny
[100er]".

### Warum kein gespeicherter Erledigt-Status

In einer frueheren Planung bekam jeder gesuchte Exemplar-Platz einen eigenen
Datensatz mit Zeitstempel "erledigt". Damit liess sich ein aufgeteiltes Ziel
("4x Shiny + 1x Shiny 100er") exakt zaehlen, weil festgehalten wurde, fuer
welche Zeile ein Exemplar gezaehlt hat. Der Preis war hoch: Erledigt
markieren, Wiederoeffnen nach Aenderungen, Vorschlaege beim Loeschen und eine
Plausibilitaetspruefung, falls Erledigt-Status und "Habe ich" auseinanderlaufen.
Mit eigenstaendigen Zielen entfaellt all das, und das Verhalten ist leichter
nachzuvollziehen.

### Doppelte Datensaetze

Datensaetze mit gleichem Pokemon, gleicher Form und gleichen Anforderungen
bilden eine Zeile. Entstehen sie trotzdem (z.B. durch Bearbeiten der Datei von
Hand), addiert die Anzeige ihre Anzahlen, und beim naechsten Laden werden sie
zu einem Datensatz zusammengefuehrt.

### Entwickeln

Beim Entwickeln kann das Programm nicht wissen, fuer welche Ziele ein Exemplar
gedacht war. Es bietet deshalb alle Zeilen an, deren Anforderungen das
Exemplar vorher erfuellt hat, und der Anwender entscheidet per Haken. Das
Hochziehen verschiebt jeweils eine Einheit der Anzahl von der alten auf die
neue Form.

Entwicklungsketten sind bewusst nicht enthalten, um den Pflegeaufwand der
Nutzdaten gering zu halten. Die Zielform beim Entwickeln wird deshalb von
Hand gewaehlt.

## Warum "Habe ich ueber" keine Anzahl hat

Die Tauschliste soll zeigen, was grundsaetzlich abgegeben werden kann. Die
genaue Menge ist fuer den einzelnen Tausch irrelevant. Ausserdem bleibt ein
Eintrag nach einem Tausch oft einfach stehen, weil man meist noch weitere
Exemplare uebrig hat; bekommt man ein weiteres dazu, muss nichts gepflegt
werden. Eine Anzahl wuerde nur zusaetzlichen Pflegeaufwand bedeuten.

"Habe ich" und "Suche ich" haben dagegen eine Anzahl: In "Suche ich" ist sie
das Ziel, in "Habe ich" der Fortschritt.

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
verwendet. Spieler archivieren eigene Formen nicht selbst: Wird eine eigene
Form nicht mehr gebraucht, wird sie geloescht. Archiviert wird nur beim
Update der Nutzdaten.

## Loeschen und Erkennen von Aenderungen

Geloeschte Eintraege werden sofort aus der Datei entfernt. Die Dateien
enthalten keine Zeitstempel pro Zeile.

Fuer den Abgleich zwischen Geraeten muss trotzdem erkennbar sein, welches
Geraet zuletzt geaendert wurde, auch wenn die letzte Aenderung ein Loeschen
war. Dafuer gibt es genau einen Zeitstempel pro Geraet: `LetzteAenderung` in
`abgleich_info.csv`, gesetzt bei jedem erfolgreichen Speichern.

Eine fruehere Planung sah `GeaendertAm` und `GeloeschtAm` pro Zeile und eine
Aufbewahrungsfrist von 30 Tagen fuer geloeschte Eintraege vor. Da der Abgleich
immer vollstaendig ist, reicht ein Zeitstempel pro Geraet aus; Zeitstempel
pro Zeile waeren reiner Ballast.

Schlaegt das Schreiben von `abgleich_info.csv` fehl, bleibt die eigentliche
Aenderung trotzdem gespeichert; nur die Empfehlung beim Abgleich kann dann
ungenau sein.

## Sicherungen

Sicherungen liegen im Unterordner `sicherungen` des Datenordners, z.B.
`sicherungen/sicherung_2026-11-25_14-30-05.zip`. Der Zeitpunkt im Namen ist
in UTC und sekundengenau. Enthalten sind `habe_ich.csv`, `suche_ich.csv`,
`habe_ich_ueber.csv`, `eigene_formen.csv` und `abgleich_info.csv`, soweit
vorhanden; die Nutzdaten nicht, weil sie mit dem Programm kommen.

- **Zeitpunkt vom Aufrufer:** Wer eine Sicherung ausloest (Button, "+ Mehrere",
  Wiederherstellen), ermittelt den Zeitpunkt vorher und uebergibt ihn. Gibt es
  fuer diesen Zeitpunkt bereits eine Sicherung, passiert nichts, ohne Meldung.
  Ein zweiter Klick in derselben Sekunde wirkt so, als waere der Button nicht
  getroffen worden; eine Sicherung aus derselben Sekunde enthaelt ohnehin
  denselben Stand.
- **Aufraeumen** nach jeder neuen Sicherung in dieser Reihenfolge: pro
  Kalendertag die neueste, pro Kalendermonat die neuesten 3 Tage, insgesamt
  die 10 neuesten. Tage und Monate werden in UTC bestimmt.
- **Wiederherstellen:** Zuerst wird die gewaehlte Sicherung eingelesen, dann
  der aktuelle Stand gesichert (ohne Aufraeumen), dann werden die Dateien
  ersetzt und erst zum Schluss aufgeraeumt. So kann das Aufraeumen die
  gewaehlte Sicherung nicht vorher entfernen. Dateien, die in der Sicherung
  fehlen, werden entfernt und beim Laden leer neu angelegt. Danach wird
  `LetzteAenderung` gesetzt, weil eine Wiederherstellung eine Aenderung auf
  diesem Geraet ist.
- Geschrieben wird jeweils ueber eine temporaere Datei, wie bei den
  CSV-Dateien.

Vor einem Abgleich wird standardmaessig keine Sicherung erstellt, da
haeufige Abgleiche sonst viele Sicherungen erzeugen wuerden.

## Abgleich zwischen Geraeten

Ein Abgleich uebertraegt immer das vollstaendige Exportpaket und ersetzt auf
dem Zielgeraet alle eigenen Listen, wie der Import einer Datei, mit der
Rueckfrage "Wirklich ersetzen?". Die Nutzdaten werden nicht uebertragen; wie
beim Import gilt: Das Paket darf nicht aus einer neueren Programmversion
stammen, sonst koennten Verweise auf unbekannte Formen entstehen.

Empfehlung der Richtung: Jede Seite liefert ihren Wert `LetzteAenderung` aus
`abgleich_info.csv`. Die Seite mit dem juengeren Wert wird als Quelle
empfohlen. Liegen auf beiden Seiten Aenderungen nach
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
- **Windows-Zeilenumbruch beim Schreiben**: entspricht dem ueblichen
  CSV-Format. Gelesen werden auch Dateien mit Linux-Zeilenumbruch. Bei der
  verwendeten Bibliothek CsvHelper darf `NewLine` dafuer nur beim Schreiben
  gesetzt werden: Ist es beim Lesen gesetzt, wird ausschliesslich dieser
  Zeilenumbruch akzeptiert. Die Tauschliste (Text fuer Messenger) verwendet
  dagegen immer einen einfachen Zeilenvorschub.
- **Kopfzeile immer vorhanden**: Auch eine leere Liste wird mit Kopfzeile
  geschrieben, damit die Spalten in einer Tabellenkalkulation sichtbar sind.
- **Sicheres Speichern**: Es wird zuerst in eine temporaere Datei
  geschrieben, die dann die alte ersetzt. Bricht das Speichern ab (z.B. weil
  Android die App beendet), bleibt die alte Datei vollstaendig erhalten.
- **`.gitattributes`**: Im Repository sind CSV-Dateien von der
  Zeilenende-Umwandlung durch Git ausgenommen (`*.csv -text`), damit BOM und
  Zeilenumbrueche erhalten bleiben. Shell-Skripte behalten immer LF
  (`*.sh text eol=lf`).
- **Keine Anfuehrungszeichen in freien Texten**: Im Export begrenzen
  Anfuehrungszeichen die freien Texte, deshalb duerfen diese selbst keine
  enthalten.

## Laden der persoenlichen Dateien

Die Dateien werden tolerant gelesen, damit auch von Hand bearbeitete oder
aeltere Dateien funktionieren:

1. Rohdaten lesen und die Kopfzeile mit den erwarteten Spalten vergleichen.
   Die Reihenfolge der Spalten spielt keine Rolle.
2. Datensaetze in die erwartete Spaltenreihenfolge bringen, fehlende Spalten
   bleiben leer und erhalten damit ihren Standardwert.
3. Jeden Datensatz einzeln umwandeln. Fehlerhafte Datensaetze werden mit
   Zeilennummer gemeldet (Leerzeilen werden dabei nicht mitgezaehlt).
4. Nur `habe_ich.csv` und `suche_ich.csv`: Datensaetze derselben Zeile
   zusammenfuehren, die Anzahlen addieren und die ueberzaehligen Datensaetze
   entfernen.

Ergebnis:

- Gibt es fehlerhafte Datensaetze, wird die Datei gesperrt und nicht
  geschrieben, bis der Fehler behoben ist. Wuerde das Programm sie
  speichern, gingen die fehlerhaften Zeilen verloren.
- Wurde in der Kopfzeile keine einzige bekannte Spalte gefunden, wird die
  Datei gesperrt und nicht veraendert. Andernfalls wuerde das Programm alle
  Spalten als "fehlend" ergaenzen und den Inhalt als "unbekannte Spalte"
  verwerfen; die Daten waeren damit unbrauchbar (beobachtet bei einer Datei,
  die mit Komma statt Semikolon gespeichert wurde).
- Fehlten Spalten, gab es unbekannte Spalten, mussten Kennungen ergaenzt oder
  doppelte Zeilen zusammengefuehrt werden, wird die Datei zuerst als
  `<datei>.bak_<N>` gesichert (naechste freie Nummer) und dann im aktuellen Format neu
  geschrieben. Diese Sicherungen zaehlen nicht zu den regulaeren Sicherungen
  und werden nicht automatisch geloescht.
- Fehlt eine Datei, wird sie leer mit Kopfzeile angelegt.

Unbekannte Spalten werden nicht aufbewahrt, weil sie auch von einem
Versionskonflikt stammen koennen (z.B. eine Datei aus einer neueren
Programmversion). Die Sicherung erhaelt den urspruenglichen Inhalt.

Die mitgelieferten Nutzdaten werden nicht so geprueft: Sie sind Teil des
Programms und werden durch die Tests in `NutzdatenTests` abgesichert.

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
- Der Datenordner wird ueber `Environment.SpecialFolder.LocalApplicationData`
  ermittelt (siehe "Wo die Daten liegen"). Unter Android ergibt das
  `/data/user/0/com.GeritF.PoGoExchange/files/PoGoExchange` (gemessen auf dem
  Entwicklungsgeraet).

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
- Laden und Speichern der Daten uebernimmt `Services/Datenbestand.cs`, das
  tolerante Lesen der CSV-Dateien `CsvSpeicher.LesenMitPruefung`.
- Tests haengen nicht an den echten Nutzdaten: Wer einen `Datenbestand`
  laedt, nutzt `TestNutzdaten.Laden` mit festen Testdaten (die frueheren 11
  Beispiel-Pokemon in `PoGoExchange.Tests/Testdaten/`, eingebettet).
  `Datenbestand.Laden` nimmt dafuer optional eigene `Nutzdaten` entgegen.
  `NutzdatenTests` pruefen die echten Daten, aber nur ihre Struktur.
- Den Abgleich eigener Formen mit offiziellen beim Laden uebernimmt
  `Services/FormAbgleich.cs`, Export und Import der eigenen Listen
  `Services/Exportpaket.cs`. Die Dateiauswahl laeuft ueber `AppActions`
  (`DateiSpeichern`, `DateiOeffnen`), gesetzt in `MainView.axaml.cs` mit dem
  `StorageProvider` von Avalonia.
- Fuer eigene Tests mit Beispieldaten werden die CSV-Dateien einfach in den
  Datenordner kopiert. Beispieldaten fuer persoenliche Listen sind bewusst
  nicht im Repository, damit jeder Anwender mit leeren Listen beginnt.
- Die Fachlogik fuer "Suche ich" liegt in `Services/SuchZeilen.cs` (Zeilen
  und Zaehler), `Services/SuchBearbeitung.cs` (Ziele, Anlegen, Aendern,
  Anzahl) und `Services/Entwicklung.cs` (Entwickeln und Hochziehen). Die Zeilen von
  "Habe ich" (Schluessel fuer Gruppierung und Zusammenfuehren) liefert
  `Services/HabeZeilen.cs`. Anlegen und Aendern in "Habe ich" und "Habe ich
  ueber" uebernehmen `Services/HabeBearbeitung.cs` und
  `Services/UeberBearbeitung.cs`. "+ Mehrere" nutzt `Services/SammelSuche.cs`,
  Sicherungen `Services/Sicherungen.cs`, eigene Formen
  `Services/FormVerwaltung.cs`.
- Nach jeder Aenderung an den eigenen Formen wird der Katalog mit
  `Datenbestand.KatalogAufbauen` neu aufgebaut. Seiten lesen den Katalog
  deshalb immer ueber `Datenbestand.Katalog` und merken ihn sich nicht.
- Nach dem Wiederherstellen laedt `MainViewModel` alle Daten neu und ersetzt
  den Seitenstapel durch eine neue Hauptseite (`Navigation.Ersetzen`).
- Die Eingabebloecke fuer Merkmale gibt es zweimal, weil sie Verschiedenes
  bedeuten: `AnforderungenView` fuer "Suche ich" ("egal", "beliebig") und
  `ExemplarView` fuer "Habe ich" und "Habe ich ueber" ("unbekannt", "keins",
  "nicht beschrieben"). Beide werden von mehreren Seiten wiederverwendet.
- Seiten mit Pokemon-Auswahl halten die gewaehlte Form getrennt von der
  Markierung in der Liste. So bleibt die Auswahl erhalten, wenn das Suchfeld
  sie voruebergehend ausblendet. Die
  Aufbereitung fuer die Anzeige uebernimmt `Services/ListenAnzeige.cs`.
- Die Oberflaeche verwendet eine eigene, testbare Navigation
  (`ViewModels/Navigation.cs`) mit einem Seitenstapel. Seiten werden in
  `Views/MainView.axaml` ausdruecklich ihren ViewModels zugeordnet, nicht ueber
  den `ViewLocator` (keine Reflexion, sicher fuer optimierte Android-Builds).
- Plattformfunktionen (Beenden, Zwischenablage) stellt die Oberflaeche ueber
  `AppActions` bereit, damit die ViewModels keine Oberflaechentypen brauchen.
- `test/DatenAufsHandy.sh` kopiert die persoenlichen Daten vom PC in die
  Debug-App auf dem Handy (ueber `adb` und `run-as`). Das funktioniert nur mit
  Debug-Versionen und ueberschreibt die Daten auf dem Handy.
- Die Konsistenz der persoenlichen Listen (Verweise auf Pokemon und Formen)
  prueft `Services/Konsistenz.cs`; die Namen der Listen fuer Meldungen
  stehen in `ListenNamen`.
- Klassennamen weichen an zwei Stellen von den Dateinamen ab, weil eine
  C#-Klasse keine Eigenschaft mit ihrem eigenen Namen haben darf:
  `PokemonForm` (Datei `formen.csv`, Spalte `Form`) und `RegionBereich`
  (Datei `regionen.csv`, Spalte `Region`).
