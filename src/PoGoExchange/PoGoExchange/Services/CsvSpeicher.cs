using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace PoGoExchange.Services;

public static class CsvSpeicher
{
    private static readonly Encoding Utf8MitBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private static CsvConfiguration Konfiguration() => new(CultureInfo.InvariantCulture)
    {
        Delimiter = ";",
        HasHeaderRecord = true
    };

    public static List<T> Lesen<T, TMap>(string pfad) where TMap : ClassMap<T>
    {
        using var reader = new StreamReader(pfad, Utf8MitBom, detectEncodingFromByteOrderMarks: true);
        return Lesen<T, TMap>(reader);
    }

    public static List<T> Lesen<T, TMap>(TextReader reader) where TMap : ClassMap<T>
    {
        using var csv = new CsvReader(reader, Konfiguration());
        csv.Context.RegisterClassMap<TMap>();
        return csv.GetRecords<T>().ToList();
    }

    public static void Schreiben<T, TMap>(string pfad, IEnumerable<T> datensaetze) where TMap : ClassMap<T>
    {
        // Erst in eine temporaere Datei schreiben, dann ersetzen.
        // So bleibt bei einem Abbruch (z.B. App von Android beendet)
        // die alte Datei vollstaendig erhalten.
        var temp = pfad + ".tmp";
        using (var writer = new StreamWriter(temp, false, Utf8MitBom))
        using (var csv = new CsvWriter(writer, Konfiguration()))
        {
            csv.Context.RegisterClassMap<TMap>();
            csv.WriteRecords(datensaetze);
        }
        File.Move(temp, pfad, overwrite: true);
    }
}