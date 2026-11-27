using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace PoGoExchange.Services;

// Ein Datensatz, der nicht gelesen werden konnte.
// Zeile 1 ist die Kopfzeile; Leerzeilen werden nicht mitgezaehlt.
public sealed record CsvZeilenFehler(int Zeile, string Grund);

public sealed class CsvLeseErgebnis<T>
{
  public required List<T> Eintraege { get; init; }
  public required IReadOnlyList<string> FehlendeSpalten { get; init; }
  public required IReadOnlyList<string> UnbekannteSpalten { get; init; }
  public required IReadOnlyList<CsvZeilenFehler> Fehler { get; init; }
}

public static class CsvSpeicher
{
  private static readonly Encoding Utf8MitBom =
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

  // Beim Lesen wird NewLine bewusst nicht gesetzt: Ist es gesetzt, akzeptiert
  // CsvHelper ausschliesslich diesen Zeilenumbruch. Ohne Angabe werden
  // Windows- und Linux-Zeilenumbrueche erkannt.
  private static CsvConfiguration LeseKonfiguration()
  {
    return new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      Delimiter = ";",
      HasHeaderRecord = true
    };
  }

  // Geschrieben wird immer mit Windows-Zeilenumbruch
  private static CsvConfiguration SchreibKonfiguration()
  {
    return new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      Delimiter = ";",
      HasHeaderRecord = true,
      NewLine = "\r\n"
    };
  }

  // Spaltennamen in der Reihenfolge der Map
  public static List<string> Spaltennamen<T, TMap>() where TMap : ClassMap<T>, new()
  {
    return new TMap().MemberMaps.Select(m => m.Data.Names[0]).ToList();
  }

  public static List<T> Lesen<T, TMap>(string pfad) where TMap : ClassMap<T>
  {
    using var reader = new StreamReader(pfad, Utf8MitBom, detectEncodingFromByteOrderMarks: true);
    return Lesen<T, TMap>(reader);
  }

  public static List<T> Lesen<T, TMap>(TextReader reader) where TMap : ClassMap<T>
  {
    using var csv = new CsvReader(reader, LeseKonfiguration());
    csv.Context.RegisterClassMap<TMap>();
    return csv.GetRecords<T>().ToList();
  }

  // Liest tolerant: Spalten duerfen fehlen, zusaetzlich vorhanden oder anders
  // angeordnet sein. Fehlerhafte Datensaetze werden mit Zeile und Grund
  // gemeldet, die uebrigen trotzdem gelesen.
  public static CsvLeseErgebnis<T> LesenMitPruefung<T, TMap>(string pfad)
    where TMap : ClassMap<T>, new()
  {
    var roh = RohLesen(pfad);
    var erwartet = Spaltennamen<T, TMap>();
    var kopf = roh.Count == 0
      ? new List<string>()
      : roh[0].Select(TextBereinigung.Bereinigen).ToList();

    var fehlend = erwartet
      .Where(s => !kopf.Contains(s, StringComparer.OrdinalIgnoreCase))
      .ToList();
    var unbekannt = kopf
      .Where(s => s != "" && !erwartet.Contains(s, StringComparer.OrdinalIgnoreCase))
      .ToList();
    var indizes = erwartet
      .Select(s => kopf.FindIndex(k => string.Equals(k, s, StringComparison.OrdinalIgnoreCase)))
      .ToArray();

    // Datensaetze in die erwartete Spaltenreihenfolge bringen, fehlende Spalten leer
    var normiert = new StringWriter();
    using (var writer = new CsvWriter(normiert, SchreibKonfiguration(), leaveOpen: true))
    {
      foreach (var name in erwartet)
      {
        writer.WriteField(name);
      }

      writer.NextRecord();
      foreach (var zeile in roh.Skip(1))
      {
        foreach (var index in indizes)
        {
          writer.WriteField(index >= 0 && index < zeile.Length ? zeile[index] : "");
        }

        writer.NextRecord();
      }
    }

    var eintraege = new List<T>();
    var fehler = new List<CsvZeilenFehler>();
    using (var reader = new StringReader(normiert.ToString()))
    using (var csv = new CsvReader(reader, LeseKonfiguration()))
    {
      csv.Context.RegisterClassMap<TMap>();
      csv.Read();
      csv.ReadHeader();
      var zeilenNummer = 1;
      while (csv.Read())
      {
        zeilenNummer++;
        try
        {
          eintraege.Add(csv.GetRecord<T>()!);
        }
        catch (Exception ex) when (
          ex is CsvHelperException or FormatException or ArgumentException or OverflowException)
        {
          fehler.Add(new CsvZeilenFehler(zeilenNummer, KurzeMeldung(ex)));
        }
      }
    }

    return new CsvLeseErgebnis<T>
    {
      Eintraege = eintraege,
      FehlendeSpalten = fehlend,
      UnbekannteSpalten = unbekannt,
      Fehler = fehler
    };
  }

  // Schreibt immer eine Kopfzeile, auch bei leerer Liste.
  // Erst in eine temporaere Datei schreiben, dann ersetzen: So bleibt bei
  // einem Abbruch (z.B. App von Android beendet) die alte Datei erhalten.
  public static void Schreiben<T, TMap>(string pfad, IEnumerable<T> datensaetze)
    where TMap : ClassMap<T>
  {
    var temp = pfad + ".tmp";
    using (var writer = new StreamWriter(temp, false, Utf8MitBom))
    using (var csv = new CsvWriter(writer, SchreibKonfiguration()))
    {
      csv.Context.RegisterClassMap<TMap>();
      csv.WriteHeader<T>();
      csv.NextRecord();
      foreach (var datensatz in datensaetze)
      {
        csv.WriteRecord(datensatz);
        csv.NextRecord();
      }
    }

    File.Move(temp, pfad, overwrite: true);
  }

  private static List<string[]> RohLesen(string pfad)
  {
    var zeilen = new List<string[]>();
    using var reader = new StreamReader(pfad, Utf8MitBom, detectEncodingFromByteOrderMarks: true);
    using var parser = new CsvParser(reader, LeseKonfiguration());
    while (parser.Read())
    {
      zeilen.Add(parser.Record ?? Array.Empty<string>());
    }

    return zeilen;
  }

  // Innerste Fehlermeldung, nur die erste Zeile (CsvHelper haengt viel Kontext an)
  private static string KurzeMeldung(Exception ex)
  {
    while (ex.InnerException != null)
    {
      ex = ex.InnerException;
    }

    return ex.Message.Split('\n')[0].Trim();
  }
}
