using System;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class AnforderungTests
{
    private static readonly Guid FormA = Guid.NewGuid();
    private static readonly Guid FormB = Guid.NewGuid();

    [Fact]
    public void LeereSuche_WirdVonJedemExemplarDerFormErfuellt()
    {
        var exemplar = new HabeEintrag { FormGuid = FormA, Shiny = true, Glueck = true, Geschlecht = Geschlecht.Weiblich };
        Assert.True(Anforderung.Erfuellt(exemplar, new SucheEintrag { FormGuid = FormA }));
    }

    [Fact]
    public void AndereForm_ErfuelltNicht()
    {
        Assert.False(Anforderung.Erfuellt(new HabeEintrag { FormGuid = FormA }, new SucheEintrag { FormGuid = FormB }));
    }

    [Fact]
    public void Geloeschtes_Exemplar_ErfuelltNicht()
    {
        var exemplar = new HabeEintrag { FormGuid = FormA, GeloeschtAm = DateTime.UtcNow };
        Assert.False(Anforderung.Erfuellt(exemplar, new SucheEintrag { FormGuid = FormA }));
    }

    [Theory]
    [InlineData(false, false, true)]   // Shiny egal, Exemplar normal
    [InlineData(true, false, false)]   // Shiny gefordert, Exemplar normal
    [InlineData(true, true, true)]     // Shiny gefordert, Exemplar Shiny
    public void Shiny(bool gefordert, bool vorhanden, bool erwartet)
    {
        var exemplar = new HabeEintrag { FormGuid = FormA, Shiny = vorhanden };
        var suche = new SucheEintrag { FormGuid = FormA, Shiny = gefordert };
        Assert.Equal(erwartet, Anforderung.Erfuellt(exemplar, suche));
    }

    [Theory]
    [InlineData(Geschlecht.Unbestimmt, Geschlecht.Maennlich, true)]
    [InlineData(Geschlecht.Weiblich, Geschlecht.Weiblich, true)]
    [InlineData(Geschlecht.Weiblich, Geschlecht.Maennlich, false)]
    [InlineData(Geschlecht.Weiblich, Geschlecht.Unbestimmt, false)]
    public void GeschlechtsRegel(Geschlecht gefordert, Geschlecht vorhanden, bool erwartet)
    {
        var exemplar = new HabeEintrag { FormGuid = FormA, Geschlecht = vorhanden };
        var suche = new SucheEintrag { FormGuid = FormA, Geschlecht = gefordert };
        Assert.Equal(erwartet, Anforderung.Erfuellt(exemplar, suche));
    }

    [Theory]
    [InlineData(DynamaxStufe.Keins, DynamaxStufe.Gigadynamax, true)]
    [InlineData(DynamaxStufe.Dynamax, DynamaxStufe.Dynamax, true)]
    [InlineData(DynamaxStufe.Dynamax, DynamaxStufe.Gigadynamax, false)]
    [InlineData(DynamaxStufe.Gigadynamax, DynamaxStufe.Dynamax, false)]
    public void DynamaxNurExakt(DynamaxStufe gefordert, DynamaxStufe vorhanden, bool erwartet)
    {
        var exemplar = new HabeEintrag { FormGuid = FormA, Dynamax = vorhanden };
        var suche = new SucheEintrag { FormGuid = FormA, Dynamax = gefordert };
        Assert.Equal(erwartet, Anforderung.Erfuellt(exemplar, suche));
    }

    
    [Theory]
    [InlineData("", true, "Partyhut", true)]          // beliebiges Kostuem gefordert, Exemplar hat eins
    [InlineData("partyhut", true, "Partyhut", true)]  // bestimmtes Kostuem, Gross-/Kleinschreibung egal
    [InlineData("Partyhut", true, "Zylinder", false)] // bestimmtes Kostuem, Exemplar hat ein anderes
    [InlineData("", false, "", false)]                // beliebiges Kostuem gefordert, Exemplar hat keins
    [InlineData("Partyhut", false, "", false)]        // bestimmtes Kostuem gefordert, Exemplar hat keins
    public void Kostuem(string geforderterText, bool vorhanden, string vorhandenerText, bool erwartet)
    {
        var exemplar = new HabeEintrag { FormGuid = FormA, KostuemAktiv = vorhanden, KostuemText = vorhandenerText };
        var suche = new SucheEintrag { FormGuid = FormA, KostuemAktiv = true, KostuemText = geforderterText };
        Assert.Equal(erwartet, Anforderung.Erfuellt(exemplar, suche));
    }

    [Fact]
    public void ShinyGluecks100er_ErfuelltMehrereSuchen()
    {
        // Beispiel aus der README
        var exemplar = new HabeEintrag { FormGuid = FormA, Shiny = true, Glueck = true, Hundert = true, HintergrundAktiv = true };
        Assert.True(Anforderung.Erfuellt(exemplar, new SucheEintrag { FormGuid = FormA, Shiny = true, Glueck = true }));
        Assert.True(Anforderung.Erfuellt(exemplar, new SucheEintrag { FormGuid = FormA, Hundert = true }));
    }

    [Fact]
    public void Gluecksforderung_OhneGlueck_ErfuelltNicht()
    {
        var exemplar = new HabeEintrag { FormGuid = FormA, Shiny = true };
        Assert.False(Anforderung.Erfuellt(exemplar, new SucheEintrag { FormGuid = FormA, Shiny = true, Glueck = true }));
    }
}