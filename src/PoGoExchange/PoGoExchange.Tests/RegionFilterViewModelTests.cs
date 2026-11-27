using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class RegionFilterViewModelTests
{
  [Fact]
  public void Anfangs_AlleRegionen_RegionNull()
  {
    var filter = new RegionFilterViewModel(["Kanto", "Galar"]);

    Assert.Equal([RegionFilterViewModel.AlleRegionen, "Kanto", "Galar"], filter.Regionen);
    Assert.Equal(0, filter.RegionIndex);
    Assert.Null(filter.Region);
  }

  [Fact]
  public void RegionIndex_LiefertRegion_UndMeldetAenderung()
  {
    var filter = new RegionFilterViewModel(["Kanto", "Galar"]);
    var meldungen = 0;
    filter.Geaendert += () => meldungen++;

    filter.RegionIndex = 2;

    Assert.Equal("Galar", filter.Region);
    Assert.Equal(1, meldungen);
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(3)]
  public void UngueltigerIndex_RegionNull(int index)
  {
    var filter = new RegionFilterViewModel(["Kanto", "Galar"]);

    filter.RegionIndex = index;

    Assert.Null(filter.Region);
  }
}
