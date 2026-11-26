using System;
using System.IO;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;
using PoGoExchange.ViewModels;

namespace PoGoExchange.Tests;

public class EigeneFormViewModelTests : IDisposable
{
  private readonly string _ordner =
    Path.Combine(Path.GetTempPath(), $"pogo_formen_{Guid.NewGuid():N}");

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

  [Fact]
  public void Neu_Speichern_SchreibtDatei_UndErgaenztKatalog()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    PokemonForm? angelegt = null;
    var vm = new EigeneFormViewModel(new Navigation(), bestand, (f, _) => angelegt = f);

    vm.Suchtext = "201";
    vm.Auswahl = vm.Ziele.Single();
    vm.FormText = "Z";
    vm.SpeichernCommand.Execute(null);

    Assert.NotNull(angelegt);
    Assert.Same(angelegt, bestand.Katalog.FormZuGuid(angelegt.Guid));
    var neu = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    Assert.Contains(neu.EigeneFormen, f => f.Guid == angelegt.Guid && f.Form == "Z");
  }

  [Fact]
  public void Bearbeiten_VerwendeteForm_LoeschenNichtMoeglich()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var icognito = bestand.Katalog.AllePokemon().Single(p => p.DexNr == 201);
    var (form, _) = FormVerwaltung.Anlegen(
      bestand.EigeneFormen, bestand.Katalog, icognito.Guid, "Z", "", false);
    bestand.KatalogAufbauen();
    bestand.Habe.Add(new HabeEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = icognito.Guid, FormGuid = form!.Guid
    });
    var vm = new EigeneFormViewModel(new Navigation(), bestand, (_, _) => { }, form);

    vm.LoeschenCommand.Execute(null);

    Assert.False(vm.LoeschRueckfrageSichtbar);
    Assert.Contains("Habe ich", vm.Meldung);
    Assert.Contains(form, bestand.EigeneFormen);
  }

  [Fact]
  public void HabeNeu_FormAnlegen_NeueFormIstDanachAusgewaehlt()
  {
    var bestand = TestNutzdaten.Laden(_ordner, DateTime.UtcNow);
    var navigation = new Navigation();
    navigation.Oeffnen(new Startseite());
    var habeNeu = new HabeNeuViewModel(bestand, _ => { }, navigation: navigation);
    navigation.Oeffnen(habeNeu);

    habeNeu.FormAnlegenCommand.Execute(null);
    var formSeite = Assert.IsType<EigeneFormViewModel>(navigation.AktuelleSeite);
    formSeite.Suchtext = "201";
    formSeite.Auswahl = formSeite.Ziele.Single();
    formSeite.FormText = "Z";
    formSeite.SpeichernCommand.Execute(null);

    Assert.Same(habeNeu, navigation.AktuelleSeite);
    Assert.NotNull(habeNeu.Ziel);
    Assert.Equal("#201 Icognito Z", habeNeu.Ziel.Anzeige);
  }
}
