using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class CsvSpeicherTests : IDisposable
{
    private readonly string _pfad =
        Path.Combine(Path.GetTempPath(), $"pogo_test_{Guid.NewGuid():N}.csv");

    public void Dispose()
    {
        if (File.Exists(_pfad))
            File.Delete(_pfad);
    }

    [Fact]
    public void Pokemon_HinUndZurueck_LiefertGleicheWerte()
    {
        var original = new List<Pokemon>
        {
            new() { Guid = Guid.NewGuid(), DexNr = 4, Name = "Glumanda" },
            new() { Guid = Guid.NewGuid(), DexNr = 669, Name = "Flab\u00e9b\u00e9" }
        };

        CsvSpeicher.Schreiben<Pokemon, PokemonMap>(_pfad, original);
        var gelesen = CsvSpeicher.Lesen<Pokemon, PokemonMap>(_pfad);

        Assert.Equal(original.Count, gelesen.Count);
        for (var i = 0; i < original.Count; i++)
        {
            Assert.Equal(original[i].Guid, gelesen[i].Guid);
            Assert.Equal(original[i].DexNr, gelesen[i].DexNr);
            Assert.Equal(original[i].Name, gelesen[i].Name);
            Assert.Equal(original[i].Kategorie, gelesen[i].Kategorie);
        }
    }

    [Fact]
    public void Datei_BeginntMitBom_UndNutztSemikolon()
    {
        var eintrag = new Pokemon { Guid = Guid.NewGuid(), DexNr = 4, Name = "Glumanda" };
        CsvSpeicher.Schreiben<Pokemon, PokemonMap>(_pfad, new[] { eintrag });

        var bytes = File.ReadAllBytes(_pfad);
        Assert.True(bytes.Length >= 3, $"Datei zu kurz: {bytes.Length} Bytes");
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());

        var kopf = File.ReadAllLines(_pfad, Encoding.UTF8)[0];
        Assert.Equal("Guid;DexNr;Name;Kategorie", kopf);
    }

    [Fact]
    public void SucheEintrag_WirdWieFestgelegtGeschrieben_UndGelesen()
    {
        var zeit = new DateTime(2026, 11, 23, 18, 30, 0, DateTimeKind.Utc);
        var original = new SucheEintrag
        {
            Guid = Guid.NewGuid(),
            FormGuid = Guid.NewGuid(),
            Geschlecht = Geschlecht.Weiblich,
            Shiny = true,
            Dynamax = DynamaxStufe.Gigadynamax,
            HintergrundAktiv = true,
            HintergrundText = "Paris; Eiffelturm",
            Glueck = true,
            EinsZuEins = true,
            GeaendertAm = zeit
        };

        CsvSpeicher.Schreiben<SucheEintrag, SucheEintragMap>(_pfad, new[] { original });

        // Gemessen: Rohtext der Datei
        var zeilen = File.ReadAllLines(_pfad, Encoding.UTF8);
        Assert.Equal(
            "Guid;FormGuid;Geschlecht;Shiny;Dynamax;KostuemAktiv;KostuemText;HintergrundAktiv;" +
            "HintergrundText;Glueck;Hundert;EinsZuEins;ErledigtAm;GeaendertAm;GeloeschtAm",
            zeilen[0]);
        Assert.Contains(
            ";W;1;Gigadynamax;0;;1;\"Paris; Eiffelturm\";1;0;1;;2026-11-23T18:30:00Z;",
            zeilen[1]);

        // Gemessen: zurueckgelesene Werte
        var gelesen = CsvSpeicher.Lesen<SucheEintrag, SucheEintragMap>(_pfad).Single();
        Assert.Equal(original.Guid, gelesen.Guid);
        Assert.Equal(Geschlecht.Weiblich, gelesen.Geschlecht);
        Assert.True(gelesen.Shiny);
        Assert.Equal(DynamaxStufe.Gigadynamax, gelesen.Dynamax);
        Assert.False(gelesen.KostuemAktiv);
        Assert.Equal("Paris; Eiffelturm", gelesen.HintergrundText);
        Assert.True(gelesen.Glueck);
        Assert.False(gelesen.Hundert);
        Assert.True(gelesen.EinsZuEins);
        Assert.Null(gelesen.ErledigtAm);
        Assert.Equal(zeit, gelesen.GeaendertAm);
        Assert.Equal(DateTimeKind.Utc, gelesen.GeaendertAm.Kind);
        Assert.Null(gelesen.GeloeschtAm);
    }

    [Theory]
    [InlineData("  Glumanda  ", "Glumanda")]
    [InlineData("\u200BGlumanda\u200B", "Glumanda")]
    [InlineData("\t\r\nBla Rot\u00A0", "Bla Rot")]
    [InlineData("", "")]
    public void Bereinigen_EntferntNurZeichenAmRand(string eingabe, string erwartet)
    {
        Assert.Equal(erwartet, TextBereinigung.Bereinigen(eingabe));
    }
}