using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class SicherungenTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_sicherung_{Guid.NewGuid():N}");

  public SicherungenTests()
  {
    Directory.CreateDirectory(_ordner);
  }

  public void Dispose()
  {
    if (Directory.Exists(_ordner))
    {
      Directory.Delete(_ordner, recursive: true);
    }
  }

  private static DateTime Utc(int jahr, int monat, int tag, int stunde, int minute = 0)
  {
    return new DateTime(jahr, monat, tag, stunde, minute, 0, DateTimeKind.Utc);
  }

  private void Schreiben(string datei, string inhalt)
  {
    File.WriteAllText(Path.Combine(_ordner, datei), inhalt);
  }

  private string Lesen(string datei)
  {
    return File.ReadAllText(Path.Combine(_ordner, datei));
  }

  [Fact]
  public void Anlegen_ErzeugtZip_MitZeitpunktImNamen()
  {
    Schreiben(Dateinamen.Habe, "habe");

    var angelegt = Sicherungen.Anlegen(_ordner, Utc(2026, 11, 25, 14, 30));

    Assert.True(angelegt);
    var sicherung = Assert.Single(Sicherungen.Liste(_ordner));
    Assert.Equal("sicherung_2026-11-25_14-30-00.zip", Path.GetFileName(sicherung.Pfad));
    Assert.Equal(Utc(2026, 11, 25, 14, 30), sicherung.Zeitpunkt);
  }

  [Fact]
  public void Anlegen_GleicherZeitpunkt_TutNichts()
  {
    Schreiben(Dateinamen.Habe, "vorher");
    var zeitpunkt = Utc(2026, 11, 25, 14, 30);
    Sicherungen.Anlegen(_ordner, zeitpunkt);
    var pfad = Sicherungen.Pfad(_ordner, zeitpunkt);
    var groesseVorher = new FileInfo(pfad).Length;
    Schreiben(Dateinamen.Habe, "nachher, deutlich laenger als vorher");

    var angelegt = Sicherungen.Anlegen(_ordner, zeitpunkt.AddMilliseconds(400));

    Assert.False(angelegt);
    Assert.Equal(groesseVorher, new FileInfo(pfad).Length);
    Assert.Single(Sicherungen.Liste(_ordner));
  }

  [Fact]
  public void Behalten_BeispielAusDerReadme_StuendlichEinJahr()
  {
    var zeitpunkte = new List<DateTime>();
    for (var z = Utc(2026, 1, 1, 0, 30); z <= Utc(2026, 12, 31, 19, 30); z = z.AddHours(1))
    {
      zeitpunkte.Add(z);
    }

    var behalten = Sicherungen.Behalten(zeitpunkte);

    var erwartet = new[]
    {
      Utc(2026, 12, 31, 19, 30), Utc(2026, 12, 30, 23, 30), Utc(2026, 12, 29, 23, 30),
      Utc(2026, 11, 30, 23, 30), Utc(2026, 11, 29, 23, 30), Utc(2026, 11, 28, 23, 30),
      Utc(2026, 10, 31, 23, 30), Utc(2026, 10, 30, 23, 30), Utc(2026, 10, 29, 23, 30),
      Utc(2026, 9, 30, 23, 30)
    };
    Assert.Equal(erwartet.OrderBy(z => z), behalten.OrderBy(z => z));
  }

  [Fact]
  public void Anlegen_RaeumtAuf_ProTagNurDieNeueste()
  {
    Sicherungen.Anlegen(_ordner, Utc(2026, 11, 25, 10));
    Sicherungen.Anlegen(_ordner, Utc(2026, 11, 25, 11));
    Sicherungen.Anlegen(_ordner, Utc(2026, 11, 25, 12));

    var sicherung = Assert.Single(Sicherungen.Liste(_ordner));
    Assert.Equal(Utc(2026, 11, 25, 12), sicherung.Zeitpunkt);
  }

  [Fact]
  public void Wiederherstellen_SetztInhaltZurueck_UndSichertVorher()
  {
    Schreiben(Dateinamen.Habe, "alt");
    Schreiben(Dateinamen.Suche, "suche alt");
    Sicherungen.Anlegen(_ordner, Utc(2026, 11, 24, 10));
    Schreiben(Dateinamen.Habe, "neu");
    var ziel = Sicherungen.Liste(_ordner).Single();

    Sicherungen.Wiederherstellen(_ordner, ziel, Utc(2026, 11, 25, 10));

    Assert.Equal("alt", Lesen(Dateinamen.Habe));
    Assert.Equal("suche alt", Lesen(Dateinamen.Suche));
    // Gemessen: der Stand vor der Wiederherstellung liegt als neue Sicherung vor
    Assert.Equal(2, Sicherungen.Liste(_ordner).Count);
  }

  [Fact]
  public void Wiederherstellen_FehlendeDateiInSicherung_WirdEntfernt()
  {
    Schreiben(Dateinamen.Habe, "habe");
    Sicherungen.Anlegen(_ordner, Utc(2026, 11, 24, 10));
    Schreiben(Dateinamen.Ueber, "spaeter angelegt");
    var ziel = Sicherungen.Liste(_ordner).Single();

    Sicherungen.Wiederherstellen(_ordner, ziel, Utc(2026, 11, 25, 10));

    Assert.False(File.Exists(Path.Combine(_ordner, Dateinamen.Ueber)));
  }

  [Fact]
  public void Wiederherstellen_AlteSicherung_WirdNichtVorherWeggeraeumt()
  {
    // 10 Sicherungen an 10 Tagen (verschiedene Monate, damit Regel 2 nicht greift);
    // die aelteste wuerde beim naechsten Aufraeumen nach Regel 3 entfernt
    Schreiben(Dateinamen.Habe, "aelteste");
    Sicherungen.Anlegen(_ordner, Utc(2026, 1, 1, 10));
    Schreiben(Dateinamen.Habe, "spaeter");
    for (var monat = 2; monat <= 10; monat++)
    {
      Sicherungen.Anlegen(_ordner, Utc(2026, monat, 1, 10));
    }

    var aelteste = Sicherungen.Liste(_ordner).Last();
    Assert.Equal(Utc(2026, 1, 1, 10), aelteste.Zeitpunkt);

    Sicherungen.Wiederherstellen(_ordner, aelteste, Utc(2026, 11, 1, 10));

    Assert.Equal("aelteste", Lesen(Dateinamen.Habe));
    Assert.Equal(10, Sicherungen.Liste(_ordner).Count);
  }
}
