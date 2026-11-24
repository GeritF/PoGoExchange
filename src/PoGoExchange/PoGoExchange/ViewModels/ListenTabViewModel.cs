using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

// Ueberschrift einer Regionsgruppe in der Liste
public sealed record RegionKopf(string Region);

// Eine Registerkarte mit Suchfeld und gruppierter Liste.
// aufbauen(filter, mitErledigten) liefert die anzuzeigenden Gruppen.
public sealed partial class ListenTabViewModel : ViewModelBase
{
  private readonly Func<string, bool, List<AnzeigeGruppe>> _aufbauen;

  public ListenTabViewModel(
    bool hatErledigtUmschalter,
    Func<string, bool, List<AnzeigeGruppe>> aufbauen)
  {
    HatErledigtUmschalter = hatErledigtUmschalter;
    _aufbauen = aufbauen;
    Aktualisieren();
  }

  public bool HatErledigtUmschalter { get; }

  // Ueberschriften (RegionKopf) und Zeilen (AnzeigeZeile) gemischt
  public ObservableCollection<object> Zeilen { get; } = new();

  public bool IstLeer => Zeilen.Count == 0;

  [ObservableProperty]
  private string _suchtext = "";

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(ErledigtStatus))]
  [NotifyPropertyChangedFor(nameof(ErledigtButtonText))]
  private bool _erledigteAnzeigen;

  [ObservableProperty]
  private bool _rueckfrageSichtbar;

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

  private void Aktualisieren()
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
