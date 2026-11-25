using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

// Seite "Neuer Eintrag" fuer "Habe ich ueber": Merkmale oben (ohne Glueck
// und 100er), Auswahl der Form unten. Keine Anzahl.
// gespeichert(eintrag, neu): neu ist false, wenn es den Eintrag schon gab.
public sealed partial class UeberNeuViewModel : SeiteViewModel
{
  internal const int MaxZiele = 100;

  private readonly Datenbestand _bestand;
  private readonly Action<UeberEintrag, bool> _gespeichert;
  private bool _aktualisiertGerade;

  public UeberNeuViewModel(Datenbestand bestand, Action<UeberEintrag, bool> gespeichert)
  {
    _bestand = bestand;
    _gespeichert = gespeichert;
    ZieleAktualisieren();
  }

  public override string Titel => "Neuer Eintrag (Habe ich \u00fcber)";

  public ExemplarViewModel Merkmale { get; } = new(mitGlueckUndHundert: false);

  [ObservableProperty]
  private string _suchtext = "";

  public ObservableCollection<FormZiel> Ziele { get; } = new();

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HatMehrHinweis))]
  private string _mehrHinweis = "";

  public bool HatMehrHinweis => MehrHinweis != "";

  [ObservableProperty]
  private FormZiel? _auswahl;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(AuswahlText))]
  private FormZiel? _ziel;

  public string AuswahlText => Ziel == null
    ? "Noch kein Pokemon ausgew\u00e4hlt"
    : "Ausgew\u00e4hlt: " + Ziel.Anzeige;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HatMeldung))]
  private string _meldung = "";

  public bool HatMeldung => Meldung != "";

  partial void OnSuchtextChanged(string value)
  {
    ZieleAktualisieren();
  }

  partial void OnAuswahlChanged(FormZiel? value)
  {
    if (value != null && !_aktualisiertGerade)
    {
      Ziel = value;
    }
  }

  [RelayCommand]
  private void Speichern()
  {
    if (Ziel == null)
    {
      Meldung = "Bitte ein Pokemon ausw\u00e4hlen.";
      return;
    }

    if (Merkmale.HatFehler)
    {
      Meldung = Merkmale.Fehler;
      return;
    }

    var (eintrag, neu) = UeberBearbeitung.Anlegen(_bestand.Ueber, Merkmale.Vorlage(), Ziel);
    if (neu)
    {
      var fehler = _bestand.UeberSpeichern();
      if (fehler != null)
      {
        Meldung = fehler;
        return;
      }
    }

    _gespeichert(eintrag, neu);
  }

  private void ZieleAktualisieren()
  {
    _aktualisiertGerade = true;
    try
    {
      var ziele = HabeBearbeitung.Ziele(_bestand.Katalog, Suchtext);
      Ziele.Clear();
      foreach (var ziel in ziele.Take(MaxZiele))
      {
        Ziele.Add(ziel);
      }

      MehrHinweis = ziele.Count > MaxZiele
        ? $"Es werden h\u00f6chstens {MaxZiele} Eintr\u00e4ge angezeigt. Bitte das Suchfeld nutzen."
        : "";

      Auswahl = Ziel != null && Ziele.Contains(Ziel) ? Ziele.First(z => z == Ziel) : null;
    }
    finally
    {
      _aktualisiertGerade = false;
    }
  }
}
