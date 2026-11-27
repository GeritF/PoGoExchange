using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoGoExchange.ViewModels;

public sealed partial class VerwaltungViewModel : SeiteViewModel
{
  private readonly Datenbestand? _bestand;
  private readonly Navigation? _navigation;
  private readonly Func<Datenbestand?>? _neuLaden;

  public VerwaltungViewModel(
    Datenbestand? bestand,
    string ordner,
    string ladefehler,
    Navigation? navigation = null,
    Func<Datenbestand?>? neuLaden = null)
  {
    _bestand = bestand;
    _navigation = navigation;
    _neuLaden = neuLaden;
    Status = StatusText(bestand, ordner, ladefehler);
  }

  // Sicherungen gibt es nur mit geladenen Daten
  public bool HatSicherungen => _bestand != null && _navigation != null && _neuLaden != null;

  [RelayCommand]
  private void SicherungenOeffnen()
  {
    if (HatSicherungen)
    {
      _navigation!.Oeffnen(new SicherungenViewModel(_bestand!, _neuLaden!));
    }
  }

  public bool HatEigeneFormen => _bestand != null && _navigation != null;

  [RelayCommand]
  private void EigeneFormenOeffnen()
  {
    if (HatEigeneFormen)
    {
      _navigation!.Oeffnen(new EigeneFormenViewModel(_navigation, _bestand!));
    }
  }

  public override string Titel => "Verwaltung";

  // Vorlaeufig: Datenordner, Anzahl der Eintraege und Lademeldungen
  public string Status { get; }

  // Rueckmeldung nach dem Kopieren, z.B. "In die Zwischenablage kopiert"
  [ObservableProperty] private string _rueckmeldung = "";

  [RelayCommand]
  private async Task Kopieren()
  {
    var kopieren = AppActions.InZwischenablageKopieren;
    if (kopieren == null)
    {
      Rueckmeldung = "Zwischenablage nicht verf\u00fcgbar";
      return;
    }

    try
    {
      await kopieren(Status);
      Rueckmeldung = "In die Zwischenablage kopiert";
    }
    catch (Exception ex)
    {
      Rueckmeldung = "Kopieren fehlgeschlagen: " + ex.Message;
    }
  }

  private static string StatusText(Datenbestand? bestand, string ordner, string ladefehler)
  {
    var text = new StringBuilder();
    text.AppendLine($"Datenordner: {ordner}");

    if (ladefehler != "")
    {
      text.AppendLine($"Laden fehlgeschlagen: {ladefehler}");
      return text.ToString();
    }

    if (bestand == null)
    {
      text.AppendLine("Keine Daten geladen");
      return text.ToString();
    }

    // Einfach nur, damit die Zeilen nicht so lang sind:
    var iAnzahl = bestand.Habe.Sum(e => e.Anzahl);
    var sAenderung = bestand.LetzteAenderung?.ToString("yyyy-MM-dd HH:mm:ss") ?? "noch keine";

    text.AppendLine($"Pokemon in den Nutzdaten: {bestand.Katalog.AllePokemon().Count()}");
    text.AppendLine($"Eigene Formen: {bestand.EigeneFormen.Count}");
    text.AppendLine($"Habe ich: {bestand.Habe.Count} Zeilen, {iAnzahl} Exemplare");
    text.AppendLine($"Suche ich: {bestand.Suche.Count} Zeilen");
    text.AppendLine($"Habe ich \u00fcber: {bestand.Ueber.Count}");
    text.AppendLine($"Letzte \u00c4nderung: {sAenderung} (UTC)");

    if (bestand.Meldungen.Count == 0)
    {
      text.AppendLine("Meldungen: keine");
      return text.ToString();
    }

    text.AppendLine($"Meldungen: {bestand.Meldungen.Count}");
    foreach (var meldung in bestand.Meldungen)
    {
      var art = meldung.IstFehler ? "Fehler" : "Hinweis";
      text.AppendLine($"- {art} in {meldung.Datei}: {meldung.Text}");
    }

    return text.ToString();
  }
}
