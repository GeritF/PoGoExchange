using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PoGoExchange.Models;
using PoGoExchange.Services;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

// Veraendert AppActions, deshalb nicht parallel zu anderen solchen Tests
[Collection("Zwischenablage")]
public class EigeneListenViewModelTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_listen_{Guid.NewGuid():N}");

  public void Dispose()
  {
    if (Directory.Exists(_ordner))
    {
      Directory.Delete(_ordner, recursive: true);
    }

    AppActions.DateiSpeichern = null;
    AppActions.DateiOeffnen = null;
  }

  [Fact]
  public async Task Exportieren_UebergibtNamenUndPaket()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    string? name = null;
    byte[]? inhalt = null;
    AppActions.DateiSpeichern = (n, i) =>
    {
      name = n;
      inhalt = i;
      return Task.FromResult(true);
    };
    var vm = new EigeneListenViewModel(bestand, () => null);

    await vm.ExportierenCommand.ExecuteAsync(null);

    Assert.StartsWith("PoGoExchange_", name);
    Assert.EndsWith(".zip", name);
    Assert.NotNull(inhalt);
    Assert.Contains("Exportiert", vm.Meldung);
  }

  [Fact]
  public async Task Importieren_ErstRueckfrage_JaErsetztUndLaedtNeu()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var glumanda = bestand.Katalog.AllePokemon().Single(p => p.DexNr == 4);
    var form = bestand.Katalog.FormenZuPokemon(glumanda.Guid).First();
    bestand.Habe.Add(new HabeEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = glumanda.Guid, FormGuid = form.Guid
    });
    Assert.Null(bestand.HabeSpeichern());
    var paket = Exportpaket.Erstellen(_ordner, bestand.Nutzdaten.Version, DateTime.UtcNow);
    bestand.Habe.Clear();
    Assert.Null(bestand.HabeSpeichern());
    AppActions.DateiOeffnen = () => Task.FromResult<byte[]?>(paket);
    Datenbestand? neu = null;
    var vm = new EigeneListenViewModel(bestand, () =>
    {
      neu = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
      return neu;
    });

    await vm.ImportierenCommand.ExecuteAsync(null);

    Assert.True(vm.RueckfrageSichtbar);
    Assert.Contains("Habe ich 1 Zeilen", vm.RueckfrageText);
    Assert.Null(neu);

    vm.ImportJaCommand.Execute(null);

    Assert.NotNull(neu);
    Assert.Single(neu.Habe);
  }

  [Fact]
  public async Task Importieren_Abgebrochen_TutNichts()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    AppActions.DateiOeffnen = () => Task.FromResult<byte[]?>(null);
    var vm = new EigeneListenViewModel(bestand, () => null);

    await vm.ImportierenCommand.ExecuteAsync(null);

    Assert.False(vm.RueckfrageSichtbar);
    Assert.False(vm.HatMeldung);
  }
}
