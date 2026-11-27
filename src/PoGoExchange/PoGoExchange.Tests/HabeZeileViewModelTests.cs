using PoGoExchange.Models;
using PoGoExchange.Services;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class HabeZeileViewModelTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_habe_zeile_{Guid.NewGuid():N}");

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

  private (Datenbestand Bestand, Guid Pokemon, Guid Form) Vorbereiten()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var pokemon = bestand.Katalog.AllePokemon().Single(p => p.DexNr == 4);
    var form = bestand.Katalog.FormenZuPokemon(pokemon.Guid).First();
    return (bestand, pokemon.Guid, form.Guid);
  }

  private static HabeEintrag Zeile(Guid pokemon, Guid form, int anzahl, bool glueck = false)
  {
    return new HabeEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = pokemon,
      FormGuid = form,
      Shiny = true,
      Glueck = glueck,
      Anzahl = anzahl
    };
  }

  [Fact]
  public void MerkmaleAendern_MitKonflikt_ErstRueckfrage_DannZusammenfuehren()
  {
    var (bestand, pokemon, form) = Vorbereiten();
    var mitGlueck = Zeile(pokemon, form, 2, glueck: true);
    bestand.Habe.Add(mitGlueck);
    bestand.Habe.Add(Zeile(pokemon, form, 3));
    var vm = new HabeZeileViewModel(new Navigation(), bestand, mitGlueck, _ => { });

    vm.Merkmale.Glueck = false;
    vm.MerkmaleUebernehmenCommand.Execute(null);

    Assert.True(vm.KonfliktSichtbar);
    Assert.Contains("zusammen: 5x", vm.KonfliktText);
    Assert.Equal(2, bestand.Habe.Count);

    vm.KonfliktJaCommand.Execute(null);

    var zeile = Assert.Single(bestand.Habe);
    Assert.Equal(5, zeile.Anzahl);
    var neu = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    Assert.Equal(5, Assert.Single(neu.Habe).Anzahl);
  }

  [Fact]
  public void AnzahlNull_ErstRueckfrage_JaLoeschtUndGehtZurueck()
  {
    var (bestand, pokemon, form) = Vorbereiten();
    var zeile = Zeile(pokemon, form, 2);
    bestand.Habe.Add(zeile);
    var navigation = new Navigation();
    var start = new Startseite();
    navigation.Oeffnen(start);
    var rueckmeldung = "";
    var vm = new HabeZeileViewModel(navigation, bestand, zeile, r => rueckmeldung = r);
    navigation.Oeffnen(vm);

    vm.Anzahl = 0;
    vm.AnzahlUebernehmenCommand.Execute(null);

    Assert.True(vm.LoeschRueckfrageSichtbar);
    Assert.Contains(zeile, bestand.Habe);

    vm.LoeschenJaCommand.Execute(null);

    Assert.Empty(bestand.Habe);
    Assert.Same(start, navigation.AktuelleSeite);
    Assert.Contains("gel\u00f6scht", rueckmeldung);
  }

  [Fact]
  public void AnzahlNull_NeinBehaeltZeile_UndSetztAnzahlZurueck()
  {
    var (bestand, pokemon, form) = Vorbereiten();
    var zeile = Zeile(pokemon, form, 2);
    bestand.Habe.Add(zeile);
    var vm = new HabeZeileViewModel(new Navigation(), bestand, zeile, _ => { });

    vm.Anzahl = 0;
    vm.AnzahlUebernehmenCommand.Execute(null);
    vm.LoeschenNeinCommand.Execute(null);

    Assert.False(vm.LoeschRueckfrageSichtbar);
    Assert.Contains(zeile, bestand.Habe);
    Assert.Equal(2m, vm.Anzahl);
  }

  [Fact]
  public void Entwickeln_OeffnetSeite_DanachZurueckZurListe()
  {
    var (bestand, pokemon, form) = Vorbereiten();
    var zeile = Zeile(pokemon, form, 1);
    bestand.Habe.Add(zeile);
    var navigation = new Navigation();
    var start = new Startseite();
    navigation.Oeffnen(start);
    var rueckmeldung = "";
    var vm = new HabeZeileViewModel(navigation, bestand, zeile, r => rueckmeldung = r);
    navigation.Oeffnen(vm);

    vm.EntwickelnCommand.Execute(null);
    var seite = Assert.IsType<EntwickelnViewModel>(navigation.AktuelleSeite);
    seite.Suchtext = "6";
    seite.Auswahl = seite.Ziele.Single();
    seite.EntwickelnCommand.Execute(null);

    Assert.Same(start, navigation.AktuelleSeite);
    Assert.Contains("Glurak", rueckmeldung);
  }
}
