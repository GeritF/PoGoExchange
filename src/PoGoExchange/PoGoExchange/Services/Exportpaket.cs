using PoGoExchange.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace PoGoExchange.Services;

// Ergebnis der Pruefung eines Pakets vor dem Einspielen
public sealed class PaketPruefung
{
  // null, wenn das Paket eingespielt werden kann
  public string? Fehler { get; init; }
  public bool Gueltig => Fehler == null;
  public DateTime Erstellt { get; init; }
  public int Habe { get; init; }
  public int Suche { get; init; }
  public int Ueber { get; init; }
  public int EigeneFormen { get; init; }
}

// Exportpaket der eigenen Listen: ZIP mit den persoenlichen Dateien (wie eine
// Sicherung) und paket_info.csv (Format, Erstellungszeitpunkt, Version der
// Nutzdaten). Die Nutzdaten selbst gehoeren zum Programm und sind nicht enthalten.
// Eingespielt wird nur bei gleicher Version der Nutzdaten und ersetzt alle
// eigenen Listen; "LetzteAenderung" kommt dabei aus dem Paket.
public static class Exportpaket
{
  public const string PaketInfoDatei = "paket_info.csv";
  public const string Format = "1";

  public static string Dateiname(DateTime jetzt)
  {
    return $"PoGoExchange_{jetzt.ToLocalTime():yyyy-MM-dd_HH-mm}.zip";
  }

  public static byte[] Erstellen(string datenordner, string nutzdatenVersion, DateTime jetzt)
  {
    using var puffer = new MemoryStream();
    using (var zip = new ZipArchive(puffer, ZipArchiveMode.Create, leaveOpen: true))
    {
      foreach (var datei in Sicherungen.Dateien)
      {
        var pfad = Path.Combine(datenordner, datei);
        if (File.Exists(pfad))
        {
          zip.CreateEntryFromFile(pfad, datei);
        }
      }

      var text =
        "Schluessel;Wert\r\n" +
        $"Format;{Format}\r\n" +
        $"Erstellt;{ZeitstempelKonverter.Schreiben(jetzt.ToUniversalTime())}\r\n" +
        $"NutzdatenVersion;{nutzdatenVersion}\r\n";
      using var strom = zip.CreateEntry(PaketInfoDatei).Open();
      var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
      strom.Write(bytes.GetPreamble());
      strom.Write(bytes.GetBytes(text));
    }

    return puffer.ToArray();
  }

  // Prueft ein Paket, ohne etwas zu veraendern: Format, Version der Nutzdaten
  // und ein Probeladen in einem temporaeren Ordner mit denselben Pruefungen
  // wie beim Programmstart.
  public static PaketPruefung Pruefen(byte[] paket, Nutzdaten nutzdaten, DateTime jetzt)
  {
    Dictionary<string, string> info;
    Dictionary<string, byte[]> inhalt;
    try
    {
      using var zip = new ZipArchive(new MemoryStream(paket), ZipArchiveMode.Read);
      var infoEintrag = zip.GetEntry(PaketInfoDatei);
      if (infoEintrag == null)
      {
        return Fehler($"Die Datei ist kein Paket mit eigenen Listen ({PaketInfoDatei} fehlt).");
      }

      using (var reader = new StreamReader(
               infoEintrag.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
      {
        info = CsvSpeicher.Lesen<SchluesselWert, SchluesselWertMap>(reader)
          .ToDictionary(e => e.Schluessel, e => e.Wert);
      }

      inhalt = Sicherungen.Inhalt(zip);
    }
    catch (Exception ex) when (ex is not OutOfMemoryException)
    {
      return Fehler("Die Datei ist kein g\u00fcltiges Paket: " + ex.Message);
    }

    if (info.GetValueOrDefault("Format") != Format)
    {
      return Fehler("Das Paket hat ein unbekanntes Format.");
    }

    var version = info.GetValueOrDefault("NutzdatenVersion") ?? "";
    if (!VersionPasst(version, nutzdaten.Version))
    {
      return Fehler(
        $"Das Paket stammt aus einer neueren Programmversion (Nutzdaten-Version {version}, " +
        $"dieses Programm hat Version {nutzdaten.Version}). Bitte zuerst das Programm " +
        "aktualisieren.");
    }

    DateTime erstellt;
    try
    {
      erstellt = ZeitstempelKonverter.Lesen(info.GetValueOrDefault("Erstellt") ?? "");
    }
    catch (FormatException)
    {
      return Fehler("Das Paket hat keinen g\u00fcltigen Erstellungszeitpunkt.");
    }

    var temp = Path.Combine(Path.GetTempPath(), $"pogo_paket_{Guid.NewGuid():N}");
    try
    {
      Directory.CreateDirectory(temp);
      Sicherungen.DateienErsetzen(temp, inhalt);
      var probe = Datenbestand.Laden(temp, jetzt, nutzdaten);
      var fehler = probe.Meldungen
        .Where(m => m.IstFehler)
        .Select(m => $"{m.Datei}: {m.Text}")
        .ToList();
      if (fehler.Count > 0)
      {
        return Fehler(
          "Das Paket enth\u00e4lt fehlerhafte Dateien und wird nicht eingespielt: " +
          string.Join(" | ", fehler));
      }

      return new PaketPruefung
      {
        Erstellt = erstellt,
        Habe = probe.Habe.Count,
        Suche = probe.Suche.Count,
        Ueber = probe.Ueber.Count,
        EigeneFormen = probe.EigeneFormen.Count
      };
    }
    finally
    {
      if (Directory.Exists(temp))
      {
        Directory.Delete(temp, recursive: true);
      }
    }
  }

  // Ersetzt alle eigenen Listen durch die des Pakets. Vorher wird der aktuelle
  // Stand gesichert. Das Paket muss vorher mit Pruefen geprueft worden sein.
  public static void Einspielen(string datenordner, byte[] paket, DateTime jetzt)
  {
    Dictionary<string, byte[]> inhalt;
    using (var zip = new ZipArchive(new MemoryStream(paket), ZipArchiveMode.Read))
    {
      inhalt = Sicherungen.Inhalt(zip);
    }

    Sicherungen.Anlegen(datenordner, Sicherungen.Zeitpunkt(jetzt));
    Sicherungen.DateienErsetzen(datenordner, inhalt);
  }

  // Gleiche oder aeltere Pakete sind einspielbar: Aeltere Staende werden beim
  // Laden wie nach einem Update der Nutzdaten behandelt (z.B. eigene Formen
  // uebernehmen). Nicht als Zahl lesbare Versionen muessen gleich sein.
  public static bool VersionPasst(string paket, string programm)
  {
    if (int.TryParse(paket, out var p) && int.TryParse(programm, out var e))
    {
      return p <= e;
    }

    return paket == programm;
  }

  private static PaketPruefung Fehler(string text)
  {
    return new PaketPruefung { Fehler = text };
  }
}
