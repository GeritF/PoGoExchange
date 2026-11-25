using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

// Seite "Neue eigene Form" bzw. "Eigene Form bearbeiten".
// Beim Anlegen wird das Pokemon gewaehlt (oder ist vorgegeben), beim
// Bearbeiten steht es fest. fertig(form, rueckmeldung) wird nach dem
// Speichern bzw. Loeschen aufgerufen; form ist beim Loeschen null.
public sealed partial class EigeneFormViewModel : SeiteViewModel
{
  internal const int MaxZiele = 100;
  private const string KeineRegion = "keine";

  private readonly Navigation _navigation;
  private readonly Datenbestand _bestand;
  private readonly Action<PokemonForm?, string> _fertig;
  private readonly PokemonForm? _form;
  private bool _aktualisiertGerade;

  public EigeneFormViewModel(
    Navigation navigation,
    Datenbestand bestand,
    Action<PokemonForm?, string> fertig,
    PokemonForm? form = null,
    Guid? pokemonVorgabe = null)
  {
    _navigation = navigation;
    _bestand = bestand;
    _fertig = fertig;
    _form = form;
    Regionen = new[] { KeineRegion }.Concat(bestand.Katalog.RegionenInReihenfolge()).ToList();

    if (form != null)
    {
      FormText = form.Form;
      RegionIndex = Math.Max(0, Regionen.ToList().IndexOf(form.Region));
      ShinyVerfuegbar = form.ShinyVerfuegbar;
      Kopf = Text(bestand.Katalog.PokemonZuGuid(form.PokemonGuid), form);
      Archiviert = form.ArchiviertAm != null;
      return;
    }

    if (pokemonVorgabe != null)
    {
      var pokemon = bestand.Katalog.PokemonZuGuid(pokemonVorgabe.Value);
      if (pokemon != null)
      {
        Ziel = new PokemonZiel(pokemon.Guid, pokemon.DexNr, pokemon.Name);
        Kopf = Ziel.Anzeige;
      }
    }

    ZieleAktualisieren();
  }

  public override string Titel => IstNeu ? "Neue eigene Form" : "Eigene Form bearbeiten";

  public bool IstNeu => _form == null;

  // Pokemon-Auswahl nur beim Anlegen ohne Vorgabe
  public bool PokemonWaehlbar => IstNeu && Kopf == "";

  public string Kopf { get; } = "";

  public bool HatKopf => Kopf != "";

  public bool Archiviert { get; }

  public IReadOnlyList<string> Regionen { get; }

  [ObservableProperty]
  private string _formText = "";

  [ObservableProperty]
  private int _regionIndex;

  [ObservableProperty]
  private bool _shinyVerfuegbar;

  [ObservableProperty]
  private string _suchtext = "";

  public ObservableCollection<PokemonZiel> Ziele { get; } = new();

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HatMehrHinweis))]
  private string _mehrHinweis = "";

  public bool HatMehrHinweis => MehrHinweis != "";

  [ObservableProperty]
  private PokemonZiel? _auswahl;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(AuswahlText))]
  private PokemonZiel? _ziel;

  public string AuswahlText => Ziel == null
    ? "Noch kein Pokemon ausgew\u00e4hlt"
    : "Ausgew\u00e4hlt: " + Ziel.Anzeige;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HatMeldung))]
  private string _meldung = "";

  public bool HatMeldung => Meldung != "";

  [ObservableProperty]
  private bool _loeschRueckfrageSichtbar;

  // Text einer Form fuer Listen, z.B. "#201 Icognito ? - Alola (archiviert)"
  internal static string Text(Pokemon? pokemon, PokemonForm form)
  {
    var name = pokemon == null ? "Unbekanntes Pokemon" : $"#{pokemon.DexNr} {pokemon.Name}";
    var text = form.Form == "" ? name + " (Grundform)" : name + " " + form.Form;
    if (form.Region != "")
    {
      text += " - " + form.Region;
    }

    return form.ArchiviertAm == null ? text : text + " (archiviert)";
  }

  partial void OnSuchtextChanged(string value)
  {
    ZieleAktualisieren();
  }

  partial void OnAuswahlChanged(PokemonZiel? value)
  {
    if (value != null && !_aktualisiertGerade)
    {
      Ziel = value;
    }
  }

  [RelayCommand]
  private void Speichern()
  {
    var region = RegionIndex <= 0 || RegionIndex >= Regionen.Count ? "" : Regionen[RegionIndex];
    PokemonForm? ergebnis;
    if (_form == null)
    {
      if (Ziel == null)
      {
        Meldung = "Bitte ein Pokemon ausw\u00e4hlen.";
        return;
      }

      var (neu, fehler) = FormVerwaltung.Anlegen(
        _bestand.EigeneFormen, _bestand.Katalog, Ziel.PokemonGuid, FormText, region,
        ShinyVerfuegbar);
      if (fehler != null)
      {
        Meldung = fehler;
        return;
      }

      ergebnis = neu;
    }
    else
    {
      var fehler = FormVerwaltung.Aendern(
        _bestand.Katalog, _form, FormText, region, ShinyVerfuegbar);
      if (fehler != null)
      {
        Meldung = fehler;
        return;
      }

      ergebnis = _form;
    }

    Abschliessen(ergebnis, IstNeu ? "Form angelegt" : "Gespeichert");
  }

  [RelayCommand]
  private void Loeschen()
  {
    if (_form == null)
    {
      return;
    }

    var verwendung = FormVerwaltung.Verwendung(
      _form, _bestand.Habe, _bestand.Suche, _bestand.Ueber);
    if (verwendung.Count > 0)
    {
      Meldung = "Die Form wird noch verwendet in: " + string.Join(", ", verwendung) +
        ". Erst dort entfernen oder \u00e4ndern.";
      return;
    }

    LoeschRueckfrageSichtbar = true;
  }

  [RelayCommand]
  private void LoeschenJa()
  {
    LoeschRueckfrageSichtbar = false;
    if (_form == null)
    {
      return;
    }

    var fehler = FormVerwaltung.Loeschen(
      _bestand.EigeneFormen, _form, _bestand.Habe, _bestand.Suche, _bestand.Ueber);
    if (fehler != null)
    {
      Meldung = fehler;
      return;
    }

    Abschliessen(null, "Form gel\u00f6scht");
  }

  [RelayCommand]
  private void LoeschenNein()
  {
    LoeschRueckfrageSichtbar = false;
  }

  // Datei schreiben, Katalog neu aufbauen, Aufrufer benachrichtigen
  private void Abschliessen(PokemonForm? form, string rueckmeldung)
  {
    var fehler = _bestand.EigeneFormenSpeichern();
    if (fehler != null)
    {
      Meldung = fehler;
      return;
    }

    _bestand.KatalogAufbauen();
    _fertig(form, rueckmeldung);
  }

  private void ZieleAktualisieren()
  {
    if (!PokemonWaehlbar)
    {
      return;
    }

    _aktualisiertGerade = true;
    try
    {
      var ziele = FormVerwaltung.Pokemon(_bestand.Katalog, Suchtext);
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
