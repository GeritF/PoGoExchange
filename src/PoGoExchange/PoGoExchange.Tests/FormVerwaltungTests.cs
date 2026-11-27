using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class FormVerwaltungTests
{
  private readonly Pokemon _icognito =
    new() { Guid = Guid.NewGuid(), DexNr = 201, Name = "Icognito" };

  private readonly Pokemon _vulpix =
    new() { Guid = Guid.NewGuid(), DexNr = 37, Name = "Vulpix" };

  private readonly List<PokemonForm> _offiziell;
  private readonly List<PokemonForm> _eigene = [];

  public FormVerwaltungTests()
  {
    _offiziell =
    [
      new() { Guid = Guid.NewGuid(), PokemonGuid = _icognito.Guid, Form = "A" },
      new() { Guid = Guid.NewGuid(), PokemonGuid = _vulpix.Guid },
      new() { Guid = Guid.NewGuid(), PokemonGuid = _vulpix.Guid, Region = "Alola" }
    ];
  }

  // Katalog wie im Programm: offizielle und eigene Formen zusammen
  private Katalog Katalog()
  {
    List<PokemonForm> formen =
    [
      .. _offiziell,
      .. _eigene
    ];
    return new Katalog(
      [_icognito, _vulpix], formen, []);
  }

  [Theory]
  [InlineData("?", true)]
  [InlineData("Toll, und gro\u00df", true)]
  [InlineData("Toll (und gro\u00df)", false)]
  [InlineData("Kopf [neu]", false)]
  [InlineData("A - B", false)]
  [InlineData("Sag \"Hallo\"", false)]
  public void FormTextFehler_NurProblematischeZeichenVerboten(string text, bool erlaubt)
  {
    Assert.Equal(erlaubt, FormVerwaltung.FormTextFehler(text) == null);
  }

  [Fact]
  public void Anlegen_NeueForm_WirdGetrimmtUebernommen()
  {
    var (form, fehler) = FormVerwaltung.Anlegen(
      _eigene, Katalog(), _icognito.Guid, "  ?  ", "", shinyVerfuegbar: true);

    Assert.Null(fehler);
    Assert.NotNull(form);
    Assert.Equal("?", form.Form);
    Assert.True(form.ShinyVerfuegbar);
    Assert.Same(form, Assert.Single(_eigene));
  }

  [Fact]
  public void Anlegen_VorhandeneForm_WirdAbgelehnt_OhneGrossKlein()
  {
    var (form, fehler) = FormVerwaltung.Anlegen(
      _eigene, Katalog(), _vulpix.Guid, "", "alola", shinyVerfuegbar: false);

    Assert.Null(form);
    Assert.Contains("bereits", fehler);
    Assert.Empty(_eigene);
  }

  [Fact]
  public void Aendern_AufVorhandeneForm_WirdAbgelehnt_EigenerNameBleibtErlaubt()
  {
    var (form, _) = FormVerwaltung.Anlegen(_eigene, Katalog(), _icognito.Guid, "?", "", false);
    var katalog = Katalog();

    var konflikt = FormVerwaltung.Aendern(katalog, form!, "a", "", false);
    var gleich = FormVerwaltung.Aendern(katalog, form!, "?", "", true);

    Assert.Contains("bereits", konflikt);
    Assert.Null(gleich);
    Assert.True(form!.ShinyVerfuegbar);
  }

  [Fact]
  public void Loeschen_VerwendeteForm_WirdAbgelehnt_MitNamenDerListen()
  {
    var (form, _) = FormVerwaltung.Anlegen(_eigene, Katalog(), _icognito.Guid, "?", "", false);
    var habe = new[] { new HabeEintrag { PokemonGuid = _icognito.Guid, FormGuid = form!.Guid } };
    var suche = new[] { new SucheEintrag { PokemonGuid = _icognito.Guid, FormGuid = form.Guid } };

    var fehler = FormVerwaltung.Loeschen(_eigene, form, habe, suche, []);

    Assert.Contains("Habe ich", fehler);
    Assert.Contains("Suche ich", fehler);
    Assert.Single(_eigene);
  }

  [Fact]
  public void Loeschen_UnbenutzteForm_WirdEntfernt()
  {
    var (form, _) = FormVerwaltung.Anlegen(_eigene, Katalog(), _icognito.Guid, "?", "", false);

    var fehler = FormVerwaltung.Loeschen(_eigene, form!, [], [], []);

    Assert.Null(fehler);
    Assert.Empty(_eigene);
  }
}
