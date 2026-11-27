using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.Tests;

// Regionsfilter zusaetzlich zur Textsuche (Listen und Auswahlseiten).
// Galar-Zapdos (#145) gehoert ueber die Dex-Nummer zu Kanto und ueber die
// Spalte Region der Form zu Galar.
public class RegionFilterTests
{
  private readonly Pokemon _glumanda = Pkmn(4, "Glumanda");
  private readonly Pokemon _zapdos = Pkmn(145, "Zapdos");
  private readonly Pokemon _meltan = Pkmn(808, "Meltan");
  private readonly Pokemon _krarmor = Pkmn(823, "Krarmor");

  private readonly PokemonForm _glumandaForm;
  private readonly PokemonForm _zapdosNormal;
  private readonly PokemonForm _zapdosGalar;
  private readonly PokemonForm _meltanForm;
  private readonly PokemonForm _krarmorForm;
  private readonly Katalog _katalog;

  public RegionFilterTests()
  {
    _glumandaForm = Form(_glumanda);
    _zapdosNormal = Form(_zapdos);
    _zapdosGalar = Form(_zapdos, "Galar");
    _meltanForm = Form(_meltan);
    _krarmorForm = Form(_krarmor);

    _katalog = new Katalog(
      [_glumanda, _zapdos, _meltan, _krarmor],
      [_glumandaForm, _zapdosNormal, _zapdosGalar, _meltanForm, _krarmorForm],
      [
        new RegionBereich { Region = "Kanto", DexVon = 1, DexBis = 151 },
        new RegionBereich { Region = "Galar", DexVon = 810, DexBis = 898 },
        new RegionBereich { Region = "Unbekannt" }
      ]);
  }

  private static Pokemon Pkmn(int dex, string name)
  {
    return new Pokemon { Guid = Guid.NewGuid(), DexNr = dex, Name = name };
  }

