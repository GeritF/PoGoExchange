namespace PoGoExchange.Models;

// Eine Zeile in nutzdaten_info.csv bzw. abgleich_info.csv
public sealed class SchluesselWert
{
    public string Schluessel { get; set; } = "";
    public string Wert { get; set; } = "";
}