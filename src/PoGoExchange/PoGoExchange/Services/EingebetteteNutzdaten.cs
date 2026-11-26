using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper.Configuration;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Pokemon, Formen und Regionen als Einheit. Normalerweise die mitgelieferten
// (EingebetteteNutzdaten.Alle); Tests uebergeben eigene, feste Nutzdaten
// an Datenbestand.Laden, damit sie nicht vom Inhalt der echten Daten abhaengen.
// Version stammt aus nutzdaten_info.csv (Schluessel "Version"); Exportpakete
// lassen sich nur bei gleicher Version einspielen.
public sealed record Nutzdaten(
  IReadOnlyList<Pokemon> Pokemon,
  IReadOnlyList<PokemonForm> Formen,
  IReadOnlyList<RegionBereich> Regionen,
  string Version = "");

public static class EingebetteteNutzdaten
{
  public static List<T> Lesen<T, TMap>(string dateiname) where TMap : ClassMap<T>
  {
    var name = "Nutzdaten." + dateiname;
    using var stream = typeof(EingebetteteNutzdaten).Assembly.GetManifestResourceStream(name)
      ?? throw new FileNotFoundException($"Eingebettete Datei nicht gefunden: {name}");
    using var reader = new StreamReader(
      stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
    return CsvSpeicher.Lesen<T, TMap>(reader);
  }

  // Die mit dem Programm gelieferten Nutzdaten
  public static Nutzdaten Alle()
  {
    var version = Lesen<SchluesselWert, SchluesselWertMap>("nutzdaten_info.csv")
      .FirstOrDefault(e => e.Schluessel == "Version")?.Wert ?? "";
    return new Nutzdaten(
      Lesen<Pokemon, PokemonMap>("pokemon.csv"),
      Lesen<PokemonForm, PokemonFormMap>("formen.csv"),
      Lesen<RegionBereich, RegionBereichMap>("regionen.csv"),
      version);
  }
}
