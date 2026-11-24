using System;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

public partial class MainViewModel : ViewModelBase
{
  public MainViewModel()
  {
    // In der Entwurfsansicht (XAML-Vorschau) nichts laden und keine Dateien anlegen
    Status = Design.IsDesignMode ? "Entwurfsansicht" : StatusLaden();
  }

  // Vorlaeufige Anzeige: Datenordner, Anzahl der Eintraege und Lademeldungen
  public string Status { get; }

  [RelayCommand]
  private void Close()
  {
    AppActions.CloseApp?.Invoke();
  }

  private static string StatusLaden()
  {
    var ordner = Datenordner.Standard();
    try
    {
      var bestand = Datenbestand.Laden(ordner, DateTime.UtcNow);
      var text = new StringBuilder();
      text.AppendLine($"Datenordner: {ordner}");
      text.AppendLine($"Pokemon in den Nutzdaten: {bestand.Katalog.AllePokemon().Count()}");
      text.AppendLine($"Eigene Formen: {bestand.EigeneFormen.Count}");
      text.AppendLine($"Habe ich: {bestand.Habe.Count}");
      text.AppendLine($"Suche ich: {bestand.Suche.Count}");
      text.AppendLine($"Habe ich \u00fcber: {bestand.Ueber.Count}");

      if (bestand.Meldungen.Count == 0)
      {
        text.AppendLine("Meldungen: keine");
      }
      else
      {
        text.AppendLine($"Meldungen: {bestand.Meldungen.Count}");
        foreach (var meldung in bestand.Meldungen)
        {
          var art = meldung.IstFehler ? "Fehler" : "Hinweis";
          text.AppendLine($"- {art} in {meldung.Datei}: {meldung.Text}");
        }
      }

      return text.ToString();
    }
    catch (Exception ex)
    {
      return $"Datenordner: {ordner}\nLaden fehlgeschlagen: {ex.Message}";
    }
  }
}
