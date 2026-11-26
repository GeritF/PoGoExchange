using System;
using System.IO;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class EntwickelnViewModelTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_entwickeln_{Guid.NewGuid():N}");

  public void Dispose()
  {
    if (Directory.Exists(_ordner))
    {
      Directory.Delete(_ordner, recursive: true);
    }
  }

  // Glumanda (Dex 4) mit 3 Shiny-Exemplaren, Suche nach 3 Shiny-Glumanda
  private (Datenbestand Bestand, HabeEintrag Habe, SucheEintrag Suche) Vorbereiten()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var glumanda = bestand.Katalog.AllePokemon().Single(p => p.DexNr == 4);
    var form = bestand.Katalog.FormenZuPokemon(glumanda.Guid).First();
    var habe = new HabeEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = glumanda.Guid, FormGuid = form.Guid,
      Shiny = true, Anzahl = 3
    };
    var suche = new SucheEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = glumanda.Guid, FormGuid = form.Guid,
      Shiny = true, Anzahl = 3
    };
    bestand.Habe.Add(habe);
    bestand.Suche.Add(suche);
    return (bestand, habe, suche);
  }

  private static void GlurakWaehlen(EntwickelnViewModel vm)
  {
    vm.Suchtext = "6";
    vm.Auswahl = vm.Ziele.Single();
  }

  [Fact]
  public void Ziele_OhneAktuelleForm_BetroffeneZeilenSindAngehakt()
  {
    var (bestand, habe, _) = Vorbereiten();
    var vm = new EntwickelnViewModel(bestand, habe, _ => { });

    vm.Suchtext = "4";
    Assert.Empty(vm.Ziele);

    GlurakWaehlen(vm);

    var betroffen = Assert.Single(vm.Betroffene);
    Assert.True(betroffen.Ausgewaehlt);
    Assert.True(vm.HatBetroffene);
    Assert.Contains("(3/3)", betroffen.Text);
  }

  [Fact]
  public void Entwickeln_MitHaken_ZiehtSucheHoch_UndSpeichert()
  {
    var (bestand, habe, _) = Vorbereiten();
    var rueckmeldung = "";
    var vm = new EntwickelnViewModel(bestand, habe, r => rueckmeldung = r);
    GlurakWaehlen(vm);

    vm.EntwickelnCommand.Execute(null);

    Assert.Contains("Glurak", rueckmeldung);
    var neu = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    Assert.Equal(new[] { 2, 1 }, neu.Habe.Select(h => h.Anzahl).OrderDescending());
    Assert.Equal(new[] { 2, 1 }, neu.Suche.Select(s => s.Anzahl).OrderDescending());
    Assert.All(SuchZeilen.Bilden(neu.Suche, neu.Habe), z => Assert.Equal(0, z.Offen));
  }

  [Fact]
  public void Entwickeln_OhneHaken_LaesstSucheUnveraendert()
  {
    var (bestand, habe, suche) = Vorbereiten();
    var vm = new EntwickelnViewModel(bestand, habe, _ => { });
    GlurakWaehlen(vm);

    vm.Betroffene.Single().Ausgewaehlt = false;
    vm.EntwickelnCommand.Execute(null);

    Assert.Same(suche, Assert.Single(bestand.Suche));
    Assert.Equal(3, suche.Anzahl);
    Assert.Equal(2, habe.Anzahl);
  }
}
