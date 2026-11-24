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
}

public static class Datenordner
{
  // Linux: ~/.local/share/PoGoExchange
  // Windows: %LOCALAPPDATA%\PoGoExchange
  // Android: privater Bereich der App (Vermutung, beim ersten Test pruefen)
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
public sealed class Datenbestand
{
  private readonly HashSet<string> _gesperrt = new(StringComparer.OrdinalIgnoreCase);
  private readonly List<LadeMeldung> _meldungen = new();

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

  public bool IstGesperrt(string datei)
  {
    return _gesperrt.Contains(datei);
  }

  public static Datenbestand Laden(string ordner, DateTime jetzt)
  {
    Directory.CreateDirectory(ordner);
    var bestand = new Datenbestand(ordner);

    bestand.EigeneFormen = bestand.DateiLaden<PokemonForm, PokemonFormMap>(
      Dateinamen.EigeneFormen, _ => false);
    bestand.Habe = bestand.ListeLaden<HabeEintrag, HabeEintragMap>(Dateinamen.Habe, jetzt);
    bestand.Suche = bestand.ListeLaden<SucheEintrag, SucheEintragMap>(Dateinamen.Suche, jetzt);
    bestand.SucheZusammenfuehren(jetzt);
    bestand.Ueber = bestand.ListeLaden<UeberEintrag, UeberEintragMap>(Dateinamen.Ueber, jetzt);

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

  // Ergaenzt fehlende Kennungen und Zeitstempel
  private List<T> ListeLaden<T, TMap>(string datei, DateTime jetzt)
    where T : ListenEintrag
    where TMap : ClassMap<T>, new()
  {
    return DateiLaden<T, TMap>(datei, eintrag =>
    {
      var ergaenzt = false;
      if (eintrag.Guid == Guid.Empty)
      {
        eintrag.Guid = Guid.NewGuid();
        ergaenzt = true;
      }

      if (eintrag.GeaendertAm == DateTime.MinValue)
      {
        eintrag.GeaendertAm = jetzt;
        ergaenzt = true;
      }

      return ergaenzt;
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
      aenderungen.Add("fehlende Kennungen oder Zeitstempel erg\u00e4nzt");
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

  // Fuehrt Datensaetze mit gleicher Zeile (Pokemon, Form, Anforderungen) zusammen.
  // Die Anzahlen werden addiert, die uebrigen Datensaetze als geloescht markiert.
  private void SucheZusammenfuehren(DateTime jetzt)
  {
    if (_gesperrt.Contains(Dateinamen.Suche))
    {
      return;
    }

    var gruppen = Suche
      .Where(e => e.GeloeschtAm == null)
      .GroupBy(SuchZeilen.Schluessel)
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
        weiterer.GeloeschtAm = jetzt;
        weiterer.GeaendertAm = jetzt;
      }

      behalten.GeaendertAm = jetzt;
    }

    var pfad = Path.Combine(Ordner, Dateinamen.Suche);
    var sicherung = SicherungsKopie(pfad);
    CsvSpeicher.Schreiben<SucheEintrag, SucheEintragMap>(pfad, Suche);
    Hinweis(
      Dateinamen.Suche,
      $"Doppelte Zeilen zusammengef\u00fchrt: {gruppen.Count}. " +
      $"Die Datei vor der \u00c4nderung wurde als {Path.GetFileName(sicherung)} gesichert.");
  }

  // Nutzdaten aus dem Programm plus eigene Formen des Spielers
  private void KatalogAufbauen()
  {
    var pokemon = EingebetteteNutzdaten.Lesen<Pokemon, PokemonMap>("pokemon.csv");
    var formen = EingebetteteNutzdaten.Lesen<PokemonForm, PokemonFormMap>("formen.csv");
    var regionen = EingebetteteNutzdaten.Lesen<RegionBereich, RegionBereichMap>("regionen.csv");

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

    Katalog = new Katalog(pokemon, formen, regionen);
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
      return null;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      return $"{datei} konnte nicht gespeichert werden: {ex.Message}";
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
