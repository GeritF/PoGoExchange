using System;

namespace PoGoExchange.Models;

// Gemeinsame Spalten aller drei persoenlichen Listen
public abstract class ListenEintrag
{
    public Guid Guid { get; set; }
    public Guid FormGuid { get; set; }

    // Merkmale: beim Exemplar Beschreibung, bei der Suche Anforderung
    public Geschlecht Geschlecht { get; set; }
    public bool Shiny { get; set; }
    public DynamaxStufe Dynamax { get; set; }
    public bool KostuemAktiv { get; set; }
    public string KostuemText { get; set; } = "";
    public bool HintergrundAktiv { get; set; }
    public string HintergrundText { get; set; } = "";

    public DateTime GeaendertAm { get; set; }
    public DateTime? GeloeschtAm { get; set; }
}

// Eintrag in "Habe ich" (habe_ich.csv)
public sealed class HabeEintrag : ListenEintrag
{
    public bool Glueck { get; set; }
    public bool Hundert { get; set; }
}

// Eintrag in "Suche ich" (suche_ich.csv)
public sealed class SucheEintrag : ListenEintrag
{
    public bool Glueck { get; set; }
    public bool Hundert { get; set; }
    public bool EinsZuEins { get; set; }
    public DateTime? ErledigtAm { get; set; }
}

// Eintrag in "Habe ich ueber" (habe_ich_ueber.csv)
public sealed class UeberEintrag : ListenEintrag
{
}