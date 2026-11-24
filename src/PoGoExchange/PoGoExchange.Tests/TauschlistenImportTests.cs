using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class TauschlistenImportTests
{
  private readonly Pokemon _glumanda = Pkmn(4, "Glumanda");
  private readonly Pokemon _glurak = Pkmn(6, "Glurak");
  private readonly Pokemon _vulpix = Pkmn(37, "Vulpix");
  private readonly Pokemon _flabebe = Pkmn(669, "Flab\u00e9b\u00e9");

  private readonly PokemonForm _glumandaForm;
  private readonly PokemonForm _glurakForm;
  private readonly PokemonForm _vulpixForm;
  private readonly PokemonForm _vulpixAlola;
  private readonly PokemonForm _flabebeRot;
  private readonly Katalog _katalog;

  public TauschlistenImportTests()
  {
    _glumandaForm = Form(_glumanda);
    _glurakForm = Form(_glurak);
    _vulpixForm = Form(_vulpix);
    _vulpixAlola = Form(_vulpix, region: "Alola");
    _flabebeRot = Form(_flabebe, form: "Rot");

    _katalog = new Katalog(
      new[] { _glumanda, _glurak, _vulpix, _flabebe },
      new[] { _glumandaForm, _glurakForm, _vulpixForm, _vulpixAlola, _flabebeRot },
      new[]
      {
        new RegionBereich { Region = "Kanto", DexVon = 1, DexBis = 151 },
        new RegionBereich { Region = "Kalos", DexVon = 650, DexBis = 721 },
        new RegionBereich { Region = "Alola", DexVon = 722, DexBis = 807 },
        new RegionBereich { Region = "Galar", DexVon = 810, DexBis = 898 },
        new RegionBereich { Region = "Unbekannt" }
      });
  }

  private static Pokemon Pkmn(int dex, string name)
  {
    return new Pokemon { Guid = Guid.NewGuid(), DexNr = dex, Name = name };
  }

  private static PokemonForm Form(Pokemon p, string form = "", string region = "")
  {
    return new PokemonForm
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = p.Guid,
      Form = form,
      Region = region
    };
  }

  // Vergleichbare Beschreibung eines Eintrags: Abschnitt, Form und alle Merkmale
  private static string Beschreibung(string abschnitt, ListenEintrag e)
  {
    return string.Join(
      "|",
      abschnitt,
      e.FormGuid,
      e.Geschlecht,
      e.Shiny,
      e.Dynamax,
      e.KostuemAktiv,
      e.KostuemText,
      e.HintergrundAktiv,
      e.HintergrundText);
  }

  [Fact]
  public void Rundreise_ExportUndImport_LiefernGleicheEintraege()
  {
    var glumandaEinsZuEins = new SucheEintrag
    {
      FormGuid = _glumandaForm.Guid, Shiny = true, EinsZuEins = true
    };
    var glumanda100er = new SucheEintrag
    {
      FormGuid = _glumandaForm.Guid, Shiny = true, Hundert = true
    };
    var vulpixAlola = new SucheEintrag
    {
      FormGuid = _vulpixAlola.Guid,
      Shiny = true,
      HintergrundAktiv = true,
      HintergrundText = "New York - 2026"
    };
    var flabebeWeiblich = new SucheEintrag
    {
      FormGuid = _flabebeRot.Guid, Geschlecht = Geschlecht.Weiblich
    };
    var glurakGiga = new SucheEintrag
    {
      FormGuid = _glurakForm.Guid, Dynamax = DynamaxStufe.Gigadynamax, KostuemAktiv = true
    };
    var vulpixUeber = new UeberEintrag { FormGuid = _vulpixForm.Guid };
    var glurakUeber = new UeberEintrag
    {
      FormGuid = _glurakForm.Guid,
      Geschlecht = Geschlecht.Maennlich,
      Dynamax = DynamaxStufe.Dynamax,
      HintergrundAktiv = true
    };

    var export = Tauschliste.Erzeugen(
      _katalog,
      new[] { glumandaEinsZuEins, glumanda100er, vulpixAlola, flabebeWeiblich, glurakGiga },
      new[] { vulpixUeber, glurakUeber });
    var import = TauschlistenImport.Einlesen(_katalog, export.Text);

    var erwartet = new List<string>
    {
      Beschreibung("1:1", glumandaEinsZuEins),
      Beschreibung("fehlt", glumanda100er),
      Beschreibung("fehlt", vulpixAlola),
      Beschreibung("fehlt", flabebeWeiblich),
      Beschreibung("fehlt", glurakGiga),
      Beschreibung("abzugeben", vulpixUeber),
      Beschreibung("abzugeben", glurakUeber)
    };
    var gelesen = import.Eintraege.Select(e => Beschreibung(e.Abschnitt, e)).ToList();

    Assert.Empty(export.Fehler);
    Assert.Empty(import.Fehler);
    Assert.Equal(erwartet.OrderBy(s => s), gelesen.OrderBy(s => s));
  }

  [Fact]
  public void ZaehlerUndZaehlbedingungen_WerdenIgnoriert()
  {
    var text = "Kanto fehlt:\n- #4 Glumanda - Shiny [Gl\u00fcck, 100er] (1/3)\n";

    var import = TauschlistenImport.Einlesen(_katalog, text);

    Assert.Empty(import.Fehler);
    var eintrag = Assert.Single(import.Eintraege);
    Assert.Equal("fehlt", eintrag.Abschnitt);
    Assert.Equal(_glumandaForm.Guid, eintrag.FormGuid);
    Assert.True(eintrag.Shiny);
  }

  [Theory]
  [InlineData("- #999 Irgendwas - Shiny", "Dex-Nummer 999 unbekannt")]
  [InlineData("- #4 Glurak - Shiny", "Name passt nicht")]
  [InlineData("- #4 Glumanda - Glitzer", "Unbekannter Bestandteil: Glitzer")]
  [InlineData("- #37 Vulpix - Galar", "Form nicht gefunden")]
  [InlineData("- #669 Flab\u00e9b\u00e9 Blau", "Form nicht gefunden")]
  public void NichtZuordenbareZeile_LandetMitGrundInFehlerliste(string zeile, string grundEnthaelt)
  {
    var import = TauschlistenImport.Einlesen(_katalog, "Kanto fehlt:\n" + zeile + "\n");

    Assert.Empty(import.Eintraege);
    var fehler = Assert.Single(import.Fehler);
    Assert.Equal(2, fehler.ZeilenNummer);
    Assert.Contains(grundEnthaelt, fehler.Grund);
  }

  [Fact]
  public void EintragOhneAbschnitt_LandetInFehlerliste()
  {
    var import = TauschlistenImport.Einlesen(_katalog, "- #4 Glumanda\n");

    Assert.Empty(import.Eintraege);
    var fehler = Assert.Single(import.Fehler);
    Assert.Contains("keinem bekannten Abschnitt", fehler.Grund);
  }

  [Fact]
  public void Robust_GegenBegruessungSternchenWindowsZeilenendenUndSchreibweise()
  {
    var text =
      "Hallo, hier meine Liste\r\n" +
      "\r\n" +
      "KANTO FEHLT:\r\n" +
      "* #4 glumanda - shiny\r\n" +
      "#6 Glurak\r\n" +
      "Viele Gruesse\r\n";

    var import = TauschlistenImport.Einlesen(_katalog, text);

    Assert.Empty(import.Fehler);
    Assert.Equal(2, import.Eintraege.Count);
    Assert.Contains(import.Eintraege, e => e.FormGuid == _glumandaForm.Guid && e.Shiny);
    Assert.Contains(import.Eintraege, e => e.FormGuid == _glurakForm.Guid && !e.Shiny);
  }

  [Fact]
  public void UnbekannteUeberschrift_BeendetAbschnitt()
  {
    var text =
      "Kanto fehlt:\n" +
      "- #4 Glumanda\n" +
      "Kanto Wunschliste:\n" +
      "- #6 Glurak\n";

    var import = TauschlistenImport.Einlesen(_katalog, text);

    var eintrag = Assert.Single(import.Eintraege);
    Assert.Equal(_glumandaForm.Guid, eintrag.FormGuid);
    var fehler = Assert.Single(import.Fehler);
    Assert.Equal(4, fehler.ZeilenNummer);
  }
}
