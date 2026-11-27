using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class SammelSucheRegionTests
{
  private readonly Pokemon _glumanda = Pkmn(4, "Glumanda");
  private readonly Pokemon _feurigel = Pkmn(155, "Feurigel");
  private readonly Pokemon _flabebe = Pkmn(669, "Flab\u00e9b\u00e9");
  private readonly PokemonForm _flabebeRot;
  private readonly PokemonForm _flabebeBlau;
  private readonly Katalog _katalog;

  public SammelSucheRegionTests()
  {
    _flabebeRot = Form(_flabebe, "Rot", shiny: true);
    _flabebeBlau = Form(_flabebe, "Blau", shiny: false);
    _katalog = new Katalog(
      [_glumanda, _feurigel, _flabebe],
      [Form(_glumanda), Form(_feurigel), _flabebeRot, _flabebeBlau],
      [
        new RegionBereich { Region = "Kanto", DexVon = 1, DexBis = 151 },
        new RegionBereich { Region = "Johto", DexVon = 152, DexBis = 251 },
        new RegionBereich { Region = "Kalos", DexVon = 650, DexBis = 721 }
      ]);
  }

  private static Pokemon Pkmn(int dex, string name)
  {
    return new Pokemon
    {
      Guid = Guid.NewGuid(), DexNr = dex, Name = name, InGoVerfuegbar = true
    };
  }

  private static PokemonForm Form(Pokemon p, string form = "", bool shiny = true)
  {
    return new PokemonForm
    {
      Guid = Guid.NewGuid(), PokemonGuid = p.Guid, Form = form, ShinyVerfuegbar = shiny
    };
  }

  private SammelErgebnis Anlegen(SucheEintrag vorlage, SammelModus modus, string? region)
  {
    return SammelSuche.Anlegen(
      _katalog, vorlage, modus,
      [], [], region);
  }

  [Fact]
  public void Region_SchraenktAufDexBereichEin()
  {
    var ergebnis = Anlegen(new SucheEintrag(), SammelModus.ProForm, "Johto");

    var eintrag = Assert.Single(ergebnis.NeueEintraege);
    Assert.Equal(_feurigel.Guid, eintrag.PokemonGuid);
  }

  [Fact]
  public void Region_OhneGrossKleinschreibung()
  {
    var ergebnis = Anlegen(new SucheEintrag(), SammelModus.ProForm, "jOHTO");

    var eintrag = Assert.Single(ergebnis.NeueEintraege);
    Assert.Equal(_feurigel.Guid, eintrag.PokemonGuid);
  }

  [Fact]
  public void Shiny_UeberspringtFormenOhneVerfuegbaresShiny()
  {
    var ergebnis = Anlegen(new SucheEintrag { Shiny = true }, SammelModus.ProForm, "Kalos");

    var eintrag = Assert.Single(ergebnis.NeueEintraege);
    Assert.Equal(_flabebeRot.Guid, eintrag.FormGuid);
    Assert.Equal(1, ergebnis.NichtVerfuegbar);
  }

  [Fact]
  public void OhneShiny_WerdenAlleFormenAngelegt()
  {
    var ergebnis = Anlegen(new SucheEintrag(), SammelModus.ProForm, "Kalos");

    Assert.Equal(2, ergebnis.NeueEintraege.Count);
    Assert.Equal(0, ergebnis.NichtVerfuegbar);
  }

  [Fact]
  public void ProPokemon_ShinyVerfuegbarWennEineFormVerfuegbar()
  {
    var ergebnis = Anlegen(new SucheEintrag { Shiny = true }, SammelModus.ProPokemon, null);

    Assert.Equal(3, ergebnis.NeueEintraege.Count);
    Assert.All(ergebnis.NeueEintraege, e => Assert.True(e.FormBeliebig));
  }

  [Fact]
  public void PokemonNichtInGo_WirdUebersprungen_UndGezaehlt()
  {
    var inGo = Pkmn(4, "Glumanda");
    var nichtInGo = Pkmn(5, "Glutexo");
    nichtInGo.InGoVerfuegbar = false;
    var katalog = new Katalog(
      [inGo, nichtInGo],
      [Form(inGo), Form(nichtInGo)],
      []);

    var ergebnis = SammelSuche.Anlegen(
      katalog, new SucheEintrag(), SammelModus.ProForm,
      [], []);

    Assert.Equal(inGo.Guid, Assert.Single(ergebnis.NeueEintraege).PokemonGuid);
    Assert.Equal(1, ergebnis.NichtInGo);
  }
}
