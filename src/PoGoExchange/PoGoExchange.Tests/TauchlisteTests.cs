using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class TauschlisteTests
{
  private readonly Pokemon _glumanda = Pkmn(4, "Glumanda");
  private readonly Pokemon _glurak = Pkmn(6, "Glurak");
  private readonly Pokemon _vulpix = Pkmn(37, "Vulpix");
  private readonly Pokemon _flabebe = Pkmn(669, "Flab\u00e9b\u00e9");
  private readonly Pokemon _meltan = Pkmn(808, "Meltan");

  private readonly PokemonForm _glumandaForm;
  private readonly PokemonForm _glurakForm;
  private readonly PokemonForm _vulpixForm;
  private readonly PokemonForm _vulpixAlola;
  private readonly PokemonForm _flabebeRot;
  private readonly PokemonForm _meltanForm;
  private readonly Katalog _katalog;

  public TauschlisteTests()
  {
    _glumandaForm = Form(_glumanda);
    _glurakForm = Form(_glurak);
    _vulpixForm = Form(_vulpix);
    _vulpixAlola = Form(_vulpix, region: "Alola");
    _flabebeRot = Form(_flabebe, form: "Rot");
    _meltanForm = Form(_meltan);

    _katalog = new Katalog(
      new[] { _glumanda, _glurak, _vulpix, _flabebe, _meltan },
      new[] { _glumandaForm, _glurakForm, _vulpixForm, _vulpixAlola, _flabebeRot, _meltanForm },
      new[]
      {
        new RegionBereich { Region = "Unbekannt" },
        new RegionBereich { Region = "Kalos", DexVon = 650, DexBis = 721 },
        new RegionBereich { Region = "Kanto", DexVon = 1, DexBis = 151 },
        new RegionBereich { Region = "Alola", DexVon = 722, DexBis = 807 }
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

  private static SucheEintrag Suche(
    PokemonForm form,
    bool shiny = true,
    bool erledigt = false,
    bool einsZuEins = false,
    bool glueck = false,
    bool hundert = false)
  {
    return new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      FormGuid = form.Guid,
      Shiny = shiny,
      EinsZuEins = einsZuEins,
      Glueck = glueck,
      Hundert = hundert,
      ErledigtAm = erledigt ? DateTime.UtcNow : null
    };
  }

  private static UeberEintrag Ueber(PokemonForm form, bool shiny = true)
  {
    return new UeberEintrag { Guid = Guid.NewGuid(), FormGuid = form.Guid, Shiny = shiny };
  }

  [Fact]
  public void Beispiel_WirdNachRegionUndAbschnittGegliedert()
  {
    var suche = new List<SucheEintrag>
    {
      Suche(_glumandaForm, einsZuEins: true, erledigt: true),
      Suche(_glumandaForm, einsZuEins: true),
      Suche(_glumandaForm, einsZuEins: true),
      Suche(_glurakForm, einsZuEins: true),
      Suche(_glumandaForm, hundert: true),
      Suche(_vulpixAlola),
      Suche(_flabebeRot, shiny: false),
      Suche(_flabebeRot, shiny: false)
    };
    var ueber = new List<UeberEintrag>
    {
      Ueber(_glumandaForm),
      Ueber(_glumandaForm),
      Ueber(_vulpixForm, shiny: false)
    };

    var ergebnis = Tauschliste.Erzeugen(_katalog, suche, ueber);

    var erwartet =
      "Kanto 1:1:\n" +
      "- #4 Glumanda - Shiny (1/3)\n" +
      "- #6 Glurak - Shiny\n" +
      "\n" +
      "Kanto fehlt:\n" +
      "- #4 Glumanda - Shiny [100er]\n" +
      "- #37 Vulpix - Shiny - Alola\n" +
      "\n" +
      "Kanto abzugeben:\n" +
      "- #4 Glumanda - Shiny\n" +
      "- #37 Vulpix\n" +
      "\n" +
      "Kalos fehlt:\n" +
      "- #669 Flab\u00e9b\u00e9 Rot (0/2)\n";
    Assert.Equal(erwartet, ergebnis.Text);
    Assert.Empty(ergebnis.Fehler);
  }

  [Fact]
  public void VollstaendigErledigteZeilen_FehlenImExport()
  {
    var suche = new[] { Suche(_glumandaForm, erledigt: true), Suche(_glumandaForm, erledigt: true) };

    var ergebnis = Tauschliste.Erzeugen(_katalog, suche, Array.Empty<UeberEintrag>());

    Assert.Equal("", ergebnis.Text);
  }

  [Fact]
  public void GlueckUnd100er_StehenGemeinsamInKlammern()
  {
    var suche = new[] { Suche(_glumandaForm, glueck: true, hundert: true) };

    var ergebnis = Tauschliste.Erzeugen(_katalog, suche, Array.Empty<UeberEintrag>());

    Assert.Equal("Kanto fehlt:\n- #4 Glumanda - Shiny [Gl\u00fcck, 100er]\n", ergebnis.Text);
  }

  [Fact]
  public void DexInLuecke_StehtUnterAuffangRegionAmEnde()
  {
    var suche = new[] { Suche(_meltanForm), Suche(_glumandaForm) };

    var ergebnis = Tauschliste.Erzeugen(_katalog, suche, Array.Empty<UeberEintrag>());

    var erwartet =
      "Kanto fehlt:\n" +
      "- #4 Glumanda - Shiny\n" +
      "\n" +
      "Unbekannt fehlt:\n" +
      "- #808 Meltan - Shiny\n";
    Assert.Equal(erwartet, ergebnis.Text);
  }

  [Fact]
  public void UnbekannteFormen_LandenInFehlerliste()
  {
    var defekteSuche = new SucheEintrag { Guid = Guid.NewGuid(), FormGuid = Guid.NewGuid() };
    var defektesUeber = new UeberEintrag { Guid = Guid.NewGuid(), FormGuid = Guid.NewGuid() };
    var suche = new[] { defekteSuche, Suche(_glumandaForm) };

    var ergebnis = Tauschliste.Erzeugen(_katalog, suche, new[] { defektesUeber });

    Assert.Equal("Kanto fehlt:\n- #4 Glumanda - Shiny\n", ergebnis.Text);
    Assert.Equal(2, ergebnis.Fehler.Count);

    var fehlerSuche = ergebnis.Fehler.Single(f => f.Liste == "Suche ich");
    Assert.Equal(defekteSuche.Guid, fehlerSuche.EintragGuid);
    Assert.Equal(defekteSuche.FormGuid, fehlerSuche.FormGuid);

    var fehlerUeber = ergebnis.Fehler.Single(f => f.Liste == "Habe ich \u00fcber");
    Assert.Equal(defektesUeber.Guid, fehlerUeber.EintragGuid);
  }

  [Fact]
  public void GeloeschteEintraege_FehlenImExport_AuchOhneFehlermeldung()
  {
    var geloeschtGueltig = Ueber(_glumandaForm);
    geloeschtGueltig.GeloeschtAm = DateTime.UtcNow;
    var geloeschtDefekt = new UeberEintrag
    {
      Guid = Guid.NewGuid(),
      FormGuid = Guid.NewGuid(),
      GeloeschtAm = DateTime.UtcNow
    };

    var ergebnis = Tauschliste.Erzeugen(
      _katalog,
      Array.Empty<SucheEintrag>(),
      new[] { geloeschtGueltig, geloeschtDefekt });

    Assert.Equal("", ergebnis.Text);
    Assert.Empty(ergebnis.Fehler);
  }
}
