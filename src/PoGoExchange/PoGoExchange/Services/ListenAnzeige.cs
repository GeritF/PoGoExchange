using PoGoExchange.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PoGoExchange.Services;

// Eine Zeile fuer die Anzeige einer persoenlichen Liste.
// Eintrag ist der Datensatz der Zeile ("Suche ich", "Habe ich"), sonst null.
public sealed record AnzeigeZeile(
  string Region,
  int DexNr,
  string Text,
  string Zusatz,
  bool Erledigt,
  bool Fehlerhaft,
  ListenEintrag? Eintrag = null,
  int Anzahl = 1)
{
  // Anzahl wird ab 2 vorangestellt, z.B. "3x #4 Glumanda"
  public string Anzeige => Fehlerhaft
    ? Text
    : (Anzahl > 1 ? $"{Anzahl}x " : "") + $"#{DexNr} {Text}{Zusatz}";
}

public sealed record AnzeigeGruppe(string Region, IReadOnlyList<AnzeigeZeile> Zeilen);

// Bereitet die persoenlichen Listen fuer die Anzeige auf: gruppiert nach
// Region, sortiert nach Dex-Nummer und Text, optional nach Name oder
// Dex-Nummer und zusaetzlich nach Region gefiltert (siehe RegionPasst).
// Eintraege, die die Konsistenzpruefung nicht bestehen, stehen unabhaengig
// vom Filter in einer eigenen Gruppe am Ende.
public static class ListenAnzeige
{
  public const string GruppeFehlerhaft = "Fehlerhafte Eintr\u00e4ge";
  internal const string TextEinsZuEins = "1:1";

  public static List<AnzeigeGruppe> Suche(
    Katalog katalog,
    IEnumerable<SucheEintrag> suche,
    IEnumerable<HabeEintrag> habe,
    bool mitErledigten,
    string filter,
    string? region = null)
  {
    var zeilen = new List<AnzeigeZeile>();
    var gueltig = Pruefen(katalog, suche, false, zeilen);
    foreach (var zeile in SuchZeilen.Bilden(gueltig, habe))
    {
      var erledigt = zeile.Offen == 0;
      if (erledigt && !mitErledigten)
      {
        continue;
      }

      var (pokemon, form) = Tauschliste.Aufloesen(katalog, zeile.Muster);
      if (!FilterPasst(filter, pokemon) || !RegionPasst(katalog, region, pokemon, form))
      {
        continue;
      }

      zeilen.Add(new AnzeigeZeile(
        katalog.RegionZuDex(pokemon.DexNr),
        pokemon.DexNr,
        EintragText.Erzeugen(pokemon, form, zeile.Muster),
        SuchZusatz(zeile),
        erledigt,
        false,
        zeile.Muster));
    }

    return Gruppieren(katalog, zeilen);
  }

  public static List<AnzeigeGruppe> Habe(
    Katalog katalog,
    IEnumerable<HabeEintrag> habe,
    string filter,
    string? region = null)
  {
    var zeilen = new List<AnzeigeZeile>();
    foreach (var eintrag in Pruefen(katalog, habe, true, zeilen))
    {
      var (pokemon, form) = Tauschliste.Aufloesen(katalog, eintrag);
      if (!FilterPasst(filter, pokemon) || !RegionPasst(katalog, region, pokemon, form))
      {
        continue;
      }

      zeilen.Add(new AnzeigeZeile(
        katalog.RegionZuDex(pokemon.DexNr),
        pokemon.DexNr,
        EintragText.Erzeugen(pokemon, form, eintrag),
        Klammer(Bedingungen(eintrag.Glueck, eintrag.Hundert, false)),
        false,
        false,
        Eintrag: eintrag,
        Anzahl: eintrag.Anzahl));
    }

    return Gruppieren(katalog, zeilen);
  }

  public static List<AnzeigeGruppe> Ueber(
    Katalog katalog,
    IEnumerable<UeberEintrag> ueber,
    string filter,
    string? region = null)
  {
    var zeilen = new List<AnzeigeZeile>();
    foreach (var eintrag in Pruefen(katalog, ueber, true, zeilen))
    {
      var (pokemon, form) = Tauschliste.Aufloesen(katalog, eintrag);
      if (!FilterPasst(filter, pokemon) || !RegionPasst(katalog, region, pokemon, form))
      {
        continue;
      }

      zeilen.Add(new AnzeigeZeile(
        katalog.RegionZuDex(pokemon.DexNr),
        pokemon.DexNr,
        EintragText.Erzeugen(pokemon, form, eintrag),
        "",
        false,
        false,
        Eintrag: eintrag));
    }

    return Gruppieren(katalog, zeilen);
  }

