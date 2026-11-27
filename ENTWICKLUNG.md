# PoGoExchange - Entwicklungsstand und Uebergabe

Diese Datei haelt fest, wo die Entwicklung steht, welche Regeln gelten und
was noch zu tun ist. Sie richtet sich an Gerit und an eine KI, die die
Entwicklung in einem neuen Chat fortsetzt. Was das Programm tut und warum,
steht in der `README.md`; diese Datei wiederholt das nicht, sondern verweist
darauf.

Stand: 2026-11-27, 310 Tests gruen.

## Inhalt

1. Neuen Chat starten
2. Aktueller Stand
3. Verworfene Entscheidungen
4. Code-Konventionen
5. Arbeitsweise (Anweisungen fuer die KI)
6. Stolpersteine und Erfahrungen
7. Umgebung und Werkzeuge
8. Glossar
9. ToDo-Liste

---

## 1. Neuen Chat starten

1. Aktuellen Stand als Archiv erzeugen (nur committete Dateien):

   ```bash
   cd ~/DEV/3_PoGoExchange
   git archive --format=zip -o ~/PoGoExchange.zip HEAD
   ```

2. Im neuen Chat das Archiv hochladen und etwa so beginnen:

   Hinweis: `git archive HEAD` enthaelt nur Committetes. Fuer den aktuellen
   Arbeitsstand stattdessen:

   ```bash
   cd ~/DEV/3_PoGoExchange
   rm -f ~/PoGoExchange_arbeitsstand.zip
   git ls-files -co --exclude-standard | zip -q ~/PoGoExchange_arbeitsstand.zip -@
   ```

   > Wir entwickeln PoGoExchange weiter. Lies bitte zuerst `ENTWICKLUNG.md`
   > und `README.md` aus dem Archiv und halte dich an die dort beschriebene
   > Arbeitsweise. Danach machen wir mit [naechster Schritt] weiter.

3. Die KI soll Code-Aenderungen immer am Stand aus dem Archiv ausrichten,
   nicht an Annahmen.

## 2. Aktueller Stand

### Phasen

| Phase | Inhalt                                   | Stand                          |
|-------|------------------------------------------|--------------------------------|
| 1     | Grundlagen, Toolchain, Projektaufbau     | fertig                         |
| 2     | Fachlogik (Services, Tests)              | fertig                         |
| 3     | Oberflaeche                              | fertig (siehe Bauschritte)     |
| 4     | Daten und Plattformen                    | weitgehend (Nutzdaten, Export) |
| 5     | Abgleich zwischen Geraeten (WLAN)        | offen                          |
| 6     | Windows-Build, Verteilung                | offen                          |

### Bauschritte der Oberflaeche (Phase 3)

| Schritt | Inhalt                                                      | Stand      |
|---------|-------------------------------------------------------------|------------|
| 1       | Geruest: Hauptseite, Navigation, Listen, Verwaltung         | fertig     |
| 2       | "Suche ich": Neu, Bearbeiten, Anzahl, Loeschen              | fertig     |
| 3       | "Habe ich" und "Habe ich ueber", Erhalten, Entwickeln       | fertig     |
| 4       | Import/Export: eigene Tauschliste, fremde Liste abgleichen  | fertig     |
| 5       | "+ Mehrere" (Sammel-Suche), Sicherungen mit Wiederherstellen | fertig     |
| 6       | Formverwaltung (eigene Formen)                              | fertig     |

### Bauschritt 4 (umgesetzt)

- Seite "Import/Export" mit den Registerkarten "Meine Tauschliste"
  (erzeugen, anzeigen, kopieren) und "Fremde Liste abgleichen" (einfuegen,
  abgleichen); Ergebnisseite "Abgleich" mit "Er:/Ich:"-Darstellung.
- Tauschlisten-Format: "1:1" und "fehlt" nach Region, "Abzugeben:" ohne
  Region gesammelt am Ende hinter einer Trennlinie.
- Die fremde Liste wird nicht gespeichert.

### Echte Nutzdaten (eingebaut)

- `pokemon.csv` mit 1025 Pokemon (Spalte `InGoVerfuegbar`, Kategorie
  `Paradox`), `formen.csv` mit 1262 Formen, `nutzdaten_info.csv` Version 2,
  Stand 2026-11-26. Erstellt in einem eigenen Chat nach Gerits
  `NUTZDATEN_PFLEGE.md` (nicht im Repository).
