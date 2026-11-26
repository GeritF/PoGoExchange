using System;
using System.IO;
using System.Linq;
using PoGoExchange.Services;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class SucheNeuViewModelTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_neu_{Guid.NewGuid():N}");

  public void Dispose()
  {
    if (Directory.Exists(_ordner))
    {
      Directory.Delete(_ordner, recursive: true);
    }
  }

  [Fact]
  public void Speichern_LegtZeileAn_SchreibtDatei_UndMeldetErgebnis()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    AnlegeErgebnis? ergebnis = null;
    var vm = new SucheNeuViewModel(bestand, e => ergebnis = e);

    vm.Suchtext = "4";
    vm.AuswahlVerfuegbar = vm.Verfuegbar.Single();
    vm.Anzahl = 2;
    vm.SpeichernCommand.Execute(null);

    Assert.NotNull(ergebnis);
    Assert.Equal("0/2", ergebnis.Nachher.ToString());
    var neu = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    Assert.Equal(2, Assert.Single(neu.Suche).Anzahl);
  }

  [Fact]
  public void OhneAuswahl_MeldetFehler_UndSpeichertNicht()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var aufgerufen = false;
    var vm = new SucheNeuViewModel(bestand, _ => aufgerufen = true);

    vm.SpeichernCommand.Execute(null);

    Assert.False(aufgerufen);
    Assert.Contains("ausw", vm.Meldung);
    Assert.Empty(bestand.Suche);
  }

  [Fact]
  public void Auswahl_BleibtNachAenderungDerAnforderungen()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var vm = new SucheNeuViewModel(bestand, _ => { });
    vm.Suchtext = "4";
    var gewaehlt = vm.Verfuegbar.Single();
    vm.AuswahlVerfuegbar = gewaehlt;

    vm.Anforderungen.Shiny = true;

    Assert.NotNull(vm.Ziel);
    Assert.Equal(gewaehlt.PokemonGuid, vm.Ziel.PokemonGuid);
    Assert.Equal(gewaehlt.FormGuid, vm.Ziel.FormGuid);
  }
}
