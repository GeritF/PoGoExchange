using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Models;
using PoGoExchange.Services;
using System;

namespace PoGoExchange.ViewModels;

// Seite "Exemplar bearbeiten" fuer eine Zeile in "Habe ich".
// geaendert(rueckmeldung) wird nach jeder gespeicherten Aenderung aufgerufen.
public sealed partial class HabeZeileViewModel : SeiteViewModel
{
  private readonly Navigation _navigation;
  private readonly Datenbestand _bestand;
  private readonly Action<string> _geaendert;
  private HabeEintrag _zeile;

  public HabeZeileViewModel(
    Navigation navigation,
    Datenbestand bestand,
    HabeEintrag zeile,
    Action<string> geaendert)
  {
    _navigation = navigation;
    _bestand = bestand;
    _zeile = zeile;
    _geaendert = geaendert;
    Merkmale.Uebernehmen(zeile);
    Anzahl = zeile.Anzahl;
    Auffrischen();
  }

  public override string Titel => "Exemplar bearbeiten";

  public ExemplarViewModel Merkmale { get; } = new();

  [ObservableProperty] private string _kopf = "";

  [ObservableProperty] private decimal? _anzahl;

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(HatMeldung))]
  private string _meldung = "";

  public bool HatMeldung => Meldung != "";

  [ObservableProperty] private string _konfliktText = "";

  [ObservableProperty] private bool _konfliktSichtbar;

  [ObservableProperty] private bool _loeschHinweisSichtbar;

  [ObservableProperty] private bool _loeschRueckfrageSichtbar;

  // Gilt fuer die ganze Zeile
  [RelayCommand]
  private void MerkmaleUebernehmen()
  {
    KonfliktSichtbar = false;
    if (Merkmale.HatFehler)
    {
      Meldung = Merkmale.Fehler;
      return;
    }

    var konflikt = HabeBearbeitung.Konflikt(_bestand.Habe, _zeile, Merkmale.Vorlage());
    if (konflikt != null)
    {
      KonfliktText =
        $"Diese Zeile: {_zeile.Anzahl}x, gefundene Zeile: {konflikt.Anzahl}x, " +
        $"zusammen: {_zeile.Anzahl + konflikt.Anzahl}x. Zusammenf\u00fchren?";
      KonfliktSichtbar = true;
      return;
    }

    Aendern();
  }

  [RelayCommand]
  private void KonfliktJa()
  {
    KonfliktSichtbar = false;
    Aendern();
  }

  [RelayCommand]
  private void KonfliktNein()
  {
    KonfliktSichtbar = false;
  }

  // Ueberschreibt die Anzahl ohne Rueckfrage; 0 entfernt die Zeile erst nach Rueckfrage
  [RelayCommand]
  private void AnzahlUebernehmen()
  {
    LoeschRueckfrageSichtbar = false;
    var anzahl = (int)(Anzahl ?? -1);
    if (anzahl < 0)
    {
      Meldung = "Die Anzahl darf nicht negativ sein.";
      return;
    }

    if (anzahl == 0)
    {
      LoeschRueckfrageSichtbar = true;
      return;
    }

    HabeBearbeitung.AnzahlSetzen(_bestand.Habe, _zeile, anzahl);
    if (Speichern("Gespeichert"))
    {
      Auffrischen();
      Meldung = "Gespeichert";
    }
  }

  [RelayCommand]
  private void LoeschenJa()
  {
    LoeschRueckfrageSichtbar = false;
    HabeBearbeitung.AnzahlSetzen(_bestand.Habe, _zeile, 0);
    if (Speichern("Zeile gel\u00f6scht"))
    {
      _navigation.Zurueck();
    }
  }

  // Rueckfrage abgelehnt: Anzahl im Eingabefeld auf den gespeicherten Wert zuruecksetzen
  [RelayCommand]
  private void LoeschenNein()
  {
    LoeschRueckfrageSichtbar = false;
    Anzahl = _zeile.Anzahl;
  }

  [RelayCommand]
  private void Loeschen()
  {
    LoeschHinweisSichtbar = true;
  }

  [RelayCommand]
  private void Entwickeln()
  {
    _navigation.Oeffnen(new EntwickelnViewModel(_bestand, _zeile, EntwicklungFertig));
  }

  // Zurueck bis zur Liste: Die Zeile kann durch das Entwickeln entfernt worden sein
  private void EntwicklungFertig(string rueckmeldung)
  {
    _navigation.Zurueck();
    _navigation.Zurueck();
    _geaendert(rueckmeldung);
  }

  private void Aendern()
  {
    _zeile = HabeBearbeitung.MerkmaleAendern(_bestand.Habe, _zeile, Merkmale.Vorlage());
    if (!Speichern("Gespeichert"))
    {
      return;
    }

    Merkmale.Uebernehmen(_zeile);
    Anzahl = _zeile.Anzahl;
    Auffrischen();
    Meldung = "Gespeichert";
  }

  // Liefert false, wenn das Speichern fehlgeschlagen ist (Meldung ist dann gesetzt)
  private bool Speichern(string rueckmeldung)
  {
    var fehler = _bestand.HabeSpeichern();
    _geaendert(fehler ?? rueckmeldung);
    if (fehler != null)
    {
      Meldung = fehler;
      return false;
    }

    return true;
  }

  private void Auffrischen()
  {
    var (pokemon, form) = Tauschliste.Aufloesen(_bestand.Katalog, _zeile);
    var anzahl = _zeile.Anzahl > 1 ? $"{_zeile.Anzahl}x " : "";
    Kopf = $"{anzahl}#{pokemon.DexNr} {EintragText.Erzeugen(pokemon, form, _zeile)}";
  }
}
