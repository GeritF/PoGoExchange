using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;

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

  public int SofortErledigt => NeueEintraege.Count(e => e.ErledigtAm != null);
}

// Legt viele Sucheintraege mit denselben Anforderungen auf einmal an,
// z.B. "Von jedem ein 100er". Die uebergebenen Listen werden nicht
// veraendert; die neuen Eintraege werden nur zurueckgegeben.
public static class SammelSuche
{
  public static SammelErgebnis Anlegen(
    Katalog katalog,
    SucheEintrag vorlage,
    SammelModus modus,
    IEnumerable<SucheEintrag> suche,
    IEnumerable<HabeEintrag> habe,
    DateTime jetzt)
  {
    var bisherige = suche.ToList();
    var exemplare = habe.ToList();
    var vorhandeneZeilen = bisherige
      .Where(e => e.GeloeschtAm == null)
      .Select(SuchZeilen.Schluessel)
      .ToHashSet();

    var neu = new List<SucheEintrag>();
    var uebersprungen = 0;
    foreach (var (pokemonGuid, formGuid) in Ziele(katalog, modus))
    {
      var eintrag = NeuerEintrag(vorlage, pokemonGuid, formGuid, jetzt);
      if (!vorhandeneZeilen.Add(SuchZeilen.Schluessel(eintrag)))
      {
        uebersprungen++;
        continue;
      }

      if (Plausibilitaet.SofortErledigbar(eintrag, bisherige, exemplare) > 0)
      {
        eintrag.ErledigtAm = jetzt;
      }

      neu.Add(eintrag);
    }

    return new SammelErgebnis { NeueEintraege = neu, Uebersprungen = uebersprungen };
  }

  private static IEnumerable<(Guid PokemonGuid, Guid FormGuid)> Ziele(
    Katalog katalog,
    SammelModus modus)
  {
    foreach (var pokemon in katalog.AllePokemon())
    {
      if (modus == SammelModus.ProPokemon)
      {
        yield return (pokemon.Guid, Guid.Empty);
        continue;
      }

      var formen = katalog.FormenZuPokemon(pokemon.Guid).Where(f => f.ArchiviertAm == null);
      foreach (var form in formen)
      {
        yield return (pokemon.Guid, form.Guid);
      }
    }
  }

  // Uebernimmt alle Anforderungen der Vorlage; Pokemon und Form werden gesetzt
  private static SucheEintrag NeuerEintrag(
    SucheEintrag vorlage,
    Guid pokemonGuid,
    Guid formGuid,
    DateTime jetzt)
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
      KostuemText = vorlage.KostuemText,
      HintergrundAktiv = vorlage.HintergrundAktiv,
      HintergrundText = vorlage.HintergrundText,
      Glueck = vorlage.Glueck,
      Hundert = vorlage.Hundert,
      EinsZuEins = vorlage.EinsZuEins,
      GeaendertAm = jetzt
    };
  }
}
