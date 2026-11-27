using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class AngebotsVergleichTests
{
  private static readonly Guid FormA = Guid.NewGuid();
  private static readonly Guid FormB = Guid.NewGuid();

  private static Passung Ergebnis(ListenEintrag angebot, ListenEintrag anforderung)
  {
    return AngebotsVergleich.Vergleichen(angebot, anforderung).Passung;
  }

  [Fact]
  public void AndereForm_PasstNicht()
  {
    var angebot = new UeberEintrag { FormGuid = FormA };
    var anforderung = new SucheEintrag { FormGuid = FormB };

    Assert.Equal(Passung.PasstNicht, Ergebnis(angebot, anforderung));
  }

  [Theory]
  [InlineData(Geschlecht.Unbestimmt, Geschlecht.Weiblich, Passung.Passt)]
  [InlineData(Geschlecht.Weiblich, Geschlecht.Weiblich, Passung.Passt)]
  [InlineData(Geschlecht.Weiblich, Geschlecht.Unbestimmt, Passung.Pruefen)]
  [InlineData(Geschlecht.Weiblich, Geschlecht.Maennlich, Passung.PasstNicht)]
  public void GeschlechtsRegel(Geschlecht gefordert, Geschlecht angeboten, Passung erwartet)
  {
    var angebot = new UeberEintrag { FormGuid = FormA, Geschlecht = angeboten };
    var anforderung = new SucheEintrag { FormGuid = FormA, Geschlecht = gefordert };

    Assert.Equal(erwartet, Ergebnis(angebot, anforderung));
  }

  [Theory]
  [InlineData(true, false, Passung.PasstNicht)]
  [InlineData(true, true, Passung.Passt)]
  [InlineData(false, true, Passung.Passt)]
  public void ShinyRegel(bool gefordert, bool angeboten, Passung erwartet)
  {
    var angebot = new UeberEintrag { FormGuid = FormA, Shiny = angeboten };
    var anforderung = new SucheEintrag { FormGuid = FormA, Shiny = gefordert };

    Assert.Equal(erwartet, Ergebnis(angebot, anforderung));
  }

  [Theory]
  [InlineData(DynamaxStufe.Keins, DynamaxStufe.Gigadynamax, Passung.Passt)]
  [InlineData(DynamaxStufe.Dynamax, DynamaxStufe.Gigadynamax, Passung.PasstNicht)]
  [InlineData(DynamaxStufe.Gigadynamax, DynamaxStufe.Gigadynamax, Passung.Passt)]
  public void DynamaxRegel(DynamaxStufe gefordert, DynamaxStufe angeboten, Passung erwartet)
  {
    var angebot = new UeberEintrag { FormGuid = FormA, Dynamax = angeboten };
    var anforderung = new SucheEintrag { FormGuid = FormA, Dynamax = gefordert };

    Assert.Equal(erwartet, Ergebnis(angebot, anforderung));
  }

  [Theory]
  [InlineData(false, "", true, "Partyhut", Passung.Passt)] // nicht gefordert
  [InlineData(true, "", true, "Partyhut", Passung.Passt)] // beliebig gefordert
  [InlineData(true, "partyhut", true, "Partyhut", Passung.Passt)] // gleicher Text
  [InlineData(true, "Partyhut", true, "", Passung.Pruefen)] // Angebot ohne Beschreibung
  [InlineData(true, "Partyhut", true, "Zylinder", Passung.PasstNicht)] // anderer Text
  [InlineData(true, "", false, "", Passung.PasstNicht)] // gefordert, aber keins
  public void KostuemRegel(
    bool gefordert,
    string geforderterText,
    bool angeboten,
    string angebotenerText,
    Passung erwartet)
  {
    var angebot = new UeberEintrag
    {
      FormGuid = FormA, KostuemAktiv = angeboten, KostuemText = angebotenerText
    };
    var anforderung = new SucheEintrag
    {
      FormGuid = FormA, KostuemAktiv = gefordert, KostuemText = geforderterText
    };

    Assert.Equal(erwartet, Ergebnis(angebot, anforderung));
  }

  [Fact]
  public void GlueckUnd100er_WerdenNichtVerglichen()
  {
    var angebot = new UeberEintrag { FormGuid = FormA };
    var anforderung = new SucheEintrag { FormGuid = FormA, Glueck = true, Hundert = true };

    Assert.Equal(Passung.Passt, Ergebnis(angebot, anforderung));
  }

  [Fact]
  public void Pruefen_LiefertAlleHinweise()
  {
    var angebot = new UeberEintrag { FormGuid = FormA, HintergrundAktiv = true };
    var anforderung = new SucheEintrag
    {
      FormGuid = FormA,
      Geschlecht = Geschlecht.Weiblich,
      HintergrundAktiv = true,
      HintergrundText = "New York 2026"
    };

    var ergebnis = AngebotsVergleich.Vergleichen(angebot, anforderung);

    Assert.Equal(Passung.Pruefen, ergebnis.Passung);
    Assert.Equal(2, ergebnis.Hinweise.Count);
    Assert.Contains(ergebnis.Hinweise, h => h.Contains("Geschlecht"));
    Assert.Contains(ergebnis.Hinweise, h => h.Contains("Hintergrund"));
  }

  [Fact]
  public void AnforderungFormBeliebig_PasstZuJederFormDesPokemon()
  {
    var pokemon = Guid.NewGuid();
    var angebot = new UeberEintrag { PokemonGuid = pokemon, FormGuid = FormA };
    var anforderung = new SucheEintrag { PokemonGuid = pokemon, FormGuid = Guid.Empty };

    Assert.Equal(Passung.Passt, Ergebnis(angebot, anforderung));
  }

  [Fact]
  public void AngebotOhneForm_ErgibtPruefen()
  {
    var pokemon = Guid.NewGuid();
    var angebot = new ImportEintrag { PokemonGuid = pokemon, FormGuid = Guid.Empty };
    var anforderung = new SucheEintrag { PokemonGuid = pokemon, FormGuid = FormA };

    var ergebnis = AngebotsVergleich.Vergleichen(angebot, anforderung);

    Assert.Equal(Passung.Pruefen, ergebnis.Passung);
    Assert.Contains(ergebnis.Hinweise, h => h.Contains("Form"));
  }

  [Fact]
  public void AnderesPokemon_PasstNicht_AuchBeiFormBeliebig()
  {
    var angebot = new UeberEintrag { PokemonGuid = Guid.NewGuid(), FormGuid = FormA };
    var anforderung = new SucheEintrag { PokemonGuid = Guid.NewGuid(), FormGuid = Guid.Empty };

    Assert.Equal(Passung.PasstNicht, Ergebnis(angebot, anforderung));
  }
}
