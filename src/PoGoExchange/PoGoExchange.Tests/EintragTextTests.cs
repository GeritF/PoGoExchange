using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class EintragTextTests
{
  private static Pokemon Pkmn(string name, int dex) =>
    new() { Guid = Guid.NewGuid(), DexNr = dex, Name = name };

  private static PokemonForm Form(Pokemon p, string form = "", string region = "") =>
    new() { Guid = Guid.NewGuid(), PokemonGuid = p.Guid, Form = form, Region = region };

  [Fact]
  public void NurName_OhneMerkmale()
  {
    var p = Pkmn("Glumanda", 4);
    var text = EintragText.Erzeugen(p, Form(p), new SucheEintrag());

    Assert.Equal("Glumanda", text);
  }

  [Fact]
  public void FormOhneTrennstrich_HintergrundBeliebig()
  {
    var p = Pkmn("Icognito", 201);
    var e = new SucheEintrag { Shiny = true, HintergrundAktiv = true };
    var text = EintragText.Erzeugen(p, Form(p, "Q"), e);

    Assert.Equal("Icognito Q - Shiny - Hintergrund (beliebig)", text);
  }

  [Fact]
  public void FormMitGeschlecht_UndSonderzeichen()
  {
    var p = Pkmn("Flab\u00e9b\u00e9", 669);
    var e = new HabeEintrag { Geschlecht = Geschlecht.Maennlich, Shiny = true };
    var text = EintragText.Erzeugen(p, Form(p, "Rot"), e);

    Assert.Equal("Flab\u00e9b\u00e9 Rot - M\u00e4nnlich - Shiny", text);
  }

  [Fact]
  public void RegionNachShiny_HintergrundMitBeschreibung()
  {
    var p = Pkmn("Vulpix", 37);
    var e = new SucheEintrag
      { Shiny = true, HintergrundAktiv = true, HintergrundText = "New York 2026" };
    var text = EintragText.Erzeugen(p, Form(p, region: "Alola"), e);

    Assert.Equal("Vulpix - Shiny - Alola - Hintergrund: \"New York 2026\"", text);
  }

  [Fact]
  public void Gigadynamax_UndKostuemMitBeschreibung()
  {
    var p = Pkmn("Glurak", 6);
    var e = new UeberEintrag
    {
      Geschlecht = Geschlecht.Weiblich,
      Dynamax = DynamaxStufe.Gigadynamax,
      KostuemAktiv = true,
      KostuemText = "Partyhut"
    };
    var text = EintragText.Erzeugen(p, Form(p), e);

    Assert.Equal("Glurak - Weiblich - Gigadynamax - Kost\u00fcm: \"Partyhut\"", text);
  }

  [Fact]
  public void KostuemTextOhneHaken_WirdIgnoriert()
  {
    var p = Pkmn("Glumanda", 4);
    var e = new SucheEintrag { KostuemAktiv = false, KostuemText = "Partyhut" };
    var text = EintragText.Erzeugen(p, Form(p), e);

    Assert.Equal("Glumanda", text);
  }

  [Fact]
  public void FormBeliebig_StehtDirektNachDemNamen()
  {
    var p = Pkmn("Vulpix", 37);
    var e = new SucheEintrag { PokemonGuid = p.Guid, Shiny = true };
    var text = EintragText.Erzeugen(p, null, e);

    Assert.Equal("Vulpix - Form (beliebig) - Shiny", text);
  }
}
