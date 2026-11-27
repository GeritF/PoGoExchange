using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

// Seite "Mehrere auf einmal suchen" (Button "+ Mehrere" in "Suche ich"):
// legt fuer alle Pokemon einer Region bzw. aller Regionen Suchzeilen mit
// denselben Anforderungen an. Logik: Services/SammelSuche.cs
public sealed partial class MehrereSuchenViewModel : SeiteViewModel
{
  private const string AlleRegionen = "Alle Regionen";

  private readonly Datenbestand _bestand;
  private readonly Action<SammelErgebnis> _gespeichert;

  public MehrereSuchenViewModel(Datenbestand bestand, Action<SammelErgebnis> gespeichert)
  {
    _bestand = bestand;
    _gespeichert = gespeichert;
    Regionen = new[] { AlleRegionen }
      .Concat(bestand.Katalog.RegionenInReihenfolge())
      .ToList();
    Anforderungen.Geaendert += VorschauAktualisieren;
    VorschauAktualisieren();
  }

  public override string Titel => "Mehrere auf einmal suchen";

  public AnforderungenViewModel Anforderungen { get; } = new();

  public IReadOnlyList<string> Regionen { get; }

  public IReadOnlyList<string> ModusAuswahl { get; } = new[]
  {
    "Eine Zeile pro Form",
    "Eine Zeile pro Pokemon (Form beliebig)"
  };

  [ObservableProperty] private int _regionIndex;

  [ObservableProperty] private int _modusIndex;

  [ObservableProperty] private string _vorschau = "";

  [ObservableProperty] private bool _sicherungVorher = true;

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(HatMeldung))]
  private string _meldung = "";

  public bool HatMeldung => Meldung != "";

  partial void OnRegionIndexChanged(int value)
  {
    VorschauAktualisieren();
  }

  partial void OnModusIndexChanged(int value)
  {
    VorschauAktualisieren();
  }

  [RelayCommand]
  private void Anlegen()
  {
    if (Anforderungen.HatFehler)
    {
      Meldung = Anforderungen.Fehler;
      return;
    }

    var ergebnis = Berechnen();
    if (ergebnis.NeueEintraege.Count == 0)
    {
      Meldung = "Es gibt keine neuen Zeilen anzulegen.";
      return;
    }

    if (SicherungVorher)
    {
      try
      {
        Sicherungen.Anlegen(_bestand.Ordner, Sicherungen.Zeitpunkt(DateTime.UtcNow));
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
      {
        Meldung = "Die Sicherung ist fehlgeschlagen, es wurde nichts angelegt: " + ex.Message;
        return;
      }
    }

    _bestand.Suche.AddRange(ergebnis.NeueEintraege);
    var fehler = _bestand.SucheSpeichern();
    if (fehler != null)
    {
      // Ohne gespeicherte Datei auch im Speicher nichts aendern
      foreach (var eintrag in ergebnis.NeueEintraege)
      {
        _bestand.Suche.Remove(eintrag);
      }

      Meldung = fehler;
      return;
    }

    _gespeichert(ergebnis);
  }

  private SammelErgebnis Berechnen()
  {
    var region = RegionIndex <= 0 || RegionIndex >= Regionen.Count ? null : Regionen[RegionIndex];
    var modus = ModusIndex == 1 ? SammelModus.ProPokemon : SammelModus.ProForm;
    return SammelSuche.Anlegen(
      _bestand.Katalog,
      Anforderungen.Vorlage(),
      modus,
      _bestand.Suche,
      _bestand.Habe,
      region);
  }

  private void VorschauAktualisieren()
  {
    var e = Berechnen();
    var teile = new List<string>
    {
      $"Neue Zeilen: {e.NeueEintraege.Count}",
      $"\u00fcbersprungen (gibt es schon): {e.Uebersprungen}"
    };
    if (Anforderungen.Shiny)
    {
      teile.Add($"Shiny nicht verf\u00fcgbar: {e.NichtVerfuegbar}");
    }

    if (e.NichtInGo > 0)
    {
      teile.Add($"nicht in GO: {e.NichtInGo}");
    }

    teile.Add($"davon bereits erledigt: {e.SofortErledigt}");
    Vorschau = string.Join(" \u00b7 ", teile);
  }
}