  private static PokemonForm Form(Pokemon p, string region = "")
  {
    return new PokemonForm
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = p.Guid,
      Region = region,
      ShinyVerfuegbar = true
    };
  }

  private static SucheEintrag Suche(PokemonForm form)
  {
    return new SucheEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = form.PokemonGuid, FormGuid = form.Guid
    };
  }

  private static HabeEintrag Habe(PokemonForm form)
  {
    return new HabeEintrag
    {
      Guid = Guid.NewGuid(), PokemonGuid = form.PokemonGuid, FormGuid = form.Guid
    };
  }

  private SucheEintrag[] AlleSuchen()
  {
    return
    [
      Suche(_glumandaForm), Suche(_zapdosNormal), Suche(_zapdosGalar),
      Suche(_meltanForm), Suche(_krarmorForm)
    ];
  }

  private (Pokemon Pokemon, PokemonForm? Form) Ziel(string name)
  {
    return name switch
    {
      "glumanda" => (_glumanda, _glumandaForm),
      "zapdos" => (_zapdos, _zapdosNormal),
      "zapdosGalar" => (_zapdos, _zapdosGalar),
      "zapdosBeliebig" => (_zapdos, null),
      "meltan" => (_meltan, _meltanForm),
      "krarmor" => (_krarmor, _krarmorForm),
      _ => throw new ArgumentException("Unbekanntes Testziel: " + name)
    };
  }

  [Theory]
  [InlineData("zapdosGalar", "Kanto", true)]
  [InlineData("zapdosGalar", "Galar", true)]
  [InlineData("zapdosGalar", "galar", true)]
  [InlineData("zapdos", "Galar", false)]
  [InlineData("zapdosBeliebig", "Galar", false)]
  [InlineData("zapdosBeliebig", "Kanto", true)]
  [InlineData("krarmor", "Galar", true)]
  [InlineData("krarmor", "Kanto", false)]
  [InlineData("meltan", "Unbekannt", true)]
  [InlineData("glumanda", null, true)]
  [InlineData("glumanda", "", true)]
  public void RegionPasst_DexNummerOderFormRegion(string name, string? region, bool erwartet)
  {
    var (pokemon, form) = Ziel(name);

    var passt = ListenAnzeige.RegionPasst(_katalog, region, pokemon, form);

    Assert.Equal(erwartet, passt);
  }

  [Fact]
  public void Suche_Galar_ZeigtGalarZapdosUnterKantoUndKrarmorUnterGalar()
  {
    var gruppen = ListenAnzeige.Suche(_katalog, AlleSuchen(), [], false, "", "Galar");

    Assert.Equal(["Kanto", "Galar"], gruppen.Select(g => g.Region));
    Assert.Equal(_zapdosGalar.Guid, gruppen[0].Zeilen.Single().Eintrag?.FormGuid);
    Assert.Equal(_krarmorForm.Guid, gruppen[1].Zeilen.Single().Eintrag?.FormGuid);
  }

  [Theory]
  [InlineData("", 2)]
  [InlineData("zap", 1)]
  [InlineData("#145", 1)]
  [InlineData("glum", 0)]
  public void Suche_RegionUndText_GeltenZusammen(string filter, int erwarteteZeilen)
  {
    var gruppen = ListenAnzeige.Suche(_katalog, AlleSuchen(), [], false, filter, "Galar");

    Assert.Equal(erwarteteZeilen, gruppen.Sum(g => g.Zeilen.Count));
  }

  [Fact]
  public void Habe_Galar_ZeigtNurPassendeZeilen()
  {
    var habe = new[] { Habe(_glumandaForm), Habe(_zapdosNormal), Habe(_zapdosGalar) };

    var gruppen = ListenAnzeige.Habe(_katalog, habe, "", "Galar");

    var zeile = Assert.Single(gruppen.SelectMany(g => g.Zeilen));
    Assert.Equal(_zapdosGalar.Guid, zeile.Eintrag?.FormGuid);
  }

  [Fact]
  public void Ueber_Kanto_ZeigtAuchGalarZapdos()
  {
    var ueber = new[]
    {
      new UeberEintrag
      {
        Guid = Guid.NewGuid(), PokemonGuid = _zapdos.Guid, FormGuid = _zapdosGalar.Guid
      },
      new UeberEintrag
      {
        Guid = Guid.NewGuid(), PokemonGuid = _krarmor.Guid, FormGuid = _krarmorForm.Guid
      }
    };

    var gruppen = ListenAnzeige.Ueber(_katalog, ueber, "", "Kanto");

    var zeile = Assert.Single(gruppen.SelectMany(g => g.Zeilen));
    Assert.Equal(_zapdosGalar.Guid, zeile.Eintrag?.FormGuid);
  }

  [Fact]
  public void FehlerhafteEintraege_BleibenAuchMitRegionSichtbar()
  {
    var ohneForm = new HabeEintrag { Guid = Guid.NewGuid(), PokemonGuid = _glumanda.Guid };

    var gruppen = ListenAnzeige.Habe(_katalog, [ohneForm, Habe(_glumandaForm)], "", "Galar");

    var gruppe = Assert.Single(gruppen);
    Assert.Equal(ListenAnzeige.GruppeFehlerhaft, gruppe.Region);
    Assert.True(gruppe.Zeilen.Single().Fehlerhaft);
  }

  [Fact]
  public void HabeZiele_Galar_LiefertGalarZapdosUndKrarmor()
  {
    var ziele = HabeBearbeitung.Ziele(_katalog, "", region: "Galar");

    Assert.True(ziele.Count > 0, "Keine Ziele geliefert");
    Assert.Equal(
      new[] { _zapdosGalar.Guid, _krarmorForm.Guid }.Order(),
      ziele.Select(z => z.FormGuid).Order());
  }

  [Theory]
  [InlineData(null, 6)]
  [InlineData("Kanto", 4)]
  [InlineData("Galar", 2)]
  [InlineData("Unbekannt", 1)]
  public void SuchZiele_AnzahlJeRegion(string? region, int erwarteteZiele)
  {
    var ziele = SuchBearbeitung.Ziele(_katalog, [], new SucheEintrag(), "", region);

    Assert.Equal(erwarteteZiele, ziele.Count);
  }

  [Fact]
  public void SuchZiele_Galar_OhneFormBeliebig()
  {
    var ziele = SuchBearbeitung.Ziele(_katalog, [], new SucheEintrag(), "", "Galar");

    Assert.True(ziele.Count > 0, "Keine Ziele geliefert");
    Assert.DoesNotContain(ziele, z => z.FormBeliebig);
    Assert.Equal(
      new[] { _zapdosGalar.Guid, _krarmorForm.Guid }.Order(),
      ziele.Select(z => z.FormGuid).Order());
  }

  [Fact]
  public void FormVerwaltungPokemon_RegionNurUeberDexNummer()
  {
    var galar = FormVerwaltung.Pokemon(_katalog, "", "Galar");
    var kanto = FormVerwaltung.Pokemon(_katalog, "", "Kanto");

    Assert.Equal([_krarmor.Guid], galar.Select(p => p.PokemonGuid));
    Assert.Equal([_glumanda.Guid, _zapdos.Guid], kanto.Select(p => p.PokemonGuid));
  }
}
