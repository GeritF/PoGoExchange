using PoGoExchange.Models;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class UeberViewModelTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_ueber_{Guid.NewGuid():N}");

  public void Dispose()
  {
    if (Directory.Exists(_ordner))
    {
      Directory.Delete(_ordner, recursive: true);
    }
  }

  private sealed class Startseite : SeiteViewModel
  {
    public override string Titel => "Start";
  }

  [Fact]
  public void Neu_Speichern_LegtEintragAn_UndSchreibtDatei()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var neuGemeldet = false;
    var vm = new UeberNeuViewModel(bestand, (_, neu) => neuGemeldet = neu);

    vm.Suchtext = "4";
    vm.Auswahl = vm.Ziele.Single();
    vm.Merkmale.Shiny = true;
    vm.SpeichernCommand.Execute(null);

    Assert.True(neuGemeldet);
    var neuGeladen = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    Assert.True(Assert.Single(neuGeladen.Ueber).Shiny);
  }

  [Fact]
  public void Neu_GleicherEintrag_WirdAlsVorhandenGemeldet()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);

    UeberNeuViewModel Anlegen(Action<UeberEintrag, bool> gespeichert)
    {
      var vm = new UeberNeuViewModel(bestand, gespeichert);
      vm.Suchtext = "4";
      vm.Auswahl = vm.Ziele.Single();
      vm.SpeichernCommand.Execute(null);
      return vm;
    }

    Anlegen((_, _) => { });
    bool? zweiterNeu = null;
    Anlegen((_, neu) => zweiterNeu = neu);

    Assert.False(zweiterNeu);
    Assert.Single(bestand.Ueber);
  }

  [Fact]
  public void Zeile_Loeschen_ErstRueckfrage_JaEntferntUndGehtZurueck()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var pokemon = bestand.Katalog.AllePokemon().Single(p => p.DexNr == 4);
    var form = bestand.Katalog.FormenZuPokemon(pokemon.Guid).First();
    var eintrag = new UeberEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = pokemon.Guid, FormGuid = form.Guid
    };
    bestand.Ueber.Add(eintrag);
    var navigation = new Navigation();
    var start = new Startseite();
    navigation.Oeffnen(start);
    var vm = new UeberZeileViewModel(navigation, bestand, eintrag, _ => { });
    navigation.Oeffnen(vm);

    vm.LoeschenCommand.Execute(null);

    Assert.True(vm.LoeschRueckfrageSichtbar);
    Assert.Contains(eintrag, bestand.Ueber);

    vm.LoeschenJaCommand.Execute(null);

    Assert.Empty(bestand.Ueber);
    Assert.Same(start, navigation.AktuelleSeite);
  }
}