  // Leerer Filter passt immer. Zahl (auch mit #) = Dex-Nummer, sonst Teil des Namens.
  internal static bool FilterPasst(string filter, Pokemon pokemon)
  {
    var text = TextBereinigung.Bereinigen(filter).TrimStart('#');
    if (text == "")
    {
      return true;
    }

    if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var dexNr))
    {
      return pokemon.DexNr == dexNr;
    }

    return pokemon.Name.Contains(text, StringComparison.OrdinalIgnoreCase);
  }

  // Leere Region passt immer. Sonst passt die Region zur Dex-Nummer
  // (Katalog.RegionZuDex) oder zur Spalte Region der Form (ganzer Name, ohne
  // Gross-/Kleinschreibung). Galar-Zapdos passt also zu Kanto und zu Galar.
  // Ohne Form ("Form beliebig", Auswahl eines Pokemon) zaehlt nur die
  // Dex-Nummer.
  public static bool RegionPasst(
    Katalog katalog,
    string? region,
    Pokemon pokemon,
    PokemonForm? form)
  {
    if (string.IsNullOrEmpty(region))
    {
      return true;
    }

    if (katalog.RegionZuDex(pokemon.DexNr) == region)
    {
      return true;
    }

    return form != null && string.Equals(form.Region, region, StringComparison.OrdinalIgnoreCase);
  }

  // Gueltige Eintraege; ungueltige werden als fehlerhafte Zeilen gesammelt
  private static List<T> Pruefen<T>(
    Katalog katalog,
    IEnumerable<T> eintraege,
    bool formPflicht,
    List<AnzeigeZeile> zeilen)
    where T : ListenEintrag
  {
    var gueltig = new List<T>();
    foreach (var eintrag in eintraege)
    {
      var grund = Konsistenz.EintragPruefen(katalog, eintrag, formPflicht);
      if (grund != null)
      {
        zeilen.Add(new AnzeigeZeile(
          GruppeFehlerhaft, 0, $"Eintrag {eintrag.Guid}: {grund}", "", false, true));
        continue;
      }

      gueltig.Add(eintrag);
    }

    return gueltig;
  }

  // Ergibt z.B. " [Glueck, 1:1] (1/3)"
  internal static string SuchZusatz(SuchZeile zeile)
  {
    var klammer = Klammer(Bedingungen(
      zeile.Muster.Glueck, zeile.Muster.Hundert, zeile.Muster.EinsZuEins));
    return zeile.Zaehler == "" ? klammer : klammer + " " + zeile.Zaehler;
  }

  private static List<string> Bedingungen(bool glueck, bool hundert, bool einsZuEins)
  {
    var bedingungen = new List<string>();
    if (glueck)
    {
      bedingungen.Add(Tauschliste.TextGlueck);
    }

    if (hundert)
    {
      bedingungen.Add(Tauschliste.TextHundert);
    }

    if (einsZuEins)
    {
      bedingungen.Add(TextEinsZuEins);
    }

    return bedingungen;
  }

  private static string Klammer(List<string> bedingungen)
  {
    return bedingungen.Count == 0 ? "" : " [" + string.Join(", ", bedingungen) + "]";
  }

  private static List<AnzeigeGruppe> Gruppieren(Katalog katalog, List<AnzeigeZeile> zeilen)
  {
    var gruppen = new List<AnzeigeGruppe>();
    foreach (var region in katalog.RegionenInReihenfolge().Append(GruppeFehlerhaft))
    {
      var inRegion = zeilen
        .Where(z => z.Region == region)
        .OrderBy(z => z.DexNr)
        .ThenBy(z => z.Text, StringComparer.OrdinalIgnoreCase)
        .ThenBy(z => z.Zusatz, StringComparer.Ordinal)
        .ToList();

      if (inRegion.Count > 0)
      {
        gruppen.Add(new AnzeigeGruppe(region, inRegion));
      }
    }

    return gruppen;
  }
}
