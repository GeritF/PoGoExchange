using System.Collections.Generic;
using System.IO;
using System.Text;
using CsvHelper.Configuration;

namespace PoGoExchange.Services;

public static class EingebetteteNutzdaten
{
    public static List<T> Lesen<T, TMap>(string dateiname) where TMap : ClassMap<T>
    {
        var name = "Nutzdaten." + dateiname;
        using var stream = typeof(EingebetteteNutzdaten).Assembly.GetManifestResourceStream(name)
                           ?? throw new FileNotFoundException($"Eingebettete Datei nicht gefunden: {name}");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return CsvSpeicher.Lesen<T, TMap>(reader);
    }
}