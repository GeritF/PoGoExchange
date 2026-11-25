using System;
using System.IO;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class SuchZeileViewModelTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_zeile_{Guid.NewGuid():N}");

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
    var bestand = Datenbestand.Laden(_ordner, DateTime.UtcNow);
    var pokemon = bestand.Katalog.AllePokemon().Single(p => p.DexNr == 4);
    var form = bestand.Katalog.FormenZuPokemon(pokemon.Guid).First();
    return (bestand, pokemon.Guid, form.Guid);
  }

  private static SucheEintrag Zeile(Guid pokemon, Guid form, int anzahl, bool glueck = false)
  {
    return new SucheEintrag
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
  public void AnforderungenAendern_OhneKonflikt_SpeichertSofort()
  {
    var (bestand, pokemon, form) = Vorbereiten();
    var eintrag = Zeile(pokemon, form, 2);
    bestand.Suche.Add(eintrag);
    var vm = new SuchZeileViewModel(new Navigation(), bestand, eintrag, _ => { });

    vm.Anforderungen.Hundert = true;
    vm.AnforderungenUebernehmenCommand.Execute(null);

    Assert.False(vm.KonfliktSichtbar);
    var neu = Datenbestand.Laden(_ordner, DateTime.UtcNow);
    Assert.True(Assert.Single(neu.Suche).Hundert);
  }

  [Fact]
  public void AnforderungenAendern_MitKonflikt_ErstRueckfrage_DannZusammenfuehren()
  {
    var (bestand, pokemon, form) = Vorbereiten();
    var mitGlueck = Zeile(pokemon, form, 2, glueck: true);
    bestand.Suche.Add(mitGlueck);
    bestand.Suche.Add(Zeile(pokemon, form, 3));
    var vm = new SuchZeileViewModel(new Navigation(), bestand, mitGlueck, _ => { });

    vm.Anforderungen.Glueck = false;
    vm.AnforderungenUebernehmenCommand.Execute(null);

    Assert.True(vm.KonfliktSichtbar);
    Assert.Contains("zusammen: 0/5", vm.KonfliktText);
    Assert.Equal(2, bestand.Suche.Count);

    vm.KonfliktJaCommand.Execute(null);

    var aktiv = Assert.Single(bestand.Suche);
    Assert.Equal(5, aktiv.Anzahl);
    Assert.False(aktiv.Glueck);
  }

  [Fact]
  public void AnzahlNull_ErstRueckfrage_JaLoeschtUndGehtZurueck()
  {
    var (bestand, pokemon, form) = Vorbereiten();
    var eintrag = Zeile(pokemon, form, 2);
    bestand.Suche.Add(eintrag);
    var navigation = new Navigation();
    var start = new Startseite();
    navigation.Oeffnen(start);
    var rueckmeldung = "";
    var vm = new SuchZeileViewModel(navigation, bestand, eintrag, r => rueckmeldung = r);
    navigation.Oeffnen(vm);

    vm.Anzahl = 0;
    vm.AnzahlUebernehmenCommand.Execute(null);

    // Noch nichts geloescht, nur die Rueckfrage
    Assert.True(vm.LoeschRueckfrageSichtbar);
    Assert.Contains(eintrag, bestand.Suche);
    Assert.Same(vm, navigation.AktuelleSeite);

    vm.LoeschenJaCommand.Execute(null);

    Assert.DoesNotContain(eintrag, bestand.Suche);
    Assert.Same(start, navigation.AktuelleSeite);
    Assert.Contains("gel\u00f6scht", rueckmeldung);
  }

  [Fact]
  public void AnzahlNull_NeinBehaeltZeile_UndSetztAnzahlZurueck()
  {
    var (bestand, pokemon, form) = Vorbereiten();
    var eintrag = Zeile(pokemon, form, 2);
    bestand.Suche.Add(eintrag);
    var vm = new SuchZeileViewModel(new Navigation(), bestand, eintrag, _ => { });

    vm.Anzahl = 0;
    vm.AnzahlUebernehmenCommand.Execute(null);
    vm.LoeschenNeinCommand.Execute(null);

    Assert.False(vm.LoeschRueckfrageSichtbar);
    Assert.Contains(eintrag, bestand.Suche);
    Assert.Equal(2, eintrag.Anzahl);
    Assert.Equal(2m, vm.Anzahl);
  }

  [Fact]
  public void AnzahlErhoehen_BehaeltZeile_UndSpeichert()
  {
    var (bestand, pokemon, form) = Vorbereiten();
    var eintrag = Zeile(pokemon, form, 1, glueck: true);
    eintrag.EinsZuEins = true;
    bestand.Suche.Add(eintrag);
    var vm = new SuchZeileViewModel(new Navigation(), bestand, eintrag, _ => { });

    vm.Anzahl = 2;
    vm.AnzahlUebernehmenCommand.Execute(null);

    // Gemessen: nach dem Neuladen steht die Zeile mit Anzahl 2 in der Datei
    var neu = Datenbestand.Laden(_ordner, DateTime.UtcNow);
    Assert.Equal(2, Assert.Single(neu.Suche).Anzahl);
  }
}
