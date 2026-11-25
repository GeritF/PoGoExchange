using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Ergebnis einer Entwicklung: ein Exemplar im Zustand vorher und die Zielzeile
public sealed record EntwicklungsErgebnis(HabeEintrag Vorher, HabeEintrag Nachher);

// Entwickeln eines Exemplars in "Habe ich" und Hochziehen betroffener
// Zeilen in "Suche ich". Welche Zeilen hochgezogen werden, waehlt der Anwender.
public static class Entwicklung
{
  // Entwickelt genau ein Exemplar aus der Zeile. Die Zeile sinkt um 1; beim
  // letzten Exemplar wird sie entfernt. Eine Zeile mit denselben Merkmalen fuer
  // die neue Form steigt um 1 oder wird mit Anzahl 1 angelegt.
  public static EntwicklungsErgebnis Entwickeln(
    List<HabeEintrag> habe,
    HabeEintrag zeile,
    PokemonForm neueForm)
  {
    var vorher = zeile.Kopie();
    vorher.Anzahl = 1;

    var ziel = zeile.Kopie();
    ziel.Guid = Guid.NewGuid();
    ziel.PokemonGuid = neueForm.PokemonGuid;
    ziel.FormGuid = neueForm.Guid;
    ziel.Anzahl = 1;

    if (zeile.Anzahl > 1)
    {
      zeile.Anzahl -= 1;
    }
    else
    {
      habe.Remove(zeile);
    }

    var vorhanden = HabeZeilen.Aktive(habe, ziel).FirstOrDefault();
    if (vorhanden != null)
    {
      vorhanden.Anzahl += 1;
      return new EntwicklungsErgebnis(vorher, vorhanden);
    }

    habe.Add(ziel);
    return new EntwicklungsErgebnis(vorher, ziel);
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

  // Die Anzahl der Zeile sinkt um 1 (bei 0 wird sie entfernt). Eine Zeile mit
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
      return vorhanden;
    }

    suche.Add(ziel);
    return ziel;
  }
}
