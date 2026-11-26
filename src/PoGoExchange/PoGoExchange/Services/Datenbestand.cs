using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

public sealed record LadeMeldung(string Datei, string Text, bool IstFehler);

public static class Dateinamen
{
  public const string Habe = "habe_ich.csv";
  public const string Suche = "suche_ich.csv";
  public const string Ueber = "habe_ich_ueber.csv";
  public const string EigeneFormen = "eigene_formen.csv";
  public const string AbgleichInfo = "abgleich_info.csv";
}

public static class Datenordner
{
  // Linux: ~/.local/share/PoGoExchange
  // Windows: %LOCALAPPDATA%\PoGoExchange
  // Android: /data/user/0/com.GeritF.PoGoExchange/files/PoGoExchange (gemessen)
  public static string Standard()
  {
    var basis = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    return Path.Combine(basis, "PoGoExchange");
  }
}

// Laedt Nutzdaten und persoenliche Daten und speichert Aenderungen.
// Fehlende Dateien werden mit Kopfzeile angelegt. Fehlende oder unbekannte
// Spalten werden angeglichen, die alte Datei vorher als .bak_N gesichert.
// Dateien mit ungueltigen Werten werden gesperrt und nicht ueberschrieben.
// Jedes erfolgreiche Speichern vermerkt die letzte Aenderung in abgleich_info.csv.
public sealed class Datenbestand
{
  // Schluessel in abgleich_info.csv
  internal const string SchluesselLetzteAenderung = "LetzteAenderung";

  private readonly HashSet<string> _gesperrt = new(StringComparer.OrdinalIgnoreCase);
  private readonly List<LadeMeldung> _meldungen = new();
  private List<SchluesselWert> _abgleichInfo = new();
  private Nutzdaten? _nutzdaten;

  private Datenbestand(string ordner)
  {
    Ordner = ordner;
  }

  public string Ordner { get; }
  public Katalog Katalog { get; private set; } = null!;
  public List<PokemonForm> EigeneFormen { get; private set; } = new();
  public List<HabeEintrag> Habe { get; private set; } = new();
  public List<SucheEintrag> Suche { get; private set; } = new();
  public List<UeberEintrag> Ueber { get; private set; } = new();
  public IReadOnlyList<LadeMeldung> Meldungen => _meldungen;
  // Die verwendeten Nutzdaten (mitgelieferte oder in Tests uebergebene)
  public Nutzdaten Nutzdaten => _nutzdaten ??= EingebetteteNutzdaten.Alle();

  // Zeitpunkt der letzten Aenderung auf diesem Geraet (fuer den Abgleich),
  // oder null, wenn noch nichts gespeichert wurde
  public DateTime? LetzteAenderung
  {
    get
    {
      var wert = _abgleichInfo
        .FirstOrDefault(e => e.Schluessel == SchluesselLetzteAenderung)?.Wert;
      if (string.IsNullOrEmpty(wert))
      {
        return null;
      }

      try
      {
        return ZeitstempelKonverter.Lesen(wert);
      }
      catch (FormatException)
      {
        return null;
      }
    }
  }

  public bool IstGesperrt(string datei)
  {
    return _gesperrt.Contains(datei);
  }

  // nutzdaten: ohne Angabe die mitgelieferten (Tests uebergeben feste Testdaten)
  public static Datenbestand Laden(string ordner, DateTime jetzt, Nutzdaten? nutzdaten = null)
  {
    Directory.CreateDirectory(ordner);
    var bestand = new Datenbestand(ordner) { _nutzdaten = nutzdaten };

    bestand.EigeneFormen = bestand.DateiLaden<PokemonForm, PokemonFormMap>(
      Dateinamen.EigeneFormen, _ => false);
    bestand.Habe = bestand.ListeLaden<HabeEintrag, HabeEintragMap>(Dateinamen.Habe, jetzt);
    bestand.Zusammenfuehren<HabeEintrag, HabeEintragMap>(
      Dateinamen.Habe, bestand.Habe, e => HabeZeilen.Schluessel(e));
    bestand.Suche = bestand.ListeLaden<SucheEintrag, SucheEintragMap>(Dateinamen.Suche, jetzt);
    bestand.Zusammenfuehren<SucheEintrag, SucheEintragMap>(
      Dateinamen.Suche, bestand.Suche, e => SuchZeilen.Schluessel(e));
    bestand.Ueber = bestand.ListeLaden<UeberEintrag, UeberEintragMap>(Dateinamen.Ueber, jetzt);
    bestand._abgleichInfo = bestand.DateiLaden<SchluesselWert, SchluesselWertMap>(
      Dateinamen.AbgleichInfo, _ => false);

    bestand.EigeneFormenAbgleichen(jetzt);
    bestand.KatalogAufbauen();
    bestand.KonsistenzMelden();
    return bestand;
  }

