using System;

namespace PoGoExchange.Models;

// Eine Form eines Pokemon (formen.csv, eigene_formen.csv)
public sealed class PokemonForm
{
  public Guid Guid { get; set; }
  public Guid PokemonGuid { get; set; }
  public string Form { get; set; } = "";
  public string Region { get; set; } = "";
  public bool ShinyVerfuegbar { get; set; }
  public DateTime? ArchiviertAm { get; set; }
}
