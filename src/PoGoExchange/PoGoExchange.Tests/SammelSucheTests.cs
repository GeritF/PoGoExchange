using System;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class SammelSucheTests
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
  private readonly PokemonForm _flabebeArchiviert;
  private readonly Katalog _katalog;

  public SammelSucheTests()
  {
    _glumandaForm = Form(_glumanda);
    _vulpixKanto = Form(_vulpix);
    _vulpixAlola = Form(_vulpix, region: "Alola");
    _flabebeRot = Form(_flabebe, form: "Rot");
    _flabebeBlau = Form(_flabebe, form: "Blau");
    _flabebeArchiviert = Form(_flabebe, form: "Alt");
    _flabebeArchiviert.ArchiviertAm = Jetzt.AddYears(-1);

    _katalog = new Katalog(
      new[] { _flabebe, _vulpix, _glumanda },
      new[]
      {
        _glumandaForm, _vulpixKanto, _vulpixAlola, _flabebeRot, _flabebeBlau, _flabebeArchiviert
      },
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

  private static HabeEintrag Habe(PokemonForm form, bool hundert = false)
  {
    return new HabeEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = form.PokemonGuid,
      FormGuid = form.Guid,
      Hundert = hundert
    };
  }

  private static readonly SucheEintrag Hundert = new() { Hundert = true };

  [Fact]
  public void ProForm_LegtEinenEintragProNichtArchivierterFormAn()
  {
    var ergebnis = SammelSuche.Anlegen(
      _katalog, Hundert, SammelModus.ProForm,
      Array.Empty<SucheEintrag>(), Array.Empty<HabeEintrag>(), Jetzt);

    // Glumanda 1, Vulpix 2, Flabebe 2 (archivierte Form ausgenommen)
    Assert.Equal(5, ergebnis.NeueEintraege.Count);
    Assert.DoesNotContain(ergebnis.NeueEintraege, e => e.FormGuid == _flabebeArchiviert.Guid);
    Assert.All(ergebnis.NeueEintraege, e => Assert.False(e.FormBeliebig));
  }

  [Fact]
  public void ProForm_ReihenfolgeNachDexNummer()
  {
    var ergebnis = SammelSuche.Anlegen(
      _katalog, Hundert, SammelModus.ProForm,
      Array.Empty<SucheEintrag>(), Array.Empty<HabeEintrag>(), Jetzt);

    var pokemonFolge = ergebnis.NeueEintraege.Select(e => e.PokemonGuid).Distinct().ToList();
    Assert.Equal(new[] { _glumanda.Guid, _vulpix.Guid, _flabebe.Guid }, pokemonFolge);
  }

  [Fact]
  public void ProPokemon_LegtEinenEintragMitFormBeliebigAn()
  {
    var ergebnis = SammelSuche.Anlegen(
      _katalog, Hundert, SammelModus.ProPokemon,
      Array.Empty<SucheEintrag>(), Array.Empty<HabeEintrag>(), Jetzt);

    Assert.Equal(3, ergebnis.NeueEintraege.Count);
    Assert.All(ergebnis.NeueEintraege, e => Assert.True(e.FormBeliebig));
  }

  [Fact]
  public void VorhandenesExemplar_WirdSofortErledigt()
  {
    var habe = new[] { Habe(_glumandaForm, hundert: true), Habe(_vulpixKanto) };

    var ergebnis = SammelSuche.Anlegen(
      _katalog, Hundert, SammelModus.ProForm,
      Array.Empty<SucheEintrag>(), habe, Jetzt);

    // Nur Glumanda ist ein 100er; das Vulpix ohne 100er zaehlt nicht
    Assert.Equal(1, ergebnis.SofortErledigt);
  }

  [Fact]
  public void ProPokemon_ExemplarIrgendeinerForm_WirdSofortErledigt()
  {
    var habe = new[] { Habe(_vulpixAlola, hundert: true) };

    var ergebnis = SammelSuche.Anlegen(
      _katalog, Hundert, SammelModus.ProPokemon,
      Array.Empty<SucheEintrag>(), habe, Jetzt);

    Assert.Equal(1, ergebnis.SofortErledigt);
  }

  [Fact]
  public void BereitsVorhandeneZeilen_WerdenUebersprungen()
  {
    var vorhanden = new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = _glumanda.Guid,
      FormGuid = _glumandaForm.Guid,
      Hundert = true
    };

    var ergebnis = SammelSuche.Anlegen(
      _katalog, Hundert, SammelModus.ProForm,
      new[] { vorhanden }, Array.Empty<HabeEintrag>(), Jetzt);

    Assert.Equal(4, ergebnis.NeueEintraege.Count);
    Assert.Equal(1, ergebnis.Uebersprungen);
    Assert.DoesNotContain(ergebnis.NeueEintraege, e => e.FormGuid == _glumandaForm.Guid);
  }

  [Fact]
  public void Vorlage_WirdVollstaendigUebernommen()
  {
    var vorlage = new SucheEintrag
    {
      Geschlecht = Geschlecht.Weiblich,
      Shiny = true,
      Dynamax = DynamaxStufe.Dynamax,
      KostuemAktiv = true,
      KostuemText = "Partyhut",
      HintergrundAktiv = true,
      Glueck = true,
      Hundert = true,
      EinsZuEins = true
    };

    var ergebnis = SammelSuche.Anlegen(
      _katalog, vorlage, SammelModus.ProPokemon,
      Array.Empty<SucheEintrag>(), Array.Empty<HabeEintrag>(), Jetzt);

    Assert.All(ergebnis.NeueEintraege, e =>
    {
      Assert.Equal(Geschlecht.Weiblich, e.Geschlecht);
      Assert.True(e.Shiny);
      Assert.Equal(DynamaxStufe.Dynamax, e.Dynamax);
      Assert.True(e.KostuemAktiv);
      Assert.Equal("Partyhut", e.KostuemText);
      Assert.True(e.HintergrundAktiv);
      Assert.True(e.Glueck);
      Assert.True(e.Hundert);
      Assert.True(e.EinsZuEins);
      Assert.Equal(Jetzt, e.GeaendertAm);
      Assert.Equal(1, e.Anzahl);
    });
    Assert.Equal(
      ergebnis.NeueEintraege.Count,
      ergebnis.NeueEintraege.Select(e => e.Guid).Distinct().Count());
  }
}
