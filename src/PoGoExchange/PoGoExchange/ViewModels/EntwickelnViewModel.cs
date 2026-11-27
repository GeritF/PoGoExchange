using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

// Eine Zeile aus "Suche ich", die beim Entwickeln mit hochgezogen werden kann
public sealed partial class BetroffeneZeile : ViewModelBase
{
  public BetroffeneZeile(SuchZeile zeile, string text)
  {
    Zeile = zeile;
    Text = text;
  }

  public SuchZeile Zeile { get; }

  public string Text { get; }

  // Standardmaessig angehakt
  [ObservableProperty] private bool _ausgewaehlt = true;
}

// Seite "Entwickeln": Zielform waehlen, betroffene Zeilen in "Suche ich"
// anhaken, dann genau ein Exemplar der Zeile entwickeln.
// fertig(rueckmeldung) wird nach dem Speichern aufgerufen.
public sealed partial class EntwickelnViewModel : SeiteViewModel
{
  internal const int MaxZiele = 100;

  private readonly Datenbestand _bestand;
  private readonly HabeEintrag _zeile;
  private readonly Action<string> _fertig;
  private bool _aktualisiertGerade;

  public EntwickelnViewModel(Datenbestand bestand, HabeEintrag zeile, Action<string> fertig)
  {
    _bestand = bestand;
    _zeile = zeile;
    _fertig = fertig;

    var (pokemon, form) = Tauschliste.Aufloesen(bestand.Katalog, zeile);
    Kopf = $"#{pokemon.DexNr} {EintragText.Erzeugen(pokemon, form, zeile)}";
    AnzahlHinweis = zeile.Anzahl > 1
      ? $"Es wird 1 von {zeile.Anzahl} Exemplaren entwickelt."
      : "";
    ZieleAktualisieren();
  }

  public override string Titel => "Entwickeln";

  public string Kopf { get; }

  public string AnzahlHinweis { get; }

  public bool HatAnzahlHinweis => AnzahlHinweis != "";

  [ObservableProperty] private string _suchtext = "";

  public ObservableCollection<FormZiel> Ziele { get; } = new();

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(HatMehrHinweis))]
  private string _mehrHinweis = "";

  public bool HatMehrHinweis => MehrHinweis != "";

  [ObservableProperty] private FormZiel? _auswahl;

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(AuswahlText))]
  private FormZiel? _ziel;

  public string AuswahlText => Ziel == null
    ? "Noch keine Zielform ausgew\u00e4hlt"
    : "Zielform: " + Ziel.Anzeige;

  // Zeilen in "Suche ich", deren Anforderungen das Exemplar vorher erfuellt
  public ObservableCollection<BetroffeneZeile> Betroffene { get; } = new();

  [ObservableProperty] private bool _hatBetroffene;

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(HatMeldung))]
  private string _meldung = "";

  public bool HatMeldung => Meldung != "";

  partial void OnSuchtextChanged(string value)
  {
    ZieleAktualisieren();
  }

  partial void OnAuswahlChanged(FormZiel? value)
  {
    if (value != null && !_aktualisiertGerade)
    {
      Ziel = value;
    }
  }

  partial void OnZielChanged(FormZiel? value)
  {
    BetroffeneAktualisieren();
  }

  [RelayCommand]
  private void Entwickeln()
  {
    if (Ziel == null)
    {
      Meldung = "Bitte eine Zielform ausw\u00e4hlen.";
      return;
    }

    var neueForm = _bestand.Katalog.FormZuGuid(Ziel.FormGuid);
    if (neueForm == null)
    {
      Meldung = "Die Zielform wurde nicht gefunden.";
      return;
    }

    var ausgewaehlt = Betroffene.Where(b => b.Ausgewaehlt).ToList();
    Entwicklung.Entwickeln(_bestand.Habe, _zeile, neueForm);
    foreach (var betroffen in ausgewaehlt)
    {
      Entwicklung.Hochziehen(_bestand.Suche, betroffen.Zeile, neueForm);
    }

    var fehler = _bestand.HabeSpeichern();
    if (fehler == null && ausgewaehlt.Count > 0)
    {
      fehler = _bestand.SucheSpeichern();
    }

    if (fehler != null)
    {
      Meldung = fehler;
      return;
    }

    var suche = ausgewaehlt.Count == 0
      ? "Suche nicht angepasst"
      : $"Suche angepasst: {ausgewaehlt.Count} Zeile(n)";
    _fertig($"Entwickelt zu {Ziel.Anzeige}. {suche}");
  }

  // Alle Formen ausser der aktuellen; Entwicklungsketten gibt es nicht
  private void ZieleAktualisieren()
  {
    _aktualisiertGerade = true;
    try
    {
      var ziele = HabeBearbeitung.Ziele(_bestand.Katalog, Suchtext)
        .Where(z => z.FormGuid != _zeile.FormGuid)
        .ToList();
      Ziele.Clear();
      foreach (var ziel in ziele.Take(MaxZiele))
      {
        Ziele.Add(ziel);
      }

      MehrHinweis = ziele.Count > MaxZiele
        ? $"Es werden h\u00f6chstens {MaxZiele} Eintr\u00e4ge angezeigt. Bitte das Suchfeld nutzen."
        : "";

      Auswahl = Ziel != null && Ziele.Contains(Ziel) ? Ziele.First(z => z == Ziel) : null;
    }
    finally
    {
      _aktualisiertGerade = false;
    }
  }

  private void BetroffeneAktualisieren()
  {
    Betroffene.Clear();
    if (Ziel != null)
    {
      var vorher = _zeile.Kopie();
      var nachher = _zeile.Kopie();
      nachher.PokemonGuid = Ziel.PokemonGuid;
      nachher.FormGuid = Ziel.FormGuid;

      var zeilen = Entwicklung.BetroffeneZeilen(vorher, nachher, _bestand.Suche, _bestand.Habe);
      foreach (var zeile in zeilen)
      {
        var (pokemon, form) = Tauschliste.Aufloesen(_bestand.Katalog, zeile.Muster);
        var text = $"#{pokemon.DexNr} {EintragText.Erzeugen(pokemon, form, zeile.Muster)}" +
          ListenAnzeige.SuchZusatz(zeile);
        Betroffene.Add(new BetroffeneZeile(zeile, text));
      }
    }

    HatBetroffene = Betroffene.Count > 0;
  }
}
