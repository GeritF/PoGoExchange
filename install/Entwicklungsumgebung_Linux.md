# PoGoExchange - Entwicklungsumgebung unter Linux einrichten

Diese Anleitung beschreibt, wie das Projekt PoGoExchange (Avalonia UI, C#, .NET)
auf einem Linux-Rechner gebaut, getestet und auf dem PC sowie auf einem
Android-Handy gestartet werden kann.

Was das Programm tut, steht in der `README.md`. Code-Konventionen,
Arbeitsweise und Entwicklungsstand stehen in der `ENTWICKLUNG.md`; diese
Anleitung wiederholt sie nicht.

## Getestete Umgebung

Getestet ist nur, was hier steht. Andere Distributionen oder Versionen
funktionieren vermutlich aehnlich, sind aber nicht geprueft.

| Komponente          | Version (Stand 27.11.2026)                      |
|---------------------|-------------------------------------------------|
| Betriebssystem      | Linux Mint 22.3 Cinnamon (Ubuntu 24.04 Noble)   |
| Kernel, Architektur | Linux 7.0.0-44-generic, x86_64                  |
| .NET SDK            | 10.0.404                                        |
| Android-Workload    | 36.1.118 (SDK-Band 10.0.400)                    |
| Java JDK            | OpenJDK 21.0.13.1                               |
| Avalonia Templates  | 12.1.6 (Pakete ebenfalls 12.1.6)                |
| Android SDK         | Plattform android-36, Build-Tools 36.0.0        |
| JetBrains Toolbox   | 3.9.2.89815                                     |
| JetBrains Rider     | 2026.3.0.1                                      |
| Werkzeuge           | git, perl, awk, zip (vorhanden, fuer `test/`)   |
| Testgeraet Android  | Samsung Galaxy S24, Android 17, One UI 9.0      |

Alle Befehle werden in einem normalen Terminal ausgefuehrt, wenn nicht anders
angegeben.

---

## 1. Ausgangszustand pruefen

```bash
echo "=== System ==="
echo "Mint: $(grep '^PRETTY_NAME=' /etc/os-release | cut -d= -f2)"
ub=$(grep '^DISTRIB_DESCRIPTION=' /etc/upstream-release/lsb-release 2>/dev/null | cut -d= -f2)
echo "Ubuntu-Basis: ${ub:-nicht ermittelbar}"
echo "Architektur: $(uname -m)"
echo "Vorhandenes dotnet: $(command -v dotnet || echo 'nichts gefunden')"
echo "Vorhandenes java: $(command -v java || echo 'nichts gefunden')"
```

Falls bereits ein `dotnet` unter `/usr/bin/dotnet` (also aus den
Paketquellen) vorhanden ist: vor dem Weitermachen klaeren, ob es entfernt
werden kann. Es kann sich mit der Installation aus Schritt 2 beissen.

Ein bereits vorhandenes Java stoert nicht, das JDK 21 wird daneben installiert.

## 2. .NET SDK installieren

Installation ueber das offizielle Skript von Microsoft nach `~/.dotnet`
(ohne root-Rechte).

```bash
cd ~
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0
```

> Randnotiz: Nicht das .NET-Paket aus den Paketquellen (apt) verwenden.
> Die Workloads, insbesondere Android, werden damit nach bisherigem
> Kenntnisstand nicht zuverlaessig unterstuetzt.

## 3. Java JDK 21 installieren

```bash
sudo apt update
sudo apt install openjdk-21-jdk-headless
```

## 4. Umgebungsvariablen setzen

Die Variablen kommen in `~/.profile` (nicht `~/.bashrc`), damit auch Programme
sie sehen, die ueber das Startmenue gestartet werden (z.B. Rider).

```bash
cat >> ~/.profile <<'EOF'

# .NET, Java und Android fuer Avalonia
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"
export JAVA_HOME="/usr/lib/jvm/java-21-openjdk-amd64"
export ANDROID_HOME="$HOME/Android/Sdk"
EOF
```

Optional, falls die .NET-Telemetrie abgeschaltet werden soll:

```bash
echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1' >> ~/.profile
```

Danach **einmal ab- und wieder anmelden**.

## 5. Pruefung

In einem neuen Terminal:

```bash
echo "=== Pruefung ==="
echo "dotnet Pfad: $(command -v dotnet || echo 'nichts gefunden')"
sdks=$(dotnet --list-sdks 2>/dev/null)
echo "dotnet SDKs: ${sdks:-nichts gefunden}"
echo "DOTNET_ROOT: ${DOTNET_ROOT:-nicht gesetzt}"
echo "JAVA_HOME: ${JAVA_HOME:-nicht gesetzt}"
echo "JAVA_HOME existiert: $( [ -d "$JAVA_HOME" ] && echo True || echo False )"
if [ -x "$JAVA_HOME/bin/java" ]; then echo "JDK Version: $("$JAVA_HOME/bin/java" -version 2>&1 | head -n1)"; else echo "JDK Version: nichts gefunden"; fi
echo "ANDROID_HOME: ${ANDROID_HOME:-nicht gesetzt}"
```

Erwartet: ein SDK 10.0.x, gesetzte Variablen, JDK-Version 21.

## 6. Android-Workload und Avalonia-Vorlagen

```bash
dotnet workload install android
dotnet new install Avalonia.Templates
```

Nach der Vorlagen-Installation erscheint eine lange Liste mit dem Hinweis,
einige Vorlagen wuerden "moeglicherweise nicht funktionieren", weil kein
Projekt im Pfad gefunden wurde. Das ist **kein Fehler**: Es betrifft nur
Element-Vorlagen (Window, UserControl usw.), die innerhalb eines Projekts
angelegt werden.

## 7. JetBrains Toolbox installieren

1. Download des Tarballs von jetbrains.com (Products > Toolbox > Download).
2. **Vor dem ersten Start** an einen festen Ort ausserhalb von Projektordnern
   entpacken, z.B. `~/Apps/jetbrains-toolbox`:

```bash
mkdir -p ~/Apps
tar -xzf ~/Downloads/jetbrains-toolbox-*.tar.gz -C ~/Apps
mv ~/Apps/jetbrains-toolbox-*/ ~/Apps/jetbrains-toolbox
```

3. Erst jetzt starten:

```bash
~/Apps/jetbrains-toolbox/bin/jetbrains-toolbox &
```

> Randnotiz: Die Toolbox installiert sich **nicht** an einen eigenen Ort,
> sondern laeuft direkt aus dem Ordner, in den sie entpackt wurde.
> Startmenue- und Autostart-Eintrag zeigen auf diesen Ordner. Deshalb nicht
> in einem Download- oder Projektordner entpacken und danach verschieben.
> Wurde sie doch verschoben: Toolbox beenden, einmal vom neuen Ort starten,
> dann aktualisiert sie ihre Eintraege selbst.

> Randnotiz: Nicht ueber Snap installieren, Linux Mint blockiert Snap
> standardmaessig.

> Beobachtung: Beim Start aus einem Terminal erscheint beim Beenden der
> Toolbox eine Absturzmeldung der Java-Laufzeit (SIGSEGV). Die Toolbox
> funktioniert trotzdem. Beim Start ueber das Startmenue sieht man davon
> nichts.

## 8. Rider installieren und Lizenz aktivieren

1. In der Toolbox **Rider** installieren.
2. Rider startet zunaechst mit einer 30-Tage-Testversion. Fuer private,
   nicht-kommerzielle Nutzung (Hobby, Lernen, Open Source) gibt es eine
   kostenlose Lizenz:
   - Help > Manage Licenses...
   - Option **Non-commercial use** waehlen, Bedingungen akzeptieren
   - mit einem JetBrains-Account anmelden
3. Hinweise zur nicht-kommerziellen Lizenz:
   - Die Uebertragung anonymer Nutzungsstatistiken kann nicht abgeschaltet
     werden.
   - Die Lizenz gilt jeweils ein Jahr und wird online verlaengert.

## 9. Rider-Plugins und Einstellungen

Unter File > Settings > Plugins > Marketplace installieren:

| Plugin                 | Zweck                                         |
|------------------------|-----------------------------------------------|
| AvaloniaRider          | XAML-Vorschau fuer Avalonia                   |
| Rider Android Support  | Android-Einstellungen, Geraeteauswahl, Start  |

Danach Rider neu starten. Erst mit dem Android-Plugin erscheint unter
File > Settings > Build, Execution, Deployment der Eintrag **Android**.
Dort eintragen (falls nicht schon automatisch erkannt):

- Android SDK: `/home/<BENUTZER>/Android/Sdk`
- JDK: `/usr/lib/jvm/java-21-openjdk-amd64`

Falls Rider das .NET SDK nicht findet: unter Settings > Build, Execution,
Deployment > Toolset and Build als .NET CLI den Pfad `~/.dotnet/dotnet`
eintragen.

Formatierung:

- Unter Settings > Editor > Code Style **"Enable EditorConfig support"**
  einschalten. Die Regeln stehen in `src/PoGoExchange/.editorconfig`.
- Zum Formatieren nur das Profil **"Built-in: Reformat Code"** verwenden,
  nicht den Code-Cleanup; dieser kann Code inhaltlich aendern. Einzelheiten
  und Erfahrungen stehen in `ENTWICKLUNG.md` (Abschnitte 4 und 6).

## 10. Repository klonen

Das Repository in einen **leeren bzw. noch nicht existierenden** Ordner
klonen.

Mit SSH (empfohlen, wenn ein SSH-Key bei GitHub hinterlegt ist):

```bash
mkdir -p ~/DEV
cd ~/DEV
git clone git@github.com:GeritF/PoGoExchange.git 3_PoGoExchange
```

Alternativ per HTTPS:

```bash
git clone https://github.com/GeritF/PoGoExchange.git 3_PoGoExchange
```

> Randnotiz: Bei HTTPS verlangt GitHub beim Push einen Personal Access Token
> statt des Passworts. Grafische Git-Programme koennen danach oft nicht
> fragen und melden dann sinngemaess "Benutzer nicht gesetzt". Mit SSH
> tritt das nicht auf. Wer schon andere Projekte per SSH nutzt, sollte alle
> Projekte gleich konfigurieren.

Falls noch nie auf diesem Rechner committet wurde:

```bash
git config --global user.name "NAME"
git config --global user.email "MAIL@ADRESSE"
```

## 11. Android SDK installieren

Das .NET-Build-System laedt das passende Android SDK selbst herunter.
Dafuer wird das Android-Projekt benoetigt, deshalb erst nach dem Klonen:

```bash
cd ~/DEV/3_PoGoExchange/src/PoGoExchange
dotnet build PoGoExchange.Android -t:InstallAndroidDependencies "-p:AndroidSdkDirectory=$HOME/Android/Sdk" "-p:JavaSdkDirectory=$JAVA_HOME" -p:AcceptAndroidSDKLicenses=True
```

Die Warnungen `XA5300` ("Android SDK Verzeichnis wurde nicht gefunden") und
`XARAT7001` (NullReferenceException) sind beim ersten Lauf zu erwarten, weil
das SDK-Verzeichnis zu diesem Zeitpunkt noch nicht existiert. Ob die
Installation geklappt hat, zeigt erst die Pruefung in Schritt 12.

## 12. Handy vorbereiten und pruefen

Am Handy:

1. Einstellungen > Ueber das Telefon > 7x auf "Build-Nummer" tippen
   (schaltet die Entwickleroptionen frei).
2. In den Entwickleroptionen **USB-Debugging** einschalten.
3. Handy per USB anschliessen, entsperren und als USB-Modus
   **Dateiuebertragung** waehlen (nicht "Nur laden").
4. Die Nachfrage "USB-Debugging zulassen?" bestaetigen.

Pruefung am PC:

```bash
sdk="$HOME/Android/Sdk"
echo "=== Android SDK ==="
echo "SDK-Ordner existiert: $( [ -d "$sdk" ] && echo True || echo False )"
pl=$(ls "$sdk/platforms" 2>/dev/null | tr '\n' ' ')
echo "Plattformen: ${pl:-nichts gefunden}"
bt=$(ls "$sdk/build-tools" 2>/dev/null | tr '\n' ' ')
echo "Build-Tools: ${bt:-nichts gefunden}"
echo "adb vorhanden: $( [ -x "$sdk/platform-tools/adb" ] && echo True || echo False )"
echo "=== Handy ==="
"$sdk/platform-tools/adb" kill-server >/dev/null 2>&1
"$sdk/platform-tools/adb" start-server >/dev/null 2>&1
dev=$("$sdk/platform-tools/adb" devices -l 2>/dev/null | tail -n +2 | grep -v '^$')
echo "Verbundene Geraete: ${dev:-nichts gefunden}"
```

Erwartet: Das Handy erscheint mit dem Status `device`.

> Randnotiz (Samsung): Die Funktion "Automatische Blockierung" unter
> Sicherheit und Datenschutz kann Befehle und Installationen per USB
> blockieren.

## 13. Projekt bauen und starten

### Komplette Solution bauen

```bash
cd ~/DEV/3_PoGoExchange/src/PoGoExchange
dotnet build PoGoExchange.slnx
```

Alle NuGet-Pakete (Avalonia, CommunityToolkit.Mvvm, CsvHelper, xUnit usw.)
stehen mit ihren Versionen zentral in `src/PoGoExchange/Directory.Packages.props`
und werden beim ersten Build automatisch geladen. Von Hand ist nichts zu
installieren.

### Desktop

In Rider die Solution `src/PoGoExchange/PoGoExchange.slnx` oeffnen, oben die
Run-Konfiguration **PoGoExchange.Desktop** waehlen und auf Run klicken.

### Android

In Rider die Run-Konfiguration **PoGoExchange.Android** waehlen, daneben das
Handy als Geraet auswaehlen und auf Run klicken.

Alternativ im Terminal (installiert und startet die App auf dem Handy):

```bash
cd ~/DEV/3_PoGoExchange/src/PoGoExchange
dotnet build PoGoExchange.Android -t:Run "-p:AndroidSdkDirectory=$HOME/Android/Sdk"
```

> Hinweis: Wird die App auf dem Handy geschlossen, haelt Android den Prozess
> oft noch eine Weile im Hintergrund. Rider zeigt die Sitzung dann weiter als
> laufend an. Zum Beenden den roten Stop-Button in Rider verwenden.

> Randnotiz: Der Debug-Start aus Rider dauert auf Android lange. Ist die App
> einmal installiert, startet sie ueber das App-Symbol schnell.

## 14. Tests ausfuehren

Die Fachlogik wird im Projekt `PoGoExchange.Tests` mit xUnit getestet:

```bash
cd ~/DEV/3_PoGoExchange
./test/Test_XUnit.sh
```

Das Skript bereinigt zuerst (`dotnet clean`), fuehrt dann alle Tests aus und
zeigt eine Zusammenfassung und eine Auswertung pro Testklasse. Erwartet:
"Tests erfolgreich: True" und die in `ENTWICKLUNG.md` genannte Anzahl.

Alternativ ohne Auswertung:

```bash
cd ~/DEV/3_PoGoExchange/src/PoGoExchange
dotnet test PoGoExchange.Tests
```

Der Testlauf baut Desktop- und Android-Projekt nicht mit. Nach Aenderungen,
die diese Projekte betreffen koennen (auch Views und gemeinsamer Code), dazu
`./test/Build_Plattformen.sh` ausfuehren.

## 15. Skripte im Ordner `test/`

Alle Skripte sind Bash-Skripte fuer Linux und lassen sich aus jedem Ordner
starten. Ihre Ausgaben folgen dem Muster `=== Ueberschrift ===` und
`Schluessel: Wert`.

| Skript                 | Zweck                                               | Braucht           |
|------------------------|-----------------------------------------------------|-------------------|
| `Test_XUnit.sh`        | Testlauf mit Auswertung pro Testklasse              | dotnet, perl      |
| `Build_Plattformen.sh` | baut Desktop- und Android-Projekt                   | dotnet            |
| `Pruefung_Format.sh`   | prueft Formatierungsregeln (nur lesend)             | awk               |
| `Diff_Export.sh`       | schreibt Aenderungen nach `~/PoGoExchange_diff.txt` | git               |
| `DatenAufsHandy.sh`    | kopiert persoenliche Daten in die Debug-App         | adb (Android SDK) |

Hinweise:

- `Test_XUnit.sh`: Im Skript schaltet `ALLE_TESTS=1` die Liste aller
  einzelnen Tests ein, `SAUBER=0` das vorherige `dotnet clean` aus.
- `Diff_Export.sh` ohne Parameter vergleicht mit dem Index (`git add -A`),
  mit `head` mit dem letzten Commit.
- `DatenAufsHandy.sh` funktioniert nur mit Debug-Versionen der App und
  **ueberschreibt** die Daten auf dem Handy.

---

## Anhang: Hinweise zur Arbeit am Projekt

### Ordnerstruktur

```
3_PoGoExchange/
  README.md                   Funktionen, Datenstrukturen, Begruendungen
  ENTWICKLUNG.md              Stand, Konventionen, Arbeitsweise, ToDo
  install/                    diese Anleitung (Archive werden ignoriert)
    Zusatz_fuer_Gerit/        tatsaechlich ausgefuehrte Schritte (siehe unten)
  test/                       Skripte fuer Tests, Builds, Pruefungen
  src/PoGoExchange/
    PoGoExchange.slnx         Solution
    Directory.Packages.props  Paketversionen (zentral)
    .editorconfig             Formatierungsregeln
    PoGoExchange/             gemeinsamer Code
      Models/ Services/       Fachlogik (ohne Oberflaechentypen)
      ViewModels/ Views/      Oberflaeche
      Nutzdaten/              mitgelieferte Daten (eingebettet)
    PoGoExchange.Android/     Android-Startprojekt
    PoGoExchange.Desktop/     Desktop-Startprojekt (Linux, Windows)
    PoGoExchange.Tests/       xUnit-Tests
      Testdaten/              feste Testdaten (nicht mit Nutzdaten ersetzen)
```

Die persoenlichen Daten liegen nicht im Repository, sondern im Datenordner
(Linux `~/.local/share/PoGoExchange`, siehe README "Wo die Daten liegen").

Die von der Vorlage zusaetzlich erzeugten Projekte fuer iOS und Browser sind
entfernt. iOS laesst sich unter Linux ohnehin nicht bauen.

### Neue Dateien anlegen

Neue Dateien in Rider per Rechtsklick auf das **Projekt** (nicht die
Solution) und Add anlegen.

> Randnotiz: Nicht per Drag and Drop in Rider verschieben. Rider legt dabei
> unter Umstaenden nur einen Verweis (`<Compile Include="..."><Link>`) in der
> `.csproj` an, waehrend die Datei am alten Ort bleibt.

### Projekte aus der Solution entfernen

Projekt im Solution Explorer markieren und die **Entf-Taste** druecken.

> Randnotiz: "Unload Project" entfernt das Projekt nicht, es blendet es nur
> lokal in Rider aus. Ein `dotnet build` der Solution baut es weiterhin.

### Grosse Dateien nicht committen

GitHub lehnt Dateien ueber 100 MB ab.

> Randnotiz: Downloads (z.B. das Toolbox-Archiv) nicht im Projektordner
> ablegen. Ist eine solche Datei einmal committet, hilft ein spaeterer
> Eintrag in `.gitignore` nicht mehr, dann muss der Commit zurueckgenommen
> werden. Die `.gitignore` enthaelt dafuer bereits:

```
.idea/
install/**/*.tar.gz
install/**/jetbrains-toolbox-*/
```

Git versioniert keine leeren Ordner. Ein leerer Ordner kommt erst ins
Repository, sobald eine Datei darin liegt.

### Ordner `install/Zusatz_fuer_Gerit`

Die Skripte dort sind die Schritte, die bei der ersten Einrichtung
tatsaechlich ausgefuehrt wurden (Stand 22.11.2026). Sie bleiben als Protokoll
erhalten und werden nicht an diese Anleitung angeglichen. Bekannte
Abweichung: `4_Register.sh` setzt kein `ANDROID_HOME`; die Einrichtung
funktionierte trotzdem. Ob die Variable noetig ist, ist nicht geprueft.
Massgeblich ist diese Anleitung.

### Noch nicht beschrieben

- Einrichtung und Build unter Windows
- Erstellen einer Release-Version (APK bzw. ausfuehrbare Datei zur Weitergabe)
