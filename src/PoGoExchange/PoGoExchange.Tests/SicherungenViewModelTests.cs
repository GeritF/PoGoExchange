using System;
using System.IO;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class SicherungenViewModelTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_sicherung_vm_{Guid.NewGuid():N}");

  public void Dispose()
  {
    if (Directory.Exists(_ordner))
    {
      Directory.Delete(_ordner, recursive: true);
    }
  }

  [Fact]
  public void JetztSichern_ErscheintInDerListe()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var vm = new SicherungenViewModel(bestand, () => null);
    Assert.True(vm.KeineVorhanden);

    vm.JetztSichernCommand.Execute(null);

    Assert.Single(vm.Liste);
    Assert.False(vm.KeineVorhanden);
    Assert.Equal("Sicherung angelegt", vm.Meldung);
  }

  [Fact]
  public void Wiederherstellen_ErstRueckfrage_DannNeuLaden()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var glumanda = bestand.Katalog.AllePokemon().Single(p => p.DexNr == 4);
    var form = bestand.Katalog.FormenZuPokemon(glumanda.Guid).First();
    // Aelterer Stand: eine Zeile in "Habe ich"; Sicherung mit festem, altem Zeitpunkt
    bestand.Habe.Add(new HabeEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = glumanda.Guid, FormGuid = form.Guid
    });
    Assert.Null(bestand.HabeSpeichern());
    Sicherungen.Anlegen(_ordner, new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
    bestand.Habe.Clear();
    Assert.Null(bestand.HabeSpeichern());

    Datenbestand? neu = null;
    var vm = new SicherungenViewModel(bestand, () =>
    {
      neu = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
      return neu;
    });

    vm.Liste.Last().WiederherstellenCommand.Execute(null);
    Assert.True(vm.RueckfrageSichtbar);
    Assert.Null(neu);

    vm.WiederherstellenJaCommand.Execute(null);

    Assert.NotNull(neu);
    Assert.Single(neu.Habe);
    Assert.NotNull(neu.LetzteAenderung);
  }
}
