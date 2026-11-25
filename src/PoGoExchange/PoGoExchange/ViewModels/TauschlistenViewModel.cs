using System.Collections.Generic;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

public sealed class TauschlistenViewModel : SeiteViewModel
{
  private readonly Navigation _navigation;
  private readonly Datenbestand? _bestand;

  public TauschlistenViewModel(
    Navigation navigation,
    Datenbestand? bestand,
    string meldungshinweis)
  {
    _navigation = navigation;
    _bestand = bestand;

    var sucheGesperrt = bestand != null && bestand.IstGesperrt(Dateinamen.Suche);
    var habeGesperrt = bestand != null && bestand.IstGesperrt(Dateinamen.Habe);
    var ueberGesperrt = bestand != null && bestand.IstGesperrt(Dateinamen.Ueber);
    var sucheBearbeitbar = bestand != null && !sucheGesperrt;
    var habeBearbeitbar = bestand != null && !habeGesperrt;
    var ueberBearbeitbar = bestand != null && !ueberGesperrt;

    var hinweis = meldungshinweis;
    if (ueberGesperrt)
    {
      hinweis = $"{Dateinamen.Ueber} enth\u00e4lt Fehler, \"Habe ich \u00fcber\" kann nicht " +
        "bearbeitet werden. " + hinweis;
    }

    if (habeGesperrt)
    {
      hinweis = $"{Dateinamen.Habe} enth\u00e4lt Fehler, \"Habe ich\" kann nicht bearbeitet " +
        "werden. " + hinweis;
    }

    if (sucheGesperrt)
    {
      hinweis = $"{Dateinamen.Suche} enth\u00e4lt Fehler, \"Suche ich\" kann nicht bearbeitet " +
        "werden. " + hinweis;
    }

    Meldungshinweis = hinweis;

    Suche = new ListenTabViewModel(
      true,
      (filter, mitErledigten) =>
        bestand == null
          ? new List<AnzeigeGruppe>()
          : ListenAnzeige.Suche(
            bestand.Katalog, bestand.Suche, bestand.Habe, mitErledigten, filter),
      sucheBearbeitbar ? SucheNeuOeffnen : null,
      sucheBearbeitbar ? SucheZeileOeffnen : null);

    Habe = new ListenTabViewModel(
      false,
      (filter, _) =>
        bestand == null
          ? new List<AnzeigeGruppe>()
          : ListenAnzeige.Habe(bestand.Katalog, bestand.Habe, filter),
      habeBearbeitbar ? HabeNeuOeffnen : null,
      habeBearbeitbar ? HabeZeileOeffnen : null);

    Ueber = new ListenTabViewModel(
      false,
      (filter, _) =>
        bestand == null
          ? new List<AnzeigeGruppe>()
          : ListenAnzeige.Ueber(bestand.Katalog, bestand.Ueber, filter),
      ueberBearbeitbar ? UeberNeuOeffnen : null,
      ueberBearbeitbar ? UeberZeileOeffnen : null);
  }

  public override string Titel => "Tauschlisten";

  public string Meldungshinweis { get; }

  public bool HatMeldungen => Meldungshinweis != "";

  public ListenTabViewModel Suche { get; }
  public ListenTabViewModel Habe { get; }
  public ListenTabViewModel Ueber { get; }

  // --- Suche ich ---

  private void SucheNeuOeffnen()
  {
    _navigation.Oeffnen(new SucheNeuViewModel(_bestand!, SucheGespeichert));
  }

  // Nach dem Anlegen: zurueck zur Liste; gab es die Zeile schon, wird sie geoeffnet
  private void SucheGespeichert(AnlegeErgebnis ergebnis)
  {
    _navigation.Zurueck();
    Suche.Aktualisieren();

    if (ergebnis.Vorher == null)
    {
      Suche.Rueckmeldung = ergebnis.Nachher.Erledigt > 0
        ? $"{ergebnis.Nachher.Erledigt} von {ergebnis.Nachher.Ziel} bereits erledigt"
        : "Gespeichert";
      return;
    }

    var text = $"Diese Zeile gab es bereits: {ergebnis.Vorher}, zusammen jetzt: {ergebnis.Nachher}";
    Suche.Rueckmeldung = text;
    _navigation.Oeffnen(
      new SuchZeileViewModel(_navigation, _bestand!, ergebnis.Eintrag, SucheGeaendert, text));
  }

  private void SucheZeileOeffnen(AnzeigeZeile zeile)
  {
    if (zeile.Eintrag is not SucheEintrag eintrag)
    {
      return;
    }

    _navigation.Oeffnen(new SuchZeileViewModel(_navigation, _bestand!, eintrag, SucheGeaendert));
  }

  // Auch "Habe ich" neu aufbauen: beim Erhalten kommt dort ein Exemplar hinzu
  private void SucheGeaendert(string rueckmeldung)
  {
    Suche.Aktualisieren();
    Habe.Aktualisieren();
    Suche.Rueckmeldung = rueckmeldung;
  }

  // --- Habe ich ---
  // Aenderungen in "Habe ich" veraendern auch die Zaehler in "Suche ich"

  private void HabeNeuOeffnen()
  {
    _navigation.Oeffnen(new HabeNeuViewModel(_bestand!, HabeGespeichert));
  }

  private void HabeGespeichert(HabeAnlegeErgebnis ergebnis)
  {
    _navigation.Zurueck();
    Habe.Aktualisieren();
    Suche.Aktualisieren();
    Habe.Rueckmeldung = ergebnis.Vorher == null
      ? "Gespeichert"
      : $"Diese Zeile gab es bereits: {ergebnis.Vorher}x, jetzt {ergebnis.Nachher}x";
  }

  private void HabeZeileOeffnen(AnzeigeZeile zeile)
  {
    if (zeile.Eintrag is not HabeEintrag eintrag)
    {
      return;
    }

    _navigation.Oeffnen(new HabeZeileViewModel(_navigation, _bestand!, eintrag, HabeGeaendert));
  }

  private void HabeGeaendert(string rueckmeldung)
  {
    Habe.Aktualisieren();
    Suche.Aktualisieren();
    Habe.Rueckmeldung = rueckmeldung;
  }

  // --- Habe ich ueber ---

  private void UeberNeuOeffnen()
  {
    _navigation.Oeffnen(new UeberNeuViewModel(_bestand!, UeberGespeichert));
  }

  private void UeberGespeichert(UeberEintrag eintrag, bool neu)
  {
    _navigation.Zurueck();
    Ueber.Aktualisieren();
    Ueber.Rueckmeldung = neu ? "Gespeichert" : "Diesen Eintrag gibt es bereits";
  }

  private void UeberZeileOeffnen(AnzeigeZeile zeile)
  {
    if (zeile.Eintrag is not UeberEintrag eintrag)
    {
      return;
    }

    _navigation.Oeffnen(new UeberZeileViewModel(_navigation, _bestand!, eintrag, UeberGeaendert));
  }

  private void UeberGeaendert(string rueckmeldung)
  {
    Ueber.Aktualisieren();
    Ueber.Rueckmeldung = rueckmeldung;
  }
}
