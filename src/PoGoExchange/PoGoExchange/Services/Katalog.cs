using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Nachschlagewerk fuer Pokemon, Formen und Regionen.
//
// Hinweis zu den Regionen: Sie werden fuer die Gruppierung der Tauschliste
// benoetigt. Jede Ueberschrift der Tauschliste besteht aus Region und
// Abschnitt, z.B. "Kanto 1:1:". Massgeblich ist die Dex-Nummer des Pokemon,
// nicht die Spalte "Region" der Form (Alola-Vulpix steht unter Kanto).
// Regionen ohne Dex-Bereich nehmen alle Dex-Nummern auf, die in keinem
// Bereich liegen (z.B. Meltan und Melmetal unter "Unbekannt").
public sealed class Katalog
{
  // Name der Auffang-Region, falls regionen.csv keine Region ohne Bereich enthaelt
  public const string RegionErsatz = "Unbekannt";

  private readonly Dictionary<Guid, Pokemon> _pokemon;
  private readonly Dictionary<Guid, PokemonForm> _formen;
  private readonly Dictionary<int, Pokemon> _pokemonNachDex;
  private readonly ILookup<Guid, PokemonForm> _formenNachPokemon;

  // Regionen fuer die Gruppierung der Tauschliste (siehe Hinweis oben)
  private readonly List<RegionBereich> _regionenMitBereich;
  private readonly List<string> _regionenReihenfolge;
  private readonly string _auffangRegion;

  public Katalog(
    IEnumerable<Pokemon> pokemon,
    IEnumerable<PokemonForm> formen,
    IEnumerable<RegionBereich> regionen)
  {
    _pokemon = pokemon.ToDictionary(p => p.Guid);
    _formen = formen.ToDictionary(f => f.Guid);
    _pokemonNachDex = _pokemon.Values
      .GroupBy(p => p.DexNr)
      .ToDictionary(g => g.Key, g => g.First());
    _formenNachPokemon = _formen.Values.ToLookup(f => f.PokemonGuid);

    var alle = regionen.ToList();
    _regionenMitBereich = alle
      .Where(r => r.DexVon != null && r.DexBis != null)
      .OrderBy(r => r.DexVon)
      .ToList();
    var ohneBereich = alle
      .Where(r => r.DexVon == null || r.DexBis == null)
      .Select(r => r.Region)
      .ToList();

    _auffangRegion = ohneBereich.FirstOrDefault() ?? RegionErsatz;
    _regionenReihenfolge = _regionenMitBereich
      .Select(r => r.Region)
      .Concat(ohneBereich)
      .ToList();
    if (!_regionenReihenfolge.Contains(_auffangRegion))
    {
      _regionenReihenfolge.Add(_auffangRegion);
    }
  }

  // Reihenfolge der Ueberschriften in der Tauschliste (siehe Hinweis oben):
  // Regionen mit Bereich nach Dex-Nummer, Regionen ohne Bereich zuletzt
  public IReadOnlyList<string> RegionenInReihenfolge()
  {
    return _regionenReihenfolge;
  }

  // Region fuer die Gruppierung in der Tauschliste (siehe Hinweis oben)
  public string RegionZuDex(int dexNr)
  {
    var bereich = _regionenMitBereich
      .FirstOrDefault(r => dexNr >= r.DexVon && dexNr <= r.DexBis);
    return bereich?.Region ?? _auffangRegion;
  }

  // Liefert den Regionsnamen in der Schreibweise aus regionen.csv, oder null.
  // Wird beim Einlesen gebraucht, um Regionalformen (z.B. "Alola") zu erkennen.
  public string? RegionsnameFinden(string text)
  {
    return _regionenReihenfolge.FirstOrDefault(
      r => string.Equals(r, text, StringComparison.OrdinalIgnoreCase));
  }

  // Liefert Pokemon und Form zu einer FormGuid, oder null wenn unbekannt
  public (Pokemon Pokemon, PokemonForm Form)? Aufloesen(Guid formGuid)
  {
    if (!_formen.TryGetValue(formGuid, out var form))
    {
      return null;
    }

    if (!_pokemon.TryGetValue(form.PokemonGuid, out var pokemon))
    {
      return null;
    }

    return (pokemon, form);
  }

  public Pokemon? PokemonZuDex(int dexNr)
  {
    return _pokemonNachDex.TryGetValue(dexNr, out var pokemon) ? pokemon : null;
  }

  // Sucht eine Form ueber Form-Text und Region, ohne Gross-/Kleinschreibung
  public PokemonForm? FormSuchen(Guid pokemonGuid, string form, string region)
  {
    return _formenNachPokemon[pokemonGuid].FirstOrDefault(f =>
      string.Equals(f.Form, form, StringComparison.OrdinalIgnoreCase) &&
      string.Equals(f.Region, region, StringComparison.OrdinalIgnoreCase));
  }
}
