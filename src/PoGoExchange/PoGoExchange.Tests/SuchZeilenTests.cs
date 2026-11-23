using System;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class SuchZeilenTests
{
    private static readonly Guid Glumanda = Guid.NewGuid();

    private static SucheEintrag Suche(bool erledigt = false, bool hundert = false) => new()
    {
        Guid = Guid.NewGuid(),
        FormGuid = Glumanda,
        Shiny = true,
        Hundert = hundert,
        ErledigtAm = erledigt ? DateTime.UtcNow : null
    };

    [Fact]
    public void GemischteHaken_ErgebenGetrennteZeilen()
    {
        // 3x Glumanda Shiny (1 erledigt) und 1x Glumanda Shiny [100er]
        var zeilen = SuchZeilen.Bilden(new[]
        {
            Suche(erledigt: true), Suche(), Suche(), Suche(hundert: true)
        });

        Assert.Equal(2, zeilen.Count);

        var normal = zeilen.Single(z => !z.Muster.Hundert);
        Assert.Equal(1, normal.Erledigt);
        Assert.Equal(3, normal.Ziel);
        Assert.Equal("(1/3)", normal.Zaehler);

        var hundert = zeilen.Single(z => z.Muster.Hundert);
        Assert.Equal(0, hundert.Erledigt);
        Assert.Equal(1, hundert.Ziel);
        Assert.Equal("", hundert.Zaehler);
    }

    [Theory]
    [InlineData(0, 1, "")]
    [InlineData(1, 1, "(1/1)")]
    [InlineData(0, 2, "(0/2)")]
    [InlineData(2, 3, "(2/3)")]
    public void Zaehler_StandardfallWirdAusgeblendet(int erledigt, int ziel, string erwartet)
    {
        var eintraege = Enumerable.Range(0, ziel).Select(i => Suche(erledigt: i < erledigt));
        var zeile = SuchZeilen.Bilden(eintraege).Single();

        Assert.Equal(erwartet, zeile.Zaehler);
    }

    [Fact]
    public void GeloeschteEintraege_WerdenIgnoriert()
    {
        var geloescht = Suche();
        geloescht.GeloeschtAm = DateTime.UtcNow;

        var zeile = SuchZeilen.Bilden(new[] { Suche(), geloescht }).Single();
        Assert.Equal(1, zeile.Ziel);
    }

    [Fact]
    public void EinsZuEins_TrenntZeilen()
    {
        var a = Suche();
        var b = Suche();
        b.EinsZuEins = true;

        Assert.Equal(2, SuchZeilen.Bilden(new[] { a, b }).Count);
    }

    [Fact]
    public void Kostuemtext_GrossKleinschreibungEgal_OhneHakenIgnoriert()
    {
        var a = Suche(); a.KostuemAktiv = true; a.KostuemText = "Partyhut";
        var b = Suche(); b.KostuemAktiv = true; b.KostuemText = "partyhut";
        var c = Suche(); c.KostuemAktiv = false; c.KostuemText = "Zylinder";
        var d = Suche();

        var zeilen = SuchZeilen.Bilden(new[] { a, b, c, d });

        // a und b gehoeren zusammen, c und d ebenfalls (Text ohne Haken zaehlt nicht)
        Assert.Equal(2, zeilen.Count);
        Assert.All(zeilen, z => Assert.Equal(2, z.Ziel));
    }
}