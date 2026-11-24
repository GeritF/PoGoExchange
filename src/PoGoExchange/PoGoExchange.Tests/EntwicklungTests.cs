using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class EntwicklungTests
{
  private static readonly DateTime Jetzt = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

  private static readonly PokemonForm Glumanda = new()
  {
    Guid = Guid.NewGuid(), PokemonGuid = Guid.NewGuid()
  };
  private static readonly PokemonForm Glurak = new()
  {
    Guid = Guid.NewGuid(), PokemonGuid = Guid.NewGuid()
  };

  private static HabeEintrag Habe(PokemonForm form, bool hundert = false)
  {
    return new HabeEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Shiny = true,
      Hundert = hundert
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

  [Fact]
  public void Beispiel_DreiGlumanda_EinesWirdGlurak_ZeileWirdHochgezogen()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda), Habe(Glumanda), Habe(Glumanda) };
    var suche = new List<SucheEintrag> { Suche(Glumanda, anzahl: 3) };

    var vorher = Entwicklung.Entwickeln(habe[0], Glurak, Jetzt);
    var zeile = Assert.Single(Entwicklung.BetroffeneZeilen(vorher, habe[0], suche, habe));
    Entwicklung.Hochziehen(suche, zeile, Glurak, Jetzt);

    Assert.Equal("(2/2)", Zaehler(suche, habe, Glumanda));
    Assert.Equal("(1/1)", Zaehler(suche, habe, Glurak));
  }

  [Fact]
  public void OhneHochziehen_SinktNurDerZaehler()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda), Habe(Glumanda), Habe(Glumanda) };
    var suche = new List<SucheEintrag> { Suche(Glumanda, anzahl: 3) };

    Entwicklung.Entwickeln(habe[0], Glurak, Jetzt);

    Assert.Equal("(2/3)", Zaehler(suche, habe, Glumanda));
    Assert.Single(suche);
  }

  [Fact]
  public void Exemplar100er_BetrifftBeideZeilen_NormalesNurEine()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda, hundert: true), Habe(Glumanda) };
    var suche = new List<SucheEintrag>
    {
      Suche(Glumanda, anzahl: 2),
      Suche(Glumanda, hundert: true)
    };

    var vorher100er = habe[0].Kopie();
    var nachher100er = habe[0].Kopie();
    nachher100er.PokemonGuid = Glurak.PokemonGuid;
    nachher100er.FormGuid = Glurak.Guid;
    var vorherNormal = habe[1].Kopie();
    var nachherNormal = habe[1].Kopie();
    nachherNormal.PokemonGuid = Glurak.PokemonGuid;
    nachherNormal.FormGuid = Glurak.Guid;

    var betroffen100er = Entwicklung.BetroffeneZeilen(vorher100er, nachher100er, suche, habe);
    var betroffenNormal = Entwicklung.BetroffeneZeilen(vorherNormal, nachherNormal, suche, habe);

    Assert.Equal(2, betroffen100er.Count);
    var zeile = Assert.Single(betroffenNormal);
    Assert.False(zeile.Muster.Hundert);
  }

  [Fact]
  public void Hochziehen_InVorhandeneZeile_ErhoehtDortDieAnzahl()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda) };
    var glurakZeile = Suche(Glurak);
    var suche = new List<SucheEintrag> { Suche(Glumanda, anzahl: 2), glurakZeile };

    var vorher = Entwicklung.Entwickeln(habe[0], Glurak, Jetzt);
    var zeile = Assert.Single(Entwicklung.BetroffeneZeilen(vorher, habe[0], suche, habe));
    var ziel = Entwicklung.Hochziehen(suche, zeile, Glurak, Jetzt);

    Assert.Same(glurakZeile, ziel);
    Assert.Equal(2, glurakZeile.Anzahl);
    Assert.Equal(2, suche.Count);
  }

  [Fact]
  public void Hochziehen_LetzteEinheit_LoeschtAlteZeile()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda) };
    var alt = Suche(Glumanda);
    var suche = new List<SucheEintrag> { alt };

    var vorher = Entwicklung.Entwickeln(habe[0], Glurak, Jetzt);
    var zeile = Assert.Single(Entwicklung.BetroffeneZeilen(vorher, habe[0], suche, habe));
    Entwicklung.Hochziehen(suche, zeile, Glurak, Jetzt);

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

    var vorher = Entwicklung.Entwickeln(habe[0], Glurak, Jetzt);
    var zeile = Assert.Single(Entwicklung.BetroffeneZeilen(vorher, habe[0], suche, habe));
    var ziel = Entwicklung.Hochziehen(suche, zeile, Glurak, Jetzt);

    Assert.Equal(Glurak.PokemonGuid, ziel.PokemonGuid);
    Assert.True(ziel.FormBeliebig);
  }

  [Fact]
  public void OhnePassendeSuche_KeineBetroffenenZeilen()
  {
    var habe = new List<HabeEintrag> { Habe(Glumanda) };
    var suche = new List<SucheEintrag> { Suche(Glumanda, hundert: true) };

    var vorher = Entwicklung.Entwickeln(habe[0], Glurak, Jetzt);

    Assert.Empty(Entwicklung.BetroffeneZeilen(vorher, habe[0], suche, habe));
  }

  [Fact]
  public void Entwickeln_StelltPokemonUndFormUm()
  {
    var exemplar = Habe(Glumanda);

    var vorher = Entwicklung.Entwickeln(exemplar, Glurak, Jetzt);

    Assert.Equal(Glurak.PokemonGuid, exemplar.PokemonGuid);
    Assert.Equal(Glurak.Guid, exemplar.FormGuid);
    Assert.Equal(Glumanda.Guid, vorher.FormGuid);
  }
}
