using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class UeberBearbeitungTests
{
  private static readonly PokemonForm Glumanda = new()
  {
    Guid = Guid.NewGuid(), PokemonGuid = Guid.NewGuid()
  };

  private static readonly FormZiel GlumandaZiel =
    new(Glumanda.PokemonGuid, Glumanda.Guid, 4, "Glumanda");

  private static UeberEintrag Eintrag(bool shiny)
  {
    return new UeberEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = Glumanda.PokemonGuid,
      FormGuid = Glumanda.Guid,
      Shiny = shiny
    };
  }

  [Fact]
  public void Anlegen_NeuerEintrag()
  {
    var ueber = new List<UeberEintrag>();

    var (eintrag, neu) = UeberBearbeitung.Anlegen(
      ueber, new HabeEintrag { Shiny = true }, GlumandaZiel);

    Assert.True(neu);
    Assert.Same(eintrag, Assert.Single(ueber));
    Assert.True(eintrag.Shiny);
  }

  [Fact]
  public void Anlegen_GleicherEintrag_WirdNichtDoppeltAngelegt()
  {
    var vorhanden = Eintrag(shiny: true);
    var ueber = new List<UeberEintrag> { vorhanden };

    var (eintrag, neu) = UeberBearbeitung.Anlegen(
      ueber, new HabeEintrag { Shiny = true }, GlumandaZiel);

    Assert.False(neu);
    Assert.Same(vorhanden, eintrag);
    Assert.Single(ueber);
  }

  [Fact]
  public void Konflikt_ErkenntGleichenEintrag_UndUnveraenderteMerkmale()
  {
    var shiny = Eintrag(shiny: true);
    var normal = Eintrag(shiny: false);
    var ueber = new List<UeberEintrag> { shiny, normal };

    Assert.Same(normal, UeberBearbeitung.Konflikt(ueber, shiny, new HabeEintrag()));
    Assert.Null(UeberBearbeitung.Konflikt(ueber, shiny, new HabeEintrag { Shiny = true }));
  }

  [Fact]
  public void MerkmaleAendern_MitKonflikt_BehaeltNurEinen()
  {
    var shiny = Eintrag(shiny: true);
    var normal = Eintrag(shiny: false);
    var ueber = new List<UeberEintrag> { shiny, normal };

    var ergebnis = UeberBearbeitung.MerkmaleAendern(ueber, shiny, new HabeEintrag());

    Assert.Same(shiny, ergebnis);
    Assert.False(ergebnis.Shiny);
    Assert.Same(shiny, Assert.Single(ueber));
  }

  [Fact]
  public void Loeschen_EntferntEintrag()
  {
    var eintrag = Eintrag(shiny: true);
    var ueber = new List<UeberEintrag> { eintrag };

    UeberBearbeitung.Loeschen(ueber, eintrag);

    Assert.Empty(ueber);
  }
}
