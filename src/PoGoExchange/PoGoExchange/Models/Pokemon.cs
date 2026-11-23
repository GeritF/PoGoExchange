using System;

namespace PoGoExchange.Models;

// Eine Pokemon-Art (pokemon.csv)
public sealed class Pokemon
{
    public Guid Guid { get; set; }
    public int DexNr { get; set; }
    public string Name { get; set; } = "";
    public string Kategorie { get; set; } = "";
}