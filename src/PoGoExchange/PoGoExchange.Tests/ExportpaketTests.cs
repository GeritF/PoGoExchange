using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class ExportpaketTests : IDisposable
{
  private static readonly DateTime Jetzt = new(2026, 11, 26, 15, 30, 0, DateTimeKind.Utc);

  private readonly string _quelle =
    Path.Combine(Path.GetTempPath(), $"pogo_export_a_{Guid.NewGuid():N}");

  private readonly string _ziel =
    Path.Combine(Path.GetTempPath(), $"pogo_export_b_{Guid.NewGuid():N}");

  public void Dispose()
  {
    foreach (var ordner in new[] { _quelle, _ziel })
    {
      if (Directory.Exists(ordner))
      {
        Directory.Delete(ordner, recursive: true);
      }
    }
  }

  // Quelle mit einer Zeile in "Habe ich" (gespeichert, also mit LetzteAenderung)
  private Datenbestand QuelleMitGlumanda()
  {
    var bestand = TestNutzdaten.Laden(_quelle, Jetzt);
    var glumanda = bestand.Katalog.AllePokemon().Single(p => p.DexNr == 4);
    var form = bestand.Katalog.FormenZuPokemon(glumanda.Guid).First();
    bestand.Habe.Add(new HabeEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = glumanda.Guid, FormGuid = form.Guid, Anzahl = 2
    });
    Assert.Null(bestand.HabeSpeichern());
    return bestand;
  }

  private static byte[] Zip(params (string Name, string Inhalt)[] dateien)
  {
    using var puffer = new MemoryStream();
    using (var zip = new ZipArchive(puffer, ZipArchiveMode.Create, leaveOpen: true))
    {
      foreach (var (name, inhalt) in dateien)
      {
        using var schreiber = new StreamWriter(zip.CreateEntry(name).Open());
        schreiber.Write(inhalt);
      }
    }

    return puffer.ToArray();
  }

  private static string Info(string version)
  {
    return "Schluessel;Wert\r\nFormat;1\r\nErstellt;2026-11-26T15:30:00Z\r\n" +
      $"NutzdatenVersion;{version}\r\n";
  }

  [Fact]
  public void Erstellen_EnthaeltDateienUndPaketInfo()
  {
    var bestand = QuelleMitGlumanda();

    var paket = Exportpaket.Erstellen(_quelle, bestand.Nutzdaten.Version, Jetzt);

    using var zip = new ZipArchive(new MemoryStream(paket), ZipArchiveMode.Read);
    var namen = zip.Entries.Select(e => e.FullName).ToList();
    Assert.Contains(Dateinamen.Habe, namen);
    Assert.Contains(Dateinamen.AbgleichInfo, namen);
    Assert.Contains(Exportpaket.PaketInfoDatei, namen);
  }

  [Fact]
  public void Rundreise_PruefenUndEinspielen_UebernimmtListenUndLetzteAenderung()
  {
    var quelle = QuelleMitGlumanda();
    var paket = Exportpaket.Erstellen(_quelle, quelle.Nutzdaten.Version, Jetzt);
    var ziel = TestNutzdaten.Laden(_ziel, Jetzt);

    var pruefung = Exportpaket.Pruefen(paket, ziel.Nutzdaten, Jetzt);
    Exportpaket.Einspielen(_ziel, paket, Jetzt.AddMinutes(1));

    Assert.True(pruefung.Gueltig, pruefung.Fehler);
    Assert.Equal(1, pruefung.Habe);
    Assert.Equal(Jetzt, pruefung.Erstellt);
    var neu = TestNutzdaten.Laden(_ziel, Jetzt.AddMinutes(1));
    Assert.Equal(2, Assert.Single(neu.Habe).Anzahl);
    Assert.Equal(
      TestNutzdaten.Laden(_quelle, Jetzt).LetzteAenderung,
      neu.LetzteAenderung);
  }

  [Fact]
  public void Einspielen_SichertVorherDenAktuellenStand()
  {
    var quelle = QuelleMitGlumanda();
    var paket = Exportpaket.Erstellen(_quelle, quelle.Nutzdaten.Version, Jetzt);
    TestNutzdaten.Laden(_ziel, Jetzt);

    Exportpaket.Einspielen(_ziel, paket, Jetzt.AddMinutes(1));

    Assert.Single(Sicherungen.Liste(_ziel));
  }

  [Fact]
  public void Pruefen_KeinZip_IstUngueltig()
  {
    var pruefung = Exportpaket.Pruefen(new byte[] { 1, 2, 3 }, TestNutzdaten.Daten, Jetzt);

    Assert.False(pruefung.Gueltig);
    Assert.Contains("kein g\u00fcltiges Paket", pruefung.Fehler);
  }

  [Fact]
  public void Pruefen_OhnePaketInfo_IstUngueltig()
  {
    var paket = Zip((Dateinamen.Habe, "Guid;PokemonGuid\r\n"));

    var pruefung = Exportpaket.Pruefen(paket, TestNutzdaten.Daten, Jetzt);

    Assert.Contains(Exportpaket.PaketInfoDatei, pruefung.Fehler);
  }

  [Fact]
  public void Pruefen_AndereNutzdatenVersion_IstUngueltig()
  {
    var paket = Zip((Exportpaket.PaketInfoDatei, Info("99")));

    var pruefung = Exportpaket.Pruefen(paket, TestNutzdaten.Daten, Jetzt);

    Assert.False(pruefung.Gueltig);
    Assert.Contains("Version 99", pruefung.Fehler);
  }

  [Fact]
  public void Pruefen_FehlerhafteDatei_IstUngueltig_UndNenntDatei()
  {
    var glumanda = TestNutzdaten.Daten.Pokemon.Single(p => p.DexNr == 4);
    var kopf = string.Join(";", CsvSpeicher.Spaltennamen<HabeEintrag, HabeEintragMap>());
    var zeile = $"{Guid.NewGuid()};{glumanda.Guid};{Guid.NewGuid()};;x;;0;;0;;0;0;1";
    var paket = Zip(
      (Exportpaket.PaketInfoDatei, Info(TestNutzdaten.Daten.Version)),
      (Dateinamen.Habe, kopf + "\r\n" + zeile + "\r\n"));

    var pruefung = Exportpaket.Pruefen(paket, TestNutzdaten.Daten, Jetzt);

    Assert.False(pruefung.Gueltig);
    Assert.Contains(Dateinamen.Habe, pruefung.Fehler);
  }

  [Theory]
  [InlineData("1", "2", true)]
  [InlineData("2", "2", true)]
  [InlineData("3", "2", false)]
  [InlineData("", "", true)]
  public void VersionPasst_GleichOderAelter(string paket, string programm, bool erwartet)
  {
    Assert.Equal(erwartet, Exportpaket.VersionPasst(paket, programm));
  }
}