  // Liefert null bei Erfolg, sonst den Grund
  public string? HabeSpeichern()
  {
    return Speichern<HabeEintrag, HabeEintragMap>(Dateinamen.Habe, Habe);
  }

  public string? SucheSpeichern()
  {
    return Speichern<SucheEintrag, SucheEintragMap>(Dateinamen.Suche, Suche);
  }

  public string? UeberSpeichern()
  {
    return Speichern<UeberEintrag, UeberEintragMap>(Dateinamen.Ueber, Ueber);
  }

  public string? EigeneFormenSpeichern()
  {
    return Speichern<PokemonForm, PokemonFormMap>(Dateinamen.EigeneFormen, EigeneFormen);
  }

  // Ergaenzt fehlende Kennungen
  private List<T> ListeLaden<T, TMap>(string datei, DateTime jetzt)
    where T : ListenEintrag
    where TMap : ClassMap<T>, new()
  {
    return DateiLaden<T, TMap>(datei, eintrag =>
    {
      if (eintrag.Guid != Guid.Empty)
      {
        return false;
      }

      eintrag.Guid = Guid.NewGuid();
      return true;
    });
  }

  private List<T> DateiLaden<T, TMap>(string datei, Func<T, bool> nachbearbeiten)
    where TMap : ClassMap<T>, new()
  {
    var pfad = Path.Combine(Ordner, datei);
    if (!File.Exists(pfad))
    {
      CsvSpeicher.Schreiben<T, TMap>(pfad, Array.Empty<T>());
      return new List<T>();
    }

    CsvLeseErgebnis<T> ergebnis;
    try
    {
      ergebnis = CsvSpeicher.LesenMitPruefung<T, TMap>(pfad);
    }
    catch (Exception ex) when (
      ex is IOException or UnauthorizedAccessException or CsvHelperException)
    {
      _gesperrt.Add(datei);
      Fehler(datei, "Datei konnte nicht gelesen werden: " + ex.Message);
      return new List<T>();
    }

    // Keine einzige bekannte Spalte gefunden: Die Datei hat ein falsches Format
    // (z.B. Komma statt Semikolon). Nicht reparieren, sondern sperren.
    var erwartet = CsvSpeicher.Spaltennamen<T, TMap>();
    var nichtsErkannt = ergebnis.FehlendeSpalten.Count == erwartet.Count;
    var hatInhalt = ergebnis.Eintraege.Count > 0 || ergebnis.UnbekannteSpalten.Count > 0;
    if (nichtsErkannt && hatInhalt)
    {
      _gesperrt.Add(datei);
      var kommaHinweis = ergebnis.UnbekannteSpalten.Any(s => s.Contains(','))
        ? " Vermutlich wurde ein Komma statt eines Semikolons als Trennzeichen verwendet."
        : "";
      Fehler(
        datei,
        "Die Kopfzeile enth\u00e4lt keine bekannte Spalte." + kommaHinweis +
        " Die Datei wird nicht ver\u00e4ndert, bis der Fehler behoben ist.");
      return new List<T>();
    }

    if (ergebnis.Fehler.Count > 0)
    {
      _gesperrt.Add(datei);
      foreach (var fehler in ergebnis.Fehler)
      {
        Fehler(datei, $"Zeile {fehler.Zeile}: {fehler.Grund}");
      }

      Fehler(datei, "Die Datei wird nicht ver\u00e4ndert, bis die Fehler behoben sind.");
      return ergebnis.Eintraege;
    }

    var ergaenzt = false;
    foreach (var eintrag in ergebnis.Eintraege)
    {
      ergaenzt |= nachbearbeiten(eintrag);
    }

    var aenderungen = new List<string>();
    if (ergebnis.FehlendeSpalten.Count > 0)
    {
      aenderungen.Add(
        "fehlende Spalten erg\u00e4nzt: " + string.Join(", ", ergebnis.FehlendeSpalten));
    }

    if (ergebnis.UnbekannteSpalten.Count > 0)
    {
      aenderungen.Add(
        "unbekannte Spalten entfernt: " + string.Join(", ", ergebnis.UnbekannteSpalten));
    }

    if (ergaenzt)
    {
      aenderungen.Add("fehlende Kennungen erg\u00e4nzt");
    }

    if (aenderungen.Count > 0)
    {
      var sicherung = SicherungsKopie(pfad);
      CsvSpeicher.Schreiben<T, TMap>(pfad, ergebnis.Eintraege);
      Hinweis(
        datei,
        string.Join("; ", aenderungen) +
        $". Die Datei vor der \u00c4nderung wurde als {Path.GetFileName(sicherung)} gesichert.");
    }

    return ergebnis.Eintraege;
  }

