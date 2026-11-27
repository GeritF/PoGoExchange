using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace PoGoExchange.ViewModels;

// Auswahl "Alle Regionen" oder eine bestimmte Region, z.B. unter einem
// Suchfeld. Region ist null bei "Alle Regionen". Geaendert meldet jede
// Aenderung der Auswahl. Was zur Region passt: ListenAnzeige.RegionPasst.
public sealed partial class RegionFilterViewModel : ViewModelBase
{
  public const string AlleRegionen = "Alle Regionen";

  public RegionFilterViewModel(IEnumerable<string> regionen)
  {
    Regionen = [AlleRegionen, .. regionen];
  }

  public event Action? Geaendert;

  public IReadOnlyList<string> Regionen { get; }

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(Region))]
  private int _regionIndex;

  public string? Region =>
    RegionIndex <= 0 || RegionIndex >= Regionen.Count ? null : Regionen[RegionIndex];

  partial void OnRegionIndexChanged(int value)
  {
    Geaendert?.Invoke();
  }
}
