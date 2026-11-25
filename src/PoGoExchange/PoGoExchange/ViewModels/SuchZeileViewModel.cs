using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

// Seite "Zeile bearbeiten" fuer eine Zeile in "Suche ich".
// geaendert(rueckmeldung) wird nach jeder gespeicherten Aenderung aufgerufen.
public sealed partial class SuchZeileViewModel : SeiteViewModel
{
  private readonly Navigation _navigation;
  private readonly Datenbestand _bestand;
  private readonly Action<string> _geaendert;
  private SucheEintrag _eintrag;

  public SuchZeileViewModel(
    Navigation navigation,
    Datenbestand bestand,
    SucheEintrag eintrag,
    Action<string> geaendert,
    string meldung = "")
  {
    _navigation = navigation;
    _bestand = bestand;
    _eintrag = eintrag;
    _geaendert = geaendert;
    Meldung = meldung;
    Anforderungen.Uebernehmen(eintrag);
    Anzahl = eintrag.Anzahl;
    Auffrischen();
  }

  public override string Titel => "Zeile bearbeiten";

  public AnforderungenViewModel Anforderungen { get; } = new();

  [ObservableProperty]
  private string _kopf = "";

  [ObservableProperty]
  private string _fortschritt = "";

  [ObservableProperty]
  private decimal? _anzahl;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HatMeldung))]
  private string _meldung = "";

  public bool HatMeldung => Meldung != "";

  [ObservableProperty]
  private string _konfliktText = "";

  [ObservableProperty]
  private bool _konfliktSichtbar;

  [ObservableProperty]
  private bool _loeschHinweisSichtbar;

  [RelayCommand]
  private void AnforderungenUebernehmen()
  {
    KonfliktSichtbar = false;
    if (Anforderungen.HatFehler)
    {
      Meldung = Anforderungen.Fehler;
      return;
    }

    var zeile = AktuelleZeile();
    if (zeile == null)
    {
      Meldung = "Die Zeile ist nicht mehr vorhanden.";
      return;
    }

    var konflikt = SuchBearbeitung.Konflikt(
      _bestand.Suche, _bestand.Habe, zeile, Anforderungen.Vorlage());
    if (konflikt != null)
    {
      var passend = _bestand.Habe.Count(h => Anforderung.Erfuellt(h, konflikt.Muster));
      var ziel = zeile.Ziel + konflikt.Ziel;
      KonfliktText =
        $"Diese Zeile: {zeile.Erledigt}/{zeile.Ziel}, " +
        $"gefundene Zeile: {konflikt.Erledigt}/{konflikt.Ziel}, " +
        $"zusammen: {Math.Min(passend, ziel)}/{ziel}. Zusammenf\u00fchren?";
      KonfliktSichtbar = true;
      return;
    }

    Aendern(zeile);
  }

  [RelayCommand]
  private void KonfliktJa()
  {
    KonfliktSichtbar = false;
    var zeile = AktuelleZeile();
    if (zeile != null)
    {
      Aendern(zeile);
    }
  }

  [RelayCommand]
  private void KonfliktNein()
  {
    KonfliktSichtbar = false;
  }

  // Ueberschreibt die Anzahl ohne Rueckfrage; 0 loescht die Zeile
  [RelayCommand]
  private void AnzahlUebernehmen()
  {
    var anzahl = (int)(Anzahl ?? -1);
    if (anzahl < 0)
    {
      Meldung = "Die Anzahl darf nicht negativ sein.";
      return;
    }

    var zeile = AktuelleZeile();
    if (zeile == null)
    {
      Meldung = "Die Zeile ist nicht mehr vorhanden.";
      return;
    }

    SuchBearbeitung.AnzahlSetzen(_bestand.Suche, zeile, anzahl, DateTime.UtcNow);
    if (anzahl == 0)
    {
      if (Speichern("Zeile gel\u00f6scht"))
      {
        _navigation.Zurueck();
      }

      return;
    }

    if (Speichern("Gespeichert"))
    {
      Auffrischen();
      Meldung = "Gespeichert";
    }
  }

  [RelayCommand]
  private void Loeschen()
  {
    LoeschHinweisSichtbar = true;
  }

  private void Aendern(SuchZeile zeile)
  {
    _eintrag = SuchBearbeitung.AnforderungenAendern(
      _bestand.Suche, zeile, Anforderungen.Vorlage(), DateTime.UtcNow);
    if (!Speichern("Gespeichert"))
    {
      return;
    }

    Anforderungen.Uebernehmen(_eintrag);
    Anzahl = _eintrag.Anzahl;
    Auffrischen();
    Meldung = "Gespeichert";
  }

  // Liefert false, wenn das Speichern fehlgeschlagen ist (Meldung ist dann gesetzt)
  private bool Speichern(string rueckmeldung)
  {
    var fehler = _bestand.SucheSpeichern();
    _geaendert(fehler ?? rueckmeldung);
    if (fehler != null)
    {
      Meldung = fehler;
      return false;
    }

    return true;
  }

  private SuchZeile? AktuelleZeile()
  {
    var schluessel = SuchZeilen.Schluessel(_eintrag);
    return SuchZeilen.Bilden(_bestand.Suche, _bestand.Habe)
      .FirstOrDefault(z => SuchZeilen.Schluessel(z.Muster) == schluessel);
  }

  private void Auffrischen()
  {
    var (pokemon, form) = Tauschliste.Aufloesen(_bestand.Katalog, _eintrag);
    Kopf = $"#{pokemon.DexNr} {EintragText.Erzeugen(pokemon, form, _eintrag)}";
    var zeile = AktuelleZeile();
    Fortschritt = zeile == null ? "" : $"Fortschritt: {zeile.Erledigt}/{zeile.Ziel}";
  }
}
