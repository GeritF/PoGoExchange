using System;
using System.Collections.Generic;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class FormAbgleichTests
{
  private static readonly Guid Icognito = Guid.NewGuid();

  private static readonly PokemonForm OffiziellQ = new()
  {
    Guid = Guid.NewGuid(), PokemonGuid = Icognito, Form = "Q"
  };

  private static PokemonForm Eigene(string form, string region = "")
  {
    return new PokemonForm
    {
      Guid = Guid.NewGuid(), PokemonGuid = Icognito, Form = form, Region = region
    };
  }

  private static HabeEintrag Habe(PokemonForm form, int anzahl = 1)
  {
    return new HabeEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = Icognito, FormGuid = form.Guid, Anzahl = anzahl
    };
  }

  [Fact]
  public void Finden_OhneGrossKlein_UndMitGeschuetztemLeerzeichen()
  {
    var klein = Eigene("q");
    var nbsp = Eigene("\u00a0Q");
    var andere = Eigene("Z");
    var andereRegion = Eigene("Q", "Alola");

    var uebernahmen = FormAbgleich.Finden(
      new[] { klein, nbsp, andere, andereRegion }, new[] { OffiziellQ });

    Assert.Equal(2, uebernahmen.Count);
    Assert.All(uebernahmen, u => Assert.Same(OffiziellQ, u.Offiziell));
  }

  [Fact]
  public void Finden_GleicheGuid_WirdNichtUebernommen()
  {
    var gleicheGuid = new PokemonForm
    {
      Guid = OffiziellQ.Guid, PokemonGuid = Icognito, Form = "Q"
    };

    Assert.Empty(FormAbgleich.Finden(new[] { gleicheGuid }, new[] { OffiziellQ }));
  }

  [Fact]
  public void Uebernehmen_StelltVerweiseUm_UndFuehrtZeilenZusammen()
  {
    var eigene = Eigene("q");
    var formen = new List<PokemonForm> { eigene };
    var habe = new List<HabeEintrag> { Habe(eigene, 2), Habe(OffiziellQ, 3) };
    var suche = new List<SucheEintrag>
    {
      new() { Guid = Guid.NewGuid(), PokemonGuid = Icognito, FormGuid = eigene.Guid }
    };
    var uebernahmen = FormAbgleich.Finden(formen, new[] { OffiziellQ });

    FormAbgleich.Uebernehmen(uebernahmen, formen, habe, suche, new List<UeberEintrag>());

    Assert.Empty(formen);
    var zeile = Assert.Single(habe);
    Assert.Equal(OffiziellQ.Guid, zeile.FormGuid);
    Assert.Equal(5, zeile.Anzahl);
    Assert.Equal(OffiziellQ.Guid, Assert.Single(suche).FormGuid);
  }

  [Fact]
  public void Uebernehmen_DoppelteInHabeIchUeber_BleibenEinmal()
  {
    var eigene = Eigene("q");
    var formen = new List<PokemonForm> { eigene };
    var ueber = new List<UeberEintrag>
    {
      new() { Guid = Guid.NewGuid(), PokemonGuid = Icognito, FormGuid = eigene.Guid },
      new() { Guid = Guid.NewGuid(), PokemonGuid = Icognito, FormGuid = OffiziellQ.Guid }
    };
    var uebernahmen = FormAbgleich.Finden(formen, new[] { OffiziellQ });

    FormAbgleich.Uebernehmen(
      uebernahmen, formen, new List<HabeEintrag>(), new List<SucheEintrag>(), ueber);

    Assert.Equal(OffiziellQ.Guid, Assert.Single(ueber).FormGuid);
  }
}
