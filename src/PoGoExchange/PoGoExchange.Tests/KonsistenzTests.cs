using System;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class KonsistenzTests
{
  private readonly Pokemon _glumanda = new() { Guid = Guid.NewGuid(), DexNr = 4, Name = "Glumanda" };
  private readonly Pokemon _glurak = new() { Guid = Guid.NewGuid(), DexNr = 6, Name = "Glurak" };
  private readonly PokemonForm _glumandaForm;
  private readonly Katalog _katalog;

  public KonsistenzTests()
  {
    _glumandaForm = new PokemonForm { Guid = Guid.NewGuid(), PokemonGuid = _glumanda.Guid };
    _katalog = new Katalog(
      new[] { _glumanda, _glurak },
      new[] { _glumandaForm },
      Array.Empty<RegionBereich>());
  }

  [Fact]
  public void GueltigerHabeEintrag_OhneFehler()
  {
    var eintrag = new HabeEintrag { PokemonGuid = _glumanda.Guid, FormGuid = _glumandaForm.Guid };

    Assert.Null(Konsistenz.EintragPruefen(_katalog, eintrag, formPflicht: true));
  }

  [Fact]
  public void HabeEintragOhneForm_IstFehler()
  {
    var eintrag = new HabeEintrag { PokemonGuid = _glumanda.Guid, FormGuid = Guid.Empty };

    var grund = Konsistenz.EintragPruefen(_katalog, eintrag, formPflicht: true);

    Assert.NotNull(grund);
    Assert.Contains("Form fehlt", grund);
  }

  [Fact]
  public void FormGehoertZuAnderemPokemon_IstFehler()
  {
    var eintrag = new HabeEintrag { PokemonGuid = _glurak.Guid, FormGuid = _glumandaForm.Guid };

    var grund = Konsistenz.EintragPruefen(_katalog, eintrag, formPflicht: true);

    Assert.NotNull(grund);
    Assert.Contains("geh\u00f6rt nicht zu", grund);
  }

  [Fact]
  public void SucheMitFormBeliebig_IstGueltig()
  {
    var eintrag = new SucheEintrag { PokemonGuid = _glurak.Guid, FormGuid = Guid.Empty };

    Assert.Null(Konsistenz.EintragPruefen(_katalog, eintrag, formPflicht: false));
  }

  [Fact]
  public void UnbekanntesPokemon_IstFehler()
  {
    var eintrag = new SucheEintrag { PokemonGuid = Guid.NewGuid(), FormGuid = Guid.Empty };

    var grund = Konsistenz.EintragPruefen(_katalog, eintrag, formPflicht: false);

    Assert.NotNull(grund);
    Assert.Contains("nicht gefunden", grund);
  }

  [Fact]
  public void ListenPruefen_MeldetListe_UndIgnoriertGeloeschte()
  {
    var habeOhneForm = new HabeEintrag { Guid = Guid.NewGuid(), PokemonGuid = _glumanda.Guid };
    var geloeschtOhneForm = new UeberEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = _glumanda.Guid,
      GeloeschtAm = DateTime.UtcNow
    };
    var sucheBeliebig = new SucheEintrag { Guid = Guid.NewGuid(), PokemonGuid = _glumanda.Guid };

    var fehler = Konsistenz.ListenPruefen(
      _katalog,
      new[] { habeOhneForm },
      new[] { sucheBeliebig },
      new[] { geloeschtOhneForm });

    var einziger = Assert.Single(fehler);
    Assert.Equal("Habe ich", einziger.Liste);
    Assert.Equal(habeOhneForm.Guid, einziger.EintragGuid);
  }
}
