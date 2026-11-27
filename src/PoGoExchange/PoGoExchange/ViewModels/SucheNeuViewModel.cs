using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Models;
using PoGoExchange.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PoGoExchange.ViewModels;

// Seite "Neuer Sucheintrag": Anforderungen oben, Auswahl des Ziels unten
public sealed partial class SucheNeuViewModel : SeiteViewModel
{
  // Mehr Eintraege je Abschnitt machen die Seite auf dem Handy traege
  internal const int MaxProAbschnitt = 100;

  private readonly Datenbestand _bestand;
  private readonly Action<AnlegeErgebnis> _gespeichert;
  private readonly Navigation? _navigation;
  private bool _aktualisiertGerade;

  public SucheNeuViewModel(
    Datenbestand bestand,
    Action<AnlegeErgebnis> gespeichert,
    Navigation? navigation = null)
  {
    _bestand = bestand;
    _gespeichert = gespeichert;
    _navigation = navigation;
    Anforderungen.Geaendert += ZieleAktualisieren;
    ZieleAktualisieren();
  }

  public bool FormAnlegenSichtbar => _navigation != null;

  public override string Titel => "Neuer Sucheintrag";

  public AnforderungenViewModel Anforderungen { get; } = new();

  [ObservableProperty] private decimal? _anzahl = 1;

  [ObservableProperty] private string _suchtext = "";

  public ObservableCollection<SuchZiel> Verfuegbar { get; } = [];
  public ObservableCollection<SuchZiel> HastDuSchon { get; } = [];
  public ObservableCollection<SuchZiel> NichtVerfuegbar { get; } = [];

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(HatMehrHinweis))]
  private string _mehrHinweis = "";

  public bool HatMehrHinweis => MehrHinweis != "";

  // Je Abschnitt eine eigene Auswahl, damit sich die Listen nicht gegenseitig zuruecksetzen
  [ObservableProperty] private SuchZiel? _auswahlVerfuegbar;

  [ObservableProperty] private SuchZiel? _auswahlHastDuSchon;

  [ObservableProperty] private SuchZiel? _auswahlNichtVerfuegbar;

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(AuswahlText))]
  private SuchZiel? _ziel;

  public string AuswahlText => Ziel == null
    ? "Noch kein Pokemon ausgew\u00e4hlt"
    : "Ausgew\u00e4hlt: " + Ziel.Anzeige;

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(HatMeldung))]
  private string _meldung = "";

  public bool HatMeldung => Meldung != "";

  partial void OnSuchtextChanged(string value)
  {
    ZieleAktualisieren();
  }

  partial void OnAuswahlVerfuegbarChanged(SuchZiel? value)
  {
    Waehlen(value);
  }

  partial void OnAuswahlHastDuSchonChanged(SuchZiel? value)
  {
    Waehlen(value);
  }

  partial void OnAuswahlNichtVerfuegbarChanged(SuchZiel? value)
  {
    Waehlen(value);
  }

  [RelayCommand]
  private void Speichern()
  {
    if (Ziel == null)
    {
      Meldung = "Bitte ein Pokemon ausw\u00e4hlen.";
      return;
    }

    if (Anforderungen.HatFehler)
    {
      Meldung = Anforderungen.Fehler;
      return;
    }

    var anzahl = (int)(Anzahl ?? 0);
    if (anzahl < 1)
    {
      Meldung = "Die Anzahl muss mindestens 1 sein.";
      return;
    }

    var ergebnis = SuchBearbeitung.Anlegen(
      _bestand.Suche, _bestand.Habe, Anforderungen.Vorlage(), Ziel, anzahl);
    var fehler = _bestand.SucheSpeichern();
    if (fehler != null)
    {
      Meldung = fehler;
      return;
    }

    _gespeichert(ergebnis);
  }

  [RelayCommand]
  private void FormAnlegen()
  {
    _navigation?.Oeffnen(new EigeneFormViewModel(_navigation, _bestand, FormAngelegt));
  }

  // Zurueck von "Neue eigene Form": Abschnitte neu aufbauen und die neue Form waehlen
  private void FormAngelegt(PokemonForm? form, string rueckmeldung)
  {
    _navigation?.Zurueck();
    if (form == null)
    {
      return;
    }

    var pokemon = _bestand.Katalog.PokemonZuGuid(form.PokemonGuid);
    if (pokemon != null)
    {
      Suchtext = pokemon.DexNr.ToString();
    }

    ZieleAktualisieren();
    var ziel = Verfuegbar.Concat(HastDuSchon).Concat(NichtVerfuegbar)
      .FirstOrDefault(z => z.FormGuid == form.Guid);
    if (ziel != null)
    {
      Waehlen(ziel);
    }
  }

  // Auswahl in einem Abschnitt hebt die Auswahl in den anderen auf
  private void Waehlen(SuchZiel? ziel)
  {
    if (ziel == null || _aktualisiertGerade)
    {
      return;
    }

    _aktualisiertGerade = true;
    try
    {
      Ziel = ziel;
      AuswahlSetzen(ziel);
    }
    finally
    {
      _aktualisiertGerade = false;
    }
  }

  // Baut die drei Abschnitte neu auf; die gewaehlte Form bleibt gewaehlt
  private void ZieleAktualisieren()
  {
    _aktualisiertGerade = true;
    try
    {
      var ziele = SuchBearbeitung.Ziele(
        _bestand.Katalog, _bestand.Habe, Anforderungen.Vorlage(), Suchtext);

      Fuellen(Verfuegbar, ziele, ZielKategorie.Verfuegbar);
      Fuellen(HastDuSchon, ziele, ZielKategorie.HastDuSchon);
      Fuellen(NichtVerfuegbar, ziele, ZielKategorie.NichtVerfuegbar);

      var zuViele = ziele.GroupBy(z => z.Kategorie).Any(g => g.Count() > MaxProAbschnitt);
      MehrHinweis = zuViele
        ? $"Es werden h\u00f6chstens {MaxProAbschnitt} Eintr\u00e4ge je Abschnitt angezeigt. " +
        "Bitte das Suchfeld nutzen."
        : "";

      if (Ziel != null)
      {
        var neu = ziele.FirstOrDefault(z =>
          z.PokemonGuid == Ziel.PokemonGuid && z.FormGuid == Ziel.FormGuid);
        if (neu != null)
        {
          Ziel = neu;
        }
      }

      AuswahlSetzen(Ziel);
    }
    finally
    {
      _aktualisiertGerade = false;
    }
  }

  // Markiert das Ziel in dem Abschnitt, in dem es steht; die anderen werden leer
  private void AuswahlSetzen(SuchZiel? ziel)
  {
    AuswahlVerfuegbar = ziel != null && Verfuegbar.Contains(ziel) ? ziel : null;
    AuswahlHastDuSchon = ziel != null && HastDuSchon.Contains(ziel) ? ziel : null;
    AuswahlNichtVerfuegbar = ziel != null && NichtVerfuegbar.Contains(ziel) ? ziel : null;
  }

  private static void Fuellen(
    ObservableCollection<SuchZiel> liste,
    List<SuchZiel> ziele,
    ZielKategorie kategorie)
  {
    liste.Clear();
    foreach (var ziel in ziele.Where(z => z.Kategorie == kategorie).Take(MaxProAbschnitt))
    {
      liste.Add(ziel);
    }
  }
}
