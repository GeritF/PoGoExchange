using System.Collections.Generic;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class NavigationTests
{
  private sealed class TestSeite : SeiteViewModel
  {
    public TestSeite(string titel)
    {
      Titel = titel;
    }

    public override string Titel { get; }
  }

  [Fact]
  public void Oeffnen_ZeigtNeueSeite_UndErlaubtZurueck()
  {
    var navigation = new Navigation();
    navigation.Oeffnen(new TestSeite("Haupt"));

    Assert.False(navigation.KannZurueck);

    navigation.Oeffnen(new TestSeite("Unter"));

    Assert.Equal("Unter", navigation.AktuelleSeite?.Titel);
    Assert.True(navigation.KannZurueck);
  }

  [Fact]
  public void Zurueck_ZeigtVorherigeSeite_HauptseiteBleibt()
  {
    var navigation = new Navigation();
    navigation.Oeffnen(new TestSeite("Haupt"));
    navigation.Oeffnen(new TestSeite("Unter"));

    Assert.True(navigation.Zurueck());
    Assert.Equal("Haupt", navigation.AktuelleSeite?.Titel);

    Assert.False(navigation.Zurueck());
    Assert.Equal("Haupt", navigation.AktuelleSeite?.Titel);
  }

  [Fact]
  public void Aenderungen_WerdenGemeldet()
  {
    var navigation = new Navigation();
    var gemeldet = new List<string?>();
    navigation.PropertyChanged += (_, e) => gemeldet.Add(e.PropertyName);

    navigation.Oeffnen(new TestSeite("Haupt"));

    Assert.Contains(nameof(Navigation.AktuelleSeite), gemeldet);
    Assert.Contains(nameof(Navigation.KannZurueck), gemeldet);
  }
}
