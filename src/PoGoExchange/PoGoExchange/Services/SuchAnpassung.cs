using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Vorschlaege und Aktionen, wenn ein Exemplar in "Habe ich" entwickelt,
// geaendert oder geloescht wird. Das Programm schlaegt nur vor; welche
// Zeilen angepasst werden, entscheidet der Anwender.
public static class SuchAnpassung
{
  // Entwickelt das Exemplar zur neuen Form (und damit ggf. zum neuen Pokemon).
  // Liefert den Zustand vor der Entwicklung fuer BetroffeneZeilen.
  public static HabeEintrag Entwickeln(HabeEintrag exemplar, PokemonForm neueForm, DateTime jetzt)
  {
    var vorher = exemplar.Kopie();
    exemplar.PokemonGuid = neueForm.PokemonGuid;
    exemplar.FormGuid = neueForm.Guid;
    exemplar.GeaendertAm = jetzt;
    return vorher;
  }

  // Zeilen mit erledigten Eintraegen, deren Anforderungen das Exemplar vorher
  // erfuellt hat, nachher aber nicht mehr. nachher = null: Exemplar geloescht.
  public static List<SuchZeile> BetroffeneZeilen(
    HabeEintrag vorher,
    HabeEintrag? nachher,
    IEnumerable<SucheEintrag> suche)
  {
    return SuchZeilen.Bilden(suche)
      .Where(z => z.Erledigt > 0)
      .Where(z => Anforderung.Erfuellt(vorher, z.Muster))
      .Where(z => nachher == null || !Anforderung.Erfuellt(nachher, z.Muster))
      .ToList();
  }

  // Entwickeln: stellt genau einen erledigten Eintrag der Zeile auf die neue Form um.
  // Eine Zeile mit "Form beliebig" wechselt nur das Pokemon und bleibt beliebig.
  public static SucheEintrag? Hochziehen(
    SuchZeile zeile,
    IEnumerable<SucheEintrag> suche,
    PokemonForm neueForm,
    DateTime jetzt)
  {
    var eintrag = ErledigterEintrag(zeile, suche);
    if (eintrag == null)
    {
      return null;
    }

    eintrag.PokemonGuid = neueForm.PokemonGuid;
    if (!eintrag.FormBeliebig)
    {
      eintrag.FormGuid = neueForm.Guid;
    }

    eintrag.GeaendertAm = jetzt;
    return eintrag;
  }

  // Aendern oder Loeschen: oeffnet genau einen erledigten Eintrag der Zeile wieder
  public static SucheEintrag? WiederOeffnen(
    SuchZeile zeile,
    IEnumerable<SucheEintrag> suche,
    DateTime jetzt)
  {
    var eintrag = ErledigterEintrag(zeile, suche);
    if (eintrag == null)
    {
      return null;
    }

    eintrag.ErledigtAm = null;
    eintrag.GeaendertAm = jetzt;
    return eintrag;
  }

  // Eintraege einer Zeile sind austauschbar; genommen wird der zuletzt erledigte
  private static SucheEintrag? ErledigterEintrag(SuchZeile zeile, IEnumerable<SucheEintrag> suche)
  {
    var schluessel = SuchZeilen.Schluessel(zeile.Muster);
    return suche
      .Where(e => e.GeloeschtAm == null && e.ErledigtAm != null)
      .Where(e => SuchZeilen.Schluessel(e) == schluessel)
      .OrderByDescending(e => e.ErledigtAm)
      .FirstOrDefault();
  }
}
