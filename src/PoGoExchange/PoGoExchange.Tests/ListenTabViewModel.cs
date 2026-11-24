using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Services;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class ListenTabViewModelTests
{
  [Fact]
  public void ErledigteEinblenden_ErstNachRueckfrage_AusblendenSofort()
  {
    var aufrufe = new List<bool>();
    var tab = new ListenTabViewModel(true, (_, mitErledigten) =>
    {
      aufrufe.Add(mitErledigten);
      return new List<AnzeigeGruppe>();
    });

    tab.ErledigtUmschaltenCommand.Execute(null);
    Assert.True(tab.RueckfrageSichtbar);
    Assert.False(tab.ErledigteAnzeigen);

    tab.RueckfrageJaCommand.Execute(null);
    Assert.False(tab.RueckfrageSichtbar);
    Assert.True(tab.ErledigteAnzeigen);
    Assert.Equal("Erledigte werden angezeigt", tab.ErledigtStatus);
    Assert.True(aufrufe.Last());

    tab.ErledigtUmschaltenCommand.Execute(null);
    Assert.False(tab.RueckfrageSichtbar);
    Assert.False(tab.ErledigteAnzeigen);
    Assert.False(aufrufe.Last());
  }

  [Fact]
  public void Suchtext_BautListeNeuAuf_MitUeberschriften()
  {
    var tab = new ListenTabViewModel(false, (filter, _) =>
      filter == ""
        ? new List<AnzeigeGruppe>()
        : new List<AnzeigeGruppe>
        {
          new("Kanto", new[] { new AnzeigeZeile("Kanto", 4, "Glumanda", "", false, false) })
        });

    Assert.True(tab.IstLeer);

    tab.Suchtext = "glu";

    Assert.False(tab.IstLeer);
    Assert.IsType<RegionKopf>(tab.Zeilen[0]);
    Assert.IsType<AnzeigeZeile>(tab.Zeilen[1]);
  }
}
