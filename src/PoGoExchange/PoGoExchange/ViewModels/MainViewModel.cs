using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

public partial class MainViewModel : ViewModelBase
{
  public MainViewModel()
  {
    Datenbestand? bestand = null;
    var ordner = Datenordner.Standard();
    var ladefehler = "";

    // In der Entwurfsansicht (XAML-Vorschau) nichts laden und keine Dateien anlegen
    if (!Design.IsDesignMode)
    {
      try
      {
        bestand = Datenbestand.Laden(ordner, DateTime.UtcNow);
      }
      catch (Exception ex)
      {
        // Letzte Schutzlinie, damit das Programm statt abzustuerzen eine Meldung zeigt
        ladefehler = ex.Message;
      }
    }

    Navigation.Oeffnen(new HauptseiteViewModel(Navigation, bestand, ordner, ladefehler));
  }

  public Navigation Navigation { get; } = new();

  [RelayCommand]
  private void Zurueck()
  {
    Navigation.Zurueck();
  }
}
