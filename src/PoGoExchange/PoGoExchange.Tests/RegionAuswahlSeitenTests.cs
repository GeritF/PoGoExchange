using PoGoExchange.Models;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

// Regionsfilter auf den Seiten mit Pokemon-Auswahl. Testdaten: Alola-Vulpix
// (#37) ist die einzige Form mit Region, Meltan (#808) liegt in keinem
// Bereich (Auffang-Region "Unbekannt").
public class RegionAuswahlSeitenTests : IDisposable
{
  private static readonly Guid VulpixAlola = Guid.Parse("16d50dbf-cf04-4945-8610-7d058e8324b9");
  private static readonly Guid VulpixNormal = Guid.Parse("33ee8314-3e1f-4fd4-b63d-e5f6292d59f1");
  private static readonly Guid Meltan = Guid.Parse("45b45bbe-8277-40df-a232-e9b3f4fc9f16");

  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_region_{Guid.NewGuid():N}");

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

  private static void RegionWaehlen(RegionFilterViewModel filter, string region)
  {
    var index = filter.Regionen.ToList().IndexOf(region);
    Assert.True(index > 0, $"Region {region} nicht in der Auswahl");
    filter.RegionIndex = index;
  }

  [Fact]
  public void SucheNeu_Alola_NurAlolaVulpix()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var vm = new SucheNeuViewModel(bestand, _ => { });

    RegionWaehlen(vm.RegionFilter, "Alola");

    var ziele = vm.Verfuegbar.Concat(vm.HastDuSchon).Concat(vm.NichtVerfuegbar);
    Assert.Equal(VulpixAlola, Assert.Single(ziele).FormGuid);
  }

  [Fact]
  public void HabeNeu_Alola_NurAlolaVulpix()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var vm = new HabeNeuViewModel(bestand, _ => { });

    RegionWaehlen(vm.RegionFilter, "Alola");

    Assert.Equal(VulpixAlola, Assert.Single(vm.Ziele).FormGuid);
  }

  [Fact]
  public void UeberNeu_Unbekannt_NurMeltan()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var vm = new UeberNeuViewModel(bestand, (_, _) => { });

    RegionWaehlen(vm.RegionFilter, "Unbekannt");

    Assert.Equal(Meltan, Assert.Single(vm.Ziele).FormGuid);
  }

  [Fact]
  public void Entwickeln_Alola_NurAlolaVulpix()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var habe = new HabeEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = Assert.Single(bestand.Katalog.AllePokemon(), p => p.DexNr == 37).Guid,
      FormGuid = VulpixNormal
    };
    var vm = new EntwickelnViewModel(bestand, habe, _ => { });

    RegionWaehlen(vm.RegionFilter, "Alola");

    Assert.Equal(VulpixAlola, Assert.Single(vm.Ziele).FormGuid);
  }

  [Fact]
  public void EigeneForm_Kanto_NurPokemonAusKanto()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var vm = new EigeneFormViewModel(new Navigation(), bestand, (_, _) => { });

    RegionWaehlen(vm.RegionFilter, "Kanto");

    Assert.True(vm.Ziele.Count > 0, "Keine Pokemon in der Auswahl");
    Assert.All(vm.Ziele, z => Assert.InRange(z.DexNr, 1, 151));
    Assert.Equal(6, vm.Ziele.Count);
  }

  [Fact]
  public void HabeNeu_FormAnlegen_SetztRegionZurueck_NeueFormAusgewaehlt()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var navigation = new Navigation();
    navigation.Oeffnen(new Startseite());
    var habeNeu = new HabeNeuViewModel(bestand, _ => { }, navigation: navigation);
    navigation.Oeffnen(habeNeu);
    RegionWaehlen(habeNeu.RegionFilter, "Alola");

    habeNeu.FormAnlegenCommand.Execute(null);
    var formSeite = Assert.IsType<EigeneFormViewModel>(navigation.AktuelleSeite);
    formSeite.Suchtext = "201";
    formSeite.Auswahl = formSeite.Ziele.Single();
    formSeite.FormText = "Z";
    formSeite.SpeichernCommand.Execute(null);

    Assert.Same(habeNeu, navigation.AktuelleSeite);
    Assert.Null(habeNeu.RegionFilter.Region);
    Assert.NotNull(habeNeu.Ziel);
    Assert.Equal("#201 Icognito Z", habeNeu.Ziel.Anzeige);
  }
}
