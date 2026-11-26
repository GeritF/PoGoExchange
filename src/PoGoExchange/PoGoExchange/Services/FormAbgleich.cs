using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Eine eigene Form und die offizielle Form, die sie ersetzt
public sealed record FormUebernahme(PokemonForm Eigene, PokemonForm Offiziell);

// Abgleich eigener Formen mit den offiziellen Nutzdaten: Gibt es eine eigene
// Form inzwischen offiziell (gleiches Pokemon, gleiche Form und Region, ohne
// Gross-/Kleinschreibung), werden alle Verweise auf die offizielle Form
// umgestellt und die eigene Form entfernt.
public static class FormAbgleich
{
  // Eigene Formen, die es offiziell gibt. Eigene Formen mit derselben GUID wie
  // eine offizielle bleiben aussen vor (die behandelt der Katalog).
  public static List<FormUebernahme> Finden(
    IEnumerable<PokemonForm> eigene,
    IEnumerable<PokemonForm> offizielle)
  {
    var offizielleListe = offizielle.ToList();
    var offizielleGuids = offizielleListe.Select(f => f.Guid).ToHashSet();
    var nachSchluessel = offizielleListe
      .GroupBy(Schluessel)
      .ToDictionary(g => g.Key, g => g.First());

    var uebernahmen = new List<FormUebernahme>();
    foreach (var form in eigene)
    {
      if (offizielleGuids.Contains(form.Guid))
      {
        continue;
      }

      if (nachSchluessel.TryGetValue(Schluessel(form), out var offiziell))
      {
        uebernahmen.Add(new FormUebernahme(form, offiziell));
      }
    }

    return uebernahmen;
  }

  // Stellt die Verweise in allen Listen um, entfernt die eigenen Formen und
  // fuehrt dabei entstandene doppelte Zeilen zusammen ("Habe ich ueber":
  // doppelte Eintraege werden entfernt, es gibt keine Anzahl).
  public static void Uebernehmen(
    IReadOnlyList<FormUebernahme> uebernahmen,
    List<PokemonForm> eigene,
    List<HabeEintrag> habe,
    List<SucheEintrag> suche,
    List<UeberEintrag> ueber)
  {
    var ziel = uebernahmen.ToDictionary(u => u.Eigene.Guid, u => u.Offiziell.Guid);
    foreach (var eintrag in habe.Cast<ListenEintrag>().Concat(suche).Concat(ueber))
    {
      if (ziel.TryGetValue(eintrag.FormGuid, out var neu))
      {
        eintrag.FormGuid = neu;
      }
    }

    foreach (var uebernahme in uebernahmen)
    {
      eigene.Remove(uebernahme.Eigene);
    }

    Zusammenfassen(habe, e => HabeZeilen.Schluessel(e));
    Zusammenfassen(suche, e => SuchZeilen.Schluessel(e));
    foreach (var gruppe in ueber.GroupBy(UeberBearbeitung.Schluessel).ToList())
    {
      foreach (var weiterer in gruppe.Skip(1))
      {
        ueber.Remove(weiterer);
      }
    }
  }

  private static void Zusammenfassen<T>(List<T> liste, Func<T, object> schluessel)
    where T : IMitAnzahl
  {
    foreach (var gruppe in liste.GroupBy(schluessel).Where(g => g.Count() > 1).ToList())
    {
      var behalten = gruppe.First();
      foreach (var weiterer in gruppe.Skip(1))
      {
        behalten.Anzahl += weiterer.Anzahl;
        liste.Remove(weiterer);
      }
    }
  }

  // Pokemon, Form und Region; Raender getrimmt, ohne Gross-/Kleinschreibung,
  // geschuetzte Leerzeichen wie normale behandelt
  private static (Guid, string, string) Schluessel(PokemonForm form)
  {
    return (form.PokemonGuid, Normal(form.Form), Normal(form.Region));
  }

  private static string Normal(string text)
  {
    return TextBereinigung.Bereinigen(text.Replace('\u00a0', ' ')).ToLowerInvariant();
  }
}
