using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Models;
using PoGoExchange.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PoGoExchange.ViewModels;

// Seite "Neues Exemplar" bzw. "Erhalten": Merkmale und Anzahl oben,
// Auswahl der Form unten. Beim Erhalten sind die Merkmale aus der Suche
// vorbelegt und die Auswahl ist auf das Pokemon bzw. die Form beschraenkt.
public sealed partial class HabeNeuViewModel : SeiteViewModel
{
  // Mehr Eintraege machen die Seite auf dem Handy traege
  internal const int MaxZiele = 100;

  private readonly Datenbestand _bestand;
  private readonly Action<HabeAnlegeErgebnis> _gespeichert;
  private readonly Guid? _nurPokemon;
  private readonly Guid? _nurForm;
  private readonly string _titel;
  private readonly Navigation? _navigation;
  private bool _aktualisiertGerade;

  public HabeNeuViewModel(
    Datenbestand bestand,
    Action<HabeAnlegeErgebnis> gespeichert,
    HabeEintrag? vorlage = null,
    Guid? nurPokemon = null,
    Guid? nurForm = null,
    string titel = "Neues Exemplar",
    Navigation? navigation = null)
  {
    _navigation = navigation;
    _bestand = bestand;
    _gespeichert = gespeichert;
    _nurPokemon = nurPokemon;
    _nurForm = nurForm;
    _titel = titel;
    if (vorlage != null)
    {
      Merkmale.Uebernehmen(vorlage);
    }

    ZieleAktualisieren();

    // Ist die Form fest vorgegeben, ist sie gleich ausgewaehlt
    if (_nurForm != null && Ziele.Count == 1)
    {
      Auswahl = Ziele[0];
    }
  }

  public override string Titel => _titel;

  // Das Suchfeld wird nur gebraucht, wenn alle Pokemon zur Auswahl stehen
  public bool SuchfeldSichtbar => _nurPokemon == null;

  // Link "Form fehlt?" nur, wenn die Form nicht fest vorgegeben ist
  public bool FormAnlegenSichtbar => _navigation != null && _nurForm == null;

  public ExemplarViewModel Merkmale { get; } = new();

  [ObservableProperty] private decimal? _anzahl = 1;

  [ObservableProperty] private string _suchtext = "";

  public ObservableCollection<FormZiel> Ziele { get; } = [];

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(HatMehrHinweis))]
  private string _mehrHinweis = "";

  public bool HatMehrHinweis => MehrHinweis != "";

  // Auswahl in der Liste; Ziel haelt die Auswahl auch, wenn das Suchfeld sie ausblendet
  [ObservableProperty] private FormZiel? _auswahl;

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(AuswahlText))]
  private FormZiel? _ziel;

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

  partial void OnAuswahlChanged(FormZiel? value)
  {
    if (value != null && !_aktualisiertGerade)
    {
      Ziel = value;
    }
  }

  [RelayCommand]
  private void Speichern()
  {
    if (Ziel == null)
    {
      Meldung = "Bitte ein Pokemon ausw\u00e4hlen.";
      return;
    }

    if (Merkmale.HatFehler)
    {
      Meldung = Merkmale.Fehler;
      return;
    }

    var anzahl = (int)(Anzahl ?? 0);
    if (anzahl < 1)
    {
      Meldung = "Die Anzahl muss mindestens 1 sein.";
      return;
    }

    var ergebnis = HabeBearbeitung.Anlegen(_bestand.Habe, Merkmale.Vorlage(), Ziel, anzahl);
    var fehler = _bestand.HabeSpeichern();
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
    _navigation?.Oeffnen(
      new EigeneFormViewModel(_navigation, _bestand, FormAngelegt, null, _nurPokemon));
  }

  // Zurueck von "Neue eigene Form": Liste neu aufbauen und die neue Form waehlen
  private void FormAngelegt(PokemonForm? form, string rueckmeldung)
  {
    _navigation?.Zurueck();
    if (form == null)
    {
      return;
    }

    var pokemon = _bestand.Katalog.PokemonZuGuid(form.PokemonGuid);
    if (_nurPokemon == null && pokemon != null)
    {
      Suchtext = pokemon.DexNr.ToString();
    }

    ZieleAktualisieren();
    var ziel = Ziele.FirstOrDefault(z => z.FormGuid == form.Guid);
    if (ziel != null)
    {
      Auswahl = ziel;
    }
  }

  private void ZieleAktualisieren()
  {
    _aktualisiertGerade = true;
    try
    {
      var ziele = HabeBearbeitung.Ziele(_bestand.Katalog, Suchtext, _nurPokemon, _nurForm);
      Ziele.Clear();
      foreach (var ziel in ziele.Take(MaxZiele))
      {
        Ziele.Add(ziel);
      }

      MehrHinweis = ziele.Count > MaxZiele
        ? $"Es werden h\u00f6chstens {MaxZiele} Eintr\u00e4ge angezeigt. Bitte das Suchfeld nutzen."
        : "";

      // Gleiche Werte gelten als gleiches Ziel (record)
      Auswahl = Ziel != null && Ziele.Contains(Ziel) ? Ziele.First(z => z == Ziel) : null;
    }
    finally
    {
      _aktualisiertGerade = false;
    }
  }
}
