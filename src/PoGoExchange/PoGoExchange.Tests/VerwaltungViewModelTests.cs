using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

[Collection("Zwischenablage")]
public class VerwaltungViewModelTests
{
  [Fact]
  public async Task Kopieren_UebergibtStatusText()
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
      var vm = new VerwaltungViewModel(null, "/test/ordner", "");

      await vm.KopierenCommand.ExecuteAsync(null);

      Assert.Equal(vm.Status, kopiert);
      Assert.Contains("/test/ordner", kopiert);
      Assert.Equal("In die Zwischenablage kopiert", vm.Rueckmeldung);
    }
    finally
    {
      AppActions.InZwischenablageKopieren = vorher;
    }
  }

  [Fact]
  public async Task Kopieren_OhneZwischenablage_MeldetDas()
  {
    var vorher = AppActions.InZwischenablageKopieren;
    AppActions.InZwischenablageKopieren = null;

    try
    {
      var vm = new VerwaltungViewModel(null, "/test/ordner", "");

      await vm.KopierenCommand.ExecuteAsync(null);

      Assert.Contains("nicht verf\u00fcgbar", vm.Rueckmeldung);
    }
    finally
    {
      AppActions.InZwischenablageKopieren = vorher;
    }
  }
}
