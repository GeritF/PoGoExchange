namespace PoGoExchange.Models;

// Dex-Bereich einer Region (regionen.csv).
// Ohne DexVon und DexBis ist es die Auffang-Region fuer alle Dex-Nummern,
// die in keinem Bereich liegen (z.B. "Unbekannt").
public sealed class RegionBereich
{
  public string Region { get; set; } = "";
  public int? DexVon { get; set; }
  public int? DexBis { get; set; }
}
