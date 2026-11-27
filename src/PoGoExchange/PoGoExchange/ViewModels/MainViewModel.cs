using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Services;
using System;

namespace PoGoExchange.ViewModels;

public partial class MainViewModel : ViewModelBase
{
  public MainViewModel()
  {
    Starten();
  }

  public Navigation Navigation { get; } = new();

  [RelayCommand]
  private void Zurueck()
  {
    Navigation.Zurueck();
  }

  // Laedt alle Daten und zeigt die Hauptseite als einzige Seite. Wird beim
  // Programmstart und nach dem Wiederherstellen einer Sicherung aufgerufen.
  // Liefert den geladenen Datenbestand (null bei Ladefehler oder Entwurfsansicht).
  private Datenbestand? Starten()
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

    Navigation.Ersetzen(
      new HauptseiteViewModel(Navigation, bestand, ordner, ladefehler, Starten));
    return bestand;
  }
}
