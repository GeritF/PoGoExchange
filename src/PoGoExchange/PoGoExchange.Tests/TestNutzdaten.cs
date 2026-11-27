using CsvHelper.Configuration;
using PoGoExchange.Models;
using PoGoExchange.Services;
using System.Text;

namespace PoGoExchange.Tests;

// Feste Nutzdaten fuer Tests: die frueheren 11 Beispiel-Pokemon aus
// Testdaten/ (eingebettet). Tests, die einen Datenbestand laden, nutzen
// diese statt der echten Nutzdaten, damit sie bei Datenpflege stabil bleiben.
public static class TestNutzdaten
{
  public static Nutzdaten Daten { get; } = new(
    Lesen<Pokemon, PokemonMap>("pokemon.csv"),
    Lesen<PokemonForm, PokemonFormMap>("formen.csv"),
    Lesen<RegionBereich, RegionBereichMap>("regionen.csv"));

  public static Datenbestand Laden(string ordner, DateTime jetzt)
  {
    return Datenbestand.Laden(ordner, jetzt, Daten);
  }

  private static List<T> Lesen<T, TMap>(string datei) where TMap : ClassMap<T>
  {
    var name = "Testdaten." + datei;
    using var stream =
      typeof(TestNutzdaten).Assembly.GetManifestResourceStream(name)
      ?? throw new FileNotFoundException(
        $"Eingebettete Testdatei nicht gefunden: {name}");
    using var reader = new StreamReader(
      stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
    return CsvSpeicher.Lesen<T, TMap>(reader);
  }
}