- Tests sind entkoppelt: `TestNutzdaten` mit den frueheren 11 Pokemon.
- Eigene Formen, die es offiziell gibt, werden beim Laden uebernommen
  (`FormAbgleich`). Mit Gerits Daten erprobt: Icognito "?" und "!" wurden
  umgestellt, Sicherung angelegt.

### Eigene Listen exportieren und importieren (eingebaut)

- Import/Export > Registerkarte "Eigene Listen". Paket = persoenliche Dateien
  + `paket_info.csv` (Format, Erstellt, NutzdatenVersion), keine Nutzdaten.
- Import ersetzt alles: Pruefung (Paket, Version gleich oder aelter,
  Probeladen in temporaerem Ordner), Rueckfrage, Sicherung, Ersetzen,
  Neuladen. "LetzteAenderung" kommt aus dem Paket.
- Auf Android wird der Dateiname ohne Endung vorgeschlagen, weil das System
  `.zip` selbst anhaengt.

### Was die Fachlogik bereits kann

Alle Dienste liegen in `src/PoGoExchange/PoGoExchange/Services/`:

- Laden und Speichern aller Dateien mit Pruefung, Sicherung und Meldungen
  (`Datenbestand`, `CsvSpeicher`)
- Zeilen und Zaehler in "Suche ich" (`SuchZeilen`), Anlegen und Aendern
  (`SuchBearbeitung`), "Habe ich" (`HabeZeilen`, `HabeBearbeitung`),
  "Habe ich ueber" (`UeberBearbeitung`), Entwickeln und Hochziehen
  (`Entwicklung`)
- Tauschliste erzeugen (`Tauschliste`) und einlesen (`TauschlistenImport`),
  Abgleich (`Abgleich`, `AngebotsVergleich`)
- "+ Mehrere" (`SammelSuche`, mit Region und Shiny-Verfuegbarkeit)
- Sicherungen als ZIP mit Aufraeumregeln und Wiederherstellen
  (`Sicherungen`); Reparatur-Kopien `.bak_N` beim Laden (`Datenbestand`)

- Eigene Formen anlegen, aendern, loeschen (`FormVerwaltung`) und beim
  Laden mit offiziellen abgleichen (`FormAbgleich`)
- Exportpaket der eigenen Listen (`Exportpaket`)
- Pruefung der freien Texte fuer Kostuem und Hintergrund (`Freitext`),
  gemeinsam genutzt von "Habe ich" und "Suche ich"

Alle Fachfunktionen haben eine Oberflaeche. Noch nicht gebaut sind das
Update der Nutzdaten fuer entfernte Formen und der Abgleich zwischen Geraeten.

## 3. Verworfene Entscheidungen

Diese Punkte wurden ausfuehrlich besprochen und bewusst verworfen. Sie
sollen nicht erneut vorgeschlagen werden, ausser Gerit fragt danach.

