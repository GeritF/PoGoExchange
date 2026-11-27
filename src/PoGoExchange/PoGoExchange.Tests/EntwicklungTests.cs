using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class EntwicklungTests
{
  private static readonly PokemonForm Glumanda = new()
  {
    Guid = Guid.NewGuid(), PokemonGuid = Guid.NewGuid()
  };

  private static readonly PokemonForm Glurak = new()
  {
    Guid = Guid.NewGuid(), PokemonGuid = Guid.NewGuid()
  };

  private static HabeEintrag Habe(PokemonForm form, int anzahl = 1, bool hundert = false)
  {
    return new HabeEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Shiny = true,
      Hundert = hundert,
      Anzahl = anzahl
    };
  }

  private static SucheEintrag Suche(PokemonForm form, int anzahl = 1, bool hundert = false)
  {
    return new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Shiny = true,
      Hundert = hundert,
      Anzahl = anzahl
    };
  }

  private static string Zaehler(
    List<SucheEintrag> suche,
    List<HabeEintrag> habe,
    PokemonForm form,
    bool hundert = false)
  {
    return SuchZeilen.Bilden(suche, habe)
      .Single(z => z.Muster.PokemonGuid == form.PokemonGuid && z.Muster.Hundert == hundert)
      .Zaehler;
  }

  private static List<SuchZeile> Betroffen(
    EntwicklungsErgebnis ergebnis,
    List<SucheEintrag> suche,
    List<HabeEintrag> habe)
  {
    return Entwicklung.BetroffeneZeilen(ergebnis.Vorher, ergebnis.Nachher, suche, habe);
  }

  [Fact]
  public void Beispiel_DreiGlumanda_EinesWirdGlurak_ZeileWirdHochgezogen()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda, anzahl: 3) };
    var suche = new List<SucheEintrag> { Suche(Glumanda, anzahl: 3) };

    var ergebnis = Entwicklung.Entwickeln(habe, habe[0], Glurak);
    var zeile = Assert.Single(Betroffen(ergebnis, suche, habe));
    Entwicklung.Hochziehen(suche, zeile, Glurak);

    Assert.Equal("(2/2)", Zaehler(suche, habe, Glumanda));
    Assert.Equal("(1/1)", Zaehler(suche, habe, Glurak));
  }

  [Fact]
  public void OhneHochziehen_SinktNurDerZaehler()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda, anzahl: 3) };
    var suche = new List<SucheEintrag> { Suche(Glumanda, anzahl: 3) };

    Entwicklung.Entwickeln(habe, habe[0], Glurak);

    Assert.Equal("(2/3)", Zaehler(suche, habe, Glumanda));
    Assert.Single(suche);
  }

  [Fact]
  public void Exemplar100er_BetrifftBeideZeilen_NormalesNurEine()
  {
    List<HabeEintrag> HabeListe() =>
      [Habe(Glumanda, hundert: true), Habe(Glumanda)];

    List<SucheEintrag> SucheListe() =>
      [Suche(Glumanda, anzahl: 2), Suche(Glumanda, hundert: true)];

    var habeA = HabeListe();
    var sucheA = SucheListe();
    var ergebnis100er = Entwicklung.Entwickeln(habeA, habeA[0], Glurak);

    var habeB = HabeListe();
    var sucheB = SucheListe();
    var ergebnisNormal = Entwicklung.Entwickeln(habeB, habeB[1], Glurak);

    Assert.Equal(2, Betroffen(ergebnis100er, sucheA, habeA).Count);
    var zeile = Assert.Single(Betroffen(ergebnisNormal, sucheB, habeB));
    Assert.False(zeile.Muster.Hundert);
  }

  [Fact]
  public void Hochziehen_InVorhandeneSuchzeile_ErhoehtDortDieAnzahl()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda) };
    var glurakZeile = Suche(Glurak);
    var suche = new List<SucheEintrag> { Suche(Glumanda, anzahl: 2), glurakZeile };

    var ergebnis = Entwicklung.Entwickeln(habe, habe[0], Glurak);
    var zeile = Assert.Single(Betroffen(ergebnis, suche, habe));
    var ziel = Entwicklung.Hochziehen(suche, zeile, Glurak);

    Assert.Same(glurakZeile, ziel);
    Assert.Equal(2, glurakZeile.Anzahl);
    Assert.Equal(2, suche.Count);
  }

  [Fact]
  public void Hochziehen_LetzteEinheit_EntferntAlteSuchzeile()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda) };
    var alt = Suche(Glumanda);
    var suche = new List<SucheEintrag> { alt };

    var ergebnis = Entwicklung.Entwickeln(habe, habe[0], Glurak);
    var zeile = Assert.Single(Betroffen(ergebnis, suche, habe));
    Entwicklung.Hochziehen(suche, zeile, Glurak);

    Assert.DoesNotContain(alt, suche);
    var aktiv = Assert.Single(suche);
    Assert.Equal(Glurak.Guid, aktiv.FormGuid);
    Assert.Equal(1, aktiv.Anzahl);
  }

  [Fact]
  public void Hochziehen_FormBeliebig_WechseltNurDasPokemon()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda) };
    var beliebig = Suche(Glumanda);
    beliebig.FormGuid = Guid.Empty;
    var suche = new List<SucheEintrag> { beliebig };

    var ergebnis = Entwicklung.Entwickeln(habe, habe[0], Glurak);
    var zeile = Assert.Single(Betroffen(ergebnis, suche, habe));
    var ziel = Entwicklung.Hochziehen(suche, zeile, Glurak);

    Assert.Equal(Glurak.PokemonGuid, ziel.PokemonGuid);
    Assert.True(ziel.FormBeliebig);
  }

  [Fact]
  public void OhnePassendeSuche_KeineBetroffenenZeilen()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda) };
    var suche = new List<SucheEintrag> { Suche(Glumanda, hundert: true) };

    var ergebnis = Entwicklung.Entwickeln(habe, habe[0], Glurak);

    Assert.Empty(Betroffen(ergebnis, suche, habe));
  }

  [Fact]
  public void Entwickeln_NimmtEinExemplarHeraus_UndLegtZielzeileAn()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda, anzahl: 2, hundert: true) };

    var ergebnis = Entwicklung.Entwickeln(habe, habe[0], Glurak);

    Assert.Equal(1, habe[0].Anzahl);
    Assert.Equal(2, habe.Count);
    Assert.Equal(Glurak.Guid, ergebnis.Nachher.FormGuid);
    Assert.Equal(Glurak.PokemonGuid, ergebnis.Nachher.PokemonGuid);
    Assert.Equal(1, ergebnis.Nachher.Anzahl);
    Assert.True(ergebnis.Nachher.Hundert);
    Assert.Equal(Glumanda.Guid, ergebnis.Vorher.FormGuid);
  }

  [Fact]
  public void Entwickeln_LetztesExemplar_EntferntZeile()
  {
    var glumanda = Habe(Glumanda);
    var habe = new List<HabeEintrag> { glumanda };

    Entwicklung.Entwickeln(habe, glumanda, Glurak);

    Assert.DoesNotContain(glumanda, habe);
    Assert.Equal(Glurak.Guid, Assert.Single(habe).FormGuid);
  }

  [Fact]
  public void Entwickeln_InVorhandeneHabeZeile_ErhoehtDort()
  {
    var glurak = Habe(Glurak);
    var habe = new List<HabeEintrag> { Habe(Glumanda), glurak };

    var ergebnis = Entwicklung.Entwickeln(habe, habe[0], Glurak);

    Assert.Same(glurak, ergebnis.Nachher);
    Assert.Equal(2, glurak.Anzahl);
    Assert.Single(habe);
  }
}
