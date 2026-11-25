using System;
using System.IO;
using System.Linq;
using PoGoExchange.Services;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class HabeNeuViewModelTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_habe_neu_{Guid.NewGuid():N}");

  public void Dispose()
  {
    if (Directory.Exists(_ordner))
    {
      Directory.Delete(_ordner, recursive: true);
    }
  }

  [Fact]
  public void Speichern_LegtZeileAn_UndSchreibtDatei()
  {
    var bestand = Datenbestand.Laden(_ordner, DateTime.UtcNow);
    HabeAnlegeErgebnis? ergebnis = null;
    var vm = new HabeNeuViewModel(bestand, e => ergebnis = e);

    vm.Suchtext = "4";
    vm.Auswahl = vm.Ziele.Single();
    vm.Merkmale.Shiny = true;
    vm.Anzahl = 2;
    vm.SpeichernCommand.Execute(null);

    Assert.NotNull(ergebnis);
    Assert.Equal(2, ergebnis.Nachher);
    var neu = Datenbestand.Laden(_ordner, DateTime.UtcNow);
    var zeile = Assert.Single(neu.Habe);
    Assert.Equal(2, zeile.Anzahl);
    Assert.True(zeile.Shiny);
  }

  [Fact]
  public void Auswahl_BleibtErhalten_WennSuchfeldSieAusblendet()
  {
    var bestand = Datenbestand.Laden(_ordner, DateTime.UtcNow);
    var vm = new HabeNeuViewModel(bestand, _ => { });
    vm.Suchtext = "4";
    var gewaehlt = vm.Ziele.Single();
    vm.Auswahl = gewaehlt;

    vm.Suchtext = "25";
    vm.Suchtext = "4";

    Assert.Equal(gewaehlt, vm.Ziel);
    Assert.Equal(gewaehlt, vm.Auswahl);
  }

  [Fact]
  public void Erhalten_FesteForm_IstVorausgewaehlt_UndMerkmaleVorbelegt()
  {
    var bestand = Datenbestand.Laden(_ordner, DateTime.UtcNow);
    var pokemon = bestand.Katalog.AllePokemon().Single(p => p.DexNr == 4);
    var form = bestand.Katalog.FormenZuPokemon(pokemon.Guid).First();
    var vorlage = new PoGoExchange.Models.HabeEintrag { Shiny = true, Glueck = true };

    var vm = new HabeNeuViewModel(
      bestand, _ => { }, vorlage, pokemon.Guid, form.Guid, "Erhalten");

    Assert.Equal("Erhalten", vm.Titel);
    Assert.False(vm.SuchfeldSichtbar);
    Assert.Equal(form.Guid, vm.Ziel?.FormGuid);
    Assert.True(vm.Merkmale.Shiny);
    Assert.True(vm.Merkmale.Glueck);
  }

  [Fact]
  public void Erhalten_FormBeliebig_ZeigtNurFormenDesPokemon()
  {
    var bestand = Datenbestand.Laden(_ordner, DateTime.UtcNow);
    var vulpix = bestand.Katalog.AllePokemon().Single(p => p.DexNr == 37);

    var vm = new HabeNeuViewModel(bestand, _ => { }, null, vulpix.Guid, null, "Erhalten");

    Assert.Equal(2, vm.Ziele.Count);
    Assert.All(vm.Ziele, z => Assert.Equal(vulpix.Guid, z.PokemonGuid));
    Assert.Null(vm.Ziel);
  }
}