  // Naechster freier Name: datei.csv.bak_1, datei.csv.bak_2, ...
  private static string SicherungsKopie(string pfad)
  {
    for (var nummer = 1; ; nummer++)
    {
      var ziel = $"{pfad}.bak_{nummer}";
      if (!File.Exists(ziel))
      {
        File.Copy(pfad, ziel);
        return ziel;
      }
    }
  }

  // Fuehrt Datensaetze derselben Zeile (gleicher Schluessel) zusammen.
  // Die Anzahlen werden addiert, die uebrigen Datensaetze entfernt.
  private void Zusammenfuehren<T, TMap>(
    string datei,
    List<T> liste,
    Func<T, object> schluessel)
    where T : ListenEintrag, IMitAnzahl
    where TMap : ClassMap<T>
  {
    if (_gesperrt.Contains(datei))
    {
      return;
    }

    var gruppen = liste
      .GroupBy(schluessel)
      .Where(g => g.Count() > 1)
      .ToList();
    if (gruppen.Count == 0)
    {
      return;
    }

    foreach (var gruppe in gruppen)
    {
      var behalten = gruppe.First();
      foreach (var weiterer in gruppe.Skip(1))
      {
        behalten.Anzahl += weiterer.Anzahl;
        liste.Remove(weiterer);
      }
    }

    var pfad = Path.Combine(Ordner, datei);
    var sicherung = SicherungsKopie(pfad);
    CsvSpeicher.Schreiben<T, TMap>(pfad, liste);
    Hinweis(
      datei,
      $"Doppelte Zeilen zusammengef\u00fchrt: {gruppen.Count}. " +
      $"Die Datei vor der \u00c4nderung wurde als {Path.GetFileName(sicherung)} gesichert.");
  }

  // Eigene Formen, die es inzwischen offiziell gibt, werden uebernommen
  // (siehe FormAbgleich). Vorher wird gesichert, weil persoenliche Daten
  // geaendert werden. Ist eine der betroffenen Dateien gesperrt, passiert
  // nichts, damit keine Verweise ins Leere zeigen.
  private void EigeneFormenAbgleichen(DateTime jetzt)
  {
    var daten = _nutzdaten ??= EingebetteteNutzdaten.Alle();
    var uebernahmen = FormAbgleich.Finden(EigeneFormen, daten.Formen);
    if (uebernahmen.Count == 0)
    {
      return;
    }

    var dateien = new[]
    {
      Dateinamen.EigeneFormen, Dateinamen.Habe, Dateinamen.Suche, Dateinamen.Ueber
    };
    if (dateien.Any(d => _gesperrt.Contains(d)))
    {
      Hinweis(
        Dateinamen.EigeneFormen,
        "Eigene Formen, die es jetzt offiziell gibt, werden erst \u00fcbernommen, " +
        "wenn alle Listen fehlerfrei geladen werden.");
      return;
    }

    try
    {
      Sicherungen.Anlegen(Ordner, Sicherungen.Zeitpunkt(jetzt));
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      Hinweis(
        Dateinamen.EigeneFormen,
        "Eigene Formen, die es jetzt offiziell gibt, wurden nicht \u00fcbernommen, " +
        "weil die Sicherung vorher fehlgeschlagen ist: " + ex.Message);
      return;
    }

    FormAbgleich.Uebernehmen(uebernahmen, EigeneFormen, Habe, Suche, Ueber);
    var fehler = new[]
    {
      EigeneFormenSpeichern(), HabeSpeichern(), SucheSpeichern(), UeberSpeichern()
    };
    foreach (var text in fehler.Where(f => f != null))
    {
      Hinweis(Dateinamen.EigeneFormen, text!);
    }

    var namen = daten.Pokemon.ToDictionary(p => p.Guid, p => p.Name);
    foreach (var u in uebernahmen)
    {
      var name = namen.TryGetValue(u.Offiziell.PokemonGuid, out var n) ? n : "?";
      var form = (name + " " + u.Offiziell.Form).Trim();
      if (u.Offiziell.Region != "")
      {
        form += " - " + u.Offiziell.Region;
      }

      Hinweis(
        Dateinamen.EigeneFormen,
        $"Die eigene Form \"{form}\" gibt es jetzt offiziell. Alle Eintr\u00e4ge " +
        "wurden umgestellt und die eigene Form entfernt; der Stand vorher ist gesichert.");
    }
  }

