using System;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper.Configuration;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class DatenbestandTests : IDisposable
{
  private static readonly DateTime Jetzt = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);
  private const string Zeitstempel = "2026-11-23T18:30:00Z";

  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_daten_{Guid.NewGuid():N}");

  public void Dispose()
  {
    if (Directory.Exists(_ordner))
    {
      Directory.Delete(_ordner, recursive: true);
    }
  }

  private string Pfad(string datei)
  {
    return Path.Combine(_ordner, datei);
  }

  private void DateiSchreiben(string datei, string inhalt)
  {
    Directory.CreateDirectory(_ordner);
    File.WriteAllText(Pfad(datei), inhalt, new UTF8Encoding(true));
  }

  private static string Kopf<T, TMap>(params string[] ohne) where TMap : ClassMap<T>, new()
  {
    return string.Join(";", CsvSpeicher.Spaltennamen<T, TMap>().Where(s => !ohne.Contains(s)));
  }

  private static (Guid Pokemon, Guid Form) Glumanda()
  {
    var pokemon = EingebetteteNutzdaten.Lesen<Pokemon, PokemonMap>("pokemon.csv")
      .Single(p => p.DexNr == 4);
    var form = EingebetteteNutzdaten.Lesen<PokemonForm, PokemonFormMap>("formen.csv")
      .First(f => f.PokemonGuid == pokemon.Guid);
    return (pokemon.Guid, form.Guid);
  }

  // Habe-Zeile mit allen Spalten: Shiny wie angegeben, sonst Standardwerte
  private static string HabeZeile(Guid guid, Guid pokemon, Guid form, string shiny = "1")
  {
    return $"{guid};{pokemon};{form};;{shiny};;0;;0;;0;0";
  }

  [Fact]
  public void ErsterStart_LegtLeereDateienMitKopfzeileAn()
  {
    var bestand = Datenbestand.Laden(_ordner, Jetzt);

    var dateien = new[]
    {
      Dateinamen.Habe, Dateinamen.Suche, Dateinamen.Ueber, Dateinamen.EigeneFormen
    };
    foreach (var datei in dateien)
    {
      Assert.True(File.Exists(Pfad(datei)), $"Datei fehlt: {datei}");
    }

    var zeilen = File.ReadAllLines(Pfad(Dateinamen.Habe), Encoding.UTF8);
    var kopf = Assert.Single(zeilen);
    Assert.StartsWith("Guid;PokemonGuid;FormGuid;", kopf);
    Assert.Equal(Kopf<HabeEintrag, HabeEintragMap>(), kopf);
    Assert.Empty(bestand.Habe);
    Assert.Empty(bestand.Meldungen);
  }

  [Theory]
  [InlineData("\n")]
  [InlineData("\r\n")]
  public void BeideZeilenumbrueche_WerdenGelesen_OhneDateiZuAendern(string umbruch)
  {
    var (pokemon, form) = Glumanda();
    var inhalt =
      Kopf<HabeEintrag, HabeEintragMap>() + umbruch +
      HabeZeile(Guid.NewGuid(), pokemon, form) + umbruch;
    DateiSchreiben(Dateinamen.Habe, inhalt);

    var bestand = Datenbestand.Laden(_ordner, Jetzt);

    var eintrag = Assert.Single(bestand.Habe);
    Assert.True(eintrag.Shiny);
    Assert.Empty(bestand.Meldungen);
    Assert.False(File.Exists(Pfad(Dateinamen.Habe) + ".bak_1"));
  }

  [Fact]
  public void FehlendeSpalte_WirdErgaenzt_UndAlteDateiGesichert()
  {
    var (pokemon, form) = Glumanda();
    var original =
      Kopf<HabeEintrag, HabeEintragMap>("Hundert") + "\r\n" +
      $"{Guid.NewGuid()};{pokemon};{form};;1;;0;;0;;0\r\n";
    DateiSchreiben(Dateinamen.Habe, original);

    var bestand = Datenbestand.Laden(_ordner, Jetzt);

    var eintrag = Assert.Single(bestand.Habe);
    Assert.False(eintrag.Hundert);
    var meldung = Assert.Single(bestand.Meldungen);
    Assert.False(meldung.IstFehler);
    Assert.Contains("Hundert", meldung.Text);
    Assert.Contains("habe_ich.csv.bak_1", meldung.Text);
    Assert.Equal(original, File.ReadAllText(Pfad(Dateinamen.Habe) + ".bak_1", Encoding.UTF8));
    var neuerKopf = File.ReadAllLines(Pfad(Dateinamen.Habe), Encoding.UTF8)[0];
    Assert.Equal(Kopf<HabeEintrag, HabeEintragMap>(), neuerKopf);
  }

  [Fact]
  public void VorhandeneSicherung_NaechsteBekommtNummer2()
  {
    var (pokemon, form) = Glumanda();
    DateiSchreiben(Dateinamen.Habe + ".bak_1", "alt");
    DateiSchreiben(
      Dateinamen.Habe,
      Kopf<HabeEintrag, HabeEintragMap>("Hundert") + "\r\n" +
      $"{Guid.NewGuid()};{pokemon};{form};;1;;0;;0;;0\r\n");

    var bestand = Datenbestand.Laden(_ordner, Jetzt);

    Assert.True(File.Exists(Pfad(Dateinamen.Habe) + ".bak_2"));
    Assert.Contains("habe_ich.csv.bak_2", Assert.Single(bestand.Meldungen).Text);
  }

  [Fact]
  public void UnbekannteSpalte_WirdEntfernt_UndAlteDateiGesichert()
  {
    var (pokemon, form) = Glumanda();
    DateiSchreiben(
      Dateinamen.Habe,
      Kopf<HabeEintrag, HabeEintragMap>() + ";Notiz\r\n" +
      HabeZeile(Guid.NewGuid(), pokemon, form) + ";hallo\r\n");

    var bestand = Datenbestand.Laden(_ordner, Jetzt);

    Assert.Single(bestand.Habe);
    var meldung = Assert.Single(bestand.Meldungen);
    Assert.Contains("Notiz", meldung.Text);
    Assert.Contains("bak_1", meldung.Text);
    var neuerKopf = File.ReadAllLines(Pfad(Dateinamen.Habe), Encoding.UTF8)[0];
    Assert.DoesNotContain("Notiz", neuerKopf);
  }

  [Fact]
  public void UngueltigerWert_SperrtDatei_UndMeldetZeile()
  {
    var (pokemon, form) = Glumanda();
    var original =
      Kopf<HabeEintrag, HabeEintragMap>() + "\r\n" +
      HabeZeile(Guid.NewGuid(), pokemon, form) + "\r\n" +
      HabeZeile(Guid.NewGuid(), pokemon, form, shiny: "x") + "\r\n";
    DateiSchreiben(Dateinamen.Habe, original);

    var bestand = Datenbestand.Laden(_ordner, Jetzt);

    Assert.True(bestand.IstGesperrt(Dateinamen.Habe));
    Assert.Single(bestand.Habe);
    Assert.Contains(bestand.Meldungen, m => m.IstFehler && m.Text.StartsWith("Zeile 3:"));

    var fehler = bestand.HabeSpeichern();
    Assert.NotNull(fehler);
    Assert.Equal(original, File.ReadAllText(Pfad(Dateinamen.Habe), Encoding.UTF8));
  }

  [Fact]
  public void FehlendeGuid_WirdErgaenztUndGespeichert()
  {
    var (pokemon, form) = Glumanda();
    DateiSchreiben(
      Dateinamen.Habe,
      Kopf<HabeEintrag, HabeEintragMap>("Guid") + "\r\n" +
      $"{pokemon};{form};;1;;0;;0;;0;0\r\n");

    var erster = Datenbestand.Laden(_ordner, Jetzt);
    var eintrag = Assert.Single(erster.Habe);
    Assert.NotEqual(Guid.Empty, eintrag.Guid);

    // Gemessen: die ergaenzte GUID wurde in die Datei zurueckgeschrieben
    var zweiter = Datenbestand.Laden(_ordner, Jetzt);
    Assert.Equal(eintrag.Guid, Assert.Single(zweiter.Habe).Guid);
  }

  [Fact]
  public void Speichern_UndNeuLaden_LiefertGleicheDaten()
  {
    var (pokemon, form) = Glumanda();
    var bestand = Datenbestand.Laden(_ordner, Jetzt);
    bestand.Habe.Add(new HabeEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = pokemon, FormGuid = form,
      Shiny = true, Glueck = true
    });
    bestand.Suche.Add(new SucheEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = pokemon, Hundert = true
    });
    bestand.Ueber.Add(new UeberEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = pokemon, FormGuid = form
    });

    Assert.Null(bestand.HabeSpeichern());
    Assert.Null(bestand.SucheSpeichern());
    Assert.Null(bestand.UeberSpeichern());

    var neu = Datenbestand.Laden(_ordner, Jetzt);
    Assert.Empty(neu.Meldungen);
    Assert.True(Assert.Single(neu.Habe).Glueck);
    var suche = Assert.Single(neu.Suche);
    Assert.True(suche.FormBeliebig);
    Assert.True(suche.Hundert);
    Assert.Equal(form, Assert.Single(neu.Ueber).FormGuid);
  }

  [Fact]
  public void EigeneForm_ErgaenztKatalog()
  {
    var (pokemon, _) = Glumanda();
    var neueForm = Guid.NewGuid();
    DateiSchreiben(
      Dateinamen.EigeneFormen,
      Kopf<PokemonForm, PokemonFormMap>() + "\r\n" +
      $"{neueForm};{pokemon};Testform;;1;\r\n");

    var bestand = Datenbestand.Laden(_ordner, Jetzt);

    var form = bestand.Katalog.FormZuGuid(neueForm);
    Assert.NotNull(form);
    Assert.Equal("Testform", form.Form);
  }

  [Fact]
  public void UnbekanntesPokemon_ErgibtHinweis_OhneSperre()
  {
    DateiSchreiben(
      Dateinamen.Habe,
      Kopf<HabeEintrag, HabeEintragMap>() + "\r\n" +
      HabeZeile(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()) + "\r\n");

    var bestand = Datenbestand.Laden(_ordner, Jetzt);

    var meldung = Assert.Single(bestand.Meldungen);
    Assert.False(meldung.IstFehler);
    Assert.Equal(Dateinamen.Habe, meldung.Datei);
    Assert.False(bestand.IstGesperrt(Dateinamen.Habe));
  }

  [Fact]
  public void AlteSuchdatei_WirdUmgestellt_UndDoppelteZusammengefuehrt()
  {
    var (pokemon, form) = Glumanda();
    var alterKopf =
      "Guid;PokemonGuid;FormGuid;Geschlecht;Shiny;Dynamax;KostuemAktiv;KostuemText;" +
      "HintergrundAktiv;HintergrundText;Glueck;Hundert;EinsZuEins;ErledigtAm;GeaendertAm;" +
      "GeloeschtAm";
    DateiSchreiben(
      Dateinamen.Suche,
      alterKopf + "\r\n" +
      $"{Guid.NewGuid()};{pokemon};{form};;1;;0;;0;;1;0;0;2026-11-20T18:00:00Z;{Zeitstempel};\r\n" +
      $"{Guid.NewGuid()};{pokemon};{form};;1;;0;;0;;1;0;0;;{Zeitstempel};\r\n");

    var bestand = Datenbestand.Laden(_ordner, Jetzt);

    var aktiv = Assert.Single(bestand.Suche);
    Assert.Equal(2, aktiv.Anzahl);
    Assert.Equal(2, bestand.Meldungen.Count);
    Assert.Contains(bestand.Meldungen, m => m.Text.Contains("ErledigtAm"));
    Assert.Contains(bestand.Meldungen, m => m.Text.Contains("zusammengef\u00fchrt"));
    Assert.True(File.Exists(Pfad(Dateinamen.Suche) + ".bak_1"));
    Assert.True(File.Exists(Pfad(Dateinamen.Suche) + ".bak_2"));
  }

  [Fact]
  public void AnzahlNull_IstUngueltig_UndSperrtDatei()
  {
    var (pokemon, form) = Glumanda();
    DateiSchreiben(
      Dateinamen.Suche,
      Kopf<SucheEintrag, SucheEintragMap>() + "\r\n" +
      $"{Guid.NewGuid()};{pokemon};{form};;1;;0;;0;;0;0;0;0\r\n");

    var bestand = Datenbestand.Laden(_ordner, Jetzt);

    Assert.True(bestand.IstGesperrt(Dateinamen.Suche));
    Assert.Contains(bestand.Meldungen, m => m.IstFehler && m.Text.Contains("mindestens 1"));
  }

  [Fact]
  public void KommaStattSemikolon_SperrtDatei_OhneSieZuAendern()
  {
    var (pokemon, form) = Glumanda();
    var kopf = Kopf<HabeEintrag, HabeEintragMap>().Replace(';', ',');
    var zeile = HabeZeile(Guid.NewGuid(), pokemon, form).Replace(';', ',');
    var original = kopf + "\r\n" + zeile + "\r\n";
    DateiSchreiben(Dateinamen.Habe, original);

    var bestand = Datenbestand.Laden(_ordner, Jetzt);

    Assert.True(bestand.IstGesperrt(Dateinamen.Habe));
    Assert.Empty(bestand.Habe);
    Assert.Contains(bestand.Meldungen, m => m.IstFehler && m.Text.Contains("Komma"));
    Assert.Equal(original, File.ReadAllText(Pfad(Dateinamen.Habe), Encoding.UTF8));
    Assert.False(File.Exists(Pfad(Dateinamen.Habe) + ".bak_1"));
  }
}
