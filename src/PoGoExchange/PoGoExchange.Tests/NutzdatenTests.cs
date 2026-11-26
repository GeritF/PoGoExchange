using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class NutzdatenTests
{
  [Fact]
  public void Pokemon_WerdenGelesen_MitKorrektemSonderzeichen()
  {
    var pokemon = EingebetteteNutzdaten.Lesen<Pokemon, PokemonMap>("pokemon.csv");

    Assert.NotEmpty(pokemon);
    Assert.Contains(pokemon, p => p.DexNr == 669 && p.Name == "Flab\u00e9b\u00e9");
  }

  [Fact]
  public void Formen_VerweisenNurAufVorhandenePokemon()
  {
    var pokemon = EingebetteteNutzdaten.Lesen<Pokemon, PokemonMap>("pokemon.csv");
    var formen = EingebetteteNutzdaten.Lesen<PokemonForm, PokemonFormMap>("formen.csv");
    var bekannt = pokemon.Select(p => p.Guid).ToHashSet();

    var verwaist = formen.Where(f => !bekannt.Contains(f.PokemonGuid)).ToList();
    Assert.Empty(verwaist);
  }

  [Fact]
  public void JedesPokemon_HatMindestensEineForm()
  {
    var pokemon = EingebetteteNutzdaten.Lesen<Pokemon, PokemonMap>("pokemon.csv");
    var formen = EingebetteteNutzdaten.Lesen<PokemonForm, PokemonFormMap>("formen.csv");

    var ohneForm = pokemon
      .Where(p => formen.All(f => f.PokemonGuid != p.Guid))
      .Select(p => p.Name)
      .ToList();
    Assert.Empty(ohneForm);
  }

  [Fact]
  public void Formen_SindProPokemonEindeutig()
  {
    var formen = EingebetteteNutzdaten.Lesen<PokemonForm, PokemonFormMap>("formen.csv");

    var doppelt = formen
      .GroupBy(f => (
        f.PokemonGuid,
        Form: f.Form.ToLowerInvariant(),
        Region: f.Region.ToLowerInvariant()))
      .Where(g => g.Count() > 1)
      .Select(g => g.Key)
      .ToList();
    Assert.Empty(doppelt);
  }

  [Fact]
  public void Guids_SindUeberAlleDateienEindeutig()
  {
    var pokemon = EingebetteteNutzdaten.Lesen<Pokemon, PokemonMap>("pokemon.csv");
    var formen = EingebetteteNutzdaten.Lesen<PokemonForm, PokemonFormMap>("formen.csv");

    var alle = pokemon.Select(p => p.Guid).Concat(formen.Select(f => f.Guid)).ToList();
    Assert.Equal(alle.Count, alle.Distinct().Count());
  }

  [Fact]
  public void Regionen_HabenBeideGrenzenOderKeine()
  {
    var regionen = EingebetteteNutzdaten.Lesen<RegionBereich, RegionBereichMap>("regionen.csv");

    var halb = regionen
      .Where(r => (r.DexVon == null) != (r.DexBis == null))
      .Select(r => r.Region)
      .ToList();
    Assert.Empty(halb);
  }

  [Fact]
  public void Regionen_BereicheSindGueltigUndUeberschneidungsfrei()
  {
    var bereiche = EingebetteteNutzdaten.Lesen<RegionBereich, RegionBereichMap>("regionen.csv")
      .Where(r => r.DexVon != null && r.DexBis != null)
      .OrderBy(r => r.DexVon)
      .ToList();

    foreach (var b in bereiche)
    {
      Assert.True(b.DexVon <= b.DexBis, $"{b.Region}: DexVon {b.DexVon} > DexBis {b.DexBis}");
    }

    for (var i = 1; i < bereiche.Count; i++)
    {
      Assert.True(
        bereiche[i].DexVon > bereiche[i - 1].DexBis,
        $"{bereiche[i - 1].Region} und {bereiche[i].Region} ueberschneiden sich");
    }
  }

  [Fact]
  public void Regionen_HoechstensEineOhneBereich()
  {
    var regionen = EingebetteteNutzdaten.Lesen<RegionBereich, RegionBereichMap>("regionen.csv");

    var ohneBereich = regionen.Count(r => r.DexVon == null && r.DexBis == null);
    Assert.True(ohneBereich <= 1, $"Regionen ohne Bereich: {ohneBereich}");
  }

  [Fact]
  public void Meltan_LiegtInDerAuffangRegion()
  {
    var regionen = EingebetteteNutzdaten.Lesen<RegionBereich, RegionBereichMap>("regionen.csv");
    var katalog = new Katalog(new Pokemon[0], new PokemonForm[0], regionen);

    Assert.Equal("Unbekannt", katalog.RegionZuDex(808));
    Assert.Equal("Unbekannt", katalog.RegionZuDex(809));
    Assert.Equal("Alola", katalog.RegionZuDex(807));
  }

  [Fact]
  public void FormRegionen_SindBekannteRegionen()
  {
    var regionen = EingebetteteNutzdaten.Lesen<RegionBereich, RegionBereichMap>("regionen.csv")
      .Select(r => r.Region)
      .ToHashSet();
    var formen = EingebetteteNutzdaten.Lesen<PokemonForm, PokemonFormMap>("formen.csv");

    var unbekannt = formen
      .Where(f => f.Region != "" && !regionen.Contains(f.Region))
      .Select(f => f.Region)
      .ToList();
    Assert.Empty(unbekannt);
  }

  [Fact]
  public void NutzdatenInfo_EnthaeltVersion()
  {
    var info = EingebetteteNutzdaten.Lesen<SchluesselWert, SchluesselWertMap>("nutzdaten_info.csv");

    var version = info.SingleOrDefault(e => e.Schluessel == "Version");
    Assert.NotNull(version);
    Assert.True(int.TryParse(version.Wert, out var nummer), $"Version keine Zahl: '{version.Wert}'");
    Assert.True(nummer >= 1, $"Version kleiner 1: {nummer}");
  }

  [Fact]
  public void OffizielleFormtexte_ErfuellenDieZeichenregeln()
  {
    var formen = EingebetteteNutzdaten.Lesen<PokemonForm, PokemonFormMap>("formen.csv");

    var verstoesse = formen
      .Where(f => FormVerwaltung.FormTextFehler(f.Form) != null)
      .Select(f => f.Form)
      .ToList();

    Assert.Empty(verstoesse);
  }

  [Fact]
  public void SpalteInGoVerfuegbar_WirdGelesen()
  {
    var pokemon = EingebetteteNutzdaten.Lesen<Pokemon, PokemonMap>("pokemon.csv");

    Assert.Contains(pokemon, p => p.InGoVerfuegbar);
    Assert.Contains(pokemon, p => !p.InGoVerfuegbar);
  }
}
