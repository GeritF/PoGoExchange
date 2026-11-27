using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PoGoExchange.ViewModels;

// Ueberschrift einer Regionsgruppe in der Liste
public sealed record RegionKopf(string Region);

// Eine Registerkarte mit Suchfeld und gruppierter Liste.
// aufbauen(filter, mitErledigten) liefert die anzuzeigenden Gruppen.
// neu: optional, oeffnet "Neuer Eintrag"; oeffnen: optional, oeffnet eine angetippte Zeile.
public sealed partial class ListenTabViewModel : ViewModelBase
{
  private readonly Func<string, bool, List<AnzeigeGruppe>> _aufbauen;
  private readonly Action? _neu;
  private readonly Action<AnzeigeZeile>? _oeffnen;
  private readonly Action? _mehrere;

  public ListenTabViewModel(
    bool hatErledigtUmschalter,
    Func<string, bool, List<AnzeigeGruppe>> aufbauen,
    Action? neu = null,
    Action<AnzeigeZeile>? oeffnen = null,
    Action? mehrere = null)
  {
    HatErledigtUmschalter = hatErledigtUmschalter;
    _aufbauen = aufbauen;
    _neu = neu;
    _oeffnen = oeffnen;
    _mehrere = mehrere;
    Aktualisieren();
  }

  public bool HatErledigtUmschalter { get; }

  public bool HatNeu => _neu != null;
  public bool HatMehrere => _mehrere != null;

  // Ueberschriften (RegionKopf) und Zeilen (AnzeigeZeile) gemischt
  public ObservableCollection<object> Zeilen { get; } = [];

  public bool IstLeer => Zeilen.Count == 0;

  [ObservableProperty] private string _suchtext = "";

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(ErledigtStatus))]
  [NotifyPropertyChangedFor(nameof(ErledigtButtonText))]
  private bool _erledigteAnzeigen;

  [ObservableProperty] private bool _rueckfrageSichtbar;

  // Rueckmeldung nach einer Aktion, z.B. "1 von 3 bereits erledigt"
  [ObservableProperty] [NotifyPropertyChangedFor(nameof(HatRueckmeldung))]
  private string _rueckmeldung = "";

  public bool HatRueckmeldung => Rueckmeldung != "";

  // Angetippte Zeile; wird nach dem Oeffnen zurueckgesetzt,
  // damit dieselbe Zeile erneut angetippt werden kann
  [ObservableProperty] private object? _ausgewaehlt;

  public string ErledigtStatus => ErledigteAnzeigen
    ? "Erledigte werden angezeigt"
    : "Erledigte werden nicht angezeigt";

  public string ErledigtButtonText => ErledigteAnzeigen
    ? "Erledigte ausblenden"
    : "Erledigte anzeigen";

  partial void OnSuchtextChanged(string value)
  {
    Aktualisieren();
  }

  partial void OnAusgewaehltChanged(object? value)
  {
    if (value == null)
    {
      return;
    }

    Ausgewaehlt = null;
    if (value is AnzeigeZeile { Eintrag: not null } zeile && _oeffnen != null)
    {
      _oeffnen(zeile);
    }
  }

  [RelayCommand]
  private void Neu()
  {
    _neu?.Invoke();
  }

  [RelayCommand]
  private void Mehrere()
  {
    _mehrere?.Invoke();
  }

  // Einblenden nur nach Rueckfrage, Ausblenden sofort
  [RelayCommand]
  private void ErledigtUmschalten()
  {
    if (ErledigteAnzeigen)
    {
      ErledigteAnzeigen = false;
      Aktualisieren();
      return;
    }

    RueckfrageSichtbar = true;
  }

  [RelayCommand]
  private void RueckfrageJa()
  {
    RueckfrageSichtbar = false;
    ErledigteAnzeigen = true;
    Aktualisieren();
  }

  [RelayCommand]
  private void RueckfrageNein()
  {
    RueckfrageSichtbar = false;
  }

  public void Aktualisieren()
  {
    Zeilen.Clear();
    foreach (var gruppe in _aufbauen(Suchtext, ErledigteAnzeigen))
    {
      Zeilen.Add(new RegionKopf(gruppe.Region));
      foreach (var zeile in gruppe.Zeilen)
      {
        Zeilen.Add(zeile);
      }
    }

    OnPropertyChanged(nameof(IstLeer));
  }
}
