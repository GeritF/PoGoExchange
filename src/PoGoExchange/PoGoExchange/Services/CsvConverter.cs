using System;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Freier Text: wird beim Lesen und Schreiben bereinigt
public sealed class TextKonverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        => TextBereinigung.Bereinigen(text);

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        => TextBereinigung.Bereinigen(value as string);
}

// Wahrheitswert: "1" = ja, "0" oder leer = nein
public sealed class WahrheitswertKonverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        var t = TextBereinigung.Bereinigen(text);
        return t switch
        {
            "1" => true,
            "0" or "" => false,
            _ => throw new FormatException($"Ungueltiger Wahrheitswert: '{t}'")
        };
    }

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        => value is true ? "1" : "0";
}

// Geschlecht: "M", "W" oder leer
public sealed class GeschlechtKonverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        var t = TextBereinigung.Bereinigen(text);
        return t switch
        {
            "M" => Geschlecht.Maennlich,
            "W" => Geschlecht.Weiblich,
            "" => Geschlecht.Unbestimmt,
            _ => throw new FormatException($"Ungueltiges Geschlecht: '{t}'")
        };
    }

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        => value switch
        {
            Geschlecht.Maennlich => "M",
            Geschlecht.Weiblich => "W",
            _ => ""
        };
}

// Dynamax: leer, "Dynamax" oder "Gigadynamax"
public sealed class DynamaxKonverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        var t = TextBereinigung.Bereinigen(text);
        return t switch
        {
            "Dynamax" => DynamaxStufe.Dynamax,
            "Gigadynamax" => DynamaxStufe.Gigadynamax,
            "" => DynamaxStufe.Keins,
            _ => throw new FormatException($"Ungueltige Dynamax-Stufe: '{t}'")
        };
    }

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        => value switch
        {
            DynamaxStufe.Dynamax => "Dynamax",
            DynamaxStufe.Gigadynamax => "Gigadynamax",
            _ => ""
        };
}

// Zeitstempel in UTC, z.B. 2026-11-23T18:30:00Z
public sealed class ZeitstempelKonverter : DefaultTypeConverter
{
    internal const string Format = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    internal static DateTime Lesen(string text) =>
        DateTime.ParseExact(text, Format, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    internal static string Schreiben(DateTime wert) =>
        wert.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture);

    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        => Lesen(TextBereinigung.Bereinigen(text));

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        => value is DateTime d ? Schreiben(d) : "";
}

// Optionaler Zeitstempel: leer = nicht gesetzt
public sealed class OptionalerZeitstempelKonverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        var t = TextBereinigung.Bereinigen(text);
        return t == "" ? null : ZeitstempelKonverter.Lesen(t);
    }

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        => value is DateTime d ? ZeitstempelKonverter.Schreiben(d) : "";
}

// Optionale ganze Zahl: leer = nicht gesetzt
public sealed class OptionaleZahlKonverter : DefaultTypeConverter
{
  public override object? ConvertFromString(
    string? text,
    IReaderRow row,
    MemberMapData memberMapData)
  {
    var t = TextBereinigung.Bereinigen(text);
    if (t == "")
    {
      return null;
    }

    return int.Parse(t, NumberStyles.None, CultureInfo.InvariantCulture);
  }

  public override string? ConvertToString(
    object? value,
    IWriterRow row,
    MemberMapData memberMapData)
  {
    return value is int zahl ? zahl.ToString(CultureInfo.InvariantCulture) : "";
  }
}
// Optionale GUID: leer = Guid.Empty
public sealed class OptionaleGuidKonverter : DefaultTypeConverter
{
  public override object? ConvertFromString(
    string? text,
    IReaderRow row,
    MemberMapData memberMapData)
  {
    var t = TextBereinigung.Bereinigen(text);
    return t == "" ? Guid.Empty : Guid.Parse(t);
  }

  public override string? ConvertToString(
    object? value,
    IWriterRow row,
    MemberMapData memberMapData)
  {
    return value is Guid guid && guid != Guid.Empty ? guid.ToString() : "";
  }
}
