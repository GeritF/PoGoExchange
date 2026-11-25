using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class HabeBearbeitungTests
{
  private readonly Pokemon _glumanda = Pkmn(4, "Glumanda");
  private readonly Pokemon _vulpix = Pkmn(37, "Vulpix");

  private readonly PokemonForm _glumandaForm;
  private readonly PokemonForm _vulpixKanto;
  private readonly PokemonForm _vulpixAlola;
  private readonly PokemonForm _vulpixArchiviert;
  private readonly Katalog _katalog;

  public HabeBearbeitungTests()
  {
    _glumandaForm = Form(_glumanda);
    _vulpixKanto = Form(_vulpix);
    _vulpixAlola = Form(_vulpix, region: "Alola");
    _vulpixArchiviert = Form(_vulpix, form: "Alt");
    _vulpixArchiviert.ArchiviertAm = DateTime.UtcNow;

    _katalog = new Katalog(
      new[] { _glumanda, _vulpix },
      new[] { _glumandaForm, _vulpixKanto, _vulpixAlola, _vulpixArchiviert },
      Array.Empty<RegionBereich>());
  }

  private static Pokemon Pkmn(int dex, string name)
  {
    return new Pokemon { Guid = Guid.NewGuid(), DexNr = dex, Name = name };
  }

  private static PokemonForm Form(Pokemon p, string form = "", string region = "")
  {
    return new PokemonForm
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = p.Guid,
      Form = form,
      Region = region
    };
  }

  private static HabeEintrag Zeile(PokemonForm form, int anzahl = 1, bool glueck = false)
  {
    return new HabeEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Shiny = true,
      Glueck = glueck,
      Anzahl = anzahl
    };
  }

  private static FormZiel ZielFuer(PokemonForm form)
  {
    return new FormZiel(form.PokemonGuid, form.Guid, 0, "");
  }

  private static readonly HabeEintrag NurShiny = new() { Shiny = true };

  [Fact]
  public void Ziele_NurBestimmteFormen_OhneArchivierte()
  {
    var ziele = HabeBearbeitung.Ziele(_katalog, "vulpix");

    Assert.Equal(new[] { "#37 Vulpix", "#37 Vulpix - Alola" }, ziele.Select(z => z.Anzeige));
  }

  [Fact]
  public void Anlegen_NeueZeile()
  {
    var habe = new List<HabeEintrag>();

    var ergebnis = HabeBearbeitung.Anlegen(habe, NurShiny, ZielFuer(_glumandaForm), 3);

    Assert.Null(ergebnis.Vorher);
    Assert.Equal(3, ergebnis.Nachher);
    var zeile = Assert.Single(habe);
    Assert.Same(zeile, ergebnis.Eintrag);
    Assert.True(zeile.Shiny);
    Assert.Equal(_glumandaForm.Guid, zeile.FormGuid);
  }

  [Fact]
  public void Anlegen_InVorhandeneZeile_ErhoehtAnzahl()
  {
    var habe = new List<HabeEintrag> { Zeile(_glumandaForm, anzahl: 2) };

    var ergebnis = HabeBearbeitung.Anlegen(habe, NurShiny, ZielFuer(_glumandaForm), 1);

    Assert.Equal(2, ergebnis.Vorher);
    Assert.Equal(3, ergebnis.Nachher);
    Assert.Equal(3, Assert.Single(habe).Anzahl);
  }

  [Fact]
  public void Konflikt_ErkenntGleicheZeile_UndUnveraenderteMerkmale()
  {
    var mitGlueck = Zeile(_glumandaForm, glueck: true);
    var ohneGlueck = Zeile(_glumandaForm);
    var habe = new List<HabeEintrag> { mitGlueck, ohneGlueck };

    Assert.Same(ohneGlueck, HabeBearbeitung.Konflikt(habe, mitGlueck, NurShiny));
    Assert.Null(HabeBearbeitung.Konflikt(
      habe, mitGlueck, new HabeEintrag { Shiny = true, Glueck = true }));
  }

  [Fact]
  public void MerkmaleAendern_GiltFuerGanzeZeile_PokemonUndFormBleiben()
  {
    var zeile = Zeile(_glumandaForm, anzahl: 3);
    var habe = new List<HabeEintrag> { zeile };
    var neu = new HabeEintrag
    {
      Shiny = true,
      Hundert = true,
      HintergrundAktiv = true,
      HintergrundText = "  Paris  ",
      PokemonGuid = Guid.NewGuid(),
      FormGuid = Guid.NewGuid()
    };

    var ergebnis = HabeBearbeitung.MerkmaleAendern(habe, zeile, neu);

    Assert.Same(zeile, ergebnis);
    Assert.True(zeile.Hundert);
    Assert.Equal("Paris", zeile.HintergrundText);
    Assert.Equal(3, zeile.Anzahl);
    Assert.Equal(_glumandaForm.Guid, zeile.FormGuid);
    Assert.Equal(_glumanda.Guid, zeile.PokemonGuid);
  }

  [Fact]
  public void MerkmaleAendern_MitKonflikt_FuehrtZusammen()
  {
    var mitGlueck = Zeile(_glumandaForm, anzahl: 2, glueck: true);
    var ohneGlueck = Zeile(_glumandaForm, anzahl: 3);
    var habe = new List<HabeEintrag> { mitGlueck, ohneGlueck };

    var ergebnis = HabeBearbeitung.MerkmaleAendern(habe, mitGlueck, NurShiny);

    Assert.Same(mitGlueck, ergebnis);
    Assert.Equal(5, ergebnis.Anzahl);
    Assert.False(ergebnis.Glueck);
    Assert.Single(habe);
  }

  [Fact]
  public void AnzahlSetzen_UeberschreibtAnzahl()
  {
    var zeile = Zeile(_glumandaForm, anzahl: 2);
    var habe = new List<HabeEintrag> { zeile };

    HabeBearbeitung.AnzahlSetzen(habe, zeile, 5);

    Assert.Equal(5, zeile.Anzahl);
  }

  [Fact]
  public void AnzahlSetzen_Null_EntferntZeile()
  {
    var zeile = Zeile(_glumandaForm, anzahl: 2);
    var habe = new List<HabeEintrag> { zeile };

    HabeBearbeitung.AnzahlSetzen(habe, zeile, 0);

    Assert.Empty(habe);
  }

  [Fact]
  public void Ziele_NurPokemon_LiefertNurDessenFormen()
  {
    var ziele = HabeBearbeitung.Ziele(_katalog, "", nurPokemon: _vulpix.Guid);

    Assert.Equal(2, ziele.Count);
    Assert.All(ziele, z => Assert.Equal(_vulpix.Guid, z.PokemonGuid));
  }

  [Fact]
  public void VorlageAusSuche_UebernimmtAnforderungen()
  {
    var suche = new SucheEintrag
    {
      Geschlecht = Geschlecht.Weiblich,
      Shiny = true,
      Dynamax = DynamaxStufe.Dynamax,
      KostuemAktiv = true,
      KostuemText = "Partyhut",
      Glueck = true,
      Hundert = true,
      EinsZuEins = true
    };

    var vorlage = HabeBearbeitung.VorlageAusSuche(suche);

    Assert.Equal(Geschlecht.Weiblich, vorlage.Geschlecht);
    Assert.True(vorlage.Shiny);
    Assert.Equal(DynamaxStufe.Dynamax, vorlage.Dynamax);
    Assert.Equal("Partyhut", vorlage.KostuemText);
    Assert.True(vorlage.Glueck);
    Assert.True(vorlage.Hundert);
  }
}