  // Wird beim Laden und nach jeder Aenderung an den eigenen Formen aufgerufen
  public void KatalogAufbauen()
  {
    var daten = _nutzdaten ??= EingebetteteNutzdaten.Alle();
    var formen = daten.Formen.ToList();

    var bekannt = formen.Select(f => f.Guid).ToHashSet();
    foreach (var eigene in EigeneFormen)
    {
      if (!bekannt.Add(eigene.Guid))
      {
        Hinweis(
          Dateinamen.EigeneFormen,
          $"Form {eigene.Guid} ist bereits in den Nutzdaten enthalten und wird ignoriert.");
        continue;
      }

      formen.Add(eigene);
    }

    Katalog = new Katalog(daten.Pokemon, formen, daten.Regionen);
  }

  private void KonsistenzMelden()
  {
    var dateiZuListe = new Dictionary<string, string>
    {
      [ListenNamen.Habe] = Dateinamen.Habe,
      [ListenNamen.Suche] = Dateinamen.Suche,
      [ListenNamen.Ueber] = Dateinamen.Ueber
    };

    foreach (var fehler in Konsistenz.ListenPruefen(Katalog, Habe, Suche, Ueber))
    {
      Hinweis(dateiZuListe[fehler.Liste], $"Eintrag {fehler.EintragGuid}: {fehler.Grund}");
    }
  }

  private string? Speichern<T, TMap>(string datei, List<T> daten) where TMap : ClassMap<T>
  {
    if (_gesperrt.Contains(datei))
    {
      return $"{datei} enth\u00e4lt Fehler und wird nicht ver\u00e4ndert, bis sie behoben sind.";
    }

    try
    {
      CsvSpeicher.Schreiben<T, TMap>(Path.Combine(Ordner, datei), daten);
      AenderungVermerken();
      return null;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      return $"{datei} konnte nicht gespeichert werden: {ex.Message}";
    }
  }

  // Setzt "LetzteAenderung" in abgleich_info.csv auf jetzt. Schlaegt das
  // Schreiben fehl, bleibt die eigentliche Aenderung trotzdem gespeichert;
  // nur die Empfehlung beim Abgleich kann dann ungenau sein.
  // Wird auch nach dem Wiederherstellen einer Sicherung aufgerufen.
  public void AenderungVermerken()
  {
    var eintrag = _abgleichInfo.FirstOrDefault(e => e.Schluessel == SchluesselLetzteAenderung);
    if (eintrag == null)
    {
      eintrag = new SchluesselWert { Schluessel = SchluesselLetzteAenderung };
      _abgleichInfo.Add(eintrag);
    }

    eintrag.Wert = ZeitstempelKonverter.Schreiben(DateTime.UtcNow);
    if (_gesperrt.Contains(Dateinamen.AbgleichInfo))
    {
      return;
    }

    try
    {
      CsvSpeicher.Schreiben<SchluesselWert, SchluesselWertMap>(
        Path.Combine(Ordner, Dateinamen.AbgleichInfo), _abgleichInfo);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      // Bewusst ignoriert, siehe Kommentar oben
    }
  }

  private void Fehler(string datei, string text)
  {
    _meldungen.Add(new LadeMeldung(datei, text, true));
  }

  private void Hinweis(string datei, string text)
  {
    _meldungen.Add(new LadeMeldung(datei, text, false));
  }
}
