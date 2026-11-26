using CsvHelper.Configuration;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

public sealed class PokemonMap : ClassMap<Pokemon>
{
  public PokemonMap()
  {
    Map(m => m.Guid).Name("Guid");
    Map(m => m.DexNr).Name("DexNr");
    Map(m => m.Name).Name("Name").TypeConverter<TextKonverter>();
    Map(m => m.Kategorie).Name("Kategorie").TypeConverter<TextKonverter>();
    Map(m => m.InGoVerfuegbar).Name("InGoVerfuegbar")
      .TypeConverter<WahrheitswertKonverter>().Optional();
  }
}

public sealed class PokemonFormMap : ClassMap<PokemonForm>
{
  public PokemonFormMap()
  {
    Map(m => m.Guid).Name("Guid");
    Map(m => m.PokemonGuid).Name("PokemonGuid");
    Map(m => m.Form).Name("Form").TypeConverter<TextKonverter>();
    Map(m => m.Region).Name("Region").TypeConverter<TextKonverter>();
    Map(m => m.ShinyVerfuegbar).Name("ShinyVerfuegbar").TypeConverter<WahrheitswertKonverter>();
    Map(m => m.ArchiviertAm).Name("ArchiviertAm").TypeConverter<OptionalerZeitstempelKonverter>();
  }
}

public sealed class RegionBereichMap : ClassMap<RegionBereich>
{
  public RegionBereichMap()
  {
    Map(m => m.Region).Name("Region").TypeConverter<TextKonverter>();
    Map(m => m.DexVon).Name("DexVon").TypeConverter<OptionaleZahlKonverter>();
    Map(m => m.DexBis).Name("DexBis").TypeConverter<OptionaleZahlKonverter>();
  }
}

// Gemeinsame Spalten der drei Listen: Kennungen und Merkmale
public abstract class ListenEintragMap<T> : ClassMap<T> where T : ListenEintrag
{
  protected void MapMerkmale()
  {
    Map(m => m.Guid).Name("Guid").TypeConverter<OptionaleGuidKonverter>();
    Map(m => m.PokemonGuid).Name("PokemonGuid").TypeConverter<OptionaleGuidKonverter>();
    Map(m => m.FormGuid).Name("FormGuid").TypeConverter<OptionaleGuidKonverter>();
    Map(m => m.Geschlecht).Name("Geschlecht").TypeConverter<GeschlechtKonverter>();
    Map(m => m.Shiny).Name("Shiny").TypeConverter<WahrheitswertKonverter>();
    Map(m => m.Dynamax).Name("Dynamax").TypeConverter<DynamaxKonverter>();
    Map(m => m.KostuemAktiv).Name("KostuemAktiv").TypeConverter<WahrheitswertKonverter>();
    Map(m => m.KostuemText).Name("KostuemText").TypeConverter<TextKonverter>();
    Map(m => m.HintergrundAktiv).Name("HintergrundAktiv").TypeConverter<WahrheitswertKonverter>();
    Map(m => m.HintergrundText).Name("HintergrundText").TypeConverter<TextKonverter>();
  }
}

public sealed class HabeEintragMap : ListenEintragMap<HabeEintrag>
{
  public HabeEintragMap()
  {
    MapMerkmale();
    Map(m => m.Glueck).Name("Glueck").TypeConverter<WahrheitswertKonverter>();
    Map(m => m.Hundert).Name("Hundert").TypeConverter<WahrheitswertKonverter>();
    Map(m => m.Anzahl).Name("Anzahl").TypeConverter<AnzahlKonverter>();
  }
}

public sealed class SucheEintragMap : ListenEintragMap<SucheEintrag>
{
  public SucheEintragMap()
  {
    MapMerkmale();
    Map(m => m.Glueck).Name("Glueck").TypeConverter<WahrheitswertKonverter>();
    Map(m => m.Hundert).Name("Hundert").TypeConverter<WahrheitswertKonverter>();
    Map(m => m.EinsZuEins).Name("EinsZuEins").TypeConverter<WahrheitswertKonverter>();
    Map(m => m.Anzahl).Name("Anzahl").TypeConverter<AnzahlKonverter>();
  }
}

public sealed class UeberEintragMap : ListenEintragMap<UeberEintrag>
{
  public UeberEintragMap()
  {
    MapMerkmale();
  }
}

public sealed class SchluesselWertMap : ClassMap<SchluesselWert>
{
  public SchluesselWertMap()
  {
    Map(m => m.Schluessel).Name("Schluessel").TypeConverter<TextKonverter>();
    Map(m => m.Wert).Name("Wert").TypeConverter<TextKonverter>();
  }
}
