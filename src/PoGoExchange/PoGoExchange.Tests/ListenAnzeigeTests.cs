using System;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class ListenAnzeigeTests
{
  private readonly Pokemon _glumanda = Pkmn(4, "Glumanda");
  private readonly Pokemon _vulpix = Pkmn(37, "Vulpix");
  private readonly Pokemon _flabebe = Pkmn(669, "Flab\u00e9b\u00e9");

  private readonly PokemonForm _glumandaForm;
  private readonly PokemonForm _vulpixAlola;
  private readonly PokemonForm _flabebeRot;
  private readonly Katalog _katalog;

  public ListenAnzeigeTests()
  {
    _glumandaForm = Form(_glumanda);
    _vulpixAlola = Form(_vulpix, region: "Alola");
    _flabebeRot = Form(_flabebe, form: "Rot");

    _katalog = new Katalog(
      new[] { _glumanda, _vulpix, _flabebe },
      new[] { _glumandaForm, _vulpixAlola, _flabebeRot },
      new[]
      {
        new RegionBereich { Region = "Kanto", DexVon = 1, DexBis = 151 },
        new RegionBereich { Region = "Kalos", DexVon = 650, DexBis = 721 },
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
    int anzahl = 1,
    bool glueck = false,
    bool einsZuEins = false)
  {
    return new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Shiny = true,
      Glueck = glueck,
      EinsZuEins = einsZuEins,
      Anzahl = anzahl
    };
  }

  private static HabeEintrag Habe(PokemonForm form, bool glueck = false)
  {
    return new HabeEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Shiny = true,
      Glueck = glueck
    };
  }

  [Fact]
  public void Suche_WirdNachRegionGruppiert_UndNachDexSortiert()
  {
    var suche = new[] { Suche(_flabebeRot), Suche(_vulpixAlola), Suche(_glumandaForm) };

    var gruppen = ListenAnzeige.Suche(_katalog, suche, Array.Empty<HabeEintrag>(), false, "");

    Assert.Equal(new[] { "Kanto", "Kalos" }, gruppen.Select(g => g.Region));
    Assert.Equal(
      new[] { "#4 Glumanda - Shiny", "#37 Vulpix - Shiny - Alola" },
      gruppen[0].Zeilen.Select(z => z.Anzeige));
    Assert.Equal("#669 Flab\u00e9b\u00e9 Rot - Shiny", gruppen[1].Zeilen.Single().Anzeige);
  }

  [Fact]
  public void Suche_ErledigteZeilen_NurAufWunsch()
  {
    var suche = new[] { Suche(_glumandaForm) };
    var habe = new[] { Habe(_glumandaForm) };

    var ohne = ListenAnzeige.Suche(_katalog, suche, habe, false, "");
    var mit = ListenAnzeige.Suche(_katalog, suche, habe, true, "");

    Assert.Empty(ohne);
    var zeile = mit.Single().Zeilen.Single();
    Assert.True(zeile.Erledigt);
    Assert.Equal("#4 Glumanda - Shiny (1/1)", zeile.Anzeige);
  }

  [Fact]
  public void Suche_ZusatzEnthaeltBedingungen1zu1UndZaehler()
  {
    var suche = new[] { Suche(_glumandaForm, anzahl: 3, glueck: true, einsZuEins: true) };
    var habe = new[] { Habe(_glumandaForm, glueck: true) };

    var zeile = ListenAnzeige.Suche(_katalog, suche, habe, false, "").Single().Zeilen.Single();

    Assert.Equal("#4 Glumanda - Shiny [Gl\u00fcck, 1:1] (1/3)", zeile.Anzeige);
  }

  [Theory]
  [InlineData("", 3)]
  [InlineData("glum", 1)]
  [InlineData("4", 1)]
  [InlineData("#37", 1)]
  [InlineData("37x", 0)]
  public void Filter_NachNameOderDexNummer(string filter, int erwarteteZeilen)
  {
    var suche = new[] { Suche(_flabebeRot), Suche(_vulpixAlola), Suche(_glumandaForm) };

    var gruppen = ListenAnzeige.Suche(_katalog, suche, Array.Empty<HabeEintrag>(), false, filter);

    Assert.Equal(erwarteteZeilen, gruppen.Sum(g => g.Zeilen.Count));
  }

  [Fact]
  public void Habe_ZeigtGlueckUnd100er()
  {
    var exemplar = new HabeEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = _glumanda.Guid,
      FormGuid = _glumandaForm.Guid,
      Shiny = true,
      Glueck = true,
      Hundert = true
    };

    var zeile = ListenAnzeige.Habe(_katalog, new[] { exemplar }, "").Single().Zeilen.Single();

    Assert.Equal("#4 Glumanda - Shiny [Gl\u00fcck, 100er]", zeile.Anzeige);
  }

  [Fact]
  public void FehlerhafteEintraege_EigeneGruppeAmEnde_AuchMitFilter()
  {
    var gueltig = new HabeEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = _glumanda.Guid, FormGuid = _glumandaForm.Guid
    };
    var ohneForm = new HabeEintrag { Guid = Guid.NewGuid(), PokemonGuid = _glumanda.Guid };

    var gruppen = ListenAnzeige.Habe(_katalog, new[] { ohneForm, gueltig }, "xyz");

    var gruppe = Assert.Single(gruppen);
    Assert.Equal(ListenAnzeige.GruppeFehlerhaft, gruppe.Region);
    var zeile = gruppe.Zeilen.Single();
    Assert.True(zeile.Fehlerhaft);
    Assert.Contains("Form fehlt", zeile.Anzeige);
  }

  [Fact]
  public void GeloeschteEintraege_WerdenNichtAngezeigt()
  {
    var geloescht = new UeberEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = _glumanda.Guid,
      FormGuid = _glumandaForm.Guid,
      GeloeschtAm = DateTime.UtcNow
    };

    Assert.Empty(ListenAnzeige.Ueber(_katalog, new[] { geloescht }, ""));
  }
}