- **`ErledigtAm` pro Sucheintrag** (gespeicherter Erledigt-Status). Ersetzt
  durch eigenstaendige Ziele mit Anzahl; der Fortschritt wird aus "Habe ich"
  berechnet. Begruendung in der README ("Zeilen in 'Suche ich' und ihre
  Zaehler").
- **Ein Datensatz pro gesuchtem bzw. vorhandenem Exemplar.** Ersetzt durch
  Zeilen mit `Anzahl` in "Habe ich" und "Suche ich".
- **`GeaendertAm` und `GeloeschtAm` pro Zeile, 30 Tage Aufbewahrung.**
  Ersetzt durch sofortiges Loeschen und `LetzteAenderung` in
  `abgleich_info.csv`.
- **Plausibilitaetspruefung, Wiederoeffnen, Vorschlaege beim Loeschen.**
  Mit dem Wegfall von `ErledigtAm` ueberfluessig.
- **Anzahl in "Habe ich ueber".** Bewusst nicht; Eintraege bleiben nach
  einem Tausch meist stehen.
- **Entwicklungsketten in den Nutzdaten.** Pflegeaufwand zu hoch; Zielform
  wird von Hand gewaehlt.
- **Mehrere Zeilen auf einmal entfernen** (z.B. "+ Mehrere" zuruecknehmen).
  Nicht vorgesehen, auch nicht in der ToDo-Liste; erst wenn Anwender danach
  fragen. Bis dahin dient die Sicherung vorher.
- **Aufraeumen beim Wiederherstellen ausnehmen.** Wird eine Sicherung vom
  selben Tag wiederhergestellt, verschwindet sie danach (Regel "pro Tag die
  neueste"). Bewusst so gelassen.
- **Shiny-Verfuegbarkeit fuer Kostueme und Hintergruende.** Wird nur pro Form
  gefuehrt; eine Verknuepfung waere kaum zu pflegen. Darauf achten die
  Spieler selbst.
- **Eigene Formen durch Anwender archivieren.** Unbenutzte eigene Formen
  werden geloescht; `ArchiviertAm` dient nur dem Update der Nutzdaten.
- **Freie Texteingabe der Region bei eigenen Formen.** Auswahl aus den
  bekannten Regionen, weil der Import nur diese erkennt.
- **Mehrere Exemplare auf einmal entwickeln.** Es wird immer genau eines
  entwickelt.
- **Merkmale nur fuer einen Teil einer Zeile aendern.** Aenderungen gelten
  fuer die ganze Zeile; fuer Teilmengen Anzahl verringern und neue Zeile
  anlegen.
- **`ViewLocator` per Reflexion.** Seiten werden in `MainView.axaml`
  ausdruecklich zugeordnet.
- **Andere Frameworks:** Delphi FMX, Lazarus, Qt, PWA, MAUI, Flutter,
  Kotlin (siehe README, falls dort begruendet).

## 4. Code-Konventionen

### Allgemein

- C# mit .NET 10, Avalonia 12.1.6, CommunityToolkit.Mvvm, xUnit.
- Einrueckung 2 Leerzeichen, geschweifte Klammern bei `if`, `else`, `for`,
  `foreach` und `while` immer. Details zur Formatierung im naechsten
  Abschnitt.
- **Alle Quelltexte und Dokumente in reinem ASCII**, ohne BOM:
  - Umlaute in C#-Strings als `\u00fc` usw.
  - Umlaute in XAML als `&#xFC;` usw.
  - Kommentare auf Deutsch ohne Umlaute (ae, oe, ue, ss).
- Bezeichner auf Deutsch (z.B. `SuchZeile`, `HabeBearbeitung`).
- **Dateiname = Klassenname.** Kleine zugehoerige Typen (Records, Enums)
  duerfen in derselben Datei stehen.
- Neue Views in Rider ueber "Add > Avalonia UserControl" anlegen; dann nur
  die `.axaml` ersetzen.

### Formatierung

- Festgelegt in `src/PoGoExchange/.editorconfig` (fuer `.cs`, `.axaml`,
  `.csproj`, `.props`, `.slnx`). In Rider muss "Enable EditorConfig support"
  aktiv sein.
- In Rider nur mit dem Profil "Built-in: Reformat Code" formatieren. Der
  Code-Cleanup kann Code inhaltlich aendern (siehe Stolpersteine).
- Zeilenlaenge: harte Grenze 100 Zeichen, ab etwa 80 Zeichen nach Ermessen
  umbrechen.
- Folgezeilen stehen 2 Leerzeichen tiefer als die Zeile davor, nicht unter
  dem Ausdruck ausgerichtet (auch nach `+`, `&&`, `||`, `??`).
- Lange Zuweisungen: Umbruch nach `=`, `?? throw` auf eigener Zeile:

  ```csharp
  var pokemon =
    katalog.PokemonZuGuid(eintrag.PokemonGuid)
    ?? throw new InvalidOperationException(
      "Eintrag wurde vor dem Aufloesen nicht geprueft");

  private readonly Pokemon _glumanda =
    new() { Guid = Guid.NewGuid(), DexNr = 4, Name = "Glumanda" };
  ```

- Bedingungen: Bei mehreren booleschen Ausdruecken, bei gemischtem `&&` und
  `||` oder wenn die Zeile zu lang wird, darf die Bedingung zwischen Klammern
  auf eigenen Zeilen stehen. Beide Formen kommen im Code vor; die Form wird
  nicht nachtraeglich vereinheitlicht.

  ```csharp
  if (
    OperatingSystem.IsAndroid() &&
    name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
  )
  {
  ```

- Attribute: ein einzelnes Attribut mit kurzem Feld auf einer Zeile
  (`[ObservableProperty] private bool _shiny;`), sonst jedes Attribut und
  das Feld auf einer eigenen Zeile.
- Lange Aufrufe: Ist die Zeile zu lang, duerfen die Argumente und die
  schliessende Klammer auf eigenen Zeilen stehen (wie bei Bedingungen; nach
  Ermessen, wird nicht vereinheitlicht). Beispiel:
  `ListenTabViewModelTests.Suchtext_BautListeNeuAuf_MitUeberschriften`.
- `using` alphabetisch, `System` wird nicht vorgezogen
  (`dotnet_sort_system_directives_first = false`); unbenutzte `using`
  werden entfernt.
- Sammlungsausdruecke statt `new[] { ... }`, `new()` bei Sammlungen,
  `Array.Empty<T>()` und `.ToList()`: `["a", "b"]`, `[]`, `[.. quelle]`.
  Ausnahmen: `var x = new[] { ... }` (kein Zieltyp) und einzelne
  `Assert.Equal(new[] { ... }, ...)`, die Rider nicht umstellt (vermutlich
  mehrdeutig).
- Laengere Texte in XAML als Inhalt des `TextBlock` statt im Attribut
  `Text`; Umbrueche und Einrueckung erscheinen als einfache Leerzeichen
  (Beobachtung auf Android, Stichproben):

  ```xml
  <TextBlock TextWrapping="Wrap">
    Sicherungen enthalten die pers&#xF6;nlichen Listen und eigenen Formen.
    Alte Sicherungen werden automatisch aufger&#xE4;umt.
  </TextBlock>
  ```

- Pruefung: `test/Pruefung_Format.sh` meldet zu lange Zeilen, fehlende
  Klammern, BOM, Einrueckung, Nicht-ASCII-Zeichen und ausgerichtete
  Folgezeilen.

### Architektur

- Fachlogik in `Services` als statische Klassen, ohne Oberflaechentypen,
  vollstaendig per Test abgedeckt.
- ViewModels kennen keine Avalonia-Typen. Plattformfunktionen (Beenden,
  Zwischenablage) laufen ueber `AppActions`.
- Navigation ueber den eigenen Seitenstapel `ViewModels/Navigation.cs`;
  jede Seite erbt von `SeiteViewModel`.
- Neue Seiten brauchen einen Eintrag in den `DataTemplates` von
  `Views/MainView.axaml`.
- `PlatzhalterViewModel`/`PlatzhalterView` werden derzeit nicht benutzt,
  bleiben aber bewusst fuer eine spaetere Seite "Einstellungen". Nicht als
  toten Code entfernen.
- Compiled Bindings: jede View hat `x:DataType`.
- Seiten melden Ergebnisse ueber Rueckruf-Funktionen (`Action<...>`) an die
  aufrufende Seite, die dann ihre Listen aktualisiert und eine Rueckmeldung
  setzt.
- Auswahl in Listen mit Suchfeld: getrennte Eigenschaften fuer die Markierung
  in der Liste (`Auswahl`) und die gewaehlte Form (`Ziel`), damit das
  Suchfeld die Auswahl nicht loescht.
- Loeschen ueber Anzahl 0 immer mit Rueckfrage; in "Habe ich ueber" ein
  Loeschen-Button mit Rueckfrage.
- Sicherungen: Der Ausloeser ermittelt den Zeitpunkt (`Sicherungen.Zeitpunkt`)
  und uebergibt ihn; existiert dafuer schon eine Sicherung, passiert still
  nichts.

### Daten (CSV)

- UTF-8 mit BOM, Semikolon, Kopfzeile, Spaltennamen in ASCII.
- Wahrheitswerte `1`/`0`, Zeitstempel ISO 8601 in UTC mit `Z`.
- Schreiben mit CRLF ueber eine Temp-Datei; Lesen akzeptiert LF und CRLF.
- Anzahl ist in Dateien nie 0; geloeschte Zeilen werden entfernt.

### Nutzdaten

- Bei jeder Aenderung an `pokemon.csv` oder `formen.csv` den Wert `Version`
  in `nutzdaten_info.csv` erhoehen (und `Stand` setzen). Der Import von
  Exportpaketen prueft diese Version; ohne Erhoehung wuerde ein Paket aus
  einer neueren Programmversion nicht als solches erkannt.
- GUIDs nie neu erzeugen; vorhandene Zeilen nur aendern.

### Tests

- Jede neue Logik bekommt Tests. Tests pruefen gemessene Werte, nie fest
  vorgegebenen Ergebnistext ohne Messung.
- Tests mit Dateien nutzen einen eigenen temporaeren Ordner und raeumen ihn
  in `Dispose` auf.
- Tests, die einen `Datenbestand` laden, nutzen `TestNutzdaten.Laden`, nie
  die echten Nutzdaten. Die Testdaten haben `InGoVerfuegbar=1` fuer alle 11
  Pokemon; Tests, die Pokemon im Code anlegen und "+ Mehrere" nutzen, muessen
  `InGoVerfuegbar = true` setzen.
- Tests, die statische Werte in `AppActions` veraendern, gehoeren in die
  Sammlung `[Collection("Zwischenablage")]`, damit sie nicht parallel laufen.
- Testlauf ueber `test/Test_XUnit.sh` (Auswertung pro Klasse).

## 5. Arbeitsweise (Anweisungen fuer die KI)

- **Sprache:** Deutsch. Antworten kurz und vollstaendig; ausfuehrlich nur auf
  ausdruecklichen Wunsch.
- **Erst besprechen, dann bauen:** Vor jedem Bauschritt einen Entwurf mit
  konkreten Fragen vorlegen. Erst nach Gerits Antwort Code liefern.
- **Kleine Schritte:** Groessere Umbauten in Runden teilen, die jeweils mit
  uebersetzbarem Stand und gruenen Tests enden.
- **Jede Aenderung mit Dateipfad, ungefaehrer Zeilennummer und
  Suchbegriff** angeben. Neue oder stark geaenderte Dateien vollstaendig
  liefern. Mehrere Aenderungen an einer `.cs`-Datei als komplette Datei zum
  Herunterladen.
- **Am echten Stand arbeiten:** Bei Unsicherheit um ein aktuelles Archiv
  (`git archive`) bitten statt zu raten.
- **Erwartete Testanzahl** nach jeder Runde nennen und sorgfaeltig zaehlen
  (jeder `InlineData`-Fall einer Theory zaehlt einzeln).
- **Unsicherheiten kennzeichnen:** Besonders bei Avalonia-12-APIs, die sich
  gegenueber Avalonia 11 geaendert haben koennen, als Vermutung markieren
  und bei Bedarf vorher nachschlagen.
- **Fehler offen benennen:** Liegt ein Fehler im gelieferten Code oder in
  einer falschen Aussage, das klar sagen; Gerits Tippfehler als Nebendetail
  behandeln.
- **Diffs zur Auswertung** ueber `test/Diff_Export.sh` als Datei anfordern,
  nicht aus der Konsole.
- **Skripte:** Bash fuer Linux Mint, immer vollstaendig als Code-Abschnitt
  (keine Teilstuecke zum Einfuegen). Ausgaben mit Ueberschriften
  `=== ... ===` und jedem Wert als `Schluessel: Wert`; leere Ergebnisse
  ausdruecklich melden.
- **Diagnose:** Pro Antwort nur Schritte, die unabhaengig von offenen
  Ergebnissen sinnvoll sind.
- **README** nach jedem abgeschlossenen Bauschritt aktualisieren;
  **diese Datei** bei Aenderungen an Stand, Konventionen oder ToDos.
- **Doku zuletzt:** README und diese Datei erst in einem eigenen Schritt
  aendern, nachdem Gerit die Code-Aenderungen uebernommen hat und die Tests
  gruen sind. Vorher Doku-Aenderungen hoechstens als Vorschau nennen.
- **Doku als ganze Dateien:** Geaenderte Dokumente immer vollstaendig zum
  Herunterladen bereitstellen, keine Anweisungen der Art "in Zeile x
  folgendes aendern".
- Gerit committet selbst; die KI schlaegt den Zeitpunkt vor.

## 6. Stolpersteine und Erfahrungen

- **Avalonia 12, Zwischenablage:** `SetTextAsync` und `TryGetTextAsync`
  (statt `GetTextAsync`) sind Erweiterungsmethoden; noetig ist
  `using Avalonia.Input.Platform;`.
- **`InitializeComponent` fehlt:** Dateiname, `x:Class` und Klassenname
  passen nicht zusammen (z.B. Tippfehler oder Gross-/Kleinschreibung).
- **CSV mit Komma gespeichert** (z.B. LibreOffice): Wird jetzt gesperrt
  statt "repariert". Wiederherstellung aus der `.bak_N`, die vor der ersten
  Aenderung entstand.
- **NumericUpDown:** Ein versehentlicher Klick auf "Pfeil runter" setzt 0;
  deshalb die Rueckfrage bei Anzahl 0.
- **Test-Hilfsmethoden fuer Rohzeilen** muessen neue Spalten mitfuehren,
  sonst landen Werte in der falschen Spalte. Auch `CsvSpeicherTests` nutzt
  `PokemonMap` als Beispiel: Neue Spalten in Maps aendern dort die erwartete
  Kopfzeile.
- **Neue Spalten in Nutzdaten** als `.Optional()` mappen, sonst scheitert das
  Lesen aelterer Dateien (Testdaten) an der fehlenden Kopfzeile.
- **Sichtbarkeit fuer Tests:** Das Testprojekt sieht nur `public`-Mitglieder
  (kein `InternalsVisibleTo`). Methoden, die ein Test direkt aufruft, muessen
  `public` sein; nur intern genutzte bleiben `internal` oder `private`.
- **Android, Speichern-Dialog:** Android haengt die Dateiendung selbst an
  ("abc.zip.zip"). Deshalb wird dort der Name ohne `.zip` vorgeschlagen
  (`OperatingSystem.IsAndroid()` in `MainView.axaml.cs`). Eine vorhandene
  Datei wird dabei nicht ueberschrieben.
- **Testdaten nie mit den echten Nutzdaten ueberschreiben.**
  `PoGoExchange.Tests/Testdaten/` enthaelt bewusst die 11 Test-Pokemon; ein
  Schutztest meldet "Testdaten wurden ueberschrieben". Einmalige Skripte (z.B.
  das Kopieren der Testdaten) nicht erneut ausfuehren.
- **Inkrementeller Build und eingebettete Ressourcen:** Wird eine Datei durch
  eine mit aelterem Zeitstempel als die fertige DLL ersetzt (`cp -p`,
  Dateimanager mit erhaltenem Datum), bettet der inkrementelle Build sie nicht
  neu ein; Tests laufen dann mit dem alten Inhalt. Frisch heruntergeladene
  Dateien sind unkritisch. `test/Test_XUnit.sh` fuehrt deshalb vor jedem
  Testlauf `dotnet clean` aus (abschaltbar mit `SAUBER=0`). (Gemessen bei den
  Testdaten.)
- **Rider, Code-Cleanup:** Beim Formatieren ging in `SammelSucheTests` die
  Zuweisung `InGoVerfuegbar = true` verloren; 6 Tests lieferten 0 Eintraege.
  Ob Rider oder eine versehentliche Bearbeitung die Ursache war, ist nicht
  geklaert. Deshalb nur "Reformat Code" verwenden und danach inhaltliche
  Aenderungen mit `git diff -w --ignore-blank-lines` pruefen.
- **Rider und `.editorconfig`:** Unbekannte Schluessel meldet Rider
  ("Property ... not supported"). Ob ein Schluessel wirkt, zeigt der
  Code-Style-Dialog mit einem Verweis auf die `.editorconfig`. Wirksam:
  `resharper_csharp_wrap_lines`,
  `resharper_csharp_align_multiline_binary_expressions_chain`. Nicht
  unterstuetzt: `resharper_csharp_align_multiline_binary_expr`.
- **Git, Zwischenstand vormerken:** `git add -A` (grosses A) legt den Stand
  in den Index; danach zeigt `git diff` nur die neuen Aenderungen.
- **`Assert.All` bei leerer Liste** ist immer erfuellt. Wo Eintraege erwartet
  werden, vorher pruefen, dass die Liste nicht leer ist, mit einer Meldung,
  die im Testprotokoll "keine Daten" von "falsche Daten" unterscheidet
  (Beispiel: `SammelSucheTests.Vorlage_WirdVollstaendigUebernommen` mit
  `Assert.True(... .Count > 0, "Keine Eintraege angelegt ...")`). Die
  Meldung erscheint in `Test_XUnit.sh` unter "Fehlermeldung" (geprueft mit
  einem Gegentest).
- **`Test_XUnit.sh` baut Desktop und Android nicht mit.** Aendert sich etwas
  in `PoGoExchange.Desktop` oder `PoGoExchange.Android` (auch durch einen
  Cleanup, z.B. entfernte `using`), zusaetzlich `test/Build_Plattformen.sh`
  ausfuehren.
- **Rider "Fix in solution":** Die Umstellung auf Sammlungsausdruecke liess
  an einigen Stellen ungluecklich verteilte Zeilen zurueck (z.B. `:` allein
  auf einer Zeile, Leerzeile mitten im Ausdruck). Danach den Diff
  (`test/Diff_Export.sh`) auf solche Stellen durchsehen.
- **Rider:** Drag-and-Drop zwischen Projekten erzeugt Verweise statt
  Verschiebungen (mit `git mv` verschieben). "Unload Project" blendet nur aus.
- **Debug-Start auf Android** dauert lange; der Start ueber das App-Symbol
  ist schnell.
- **Neue `ApplicationId`** auf Android bedeutet neue App mit eigenem,
  leerem Datenordner.

## 7. Umgebung und Werkzeuge

- Entwicklungsrechner: Linux Mint 22.3, .NET SDK 10 in `~/.dotnet`, JDK 21,
  Android-SDK in `~/Android/Sdk`, Rider mit "Rider Android Support" und
  "AvaloniaRider". Einrichtung: `install/Entwicklungsumgebung_Linux.md`
  (Stand vor Testprojekt und spaeteren Erweiterungen).
- Repository: `~/DEV/3_PoGoExchange`, GitHub `GeritF/PoGoExchange` (SSH).
- Projekte unter `src/PoGoExchange/`: `PoGoExchange` (gemeinsam),
  `PoGoExchange.Desktop`, `PoGoExchange.Android`, `PoGoExchange.Tests`.
- Datenordner: Linux `~/.local/share/PoGoExchange`, Windows
  `%LOCALAPPDATA%\PoGoExchange`, Android
  `/data/user/0/com.GeritF.PoGoExchange/files/PoGoExchange`.
- Skripte im Ordner `test/`:
  - `Test_XUnit.sh`: Testlauf mit Auswertung pro Klasse.
  - `Build_Plattformen.sh`: baut Desktop- und Android-Projekt.
  - `Pruefung_Format.sh`: prueft die Formatierungsregeln (nur lesend).
  - `Diff_Export.sh`: schreibt alle Aenderungen nach
    `~/PoGoExchange_diff.txt` (ohne Parameter Vergleich mit dem Index, mit
    `head` mit dem letzten Commit); die Datei wird zur Auswertung
    hochgeladen.
  - `DatenAufsHandy.sh`: persoenliche Daten in die Debug-App kopieren
    (ueberschreibt sie).
- Nutzdaten: die echten (1025 Pokemon) in `PoGoExchange/Nutzdaten/`;
  Testdaten (11 Pokemon) in `PoGoExchange.Tests/Testdaten/`. Persoenliche
  Beispieldaten nur im Datenordner, nicht im Repository.
- Datenpflege der Nutzdaten geschieht in eigenen Chats nach Gerits
  Arbeitsanweisung `NUTZDATEN_PFLEGE.md`. Sie und die Prueflisten
  (`zu_pruefen_ingo.csv`, `zu_pruefen_formen.csv`) liegen lokal im Ordner
  `Nutzdaten_Pflege/` im Projektverzeichnis, sind aber per `.gitignore`
  ausgeschlossen (nicht im Repository, nicht im Arbeitsstand-ZIP). Regeln:
  GUIDs nie neu erzeugen, vorhandene Zeilen nur aendern; Shiny nur bei
  sicherem Beleg.

## 8. Glossar

| Begriff            | Bedeutung                                                    |
|--------------------|--------------------------------------------------------------|
| Habe ich           | "Fertig"-Liste: vorhandene Exemplare, Zeilen mit Anzahl      |
| Suche ich          | Ziele mit Anzahl; Fortschritt aus "Habe ich" berechnet       |
| Habe ich ueber     | Tauschbare Pokemon, ohne Anzahl, ohne Glueck und 100er       |
| Zeile              | Ein Datensatz; gleiche Merkmale bilden genau eine Zeile      |
| Ziel               | Waehlbare Form oder "Form beliebig" beim Anlegen             |
| Form beliebig      | Suche ohne bestimmte Form (`FormGuid` leer), nur "Suche ich" |
| 1:1                | Man wuerde dasselbe Pokemon im Tausch zurueckgeben           |
| Erhalten           | Erfolgreichen Tausch aus "Suche ich" nach "Habe ich" buchen  |
| Entwickeln         | Ein Exemplar auf eine andere Form umstellen                  |
| Hochziehen         | Beim Entwickeln eine Suchzeile auf die neue Form mitnehmen   |
| passt / pruefen    | Ergebnis des Merkmalsvergleichs mit einer fremden Liste      |
| Nutzdaten          | Pokemon, Formen, Regionen; werden mit dem Programm geliefert |
| + Mehrere          | Viele Suchzeilen auf einmal anlegen (intern `SammelSuche`)   |
| Eigene Listen      | Export/Import aller eigenen Daten als Datei (Exportpaket)    |
| Sicherung          | ZIP der eigenen Daten in `sicherungen/`, mit Aufraeumregeln  |
| .bak_N             | Kopie einer Datei vor einer Reparatur beim Laden             |

## 9. ToDo-Liste

### Als Naechstes

- Mit Gerit abstimmen: Windows-Build, WLAN-Abgleich.
- **`ViewLocator` entfernen (eigene Runde):** Er ist in `App.axaml` noch als
  Reflexions-Fallback registriert, obwohl die Seiten in `MainView.axaml`
  ausdruecklich zugeordnet werden. Die KI erklaert vorher, was das Entfernen
  bewirkt, und prueft, ob irgendwo ein ViewModel ohne eigene Vorlage
  angezeigt wird.

### Spaeter

- Update der Nutzdaten fuer offiziell entfernte Formen (archivieren und nach
  `eigene_formen.csv` uebernehmen). Bisher wurde nie eine offizielle Form
  entfernt; erst bauen, wenn der Fall eintritt.
- WLAN-Abgleich: ersetzt auf dem Zielgeraet alles (wie der Import), mit
  Rueckfrage "Wirklich ersetzen?"; gleiche Paket- und Versionsregeln.
- Abgleich zwischen Geraeten per WLAN (Phase 5), vorher Testprogramm fuer
  Ports, Berechtigungen und Broadcast.
- Windows-Build und Verteilung (Phase 6).
- `install/Entwicklungsumgebung_Linux.md` auf den aktuellen Stand bringen
  (Testprojekt, CsvHelper, Skripte).

### Sehr viel spaeter

- **Kopf- und Fusstext** fuer die eigene Tauschliste (z.B. Trainername,
  Freundescode); erster Punkt unter "Einstellungen".
- **"Teilen"** der eigenen Tauschliste auf Android (Teilen-Menue, z.B.
  WhatsApp).
- **Button "Erhalten" direkt in der Liste "Suche ich"** an jeder Zeile.
- **Button "Erhalten" im Abgleich-Ergebnis** bei "Er hat, was ich suche".
- **Fremde Listen ohne Dex-Nummer erkennen:** Spieler ohne die App
  schreiben Listen ohne `#Nummer`, mit abweichenden Leerzeichen oder anderen
  Trennern. Vorher festlegen, welche Zeilen erkannt werden sollen (z.B.
  ueber den Namen). Mindestens alle Zeilen mit Aufzaehlungszeichen, die nicht
  erkannt wurden, in einer einsehbaren Liste zeigen, statt nur "nichts
  erkannt".
- **Shiny-Verfuegbarkeit offizieller Formen selbst freischalten** (z.B. wenn
  ein Shiny erscheint, bevor ein Update der Nutzdaten kommt). Bis dahin: die
  Form einzeln ueber "+ Neu" suchen; "+ Mehrere" ueberspringt sie.
- **Eigene Pokemon** mit noch nicht vergebener Dex-Nummer anlegen. Bringt ein
  Update diese Dex-Nummer, ersetzt der offizielle Eintrag den eigenen
  (Verweise umstellen, eigene Formen nach Form und Region zuordnen), egal ob
  Name oder andere Angaben abweichen.
- **Nidoran beim Einlesen fremder Listen:** Die Namen enthalten die Symbole
  U+2640 und U+2642. Zusaetzlich Schreibweisen wie "Nidoran w", "Nidoran
  weiblich", "Nidoran (w)" bzw. "Nidoran m", "Nidoran maennlich",
  "Nidoran (m)" erkennen.
- Gestaltung der Oberflaeche ("schoen machen").

### Aufraeumen bei Gelegenheit

- Derzeit keine offenen Punkte (letzte Aufraeumrunde 2026-11-27).
