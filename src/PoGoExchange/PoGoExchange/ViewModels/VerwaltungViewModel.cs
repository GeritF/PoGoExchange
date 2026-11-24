using System.Linq;
using System.Text;
using PoGoExchange.Services;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PoGoExchange.ViewModels;

public sealed partial class VerwaltungViewModel : SeiteViewModel
{
  public VerwaltungViewModel(Datenbestand? bestand, string ordner, string ladefehler)
  {
    Status = StatusText(bestand, ordner, ladefehler);
  }

  public override string Titel => "Verwaltung";

  // Vorlaeufig: Datenordner, Anzahl der Eintraege und Lademeldungen
  public string Status { get; }

  // Rueckmeldung nach dem Kopieren, z.B. "In die Zwischenablage kopiert"
  [ObservableProperty]
  private string _rueckmeldung = "";

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

    text.AppendLine($"Pokemon in den Nutzdaten: {bestand.Katalog.AllePokemon().Count()}");
    text.AppendLine($"Eigene Formen: {bestand.EigeneFormen.Count}");
    text.AppendLine($"Habe ich: {bestand.Habe.Count(e => e.GeloeschtAm == null)}");
    text.AppendLine($"Suche ich: {bestand.Suche.Count(e => e.GeloeschtAm == null)} Zeilen");
    text.AppendLine($"Habe ich \u00fcber: {bestand.Ueber.Count(e => e.GeloeschtAm == null)}");

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
