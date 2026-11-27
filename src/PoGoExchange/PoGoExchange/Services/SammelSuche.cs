using PoGoExchange.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PoGoExchange.Services;

public enum SammelModus
{
  // Ein Sucheintrag pro nicht archivierter Form
  ProForm,

  // Ein Sucheintrag pro Pokemon mit "Form beliebig"
  ProPokemon
}

public sealed class SammelErgebnis
{
  public required IReadOnlyList<SucheEintrag> NeueEintraege { get; init; }

  // Ziele, fuer die es dieselbe Zeile in "Suche ich" bereits gab
  public required int Uebersprungen { get; init; }

  // Neue Zeilen, fuer die es bereits ein passendes Exemplar in "Habe ich" gibt
  public required int SofortErledigt { get; init; }

  // Ziele, fuer die Shiny gefordert, aber (noch) nicht verfuegbar ist
  public int NichtVerfuegbar { get; init; }

  // Ziele von Pokemon, die (laut Nutzdaten) nicht in Pokemon GO erhaeltlich sind
  public int NichtInGo { get; init; }
}

// Legt viele Sucheintraege mit denselben Anforderungen auf einmal an,
// z.B. "Von jedem ein 100er" (in der Oberflaeche: "+ Mehrere").
// Die uebergebenen Listen werden nicht veraendert; die neuen Eintraege
// werden nur zurueckgegeben.
public static class SammelSuche
{
  // region schraenkt auf eine Region ein (siehe Katalog.RegionZuDex), null = alle.
  // Bewusst nur ueber die Dex-Nummer, anders als ListenAnzeige.RegionPasst:
  // Galar-Zapdos gehoert hier zu Kanto, nicht zu Galar (gewollt, siehe README).
  // Pokemon mit InGoVerfuegbar = false werden immer uebersprungen.
  // Ist Shiny gefordert, werden Ziele ohne verfuegbares Shiny uebersprungen;
  // ein spaeterer erneuter Aufruf ergaenzt neu verfuegbare.
  public static SammelErgebnis Anlegen(
    Katalog katalog,
    SucheEintrag vorlage,
    SammelModus modus,
    IEnumerable<SucheEintrag> suche,
    IEnumerable<HabeEintrag> habe,
    string? region = null)
  {
    var exemplare = habe.ToList();
    var vorhandeneZeilen = suche
      .Select(SuchZeilen.Schluessel)
      .ToHashSet();

    var neu = new List<SucheEintrag>();
    var uebersprungen = 0;
    var sofortErledigt = 0;
    var nichtVerfuegbar = 0;
    var nichtInGo = 0;
    foreach (var (pokemonGuid, formGuid, shinyVerfuegbar, inGo) in Ziele(katalog, modus, region))
    {
      if (!inGo)
      {
        nichtInGo++;
        continue;
      }

      if (vorlage.Shiny && !shinyVerfuegbar)
      {
        nichtVerfuegbar++;
        continue;
      }

      var eintrag = NeuerEintrag(vorlage, pokemonGuid, formGuid);
      if (!vorhandeneZeilen.Add(SuchZeilen.Schluessel(eintrag)))
      {
        uebersprungen++;
        continue;
      }

      if (exemplare.Any(h => Anforderung.Erfuellt(h, eintrag)))
      {
        sofortErledigt++;
      }

      neu.Add(eintrag);
    }

    return new SammelErgebnis
    {
      NeueEintraege = neu,
      Uebersprungen = uebersprungen,
      SofortErledigt = sofortErledigt,
      NichtVerfuegbar = nichtVerfuegbar,
      NichtInGo = nichtInGo
    };
  }

  // Bei "Form beliebig" gilt Shiny als verfuegbar, wenn es fuer mindestens
  // eine nicht archivierte Form verfuegbar ist
  private static IEnumerable<(Guid PokemonGuid, Guid FormGuid, bool ShinyVerfuegbar, bool InGo)>
    Ziele(
      Katalog katalog,
      SammelModus modus,
      string? region)
  {
    foreach (var pokemon in katalog.AllePokemon())
    {
      if (region != null && katalog.RegionZuDex(pokemon.DexNr) != region)
      {
        continue;
      }

      var formen = katalog.FormenZuPokemon(pokemon.Guid)
        .Where(f => f.ArchiviertAm == null)
        .ToList();
      if (modus == SammelModus.ProPokemon)
      {
        yield return (
          pokemon.Guid, Guid.Empty, formen.Any(f => f.ShinyVerfuegbar), pokemon.InGoVerfuegbar);
        continue;
      }

      foreach (var form in formen)
      {
        yield return (pokemon.Guid, form.Guid, form.ShinyVerfuegbar, pokemon.InGoVerfuegbar);
      }
    }
  }

  // Uebernimmt alle Anforderungen der Vorlage; Pokemon und Form werden gesetzt.
  // Freie Texte werden dabei getrimmt.
  private static SucheEintrag NeuerEintrag(SucheEintrag vorlage, Guid pokemonGuid, Guid formGuid)
  {
    return new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = pokemonGuid,
      FormGuid = formGuid,
      Geschlecht = vorlage.Geschlecht,
      Shiny = vorlage.Shiny,
      Dynamax = vorlage.Dynamax,
      KostuemAktiv = vorlage.KostuemAktiv,
      KostuemText = TextBereinigung.Bereinigen(vorlage.KostuemText),
      HintergrundAktiv = vorlage.HintergrundAktiv,
      HintergrundText = TextBereinigung.Bereinigen(vorlage.HintergrundText),
      Glueck = vorlage.Glueck,
      Hundert = vorlage.Hundert,
      EinsZuEins = vorlage.EinsZuEins
    };
  }
}
