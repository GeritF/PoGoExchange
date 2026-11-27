using PoGoExchange.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PoGoExchange.Services;

// Eine Zeile in "Suche ich": ein eigenstaendiges Ziel mit Anzahl.
// Der Fortschritt wird aus "Habe ich" berechnet: passende Exemplare,
// begrenzt auf die Anzahl. Ein Exemplar zaehlt in jeder Zeile,
// deren Anforderungen es erfuellt.
public sealed class SuchZeile
{
  public required SucheEintrag Muster { get; init; }
  public required int Erledigt { get; init; }
  public required int Ziel { get; init; }
  public int Offen => Ziel - Erledigt;

  // Leer beim Standardfall (0/1)
  public string Zaehler => (Erledigt == 0 && Ziel == 1) ? "" : $"({Erledigt}/{Ziel})";
}

public static class SuchZeilen
{
  // Datensaetze mit gleichem Schluessel werden zu einer Zeile addiert
  public static List<SuchZeile> Bilden(
    IEnumerable<SucheEintrag> suche,
    IEnumerable<HabeEintrag> habe)
  {
    var exemplare = habe.ToList();
    return
    [
      .. suche
        .Where(e => e.Anzahl > 0)
        .GroupBy(Schluessel)
        .Select(g =>
        {
          var muster = g.First();
          var ziel = g.Sum(e => e.Anzahl);
          var passend = exemplare
            .Where(h => Anforderung.Erfuellt(h, muster))
            .Sum(h => h.Anzahl);
          return new SuchZeile
          {
            Muster = muster,
            Ziel = ziel,
            Erledigt = Math.Min(passend, ziel)
          };
        })
    ];
  }

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
