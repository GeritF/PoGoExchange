using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class SuchZeilenTests
{
  private static readonly Guid Glumanda = Guid.NewGuid();
  private static readonly Guid GlumandaForm = Guid.NewGuid();

  private static SucheEintrag Suche(int anzahl = 1, bool hundert = false)
  {
    return new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = Glumanda,
      FormGuid = GlumandaForm,
      Shiny = true,
      Hundert = hundert,
      Anzahl = anzahl
    };
  }

  private static HabeEintrag Habe(bool hundert = false, int anzahl = 1)
  {
    return new HabeEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = Glumanda,
      FormGuid = GlumandaForm,
      Shiny = true,
      Hundert = hundert,
      Anzahl = anzahl
    };
  }

  [Theory]
  [InlineData(0, 1, "")]
  [InlineData(1, 1, "(1/1)")]
  [InlineData(0, 2, "(0/2)")]
  [InlineData(2, 3, "(2/3)")]
  [InlineData(7, 5, "(5/5)")]
  public void Zaehler_AusHabeIch_BegrenztAufAnzahl(int passende, int anzahl, string erwartet)
  {
    var habe = Enumerable.Range(0, passende).Select(_ => Habe()).ToList();

    var zeile = SuchZeilen.Bilden([Suche(anzahl)], habe).Single();

    Assert.Equal(erwartet, zeile.Zaehler);
  }

  [Fact]
  public void UnabhaengigeZiele_ExemplarZaehltInJederPassendenZeile()
  {
    // 5x Shiny und 1x Shiny 100er; vorhanden ist ein Shiny 100er
    var suche = new[] { Suche(5), Suche(1, hundert: true) };
    var habe = new[] { Habe(hundert: true) };

    var zeilen = SuchZeilen.Bilden(suche, habe);

    Assert.Equal("(1/5)", zeilen.Single(z => !z.Muster.Hundert).Zaehler);
    Assert.Equal("(1/1)", zeilen.Single(z => z.Muster.Hundert).Zaehler);
  }

  [Fact]
  public void DoppelteDatensaetze_WerdenZuEinerZeileAddiert()
  {
    var zeile = SuchZeilen.Bilden(
      [Suche(2), Suche(3)], []).Single();

    Assert.Equal(5, zeile.Ziel);
  }

  [Fact]
  public void EinsZuEins_TrenntZeilen()
  {
    var a = Suche();
    var b = Suche();
    b.EinsZuEins = true;

    Assert.Equal(2, SuchZeilen.Bilden([a, b], []).Count);
  }

  [Fact]
  public void Kostuemtext_GrossKleinschreibungEgal_OhneHakenIgnoriert()
  {
    var a = Suche();
    a.KostuemAktiv = true;
    a.KostuemText = "Partyhut";
    var b = Suche();
    b.KostuemAktiv = true;
    b.KostuemText = "partyhut";
    var c = Suche();
    c.KostuemAktiv = false;
    c.KostuemText = "Zylinder";
    var d = Suche();

    var zeilen = SuchZeilen.Bilden([a, b, c, d], []);

    // a und b gehoeren zusammen, c und d ebenfalls (Text ohne Haken zaehlt nicht)
    Assert.Equal(2, zeilen.Count);
    Assert.All(zeilen, z => Assert.Equal(2, z.Ziel));
  }

  [Fact]
  public void FormBeliebigUndBestimmteForm_SindGetrennteZeilen()
  {
    var bestimmt = Suche();
    var beliebig = Suche();
    beliebig.FormGuid = Guid.Empty;

    var zeilen = SuchZeilen.Bilden([bestimmt, beliebig], []);

    Assert.Equal(2, zeilen.Count);
  }

  [Fact]
  public void FormBeliebig_ZaehltExemplareJederForm()
  {
    var beliebig = Suche(2);
    beliebig.FormGuid = Guid.Empty;
    var andereForm = Habe();
    andereForm.FormGuid = Guid.NewGuid();

    var zeile = SuchZeilen.Bilden([beliebig], [Habe(), andereForm]).Single();

    Assert.Equal("(2/2)", zeile.Zaehler);
  }

  [Fact]
  public void HabeZeileMitAnzahl_ZaehltAlleExemplare()
  {
    var zeile = SuchZeilen.Bilden([Suche(5)], [Habe(anzahl: 3)]).Single();

    Assert.Equal("(3/5)", zeile.Zaehler);
  }
}
