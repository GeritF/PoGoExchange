using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class SuchBearbeitungTests
{
  private static readonly DateTime Jetzt = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

  private readonly Pokemon _glumanda = Pkmn(4, "Glumanda");
  private readonly Pokemon _vulpix = Pkmn(37, "Vulpix");
  private readonly Pokemon _flabebe = Pkmn(669, "Flab\u00e9b\u00e9");

  private readonly PokemonForm _glumandaForm;
  private readonly PokemonForm _vulpixKanto;
  private readonly PokemonForm _vulpixAlola;
  private readonly PokemonForm _flabebeRot;
  private readonly PokemonForm _flabebeBlau;
  private readonly Katalog _katalog;

  public SuchBearbeitungTests()
  {
    _glumandaForm = Form(_glumanda, shiny: true);
    _vulpixKanto = Form(_vulpix, shiny: true);
    _vulpixAlola = Form(_vulpix, region: "Alola", shiny: true);
    _flabebeRot = Form(_flabebe, form: "Rot", shiny: true);
    _flabebeBlau = Form(_flabebe, form: "Blau", shiny: false);

    _katalog = new Katalog(
      new[] { _glumanda, _vulpix, _flabebe },
      new[] { _glumandaForm, _vulpixKanto, _vulpixAlola, _flabebeRot, _flabebeBlau },
      Array.Empty<RegionBereich>());
  }

  private static Pokemon Pkmn(int dex, string name)
  {
    return new Pokemon { Guid = Guid.NewGuid(), DexNr = dex, Name = name };
  }

  private static PokemonForm Form(
    Pokemon p,
    string form = "",
    string region = "",
    bool shiny = true)
  {
    return new PokemonForm
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = p.Guid,
      Form = form,
      Region = region,
      ShinyVerfuegbar = shiny
    };
  }

  private static HabeEintrag Habe(PokemonForm form)
  {
    return new HabeEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Shiny = true
    };
  }

  private static SucheEintrag Suche(PokemonForm form, int anzahl = 1, bool glueck = false)
  {
    return new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Shiny = true,
      Glueck = glueck,
      Anzahl = anzahl
    };
  }

  private static SuchZiel ZielFuer(PokemonForm form)
  {
    return new SuchZiel(form.PokemonGuid, form.Guid, 0, "", ZielKategorie.Verfuegbar);
  }

  private static readonly SucheEintrag NurShiny = new() { Shiny = true };

  [Theory]
  [InlineData("Partyhut", true)]
  [InlineData("Party \"Hut\"", false)]
  [InlineData("Party\nHut", false)]
  public void FreitextFehler_NurAnfuehrungszeichenUndZeilenumbruchVerboten(
    string text,
    bool erlaubt)
  {
    Assert.Equal(erlaubt, SuchBearbeitung.FreitextFehler(text) == null);
  }

  [Fact]
  public void Ziele_FormBeliebigNurBeiMehrerenFormen()
  {
    var ziele = SuchBearbeitung.Ziele(_katalog, Array.Empty<HabeEintrag>(), NurShiny, "");

    Assert.Single(ziele, z => z.PokemonGuid == _glumanda.Guid);
    Assert.Equal(3, ziele.Count(z => z.PokemonGuid == _vulpix.Guid));
    Assert.Contains(ziele, z => z.PokemonGuid == _vulpix.Guid && z.FormBeliebig);
  }

  [Fact]
  public void Ziele_TexteEntsprechenDemEintragstext()
  {
    var ziele = SuchBearbeitung.Ziele(_katalog, Array.Empty<HabeEintrag>(), NurShiny, "vulpix");

    Assert.Equal(
      new[] { "#37 Vulpix - Form (beliebig)", "#37 Vulpix", "#37 Vulpix - Alola" },
      ziele.Select(z => z.Anzeige));
  }

  [Fact]
  public void Ziele_KategorienNachHabeUndShinyVerfuegbarkeit()
  {
    var ziele = SuchBearbeitung.Ziele(_katalog, new[] { Habe(_glumandaForm) }, NurShiny, "");

    ZielKategorie Kategorie(Guid pokemonGuid, Guid formGuid) =>
      ziele.Single(z => z.PokemonGuid == pokemonGuid && z.FormGuid == formGuid).Kategorie;

    Assert.Equal(ZielKategorie.HastDuSchon, Kategorie(_glumanda.Guid, _glumandaForm.Guid));
    Assert.Equal(ZielKategorie.Verfuegbar, Kategorie(_vulpix.Guid, _vulpixAlola.Guid));
    Assert.Equal(ZielKategorie.NichtVerfuegbar, Kategorie(_flabebe.Guid, _flabebeBlau.Guid));
    // Form beliebig: verfuegbar, weil Rot verfuegbar ist
    Assert.Equal(ZielKategorie.Verfuegbar, Kategorie(_flabebe.Guid, Guid.Empty));
  }

  [Fact]
  public void Ziele_OhneShinyAnforderung_IstNichtsNichtVerfuegbar()
  {
    var ziele = SuchBearbeitung.Ziele(
      _katalog, Array.Empty<HabeEintrag>(), new SucheEintrag(), "");

    Assert.DoesNotContain(ziele, z => z.Kategorie == ZielKategorie.NichtVerfuegbar);
  }

  [Fact]
  public void Anlegen_NeueZeile_StandAusHabeIch()
  {
    var suche = new List<SucheEintrag>();

    var ergebnis = SuchBearbeitung.Anlegen(
      suche, new[] { Habe(_glumandaForm) }, NurShiny, ZielFuer(_glumandaForm), 3, Jetzt);

    Assert.Null(ergebnis.Vorher);
    Assert.Equal("1/3", ergebnis.Nachher.ToString());
    var eintrag = Assert.Single(suche);
    Assert.Equal(3, eintrag.Anzahl);
    Assert.Same(eintrag, ergebnis.Eintrag);
  }

  [Fact]
  public void Anlegen_InVorhandeneZeile_ErhoehtAnzahl()
  {
    var suche = new List<SucheEintrag> { Suche(_glumandaForm, anzahl: 2) };

    var ergebnis = SuchBearbeitung.Anlegen(
      suche, new[] { Habe(_glumandaForm) }, NurShiny, ZielFuer(_glumandaForm), 3, Jetzt);

    Assert.Equal("1/2", ergebnis.Vorher?.ToString());
    Assert.Equal("1/5", ergebnis.Nachher.ToString());
    Assert.Equal(5, Assert.Single(suche).Anzahl);
  }

  [Fact]
  public void Konflikt_ErkenntVorhandeneZeile_UndUnveraenderteAnforderung()
  {
    var suche = new List<SucheEintrag>
    {
      Suche(_glumandaForm, glueck: true),
      Suche(_glumandaForm)
    };
    var zeileMitGlueck = SuchZeilen.Bilden(suche, Array.Empty<HabeEintrag>())
      .Single(z => z.Muster.Glueck);

    var konflikt = SuchBearbeitung.Konflikt(
      suche, Array.Empty<HabeEintrag>(), zeileMitGlueck, NurShiny);
    var unveraendert = SuchBearbeitung.Konflikt(
      suche,
      Array.Empty<HabeEintrag>(),
      zeileMitGlueck,
      new SucheEintrag { Shiny = true, Glueck = true });

    Assert.NotNull(konflikt);
    Assert.False(konflikt.Muster.Glueck);
    Assert.Null(unveraendert);
  }

  [Fact]
  public void AnforderungenAendern_PokemonUndFormBleiben_TexteGetrimmt()
  {
    var suche = new List<SucheEintrag> { Suche(_glumandaForm, anzahl: 2) };
    var zeile = SuchZeilen.Bilden(suche, Array.Empty<HabeEintrag>()).Single();
    var neu = new SucheEintrag
    {
      Shiny = true,
      Hundert = true,
      KostuemAktiv = true,
      KostuemText = "  Partyhut  ",
      PokemonGuid = Guid.NewGuid(),
      FormGuid = Guid.NewGuid()
    };

    var ergebnis = SuchBearbeitung.AnforderungenAendern(suche, zeile, neu, Jetzt);

    Assert.True(ergebnis.Hundert);
    Assert.Equal("Partyhut", ergebnis.KostuemText);
    Assert.Equal(_glumanda.Guid, ergebnis.PokemonGuid);
    Assert.Equal(_glumandaForm.Guid, ergebnis.FormGuid);
    Assert.Equal(2, ergebnis.Anzahl);
  }

  [Fact]
  public void AnforderungenAendern_MitKonflikt_FuehrtZusammen()
  {
    var mitGlueck = Suche(_glumandaForm, anzahl: 2, glueck: true);
    var ohneGlueck = Suche(_glumandaForm, anzahl: 3);
    var suche = new List<SucheEintrag> { mitGlueck, ohneGlueck };
    var zeileMitGlueck = SuchZeilen.Bilden(suche, Array.Empty<HabeEintrag>())
      .Single(z => z.Muster.Glueck);

    var ergebnis = SuchBearbeitung.AnforderungenAendern(suche, zeileMitGlueck, NurShiny, Jetzt);

    Assert.Same(mitGlueck, ergebnis);
    Assert.Equal(5, ergebnis.Anzahl);
    Assert.Single(SuchZeilen.Bilden(suche, Array.Empty<HabeEintrag>()));
  }

  [Fact]
  public void AnzahlSetzen_UeberschreibtAnzahl()
  {
    var suche = new List<SucheEintrag> { Suche(_glumandaForm, anzahl: 2) };
    var zeile = SuchZeilen.Bilden(suche, Array.Empty<HabeEintrag>()).Single();

    SuchBearbeitung.AnzahlSetzen(suche, zeile, 4, Jetzt);

    Assert.Equal(4, suche.Single().Anzahl);
  }

  [Fact]
  public void AnzahlSetzen_Null_LoeschtZeile()
  {
    var suche = new List<SucheEintrag> { Suche(_glumandaForm, anzahl: 2) };
    var zeile = SuchZeilen.Bilden(suche, Array.Empty<HabeEintrag>()).Single();

    SuchBearbeitung.AnzahlSetzen(suche, zeile, 0, Jetzt);

    Assert.Empty(SuchZeilen.Bilden(suche, Array.Empty<HabeEintrag>()));
  }
}
