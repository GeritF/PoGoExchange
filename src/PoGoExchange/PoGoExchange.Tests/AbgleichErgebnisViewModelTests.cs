using PoGoExchange.Models;
using PoGoExchange.Services;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class AbgleichErgebnisViewModelTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_abgleich_{Guid.NewGuid():N}");

  public void Dispose()
  {
    if (Directory.Exists(_ordner))
    {
      Directory.Delete(_ordner, recursive: true);
    }
  }

  // Bestand mit einer Suche nach Glumanda Shiny (optional mit Geschlecht)
  private Datenbestand MitGlumandaSuche(Geschlecht geschlecht = Geschlecht.Unbestimmt)
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var glumanda = bestand.Katalog.AllePokemon().Single(p => p.DexNr == 4);
    var form = bestand.Katalog.FormenZuPokemon(glumanda.Guid).First();
    bestand.Suche.Add(new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = glumanda.Guid,
      FormGuid = form.Guid,
      Shiny = true,
      Geschlecht = geschlecht,
      Anzahl = 2
    });
    return bestand;
  }

  private static AbgleichErgebnisViewModel Abgleichen(Datenbestand bestand, string text)
  {
    var fremd = TauschlistenImport.Einlesen(bestand.Katalog, text);
    var ergebnis = Abgleich.Durchfuehren(fremd, bestand.Suche, bestand.Habe, bestand.Ueber);
    return new AbgleichErgebnisViewModel(bestand, ergebnis);
  }

  [Fact]
  public void Treffer_ZeigtSeineOriginalzeile_UndMeinenEintragMitZaehler()
  {
    var vm = Abgleichen(MitGlumandaSuche(), "Kanto abzugeben:\n- #4 Glumanda - Shiny\n");

    var abschnitt = Assert.Single(vm.Abschnitte);
    Assert.Equal("Er hat, was ich suche (1)", abschnitt.Titel);
    var treffer = Assert.Single(abschnitt.Treffer);
    Assert.Equal("- #4 Glumanda - Shiny", treffer.Er);
    Assert.Equal("#4 Glumanda - Shiny (0/2)", treffer.Ich);
    Assert.False(treffer.IstPruefen);
    Assert.False(treffer.HatZusatz);
  }

  [Fact]
  public void Pruefen_ZeigtHinweise()
  {
    var vm = Abgleichen(
      MitGlumandaSuche(Geschlecht.Weiblich), "Kanto abzugeben:\n- #4 Glumanda - Shiny\n");

    var treffer = vm.Abschnitte.Single().Treffer.Single();
    Assert.True(treffer.IstPruefen);
    Assert.StartsWith("pr\u00fcfen: ", treffer.Zusatz);
    Assert.Contains("Geschlecht", treffer.Zusatz);
  }

  [Fact]
  public void NichtErkannteZeilen_WerdenMitNummerAufgefuehrt()
  {
    var vm = Abgleichen(MitGlumandaSuche(), "Kanto abzugeben:\n- #9999 Unbekannt\n");

    Assert.True(vm.HatFehler);
    Assert.Equal("Nicht erkannte Zeilen (1)", vm.FehlerTitel);
    Assert.StartsWith("Zeile 2: - #9999 Unbekannt", vm.Fehler.Single());
  }

  [Fact]
  public void OhneTreffer_KeineAbschnitte()
  {
    var vm = Abgleichen(MitGlumandaSuche(), "Kanto abzugeben:\n- #25 Pikachu\n");

    Assert.True(vm.KeineTreffer);
    Assert.Empty(vm.Abschnitte);
    Assert.False(vm.HatFehler);
  }
}
