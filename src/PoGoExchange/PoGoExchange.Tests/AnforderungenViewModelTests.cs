using PoGoExchange.Models;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class AnforderungenViewModelTests
{
  [Fact]
  public void Vorlage_UebertraegtAlleWerte()
  {
    var vm = new AnforderungenViewModel
    {
      GeschlechtIndex = 2,
      Shiny = true,
      DynamaxIndex = 2,
      KostuemAktiv = true,
      KostuemText = "Partyhut",
      HintergrundAktiv = true,
      Glueck = true,
      Hundert = true,
      EinsZuEins = true
    };

    var vorlage = vm.Vorlage();

    Assert.Equal(Geschlecht.Weiblich, vorlage.Geschlecht);
    Assert.True(vorlage.Shiny);
    Assert.Equal(DynamaxStufe.Gigadynamax, vorlage.Dynamax);
    Assert.True(vorlage.KostuemAktiv);
    Assert.Equal("Partyhut", vorlage.KostuemText);
    Assert.True(vorlage.HintergrundAktiv);
    Assert.True(vorlage.Glueck);
    Assert.True(vorlage.Hundert);
    Assert.True(vorlage.EinsZuEins);
  }

  [Fact]
  public void Uebernehmen_SetztAuswahllisten()
  {
    var vm = new AnforderungenViewModel();

    vm.Uebernehmen(new SucheEintrag
    {
      Geschlecht = Geschlecht.Maennlich,
      Dynamax = DynamaxStufe.Dynamax
    });

    Assert.Equal(1, vm.GeschlechtIndex);
    Assert.Equal(1, vm.DynamaxIndex);
  }

  [Fact]
  public void Fehler_NurBeiGesetztemHaken()
  {
    var vm = new AnforderungenViewModel { KostuemText = "Party \"Hut\"" };

    Assert.False(vm.HatFehler);

    vm.KostuemAktiv = true;

    Assert.True(vm.HatFehler);
    Assert.Contains("Kost", vm.Fehler);
  }
}
