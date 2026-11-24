using System.Collections.Generic;
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
    var sucheBearbeitbar = bestand != null && !sucheGesperrt;
    Meldungshinweis = sucheGesperrt
      ? $"{Dateinamen.Suche} enth\u00e4lt Fehler, \"Suche ich\" kann nicht bearbeitet werden. " +
        meldungshinweis
      : meldungshinweis;

    Suche = new ListenTabViewModel(
      true,
      (filter, mitErledigten) =>
        bestand == null
          ? new List<AnzeigeGruppe>()
          : ListenAnzeige.Suche(
            bestand.Katalog, bestand.Suche, bestand.Habe, mitErledigten, filter),
      sucheBearbeitbar ? NeuOeffnen : null,
      sucheBearbeitbar ? ZeileOeffnen : null);

    Habe = new ListenTabViewModel(false, (filter, _) =>
      bestand == null
        ? new List<AnzeigeGruppe>()
        : ListenAnzeige.Habe(bestand.Katalog, bestand.Habe, filter));

    Ueber = new ListenTabViewModel(false, (filter, _) =>
      bestand == null
        ? new List<AnzeigeGruppe>()
        : ListenAnzeige.Ueber(bestand.Katalog, bestand.Ueber, filter));
  }

  public override string Titel => "Tauschlisten";

  public string Meldungshinweis { get; }

  public bool HatMeldungen => Meldungshinweis != "";

  public ListenTabViewModel Suche { get; }
  public ListenTabViewModel Habe { get; }
  public ListenTabViewModel Ueber { get; }

  private void NeuOeffnen()
  {
    _navigation.Oeffnen(new SucheNeuViewModel(_bestand!, Gespeichert));
  }

  // Nach dem Anlegen: zurueck zur Liste; gab es die Zeile schon, wird sie geoeffnet
  private void Gespeichert(AnlegeErgebnis ergebnis)
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
      new SuchZeileViewModel(_navigation, _bestand!, ergebnis.Eintrag, ZeileGeaendert, text));
  }

  private void ZeileOeffnen(AnzeigeZeile zeile)
  {
    if (zeile.Eintrag == null)
    {
      return;
    }

    _navigation.Oeffnen(
      new SuchZeileViewModel(_navigation, _bestand!, zeile.Eintrag, ZeileGeaendert));
  }

  private void ZeileGeaendert(string rueckmeldung)
  {
    Suche.Aktualisieren();
    Suche.Rueckmeldung = rueckmeldung;
  }
}
