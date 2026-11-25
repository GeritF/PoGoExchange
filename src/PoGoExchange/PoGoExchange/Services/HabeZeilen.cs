using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Zeilen in "Habe ich": Datensaetze mit gleichem Pokemon, gleicher Form und
// gleichen Merkmalen gehoeren zu einer Zeile
public static class HabeZeilen
{
  // Freie Texte zaehlen nur bei gesetztem Haken, Vergleich in Kleinbuchstaben
  internal static HabeZeilenSchluessel Schluessel(HabeEintrag e) => new(
    e.PokemonGuid,
    e.FormGuid,
    e.Geschlecht,
    e.Shiny,
    e.Dynamax,
    e.KostuemAktiv,
    e.KostuemAktiv ? e.KostuemText.ToLowerInvariant() : "",
    e.HintergrundAktiv,
    e.HintergrundAktiv ? e.HintergrundText.ToLowerInvariant() : "",
    e.Glueck,
    e.Hundert
  );

  // Datensaetze derselben Zeile wie das Muster
  internal static List<HabeEintrag> Aktive(IEnumerable<HabeEintrag> habe, HabeEintrag muster)
  {
    var schluessel = Schluessel(muster);
    return habe.Where(e => Schluessel(e) == schluessel).ToList();
  }
}

internal readonly record struct HabeZeilenSchluessel(
  Guid PokemonGuid,
  Guid FormGuid,
  Geschlecht Geschlecht,
  bool Shiny,
  DynamaxStufe Dynamax,
  bool KostuemAktiv,
  string KostuemText,
  bool HintergrundAktiv,
  string HintergrundText,
  bool Glueck,
  bool Hundert
);
