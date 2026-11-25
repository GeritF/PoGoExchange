using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PoGoExchange.Models;
using PoGoExchange.Services;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

// Gemeinsame Sammlung mit anderen Tests, die AppActions veraendern,
// damit diese nicht parallel laufen
[Collection("Zwischenablage")]
public class ImportExportViewModelTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_import_{Guid.NewGuid():N}");

  public void Dispose()
  {
    if (Directory.Exists(_ordner))
    {
      Directory.Delete(_ordner, recursive: true);
    }
  }

  // Bestand mit einer offenen Suche nach Glumanda Shiny
  private Datenbestand MitGlumandaSuche()
  {
    var bestand = Datenbestand.Laden(_ordner, DateTime.UtcNow);
    var glumanda = bestand.Katalog.AllePokemon().Single(p => p.DexNr == 4);
    var form = bestand.Katalog.FormenZuPokemon(glumanda.Guid).First();
    bestand.Suche.Add(new SucheEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = glumanda.Guid, FormGuid = form.Guid, Shiny = true
    });
    return bestand;
  }

  [Fact]
  public void EigeneListe_WirdBeimOeffnenErzeugt()
  {
    var vm = new ImportExportViewModel(new Navigation(), MitGlumandaSuche());

    Assert.Contains("#4 Glumanda - Shiny", vm.EigeneListe);
    Assert.False(vm.ListeLeer);
    Assert.False(vm.HatExportFehler);
  }

  [Fact]
  public void OhneEintraege_IstListeLeer()
  {
    var bestand = Datenbestand.Laden(_ordner, DateTime.UtcNow);

    var vm = new ImportExportViewModel(new Navigation(), bestand);

    Assert.Equal("", vm.EigeneListe);
    Assert.True(vm.ListeLeer);
  }

  [Fact]
  public async Task Kopieren_UebergibtEigeneListe()
  {
    var vorher = AppActions.InZwischenablageKopieren;
    string? kopiert = null;
    AppActions.InZwischenablageKopieren = text =>
    {
      kopiert = text;
      return Task.CompletedTask;
    };

    try
    {
      var vm = new ImportExportViewModel(new Navigation(), MitGlumandaSuche());

      await vm.KopierenCommand.ExecuteAsync(null);

      Assert.Equal(vm.EigeneListe, kopiert);
      Assert.Equal("In die Zwischenablage kopiert", vm.ExportRueckmeldung);
    }
    finally
    {
      AppActions.InZwischenablageKopieren = vorher;
    }
  }

  [Fact]
  public async Task Einfuegen_UebernimmtTextAusZwischenablage()
  {
    var vorher = AppActions.AusZwischenablageLesen;
    const string text = "Kanto fehlt:\n- #4 Glumanda\n";
    AppActions.AusZwischenablageLesen = () => Task.FromResult<string?>(text);

    try
    {
      var vm = new ImportExportViewModel(new Navigation(), MitGlumandaSuche());

      await vm.EinfuegenCommand.ExecuteAsync(null);

      Assert.Equal(text, vm.FremderText);
      Assert.False(vm.HatAbgleichMeldung);
    }
    finally
    {
      AppActions.AusZwischenablageLesen = vorher;
    }
  }

  [Fact]
  public void Abgleichen_OhneText_MeldetDas_UndBleibtAufDerSeite()
  {
    var navigation = new Navigation();
    var vm = new ImportExportViewModel(navigation, MitGlumandaSuche());
    navigation.Oeffnen(vm);

    vm.AbgleichenCommand.Execute(null);

    Assert.Contains("einf\u00fcgen", vm.AbgleichMeldung);
    Assert.Same(vm, navigation.AktuelleSeite);
  }

  [Fact]
  public void Abgleichen_OeffnetErgebnisseite()
  {
    var navigation = new Navigation();
    var vm = new ImportExportViewModel(navigation, MitGlumandaSuche());
    navigation.Oeffnen(vm);
    vm.FremderText = "Kanto abzugeben:\n- #4 Glumanda - Shiny\n";

    vm.AbgleichenCommand.Execute(null);

    var ergebnis = Assert.IsType<AbgleichErgebnisViewModel>(navigation.AktuelleSeite);
    var abschnitt = Assert.Single(ergebnis.Abschnitte);
    Assert.Equal("Er hat, was ich suche (1)", abschnitt.Titel);
  }
}
