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

        Assert.Equal(4, pokemon.Count);
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

        var ohneForm = pokemon.Where(p => formen.All(f => f.PokemonGuid != p.Guid))
            .Select(p => p.Name).ToList();
        Assert.Empty(ohneForm);
    }

    [Fact]
    public void Formen_SindProPokemonEindeutig()
    {
        var formen = EingebetteteNutzdaten.Lesen<PokemonForm, PokemonFormMap>("formen.csv");

        var doppelt = formen
            .GroupBy(f => (f.PokemonGuid, Form: f.Form.ToLowerInvariant(), Region: f.Region.ToLowerInvariant()))
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
    public void Regionen_SindLueckenlosUndUeberschneidungsfrei()
    {
        var regionen = EingebetteteNutzdaten.Lesen<RegionBereich, RegionBereichMap>("regionen.csv")
            .OrderBy(r => r.DexVon).ToList();

        Assert.Equal(1, regionen.First().DexVon);
        for (var i = 1; i < regionen.Count; i++)
            Assert.Equal(regionen[i - 1].DexBis + 1, regionen[i].DexVon);
    }

    [Fact]
    public void FormRegionen_SindBekannteRegionen()
    {
        var regionen = EingebetteteNutzdaten.Lesen<RegionBereich, RegionBereichMap>("regionen.csv")
            .Select(r => r.Region).ToHashSet();
        var formen = EingebetteteNutzdaten.Lesen<PokemonForm, PokemonFormMap>("formen.csv");

        var unbekannt = formen.Where(f => f.Region != "" && !regionen.Contains(f.Region))
            .Select(f => f.Region).ToList();
        Assert.Empty(unbekannt);
    }

    [Fact]
    public void NutzdatenInfo_EnthaeltVersion()
    {
        var info = EingebetteteNutzdaten.Lesen<SchluesselWert, SchluesselWertMap>("nutzdaten_info.csv");

        var version = info.SingleOrDefault(e => e.Schluessel == "Version");
        Assert.NotNull(version);
        Assert.True(int.TryParse(version.Wert, out var nummer), $"Version ist keine Zahl: '{version.Wert}'");
        Assert.True(nummer >= 1, $"Version kleiner 1: {nummer}");
    }
}