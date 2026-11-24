using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Eine Zeile in "Suche ich": alle Eintraege mit gleicher Form,
// gleichen Merkmalen und gleichen Werten fuer Glueck, Hundert, EinsZuEins
public sealed class SuchZeile
{
  public required SucheEintrag Muster { get; init; }
  public int Erledigt { get; init; }
  public int Ziel { get; init; }
  public int Offen => Ziel - Erledigt;

  // Leer beim Standardfall (0/1)
  public string Zaehler => (Erledigt == 0 && Ziel == 1) ? "" : $"({Erledigt}/{Ziel})";
}

public static class SuchZeilen
{
  public static List<SuchZeile> Bilden(IEnumerable<SucheEintrag> eintraege) =>
    eintraege
      .Where(e => e.GeloeschtAm == null)
      .GroupBy(Schluessel)
      .Select(
        g =>
        new SuchZeile
        {
          Muster = g.First(),
          Erledigt = g.Count(e => e.ErledigtAm != null),
          Ziel = g.Count()
        }
      )
      .ToList();

  // Freie Texte zaehlen nur bei gesetztem Haken, Vergleich in Kleinbuchstaben
  internal static ZeilenSchluessel Schluessel(SucheEintrag e) => new(
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
    e.Hundert,
    e.EinsZuEins
  );
}

internal readonly record struct ZeilenSchluessel(
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
  bool Hundert,
  bool EinsZuEins
);
