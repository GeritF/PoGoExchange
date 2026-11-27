using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class AbgleichTests
{
  private readonly Pokemon _glumanda = Pkmn(4, "Glumanda");
  private readonly Pokemon _glurak = Pkmn(6, "Glurak");
  private readonly Pokemon _vulpix = Pkmn(37, "Vulpix");
  private readonly Pokemon _flabebe = Pkmn(669, "Flab\u00e9b\u00e9");

  private readonly PokemonForm _glumandaForm;
  private readonly PokemonForm _glurakForm;
  private readonly PokemonForm _vulpixAlola;
  private readonly PokemonForm _flabebeRot;
  private readonly Katalog _katalog;

  public AbgleichTests()
  {
    _glumandaForm = Form(_glumanda);
    _glurakForm = Form(_glurak);
    _vulpixAlola = Form(_vulpix, region: "Alola");
    _flabebeRot = Form(_flabebe, form: "Rot");

    _katalog = new Katalog(
      [_glumanda, _glurak, _vulpix, _flabebe],
      [_glumandaForm, _glurakForm, _vulpixAlola, _flabebeRot],
      [
        new RegionBereich { Region = "Kanto", DexVon = 1, DexBis = 151 },
        new RegionBereich { Region = "Kalos", DexVon = 650, DexBis = 721 },
        new RegionBereich { Region = "Alola", DexVon = 722, DexBis = 807 }
      ]);
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

  private static SucheEintrag Suche(
    PokemonForm form,
    bool shiny = false,
    bool einsZuEins = false,
    Geschlecht geschlecht = Geschlecht.Unbestimmt)
  {
    return new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Shiny = shiny,
      EinsZuEins = einsZuEins,
      Geschlecht = geschlecht
    };
  }

  private static UeberEintrag Ueber(PokemonForm form, bool shiny = false)
  {
    return new UeberEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Shiny = shiny
    };
  }

  private AbgleichErgebnis Abgleichen(
    string fremderText,
    SucheEintrag[] meineSuche,
    UeberEintrag[] meinUeber,
    HabeEintrag[]? meinHabe = null)
  {
    var fremd = TauschlistenImport.Einlesen(_katalog, fremderText);
    return Abgleich.Durchfuehren(
      fremd, meineSuche, meinHabe ?? [], meinUeber);
  }

  [Fact]
  public void EinsZuEins_PasstExakt_ErscheintNurInListe1()
  {
    var ergebnis = Abgleichen(
      "Kanto 1:1:\n- #4 Glumanda - Shiny\n",
      [Suche(_glumandaForm, shiny: true, einsZuEins: true)],
      []);

    var treffer = Assert.Single(ergebnis.EinsZuEins);
    Assert.Equal(Passung.Passt, treffer.Passung);
    Assert.Empty(ergebnis.ErHat);
    Assert.Empty(ergebnis.ErSucht);
  }

  [Fact]
  public void ErHat_WasIchSuche()
  {
    var ergebnis = Abgleichen(
      "Kanto abzugeben:\n- #37 Vulpix - Shiny - Alola\n",
      [Suche(_vulpixAlola, shiny: true)],
      []);

    var treffer = Assert.Single(ergebnis.ErHat);
    Assert.Equal(Passung.Passt, treffer.Passung);
    Assert.False(treffer.EinsZuEinsGewuenscht);
    Assert.Empty(ergebnis.EinsZuEins);
    Assert.Empty(ergebnis.ErSucht);
  }

  [Fact]
  public void ErSucht_WasIchUebrigHabe()
  {
    var ergebnis = Abgleichen(
      "Kanto fehlt:\n- #6 Glurak\n",
      [],
      [Ueber(_glurakForm, shiny: true)]);

    var treffer = Assert.Single(ergebnis.ErSucht);
    Assert.Equal(Passung.Passt, treffer.Passung);
    Assert.False(treffer.EinsZuEinsGewuenscht);
  }

  [Fact]
  public void FehlendesGeschlecht_ErgibtPruefenMitHinweis()
  {
    var ergebnis = Abgleichen(
      "Kalos abzugeben:\n- #669 Flab\u00e9b\u00e9 Rot\n",
      [Suche(_flabebeRot, geschlecht: Geschlecht.Weiblich)],
      []);

    var treffer = Assert.Single(ergebnis.ErHat);
    Assert.Equal(Passung.Pruefen, treffer.Passung);
    Assert.Contains(treffer.Hinweise, h => h.Contains("Geschlecht"));
  }

  [Fact]
  public void Seine1zu1_ZaehltAlsAngebot_MitKennzeichen()
  {
    var ergebnis = Abgleichen(
      "Kanto 1:1:\n- #4 Glumanda - Shiny\n",
      [Suche(_glumandaForm, shiny: true)],
      []);

    Assert.Empty(ergebnis.EinsZuEins);
    var treffer = Assert.Single(ergebnis.ErHat);
    Assert.True(treffer.EinsZuEinsGewuenscht);
  }

  [Fact]
  public void Meine1zu1_ZaehltAlsAngebot_MitKennzeichen()
  {
    var meine = Suche(_glumandaForm, shiny: true, einsZuEins: true);

    var ergebnis = Abgleichen(
      "Kanto fehlt:\n- #4 Glumanda\n",
      [meine],
      []);

    Assert.Empty(ergebnis.ErHat);
    var treffer = Assert.Single(ergebnis.ErSucht);
    Assert.True(treffer.EinsZuEinsGewuenscht);
    Assert.Same(meine, treffer.Eigen);
  }

  [Fact]
  public void Sortierung_1zu1_DannSicher_DannPruefen()
  {
    var text =
      "Kanto 1:1:\n" +
      "- #37 Vulpix - Shiny - Alola\n" +
      "Kanto abzugeben:\n" +
      "- #6 Glurak\n" +
      "Kalos abzugeben:\n" +
      "- #669 Flab\u00e9b\u00e9 Rot\n";

    var ergebnis = Abgleichen(
      text,
      [
        Suche(_flabebeRot, geschlecht: Geschlecht.Weiblich),
        Suche(_glurakForm),
        Suche(_vulpixAlola, shiny: true)
      ],
      []);

    // Vulpix (1:1 gewuenscht), Glurak (sicher), Flabebe (pruefen)
    Assert.Equal([37, 6, 669], ergebnis.ErHat.Select(t => t.Fremd.DexNr));
  }

  [Fact]
  public void ErledigteSuche_WirdNichtAbgeglichen()
  {
    var habe = new[]
    {
      new HabeEintrag
      {
        Guid = Guid.NewGuid(), PokemonGuid = _glumanda.Guid, FormGuid = _glumandaForm.Guid
      }
    };

    var ergebnis = Abgleichen(
      "Kanto abzugeben:\n- #4 Glumanda\n",
      [Suche(_glumandaForm)],
      [],
      habe);

    Assert.Empty(ergebnis.ErHat);
  }

  [Fact]
  public void GleicheUeberEintraege_ErgebenNurEinenTreffer()
  {
    var ergebnis = Abgleichen(
      "Kanto fehlt:\n- #6 Glurak\n",
      [],
      [Ueber(_glurakForm), Ueber(_glurakForm)]);

    Assert.Single(ergebnis.ErSucht);
  }

  [Fact]
  public void Importfehler_WerdenDurchgereicht()
  {
    var ergebnis = Abgleichen(
      "Kanto fehlt:\n- #999 Irgendwas\n",
      [],
      []);

    var fehler = Assert.Single(ergebnis.Fehler);
    Assert.Contains("999", fehler.Grund);
  }

  [Fact]
  public void MeineSucheMitFormBeliebig_PasstZuJederForm()
  {
    var beliebig = new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = _vulpix.Guid,
      Shiny = true
    };

    var ergebnis = Abgleichen(
      "Kanto abzugeben:\n- #37 Vulpix - Shiny - Alola\n",
      [beliebig],
      []);

    var treffer = Assert.Single(ergebnis.ErHat);
    Assert.Equal(Passung.Passt, treffer.Passung);
  }

  [Fact]
  public void SeinAngebotMitFormBeliebig_ErgibtPruefen()
  {
    var ergebnis = Abgleichen(
      "Kanto abzugeben:\n- #37 Vulpix - Form (beliebig) - Shiny\n",
      [Suche(_vulpixAlola, shiny: true)],
      []);

    var treffer = Assert.Single(ergebnis.ErHat);
    Assert.Equal(Passung.Pruefen, treffer.Passung);
    Assert.Contains(treffer.Hinweise, h => h.Contains("Form"));
  }
}
