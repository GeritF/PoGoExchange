using System;

namespace PoGoExchange.Models;

// Eine Pokemon-Art (pokemon.csv)
public sealed class Pokemon
{
  public Guid Guid { get; set; }
  public int DexNr { get; set; }
  public string Name { get; set; } = "";

  public string Kategorie { get; set; } = "";

  // 1 = sicher belegt, dass das Pokemon in Pokemon GO erhaeltlich ist.
  // Noch nicht genutzt (spaeter fuer "+ Mehrere"). Fehlt die Spalte: false.
  public bool InGoVerfuegbar { get; set; }
}
