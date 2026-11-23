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
        Map(m => m.DexVon).Name("DexVon");
        Map(m => m.DexBis).Name("DexBis");
    }
}

// Gemeinsame Spalten der drei Listen; Anfang und Ende getrennt,
// damit die eigenen Spalten jeder Liste dazwischen stehen
public abstract class ListenEintragMap<T> : ClassMap<T> where T : ListenEintrag
{
    protected void MapAnfang()
    {
        Map(m => m.Guid).Name("Guid");
        Map(m => m.FormGuid).Name("FormGuid");
        Map(m => m.Geschlecht).Name("Geschlecht").TypeConverter<GeschlechtKonverter>();
        Map(m => m.Shiny).Name("Shiny").TypeConverter<WahrheitswertKonverter>();
        Map(m => m.Dynamax).Name("Dynamax").TypeConverter<DynamaxKonverter>();
        Map(m => m.KostuemAktiv).Name("KostuemAktiv").TypeConverter<WahrheitswertKonverter>();
        Map(m => m.KostuemText).Name("KostuemText").TypeConverter<TextKonverter>();
        Map(m => m.HintergrundAktiv).Name("HintergrundAktiv").TypeConverter<WahrheitswertKonverter>();
        Map(m => m.HintergrundText).Name("HintergrundText").TypeConverter<TextKonverter>();
    }

    protected void MapEnde()
    {
        Map(m => m.GeaendertAm).Name("GeaendertAm").TypeConverter<ZeitstempelKonverter>();
        Map(m => m.GeloeschtAm).Name("GeloeschtAm").TypeConverter<OptionalerZeitstempelKonverter>();
    }
}

public sealed class HabeEintragMap : ListenEintragMap<HabeEintrag>
{
    public HabeEintragMap()
    {
        MapAnfang();
        Map(m => m.Glueck).Name("Glueck").TypeConverter<WahrheitswertKonverter>();
        Map(m => m.Hundert).Name("Hundert").TypeConverter<WahrheitswertKonverter>();
        MapEnde();
    }
}

public sealed class SucheEintragMap : ListenEintragMap<SucheEintrag>
{
    public SucheEintragMap()
    {
        MapAnfang();
        Map(m => m.Glueck).Name("Glueck").TypeConverter<WahrheitswertKonverter>();
        Map(m => m.Hundert).Name("Hundert").TypeConverter<WahrheitswertKonverter>();
        Map(m => m.EinsZuEins).Name("EinsZuEins").TypeConverter<WahrheitswertKonverter>();
        Map(m => m.ErledigtAm).Name("ErledigtAm").TypeConverter<OptionalerZeitstempelKonverter>();
        MapEnde();
    }
}

public sealed class UeberEintragMap : ListenEintragMap<UeberEintrag>
{
    public UeberEintragMap()
    {
        MapAnfang();
        MapEnde();
    }
}