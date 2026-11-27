using PoGoExchange.Models;
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
      return [];
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
    var tab =
      new ListenTabViewModel(
        false,
        (filter, _) =>
          filter == ""
            ? []
            :
            [
              new(
                "Kanto",
                [
                  new AnzeigeZeile(
                    "Kanto",
                    4,
                    "Glumanda",
                    "",
                    false,
                    false
                  )
                ]
              )
            ]
      );

    Assert.True(tab.IstLeer);

    tab.Suchtext = "glu";

    Assert.False(tab.IstLeer);
    Assert.IsType<RegionKopf>(tab.Zeilen[0]);
    Assert.IsType<AnzeigeZeile>(tab.Zeilen[1]);
  }

  [Fact]
  public void AntippenEinerZeile_OeffnetSie_UndHebtAuswahlAuf()
  {
    AnzeigeZeile? geoeffnet = null;
    var zeile = new AnzeigeZeile("Kanto", 4, "Glumanda", "", false, false, new SucheEintrag());
    var tab = new ListenTabViewModel(
      false,
      (_, _) => [new("Kanto", [zeile])],
      oeffnen: z => geoeffnet = z);

    tab.Ausgewaehlt = zeile;

    Assert.Same(zeile, geoeffnet);
    Assert.Null(tab.Ausgewaehlt);
  }
}
