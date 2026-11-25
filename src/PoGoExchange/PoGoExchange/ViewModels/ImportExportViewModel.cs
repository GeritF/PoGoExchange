using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

// Seite "Import/Export": eigene Tauschliste erzeugen und kopieren,
// fremde Tauschliste einfuegen und abgleichen.
// Die fremde Liste wird bewusst nicht gespeichert (sie veraltet schnell).
public sealed partial class ImportExportViewModel : SeiteViewModel
{
  private readonly Navigation _navigation;
  private readonly Datenbestand? _bestand;

  public ImportExportViewModel(Navigation navigation, Datenbestand? bestand)
  {
    _navigation = navigation;
    _bestand = bestand;

    if (bestand == null)
    {
      EigeneListe = "";
      ExportFehler = new List<string>();
      ListeLeerText = "Die Daten konnten nicht geladen werden. Details unter Verwaltung.";
      return;
    }

    var ergebnis = Tauschliste.Erzeugen(
      bestand.Katalog, bestand.Suche, bestand.Habe, bestand.Ueber);
    EigeneListe = ergebnis.Text;
    ExportFehler = ergebnis.Fehler.Select(f => $"{f.Liste}: {f.Meldung}").ToList();
    ListeLeerText = ergebnis.Text == ""
      ? "Deine Tauschliste ist leer: Es gibt keine offenen Suchen und nichts abzugeben."
      : "";
  }

  public override string Titel => "Import/Export";

  // --- Meine Tauschliste ---

  public string EigeneListe { get; }

  public string ListeLeerText { get; }

  public bool ListeLeer => ListeLeerText != "";

  // Eintraege, die nicht exportiert werden konnten (Liste und Grund)
  public IReadOnlyList<string> ExportFehler { get; }

  public bool HatExportFehler => ExportFehler.Count > 0;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HatExportRueckmeldung))]
  private string _exportRueckmeldung = "";

  public bool HatExportRueckmeldung => ExportRueckmeldung != "";

  [RelayCommand]
  private async Task Kopieren()
  {
    var kopieren = AppActions.InZwischenablageKopieren;
    if (kopieren == null)
    {
      ExportRueckmeldung = "Zwischenablage nicht verf\u00fcgbar";
      return;
    }

    try
    {
      await kopieren(EigeneListe);
      ExportRueckmeldung = "In die Zwischenablage kopiert";
    }
    catch (Exception ex)
    {
      ExportRueckmeldung = "Kopieren fehlgeschlagen: " + ex.Message;
    }
  }

  // --- Fremde Liste abgleichen ---

  [ObservableProperty]
  private string _fremderText = "";

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HatAbgleichMeldung))]
  private string _abgleichMeldung = "";

  public bool HatAbgleichMeldung => AbgleichMeldung != "";

  [RelayCommand]
  private async Task Einfuegen()
  {
    var lesen = AppActions.AusZwischenablageLesen;
    if (lesen == null)
    {
      AbgleichMeldung = "Zwischenablage nicht verf\u00fcgbar";
      return;
    }

    try
    {
      var text = await lesen();
      if (string.IsNullOrWhiteSpace(text))
      {
        AbgleichMeldung = "Die Zwischenablage enth\u00e4lt keinen Text.";
        return;
      }

      FremderText = text;
      AbgleichMeldung = "";
    }
    catch (Exception ex)
    {
      AbgleichMeldung = "Einf\u00fcgen fehlgeschlagen: " + ex.Message;
    }
  }

  [RelayCommand]
  private void Abgleichen()
  {
    if (_bestand == null)
    {
      AbgleichMeldung = "Die Daten konnten nicht geladen werden. Details unter Verwaltung.";
      return;
    }

    if (string.IsNullOrWhiteSpace(FremderText))
    {
      AbgleichMeldung = "Bitte zuerst eine Tauschliste einf\u00fcgen.";
      return;
    }

    AbgleichMeldung = "";
    var fremd = TauschlistenImport.Einlesen(_bestand.Katalog, FremderText);
    var ergebnis = Abgleich.Durchfuehren(fremd, _bestand.Suche, _bestand.Habe, _bestand.Ueber);
    _navigation.Oeffnen(new AbgleichErgebnisViewModel(_bestand, ergebnis));
  }
}
