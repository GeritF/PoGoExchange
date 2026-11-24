using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Entwickeln eines Exemplars in "Habe ich" und Hochziehen betroffener
// Zeilen in "Suche ich". Welche Zeilen hochgezogen werden, waehlt der Anwender.
public static class Entwicklung
{
  // Stellt das Exemplar auf die neue Form um (und damit ggf. auf ein neues Pokemon).
  // Liefert den Zustand vor der Entwicklung fuer BetroffeneZeilen.
  public static HabeEintrag Entwickeln(HabeEintrag exemplar, PokemonForm neueForm, DateTime jetzt)
  {
    var vorher = exemplar.Kopie();
    exemplar.PokemonGuid = neueForm.PokemonGuid;
    exemplar.FormGuid = neueForm.Guid;
    exemplar.GeaendertAm = jetzt;
    return vorher;
  }

  // Zeilen, deren Anforderungen das Exemplar vor der Entwicklung erfuellt hat
  // und danach nicht mehr
  public static List<SuchZeile> BetroffeneZeilen(
    HabeEintrag vorher,
    HabeEintrag nachher,
    IEnumerable<SucheEintrag> suche,
    IEnumerable<HabeEintrag> habe)
  {
    return SuchZeilen.Bilden(suche, habe)
      .Where(z => Anforderung.Erfuellt(vorher, z.Muster))
      .Where(z => !Anforderung.Erfuellt(nachher, z.Muster))
      .ToList();
  }

  // Die Anzahl der Zeile sinkt um 1 (bei 0 ist sie geloescht). Eine Zeile mit
  // denselben Anforderungen fuer die neue Form steigt um 1 oder wird angelegt.
  // Bei "Form beliebig" wechselt nur das Pokemon.
  // Liefert den Datensatz der Zielzeile.
  public static SucheEintrag Hochziehen(
    List<SucheEintrag> suche,
    SuchZeile zeile,
    PokemonForm neueForm,
    DateTime jetzt)
  {
    var formGuid = zeile.Muster.FormBeliebig ? Guid.Empty : neueForm.Guid;
    var ziel = SuchBearbeitung.NeuerEintrag(
      zeile.Muster, neueForm.PokemonGuid, formGuid, 1, jetzt);

    SuchBearbeitung.AnzahlSetzen(suche, zeile, zeile.Ziel - 1, jetzt);

    var vorhanden = SuchBearbeitung.Aktive(suche, ziel).FirstOrDefault();
    if (vorhanden != null)
    {
      vorhanden.Anzahl += 1;
      vorhanden.GeaendertAm = jetzt;
      return vorhanden;
    }

    suche.Add(ziel);
    return ziel;
  }
}
