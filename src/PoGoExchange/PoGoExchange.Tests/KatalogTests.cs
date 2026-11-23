using System;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class KatalogTests
{
  private static Katalog MitRegionen(params RegionBereich[] regionen)
  {
    return new Katalog(Array.Empty<Pokemon>(), Array.Empty<PokemonForm>(), regionen);
  }

  private static RegionBereich Bereich(string name, int? von, int? bis)
  {
    return new RegionBereich { Region = name, DexVon = von, DexBis = bis };
  }

  [Fact]
  public void DexImBereich_LiefertDieseRegion()
  {
    var katalog = MitRegionen(Bereich("Kanto", 1, 151), Bereich("Johto", 152, 251));

    Assert.Equal("Kanto", katalog.RegionZuDex(151));
    Assert.Equal("Johto", katalog.RegionZuDex(152));
  }

  [Fact]
  public void DexInLuecke_LiefertRegionOhneBereich()
  {
    var katalog = MitRegionen(
      Bereich("Alola", 722, 807),
      Bereich("Sonstige", null, null),
      Bereich("Galar", 810, 898));

    Assert.Equal("Sonstige", katalog.RegionZuDex(808));
    Assert.Equal("Sonstige", katalog.RegionZuDex(2000));
  }

  [Fact]
  public void OhneRegionOhneBereich_GiltErsatzname()
  {
    var katalog = MitRegionen(Bereich("Kanto", 1, 151));

    Assert.Equal(Katalog.RegionErsatz, katalog.RegionZuDex(500));
    Assert.Equal(new[] { "Kanto", Katalog.RegionErsatz }, katalog.RegionenInReihenfolge());
  }

  [Fact]
  public void Reihenfolge_NachDex_RegionOhneBereichZuletzt()
  {
    var katalog = MitRegionen(
      Bereich("Unbekannt", null, null),
      Bereich("Kalos", 650, 721),
      Bereich("Kanto", 1, 151));

    Assert.Equal(new[] { "Kanto", "Kalos", "Unbekannt" }, katalog.RegionenInReihenfolge());
  }
}
