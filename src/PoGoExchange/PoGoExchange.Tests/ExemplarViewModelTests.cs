using PoGoExchange.Models;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class ExemplarViewModelTests
{
  [Fact]
  public void Vorlage_UndUebernehmen_SindUmkehrbar()
  {
    var original = new HabeEintrag
    {
      Geschlecht = Geschlecht.Weiblich,
      Shiny = true,
      Dynamax = DynamaxStufe.Gigadynamax,
      KostuemAktiv = true,
      KostuemText = "Partyhut",
      Glueck = true,
      Hundert = true
    };
    var vm = new ExemplarViewModel();

    vm.Uebernehmen(original);
    var vorlage = vm.Vorlage();

    Assert.Equal(2, vm.GeschlechtIndex);
    Assert.Equal(2, vm.DynamaxIndex);
    Assert.Equal(Geschlecht.Weiblich, vorlage.Geschlecht);
    Assert.Equal(DynamaxStufe.Gigadynamax, vorlage.Dynamax);
    Assert.Equal("Partyhut", vorlage.KostuemText);
    Assert.True(vorlage.Glueck);
    Assert.True(vorlage.Hundert);
  }

  [Fact]
  public void OhneGlueckUndHundert_VorlageSetztBeideNie()
  {
    var vm = new ExemplarViewModel(mitGlueckUndHundert: false) { Glueck = true, Hundert = true };

    var vorlage = vm.Vorlage();

    Assert.False(vorlage.Glueck);
    Assert.False(vorlage.Hundert);
  }
}
