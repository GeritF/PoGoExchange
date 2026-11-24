using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Ein Treffer: ein Eintrag der fremden Liste und ein eigener Eintrag
public sealed class AbgleichTreffer
{
  public required ImportEintrag Fremd { get; init; }
  public required ListenEintrag Eigen { get; init; }
  public required bool EinsZuEinsGewuenscht { get; init; }
  public required Passung Passung { get; init; }
  public required IReadOnlyList<string> Hinweise { get; init; }
}

public sealed class AbgleichErgebnis
{
  // Liste 1: 1:1 passt exakt
  public required IReadOnlyList<AbgleichTreffer> EinsZuEins { get; init; }

  // Liste 2: Er hat, was ich suche
  public required IReadOnlyList<AbgleichTreffer> ErHat { get; init; }

  // Liste 3: Er sucht, was ich uebrig habe
  public required IReadOnlyList<AbgleichTreffer> ErSucht { get; init; }

  // Nicht zuordenbare Zeilen der fremden Liste
  public required IReadOnlyList<ImportFehler> Fehler { get; init; }
}

public static class Abgleich
{
  public static AbgleichErgebnis Durchfuehren(
    ImportErgebnis fremd,
    IEnumerable<SucheEintrag> meineSuche,
    IEnumerable<UeberEintrag> meinUeber)
  {
    // Eigene Seite: offene Suchzeilen (je Zeile ein Vertreter)
    var meineOffeneSuche = SuchZeilen.Bilden(meineSuche)
      .Where(z => z.Offen > 0)
      .Select(z => z.Muster)
      .ToList();
    var meineEinsZuEins = meineOffeneSuche.Where(s => s.EinsZuEins).ToList();

    // Eigene Angebote: "Habe ich ueber" (gleiche nur einmal) und eigene 1:1-Eintraege
    var meineAngebote = UeberOhneDoppelte(meinUeber)
      .Cast<ListenEintrag>()
      .Concat(meineEinsZuEins)
      .ToList();

    // Fremde Seite: 1:1-Eintraege sind zugleich Angebot und Anforderung
    var seineEinsZuEins = fremd.Eintraege.Where(IstEinsZuEins).ToList();
    var seineAngebote = fremd.Eintraege
      .Where(e => e.Abschnitt == Tauschliste.AbschnittAbzugeben || IstEinsZuEins(e))
      .ToList();
    var seineSuche = fremd.Eintraege
      .Where(e => e.Abschnitt == Tauschliste.AbschnittFehlt || IstEinsZuEins(e))
      .ToList();

    // Liste 1: beide Richtungen muessen passen
    var liste1 = new List<AbgleichTreffer>();
    foreach (var seiner in seineEinsZuEins)
    {
      foreach (var meiner in meineEinsZuEins)
      {
        var hin = AngebotsVergleich.Vergleichen(seiner, meiner);
        var rueck = AngebotsVergleich.Vergleichen(meiner, seiner);
        if (hin.Passung == Passung.PasstNicht || rueck.Passung == Passung.PasstNicht)
        {
          continue;
        }

        liste1.Add(Treffer(seiner, meiner, true, hin, rueck));
      }
    }

    // Eintraege mit 1:1-Treffer erscheinen in keiner anderen Liste
    var vergeben = new HashSet<object>(ReferenceEqualityComparer.Instance);
    foreach (var treffer in liste1)
    {
      vergeben.Add(treffer.Fremd);
      vergeben.Add(treffer.Eigen);
    }

    // Liste 2: sein Angebot gegen meine Anforderung
    var liste2 = new List<AbgleichTreffer>();
    foreach (var seiner in seineAngebote)
    {
      foreach (var meiner in meineOffeneSuche)
      {
        if (vergeben.Contains(seiner) || vergeben.Contains(meiner))
        {
          continue;
        }

        var vergleich = AngebotsVergleich.Vergleichen(seiner, meiner);
        if (vergleich.Passung == Passung.PasstNicht)
        {
          continue;
        }

        var einsZuEins = IstEinsZuEins(seiner) || meiner.EinsZuEins;
        liste2.Add(Treffer(seiner, meiner, einsZuEins, vergleich));
      }
    }

    // Liste 3: mein Angebot gegen seine Anforderung
    var liste3 = new List<AbgleichTreffer>();
    foreach (var seiner in seineSuche)
    {
      foreach (var meiner in meineAngebote)
      {
        if (vergeben.Contains(seiner) || vergeben.Contains(meiner))
        {
          continue;
        }

        var vergleich = AngebotsVergleich.Vergleichen(meiner, seiner);
        if (vergleich.Passung == Passung.PasstNicht)
        {
          continue;
        }

        var einsZuEins = IstEinsZuEins(seiner) || meiner is SucheEintrag { EinsZuEins: true };
        liste3.Add(Treffer(seiner, meiner, einsZuEins, vergleich));
      }
    }

    return new AbgleichErgebnis
    {
      EinsZuEins = Sortieren(liste1),
      ErHat = Sortieren(liste2),
      ErSucht = Sortieren(liste3),
      Fehler = fremd.Fehler
    };
  }

  private static bool IstEinsZuEins(ImportEintrag eintrag)
  {
    return eintrag.Abschnitt == Tauschliste.AbschnittEinsZuEins;
  }

  // Gleiche Eintraege aus "Habe ich ueber" nur einmal beruecksichtigen
  private static IEnumerable<UeberEintrag> UeberOhneDoppelte(IEnumerable<UeberEintrag> ueber)
  {
    return ueber
      .Where(e => e.GeloeschtAm == null)
      .GroupBy(e => (
        e.FormGuid,
        e.Geschlecht,
        e.Shiny,
        e.Dynamax,
        e.KostuemAktiv,
        Kostuem: e.KostuemAktiv ? e.KostuemText.ToLowerInvariant() : "",
        e.HintergrundAktiv,
        Hintergrund: e.HintergrundAktiv ? e.HintergrundText.ToLowerInvariant() : ""))
      .Select(g => g.First());
  }

  private static AbgleichTreffer Treffer(
    ImportEintrag fremd,
    ListenEintrag eigen,
    bool einsZuEinsGewuenscht,
    params Vergleichsergebnis[] vergleiche)
  {
    var hinweise = vergleiche.SelectMany(v => v.Hinweise).Distinct().ToList();
    return new AbgleichTreffer
    {
      Fremd = fremd,
      Eigen = eigen,
      EinsZuEinsGewuenscht = einsZuEinsGewuenscht,
      Passung = hinweise.Count == 0 ? Passung.Passt : Passung.Pruefen,
      Hinweise = hinweise
    };
  }

  // 1:1 gewuenscht zuerst, dann sicher vor pruefen, dann Dex-Nummer
  private static List<AbgleichTreffer> Sortieren(IEnumerable<AbgleichTreffer> treffer)
  {
    return treffer
      .OrderByDescending(t => t.EinsZuEinsGewuenscht)
      .ThenBy(t => t.Passung)
      .ThenBy(t => t.Fremd.DexNr)
      .ThenBy(t => t.Fremd.ZeilenNummer)
      .ToList();
  }
}
