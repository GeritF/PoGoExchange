namespace PoGoExchange.Models;

// Dex-Bereich einer Region (regionen.csv)
public sealed class RegionBereich
{
    public string Region { get; set; } = "";
    public int DexVon { get; set; }
    public int DexBis { get; set; }
}