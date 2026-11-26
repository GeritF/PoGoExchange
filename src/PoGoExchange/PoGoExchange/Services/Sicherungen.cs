using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace PoGoExchange.Services;

public sealed record SicherungInfo(string Pfad, DateTime Zeitpunkt);

// Sicherungen der persoenlichen Dateien als ZIP im Unterordner "sicherungen"
// des Datenordners. Der Zeitpunkt (UTC, sekundengenau) steht im Dateinamen.
// Der Aufrufer ermittelt den Zeitpunkt und uebergibt ihn; gibt es dafuer schon
// eine Sicherung, passiert nichts.
public static class Sicherungen
{
  public const string Ordnername = "sicherungen";
  private const string Praefix = "sicherung_";
  private const string Format = "yyyy-MM-dd_HH-mm-ss";

  // Persoenliche Dateien; die Nutzdaten kommen mit dem Programm
  internal static readonly string[] Dateien =
  {
    Dateinamen.Habe,
    Dateinamen.Suche,
    Dateinamen.Ueber,
    Dateinamen.EigeneFormen,
    Dateinamen.AbgleichInfo
  };

  // Zeitpunkt fuer eine Sicherung: UTC, auf die Sekunde abgeschnitten
  public static DateTime Zeitpunkt(DateTime jetzt)
  {
    var utc = jetzt.ToUniversalTime();
    return new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
  }

  public static string Pfad(string datenordner, DateTime zeitpunkt)
  {
    var name = Praefix + Zeitpunkt(zeitpunkt).ToString(Format, CultureInfo.InvariantCulture);
    return Path.Combine(datenordner, Ordnername, name + ".zip");
  }

  // Legt eine Sicherung an. Gibt es fuer den Zeitpunkt schon eine, passiert
  // nichts und das Ergebnis ist false. Danach wird auf Wunsch aufgeraeumt.
  public static bool Anlegen(string datenordner, DateTime zeitpunkt, bool aufraeumen = true)
  {
    var ziel = Pfad(datenordner, zeitpunkt);
    if (File.Exists(ziel))
    {
      return false;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(ziel)!);
    var temp = ziel + ".tmp";
    if (File.Exists(temp))
    {
      File.Delete(temp);
    }

    using (var zip = ZipFile.Open(temp, ZipArchiveMode.Create))
    {
      foreach (var datei in Dateien)
      {
        var quelle = Path.Combine(datenordner, datei);
        if (File.Exists(quelle))
        {
          zip.CreateEntryFromFile(quelle, datei);
        }
      }
    }

    File.Move(temp, ziel);
    if (aufraeumen)
    {
      Aufraeumen(datenordner);
    }

    return true;
  }

  // Vorhandene Sicherungen, neueste zuerst
  public static List<SicherungInfo> Liste(string datenordner)
  {
    var ordner = Path.Combine(datenordner, Ordnername);
    if (!Directory.Exists(ordner))
    {
      return new List<SicherungInfo>();
    }

    var liste = new List<SicherungInfo>();
    foreach (var pfad in Directory.GetFiles(ordner, Praefix + "*.zip"))
    {
      var name = Path.GetFileNameWithoutExtension(pfad).Substring(Praefix.Length);
      var stil = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
      if (DateTime.TryParseExact(name, Format, CultureInfo.InvariantCulture, stil, out var zeit))
      {
        liste.Add(new SicherungInfo(pfad, zeit));
      }
    }

    return liste.OrderByDescending(s => s.Zeitpunkt).ToList();
  }

  // Zeitpunkte, die nach den Aufraeumregeln erhalten bleiben (Tage und Monate
  // in UTC): 1. pro Kalendertag die neueste, 2. pro Kalendermonat die
  // neuesten 3 Tage, 3. insgesamt die 10 neuesten
  public static HashSet<DateTime> Behalten(IEnumerable<DateTime> zeitpunkte)
  {
    return zeitpunkte
      .GroupBy(z => z.Date)
      .Select(g => g.Max())
      .GroupBy(z => (z.Year, z.Month))
      .SelectMany(g => g.OrderByDescending(z => z).Take(3))
      .OrderByDescending(z => z)
      .Take(10)
      .ToHashSet();
  }

  public static void Aufraeumen(string datenordner)
  {
    var liste = Liste(datenordner);
    var behalten = Behalten(liste.Select(s => s.Zeitpunkt));
    foreach (var sicherung in liste.Where(s => !behalten.Contains(s.Zeitpunkt)))
    {
      File.Delete(sicherung.Pfad);
    }
  }

  // Stellt eine Sicherung wieder her. Vorher wird der aktuelle Stand mit dem
  // Zeitpunkt jetzt gesichert. Die gewaehlte Sicherung wird zuerst eingelesen,
  // damit das Aufraeumen sie nicht vorher entfernen kann. Dateien, die in der
  // Sicherung fehlen, werden entfernt (beim Laden entstehen sie leer neu).
  public static void Wiederherstellen(string datenordner, SicherungInfo sicherung, DateTime jetzt)
  {
    Dictionary<string, byte[]> inhalt;
    using (var zip = ZipFile.OpenRead(sicherung.Pfad))
    {
      inhalt = Inhalt(zip);
    }

    Anlegen(datenordner, jetzt, aufraeumen: false);
    DateienErsetzen(datenordner, inhalt);
    Aufraeumen(datenordner);
  }

  // Die bekannten persoenlichen Dateien aus einem ZIP (andere Eintraege werden ignoriert)
  internal static Dictionary<string, byte[]> Inhalt(ZipArchive zip)
  {
    var inhalt = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
    foreach (var eintrag in zip.Entries)
    {
      if (!Dateien.Contains(eintrag.FullName, StringComparer.OrdinalIgnoreCase))
      {
        continue;
      }

      using var strom = eintrag.Open();
      using var puffer = new MemoryStream();
      strom.CopyTo(puffer);
      inhalt[eintrag.FullName] = puffer.ToArray();
    }

    return inhalt;
  }

  // Ersetzt die persoenlichen Dateien im Datenordner; fehlende werden entfernt
  internal static void DateienErsetzen(string datenordner, Dictionary<string, byte[]> inhalt)
  {
    foreach (var datei in Dateien)
    {
      var pfad = Path.Combine(datenordner, datei);
      if (inhalt.TryGetValue(datei, out var daten))
      {
        var temp = pfad + ".tmp";
        File.WriteAllBytes(temp, daten);
        File.Move(temp, pfad, overwrite: true);
      }
      else if (File.Exists(pfad))
      {
        File.Delete(pfad);
      }
    }
  }
}
