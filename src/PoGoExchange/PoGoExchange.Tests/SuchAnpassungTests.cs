using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class SuchAnpassungTests
{
  private static readonly PokemonForm Glumanda = new()
  {
    Guid = Guid.NewGuid(), PokemonGuid = Guid.NewGuid()
  };
  private static readonly PokemonForm Glurak = new()
  {
    Guid = Guid.NewGuid(), PokemonGuid = Guid.NewGuid()
  };
  private static readonly DateTime Jetzt = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

  private static HabeEintrag Habe(PokemonForm form, bool glueck = false, bool hundert = false)
  {
    return new HabeEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Shiny = true,
      Glueck = glueck,
      Hundert = hundert
    };
  }

  private static SucheEintrag Suche(
    PokemonForm form,
    bool erledigt,
    bool glueck = false,
    bool hundert = false)
  {
    return new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Shiny = true,
      Glueck = glueck,
      Hundert = hundert,
      ErledigtAm = erledigt ? Jetzt.AddDays(-1) : null
    };
  }

  private static SuchZeile Zeile(List<SucheEintrag> suche, PokemonForm form, bool hundert = false)
  {
    return SuchZeilen.Bilden(suche)
      .Single(z => z.Muster.FormGuid == form.Guid && z.Muster.Hundert == hundert);
  }

  [Fact]
  public void Beispiel1_MitHaken_EintragWirdHochgezogen()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda), Habe(Glumanda), Habe(Glumanda) };
    var suche = new List<SucheEintrag>
    {
      Suche(Glumanda, true), Suche(Glumanda, true), Suche(Glumanda, true)
    };

    var vorher = SuchAnpassung.Entwickeln(habe[0], Glurak, Jetzt);
    var zeile = Assert.Single(SuchAnpassung.BetroffeneZeilen(vorher, habe[0], suche));
    SuchAnpassung.Hochziehen(zeile, suche, Glurak, Jetzt);

    Assert.Equal("(2/2)", Zeile(suche, Glumanda).Zaehler);
    Assert.Equal("(1/1)", Zeile(suche, Glurak).Zaehler);
    Assert.Empty(Plausibilitaet.Pruefen(suche, habe));
  }

  [Fact]
  public void Beispiel1_OhneHaken_PlausibilitaetMeldetDifferenz()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda), Habe(Glumanda), Habe(Glumanda) };
    var suche = new List<SucheEintrag>
    {
      Suche(Glumanda, true), Suche(Glumanda, true), Suche(Glumanda, true)
    };

    SuchAnpassung.Entwickeln(habe[0], Glurak, Jetzt);

    Assert.Equal("(3/3)", Zeile(suche, Glumanda).Zaehler);
    var hinweis = Assert.Single(Plausibilitaet.Pruefen(suche, habe));
    Assert.Equal(3, hinweis.Zeile.Erledigt);
    Assert.Equal(2, hinweis.PassendeExemplare);
  }

  [Fact]
  public void Beispiel2_100erExemplar_BetrifftBeideZeilen()
  {
    var habe = new List<HabeEintrag>
    {
      Habe(Glumanda, hundert: true), Habe(Glumanda), Habe(Glumanda)
    };
    var suche = new List<SucheEintrag>
    {
      Suche(Glumanda, true), Suche(Glumanda, true), Suche(Glumanda, true),
      Suche(Glumanda, true, hundert: true)
    };

    var vorher = SuchAnpassung.Entwickeln(habe[0], Glurak, Jetzt);
    var betroffen = SuchAnpassung.BetroffeneZeilen(vorher, habe[0], suche);

    Assert.Equal(2, betroffen.Count);

    foreach (var zeile in betroffen)
    {
      SuchAnpassung.Hochziehen(zeile, suche, Glurak, Jetzt);
    }

    Assert.Equal("(2/2)", Zeile(suche, Glumanda).Zaehler);
    Assert.Equal("(1/1)", Zeile(suche, Glurak).Zaehler);
    Assert.Equal("(1/1)", Zeile(suche, Glurak, hundert: true).Zaehler);
  }

  [Fact]
  public void Beispiel2_NormalesExemplar_BetrifftNurEineZeile()
  {
    var habe = new List<HabeEintrag>
    {
      Habe(Glumanda, hundert: true), Habe(Glumanda), Habe(Glumanda)
    };
    var suche = new List<SucheEintrag>
    {
      Suche(Glumanda, true), Suche(Glumanda, true), Suche(Glumanda, true),
      Suche(Glumanda, true, hundert: true)
    };

    var vorher = SuchAnpassung.Entwickeln(habe[1], Glurak, Jetzt);
    var zeile = Assert.Single(SuchAnpassung.BetroffeneZeilen(vorher, habe[1], suche));

    Assert.False(zeile.Muster.Hundert);
  }

  [Fact]
  public void Beispiel3_OhneErledigteSuche_KeineVorschlaege()
  {
    var exemplar = Habe(Glumanda);
    var suche = new List<SucheEintrag> { Suche(Glumanda, false) };

    var vorher = SuchAnpassung.Entwickeln(exemplar, Glurak, Jetzt);

    Assert.Empty(SuchAnpassung.BetroffeneZeilen(vorher, exemplar, suche));
    Assert.Equal(Glurak.Guid, exemplar.FormGuid);
    Assert.Equal(Glurak.PokemonGuid, exemplar.PokemonGuid);
  }

  [Fact]
  public void Beispiel4_Loeschen_EintragWirdWiederGeoeffnet()
  {
    var exemplar = Habe(Glumanda);
    var suche = new List<SucheEintrag>
    {
      Suche(Glumanda, true), Suche(Glumanda, true), Suche(Glumanda, true)
    };

    var zeile = Assert.Single(SuchAnpassung.BetroffeneZeilen(exemplar, null, suche));
    var geoeffnet = SuchAnpassung.WiederOeffnen(zeile, suche, Jetzt);

    Assert.NotNull(geoeffnet);
    Assert.Null(geoeffnet.ErledigtAm);
    Assert.Equal(Jetzt, geoeffnet.GeaendertAm);
    Assert.Equal("(2/3)", Zeile(suche, Glumanda).Zaehler);
  }

  [Fact]
  public void Beispiel5_GlueckEntfernt_BetrifftNurGluecksZeile()
  {
    var exemplar = Habe(Glumanda, glueck: true);
    var suche = new List<SucheEintrag>
    {
      Suche(Glumanda, true, glueck: true),
      Suche(Glumanda, true)
    };

    var vorher = exemplar.Kopie();
    exemplar.Glueck = false;
    var zeile = Assert.Single(SuchAnpassung.BetroffeneZeilen(vorher, exemplar, suche));

    Assert.True(zeile.Muster.Glueck);

    SuchAnpassung.WiederOeffnen(zeile, suche, Jetzt);
    var zeilen = SuchZeilen.Bilden(suche);
    Assert.Equal("", zeilen.Single(z => z.Muster.Glueck).Zaehler);
    Assert.Equal("(1/1)", zeilen.Single(z => !z.Muster.Glueck).Zaehler);
  }

  [Fact]
  public void SofortErledigbar_BeruecksichtigtBereitsErledigte()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda), Habe(Glumanda) };
    var neu = Suche(Glumanda, false);

    var ohneBisherige = Plausibilitaet.SofortErledigbar(neu, new List<SucheEintrag>(), habe);
    var mitEinemErledigten = Plausibilitaet.SofortErledigbar(
      neu,
      new List<SucheEintrag> { Suche(Glumanda, true) },
      habe);

    Assert.Equal(2, ohneBisherige);
    Assert.Equal(1, mitEinemErledigten);
  }

  [Fact]
  public void Hochziehen_FormBeliebig_WechseltNurDasPokemon()
  {
    var exemplar = Habe(Glumanda);
    var beliebig = new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = Glumanda.PokemonGuid,
      FormGuid = Guid.Empty,
      Shiny = true,
      ErledigtAm = Jetzt.AddDays(-1)
    };
    var suche = new List<SucheEintrag> { beliebig };

    var vorher = SuchAnpassung.Entwickeln(exemplar, Glurak, Jetzt);
    var zeile = Assert.Single(SuchAnpassung.BetroffeneZeilen(vorher, exemplar, suche));
    SuchAnpassung.Hochziehen(zeile, suche, Glurak, Jetzt);

    Assert.Equal(Glurak.PokemonGuid, beliebig.PokemonGuid);
    Assert.True(beliebig.FormBeliebig);
    Assert.NotNull(beliebig.ErledigtAm);
  }
}
