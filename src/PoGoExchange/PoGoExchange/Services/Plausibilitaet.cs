using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Eine Zeile mit mehr erledigten Eintraegen als passenden Exemplaren
public sealed record PlausibilitaetsHinweis(SuchZeile Zeile, int PassendeExemplare);

public static class Plausibilitaet
{
  // Meldet Zeilen, in denen mehr Eintraege erledigt sind, als es passende
  // Exemplare in "Habe ich" gibt. Es wird nichts korrigiert.
  public static List<PlausibilitaetsHinweis> Pruefen(
    IEnumerable<SucheEintrag> suche,
    IEnumerable<HabeEintrag> habe)
  {
    var exemplare = habe.ToList();
    var hinweise = new List<PlausibilitaetsHinweis>();
    foreach (var zeile in SuchZeilen.Bilden(suche))
    {
      if (zeile.Erledigt == 0)
      {
        continue;
      }

      var passend = exemplare.Count(h => Anforderung.Erfuellt(h, zeile.Muster));
      if (zeile.Erledigt > passend)
      {
        hinweise.Add(new PlausibilitaetsHinweis(zeile, passend));
      }
    }

    return hinweise;
  }

  // Wie viele neue Sucheintraege mit diesen Anforderungen koennen sofort als
  // erledigt markiert werden? Passende Exemplare abzueglich der Eintraege,
  // die in derselben Zeile bereits erledigt sind.
  public static int SofortErledigbar(
    SucheEintrag muster,
    IEnumerable<SucheEintrag> suche,
    IEnumerable<HabeEintrag> habe)
  {
    var passend = habe.Count(h => Anforderung.Erfuellt(h, muster));
    var schluessel = SuchZeilen.Schluessel(muster);
    var schonErledigt = suche.Count(e =>
      e.GeloeschtAm == null &&
      e.ErledigtAm != null &&
      SuchZeilen.Schluessel(e) == schluessel);

    return Math.Max(0, passend - schonErledigt);
  }
}
