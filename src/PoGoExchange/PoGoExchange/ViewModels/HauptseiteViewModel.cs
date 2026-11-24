using System.Linq;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

public sealed partial class HauptseiteViewModel : SeiteViewModel
{
  private readonly Navigation _navigation;
  private readonly Datenbestand? _bestand;
  private readonly string _ordner;
  private readonly string _ladefehler;

  public HauptseiteViewModel(
    Navigation navigation,
    Datenbestand? bestand,
    string ordner,
    string ladefehler)
  {
    _navigation = navigation;
    _bestand = bestand;
    _ordner = ordner;
    _ladefehler = ladefehler;
    Meldungshinweis = MeldungsText(bestand, ladefehler);
  }

  public override string Titel => "PoGoExchange";

  public string Meldungshinweis { get; }

  public bool HatMeldungen => Meldungshinweis != "";

  [RelayCommand]
  private void TauschlistenOeffnen()
  {
    _navigation.Oeffnen(new TauschlistenViewModel(_navigation, _bestand, Meldungshinweis));
  }

  [RelayCommand]
  private void ImportExportOeffnen()
  {
    _navigation.Oeffnen(new PlatzhalterViewModel(
      "Import/Export",
      "Folgt in einem sp\u00e4teren Bauschritt: eigene Tauschliste erzeugen " +
      "und fremde Listen abgleichen."));
  }

  [RelayCommand]
  private void VerwaltungOeffnen()
  {
    _navigation.Oeffnen(new VerwaltungViewModel(_bestand, _ordner, _ladefehler));
  }

  [RelayCommand]
  private void Beenden()
  {
    AppActions.CloseApp?.Invoke();
  }

  // Kurzer Hinweis fuer die Hinweisleiste; leer, wenn es nichts zu melden gibt
  internal static string MeldungsText(Datenbestand? bestand, string ladefehler)
  {
    if (ladefehler != "")
    {
      return "Die Daten konnten nicht geladen werden. Details unter Verwaltung.";
    }

    if (bestand == null || bestand.Meldungen.Count == 0)
    {
      return "";
    }

    var anzahl = bestand.Meldungen.Count;
    var fehler = bestand.Meldungen.Count(m => m.IstFehler);
    return fehler > 0
      ? $"{anzahl} Meldungen beim Laden, davon {fehler} Fehler. Details unter Verwaltung."
      : $"{anzahl} Hinweise beim Laden. Details unter Verwaltung.";
  }
}
