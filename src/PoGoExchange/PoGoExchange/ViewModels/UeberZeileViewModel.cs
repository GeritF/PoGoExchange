using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

// Seite "Eintrag bearbeiten" fuer "Habe ich ueber": Merkmale aendern und
// Loeschen nach Rueckfrage. geaendert(rueckmeldung) wird nach jeder
// gespeicherten Aenderung aufgerufen.
public sealed partial class UeberZeileViewModel : SeiteViewModel
{
  private readonly Navigation _navigation;
  private readonly Datenbestand _bestand;
  private readonly Action<string> _geaendert;
  private readonly UeberEintrag _eintrag;

  public UeberZeileViewModel(
    Navigation navigation,
    Datenbestand bestand,
    UeberEintrag eintrag,
    Action<string> geaendert)
  {
    _navigation = navigation;
    _bestand = bestand;
    _eintrag = eintrag;
    _geaendert = geaendert;
    Merkmale.Uebernehmen(eintrag);
    Auffrischen();
  }

  public override string Titel => "Eintrag bearbeiten";

  public ExemplarViewModel Merkmale { get; } = new(mitGlueckUndHundert: false);

  [ObservableProperty]
  private string _kopf = "";

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HatMeldung))]
  private string _meldung = "";

  public bool HatMeldung => Meldung != "";

  [ObservableProperty]
  private bool _konfliktSichtbar;

  [ObservableProperty]
  private bool _loeschRueckfrageSichtbar;

  [RelayCommand]
  private void MerkmaleUebernehmen()
  {
    KonfliktSichtbar = false;
    if (Merkmale.HatFehler)
    {
      Meldung = Merkmale.Fehler;
      return;
    }

    if (UeberBearbeitung.Konflikt(_bestand.Ueber, _eintrag, Merkmale.Vorlage()) != null)
    {
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

  [RelayCommand]
  private void Loeschen()
  {
    LoeschRueckfrageSichtbar = true;
  }

  [RelayCommand]
  private void LoeschenJa()
  {
    LoeschRueckfrageSichtbar = false;
    UeberBearbeitung.Loeschen(_bestand.Ueber, _eintrag);
    if (Speichern("Eintrag gel\u00f6scht"))
    {
      _navigation.Zurueck();
    }
  }

  [RelayCommand]
  private void LoeschenNein()
  {
    LoeschRueckfrageSichtbar = false;
  }

  private void Aendern()
  {
    UeberBearbeitung.MerkmaleAendern(_bestand.Ueber, _eintrag, Merkmale.Vorlage());
    if (!Speichern("Gespeichert"))
    {
      return;
    }

    Merkmale.Uebernehmen(_eintrag);
    Auffrischen();
    Meldung = "Gespeichert";
  }

  // Liefert false, wenn das Speichern fehlgeschlagen ist (Meldung ist dann gesetzt)
  private bool Speichern(string rueckmeldung)
  {
    var fehler = _bestand.UeberSpeichern();
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
    var (pokemon, form) = Tauschliste.Aufloesen(_bestand.Katalog, _eintrag);
    Kopf = $"#{pokemon.DexNr} {EintragText.Erzeugen(pokemon, form, _eintrag)}";
  }
}
