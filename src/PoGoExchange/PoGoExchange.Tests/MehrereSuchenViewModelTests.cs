using PoGoExchange.Services;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class MehrereSuchenViewModelTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_mehrere_{Guid.NewGuid():N}");

  public void Dispose()
  {
    if (Directory.Exists(_ordner))
    {
      Directory.Delete(_ordner, recursive: true);
    }
  }

  // In den mitgelieferten Testdaten hat Kalos 5 Formen mit Shiny
  // (Flabebe Rot, Irrbis XS/M/XL/XXL) und eine ohne (Flabebe Blau)
  private static void KalosShiny(MehrereSuchenViewModel vm)
  {
    vm.RegionIndex = vm.Regionen.ToList().IndexOf("Kalos");
    vm.Anforderungen.Shiny = true;
  }

  [Fact]
  public void Vorschau_ZeigtAnzahlen()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var vm = new MehrereSuchenViewModel(bestand, _ => { });

    KalosShiny(vm);

    Assert.Contains("Neue Zeilen: 5", vm.Vorschau);
    Assert.Contains("nicht verf\u00fcgbar: 1", vm.Vorschau);
  }

  [Fact]
  public void Anlegen_MitSicherung_SpeichertUndSichert()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    SammelErgebnis? ergebnis = null;
    var vm = new MehrereSuchenViewModel(bestand, e => ergebnis = e);
    KalosShiny(vm);

    vm.AnlegenCommand.Execute(null);

    Assert.NotNull(ergebnis);
    Assert.Equal(5, ergebnis.NeueEintraege.Count);
    Assert.Single(Sicherungen.Liste(_ordner));
    var neu = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    Assert.Equal(5, neu.Suche.Count);
  }

  [Fact]
  public void Anlegen_OhneNeueZeilen_MeldetDas()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var vm = new MehrereSuchenViewModel(bestand, _ => { });
    KalosShiny(vm);
    vm.SicherungVorher = false;
    vm.AnlegenCommand.Execute(null);

    var aufgerufen = false;
    var zweite = new MehrereSuchenViewModel(bestand, _ => aufgerufen = true);
    KalosShiny(zweite);
    zweite.AnlegenCommand.Execute(null);

    Assert.False(aufgerufen);
    Assert.Contains("keine neuen Zeilen", zweite.Meldung);
    Assert.Empty(Sicherungen.Liste(_ordner));
  }
}
