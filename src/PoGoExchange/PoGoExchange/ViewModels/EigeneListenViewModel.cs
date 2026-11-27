using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

// Registerkarte "Eigene Listen" unter Import/Export: alle eigenen Listen als
// Datei exportieren bzw. aus einer Datei importieren (ersetzt alles).
// neuLaden laedt nach dem Import alle Daten neu und zeigt die Hauptseite.
public sealed partial class EigeneListenViewModel : ViewModelBase
{
  private readonly Datenbestand _bestand;
  private readonly Func<Datenbestand?> _neuLaden;
  private byte[]? _paket;

  public EigeneListenViewModel(Datenbestand bestand, Func<Datenbestand?> neuLaden)
  {
    _bestand = bestand;
    _neuLaden = neuLaden;
  }

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(HatMeldung))]
  private string _meldung = "";

  public bool HatMeldung => Meldung != "";

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(RueckfrageSichtbar))]
  private string _rueckfrageText = "";

  public bool RueckfrageSichtbar => RueckfrageText != "";

  [RelayCommand]
  private async Task Exportieren()
  {
    var speichern = AppActions.DateiSpeichern;
    if (speichern == null)
    {
      Meldung = "Dateiauswahl nicht verf\u00fcgbar";
      return;
    }

    try
    {
      var jetzt = DateTime.UtcNow;
      var paket = Exportpaket.Erstellen(_bestand.Ordner, _bestand.Nutzdaten.Version, jetzt);
      var name = Exportpaket.Dateiname(jetzt);
      Meldung = await speichern(name, paket) ? "Exportiert: " + name : "";
    }
    catch (Exception ex) when (ex is not OutOfMemoryException)
    {
      Meldung = "Export fehlgeschlagen: " + ex.Message;
    }
  }

  [RelayCommand]
  private async Task Importieren()
  {
    RueckfrageText = "";
    _paket = null;
    var oeffnen = AppActions.DateiOeffnen;
    if (oeffnen == null)
    {
      Meldung = "Dateiauswahl nicht verf\u00fcgbar";
      return;
    }

    byte[]? paket;
    try
    {
      paket = await oeffnen();
    }
    catch (Exception ex) when (ex is not OutOfMemoryException)
    {
      Meldung = "Datei konnte nicht gelesen werden: " + ex.Message;
      return;
    }

    if (paket == null)
    {
      Meldung = "";
      return;
    }

    var pruefung = Exportpaket.Pruefen(paket, _bestand.Nutzdaten, DateTime.UtcNow);
    if (!pruefung.Gueltig)
    {
      Meldung = pruefung.Fehler!;
      return;
    }

    Meldung = "";
    _paket = paket;
    RueckfrageText =
      $"Paket vom {pruefung.Erstellt.ToLocalTime():dd.MM.yyyy HH:mm}: Habe ich " +
      $"{pruefung.Habe} Zeilen, Suche ich {pruefung.Suche} Zeilen, Habe ich \u00fcber " +
      $"{pruefung.Ueber}, eigene Formen {pruefung.EigeneFormen}. Deine aktuellen Listen " +
      "werden vollst\u00e4ndig durch dieses Paket ersetzt; der aktuelle Stand wird vorher " +
      "gesichert. Wirklich ersetzen?";
  }

  [RelayCommand]
  private void ImportJa()
  {
    var paket = _paket;
    RueckfrageText = "";
    _paket = null;
    if (paket == null)
    {
      return;
    }

    try
    {
      Exportpaket.Einspielen(_bestand.Ordner, paket, DateTime.UtcNow);
    }
    catch (Exception ex) when (ex is not OutOfMemoryException)
    {
      Meldung = "Import fehlgeschlagen: " + ex.Message;
      return;
    }

    // "LetzteAenderung" kommt bewusst aus dem Paket, daher kein AenderungVermerken
    _neuLaden();
  }

  [RelayCommand]
  private void ImportNein()
  {
    RueckfrageText = "";
    _paket = null;
  }
}
