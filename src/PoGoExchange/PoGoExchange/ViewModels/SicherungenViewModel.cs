using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace PoGoExchange.ViewModels;

// Eine Sicherung in der Liste, mit eigenem Befehl "Wiederherstellen"
public sealed partial class SicherungEintrag : ViewModelBase
{
  private readonly Action<SicherungEintrag> _wiederherstellen;

  public SicherungEintrag(SicherungInfo info, Action<SicherungEintrag> wiederherstellen)
  {
    Info = info;
    _wiederherstellen = wiederherstellen;
    Text = info.Zeitpunkt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
  }

  public SicherungInfo Info { get; }

  public string Text { get; }

  [RelayCommand]
  private void Wiederherstellen()
  {
    _wiederherstellen(this);
  }
}

// Seite "Sicherungen" (aus der Verwaltung): Sicherung anlegen, Liste,
// Wiederherstellen nach Rueckfrage. neuLaden laedt danach alle Daten neu,
// zeigt die Hauptseite und liefert den neuen Datenbestand.
public sealed partial class SicherungenViewModel : SeiteViewModel
{
  private readonly Datenbestand _bestand;
  private readonly Func<Datenbestand?> _neuLaden;

  public SicherungenViewModel(Datenbestand bestand, Func<Datenbestand?> neuLaden)
  {
    _bestand = bestand;
    _neuLaden = neuLaden;
    Aktualisieren();
  }

  public override string Titel => "Sicherungen";

  public ObservableCollection<SicherungEintrag> Liste { get; } = [];

  [ObservableProperty] private bool _keineVorhanden;

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(HatMeldung))]
  private string _meldung = "";

  public bool HatMeldung => Meldung != "";

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(RueckfrageSichtbar))]
  [NotifyPropertyChangedFor(nameof(RueckfrageText))]
  private SicherungEintrag? _zumWiederherstellen;

  public bool RueckfrageSichtbar => ZumWiederherstellen != null;

  public string RueckfrageText => ZumWiederherstellen == null
    ? ""
    : $"Sicherung vom {ZumWiederherstellen.Text} wiederherstellen? Der aktuelle Stand " +
    "wird vorher gesichert.";

  // Der Zeitpunkt wird hier ermittelt und uebergeben. Gibt es dafuer schon eine
  // Sicherung, passiert bewusst nichts (wie ein nicht getroffener Klick).
  [RelayCommand]
  private void JetztSichern()
  {
    try
    {
      if (Sicherungen.Anlegen(_bestand.Ordner, Sicherungen.Zeitpunkt(DateTime.UtcNow)))
      {
        Meldung = "Sicherung angelegt";
        Aktualisieren();
      }
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      Meldung = "Sicherung fehlgeschlagen: " + ex.Message;
    }
  }

  [RelayCommand]
  private void WiederherstellenJa()
  {
    var ziel = ZumWiederherstellen;
    ZumWiederherstellen = null;
    if (ziel == null)
    {
      return;
    }

    try
    {
      Sicherungen.Wiederherstellen(
        _bestand.Ordner, ziel.Info, Sicherungen.Zeitpunkt(DateTime.UtcNow));
    }
    catch (Exception ex)
      when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
    {
      Meldung = "Wiederherstellen fehlgeschlagen: " + ex.Message;
      Aktualisieren();
      return;
    }

    // Die Wiederherstellung ist eine Aenderung auf diesem Geraet
    _neuLaden()?.AenderungVermerken();
  }

  [RelayCommand]
  private void WiederherstellenNein()
  {
    ZumWiederherstellen = null;
  }

  private void Anfragen(SicherungEintrag eintrag)
  {
    Meldung = "";
    ZumWiederherstellen = eintrag;
  }

  private void Aktualisieren()
  {
    Liste.Clear();
    foreach (var info in Sicherungen.Liste(_bestand.Ordner))
    {
      Liste.Add(new SicherungEintrag(info, Anfragen));
    }

    KeineVorhanden = Liste.Count == 0;
  }
}
