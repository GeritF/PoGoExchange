using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Ein Eintrag, der nicht exportiert werden konnte (z.B. unbekannte Form)
public sealed record ExportFehler(string Liste, Guid EintragGuid, Guid FormGuid, string Meldung);

public sealed class TauschlistenErgebnis
{
  public required string Text { get; init; }
  public required IReadOnlyList<ExportFehler> Fehler { get; init; }
}

public static class Tauschliste
{
  internal const string AbschnittEinsZuEins = "1:1";
  internal const string AbschnittFehlt = "fehlt";
  internal const string AbschnittAbzugeben = "abzugeben";
  internal const string TextGlueck = "Gl\u00fcck";
  internal const string TextHundert = "100er";

  private static readonly string[] Abschnitte =
  {
    AbschnittEinsZuEins,
    AbschnittFehlt,
    AbschnittAbzugeben
  };

  // Gliederung: Region (siehe Katalog), dann Abschnitt, dann Dex-Nummer und Text.
  // Leere Abschnitte und Regionen entfallen. Ungueltige Eintraege (siehe
  // Konsistenz) erscheinen nicht im Text, sondern in der Fehlerliste.
  public static TauschlistenErgebnis Erzeugen(
    Katalog katalog,
    IEnumerable<SucheEintrag> suche,
    IEnumerable<UeberEintrag> ueber)
  {
    var fehler = new List<ExportFehler>();
    var gueltigeSuche = NurGueltige(katalog, suche, ListenNamen.Suche, false, fehler);
    var gueltigeUeber = NurGueltige(katalog, ueber, ListenNamen.Ueber, true, fehler);

    var zeilen = new List<ExportZeile>();
    zeilen.AddRange(SuchZeilenFuerExport(katalog, gueltigeSuche));
    zeilen.AddRange(UeberZeilenFuerExport(katalog, gueltigeUeber));

    return new TauschlistenErgebnis
    {
      Text = Formatieren(katalog, zeilen),
      Fehler = fehler
    };
  }

  // Liefert nur nicht geloeschte, gueltige Eintraege;
  // ungueltige nicht geloeschte Eintraege landen in der Fehlerliste
  private static List<T> NurGueltige<T>(
    Katalog katalog,
    IEnumerable<T> eintraege,
    string liste,
    bool formPflicht,
    List<ExportFehler> fehler)
    where T : ListenEintrag
  {
    var gueltig = new List<T>();
    foreach (var eintrag in eintraege)
    {
      if (eintrag.GeloeschtAm != null)
      {
        continue;
      }

      var grund = Konsistenz.EintragPruefen(katalog, eintrag, formPflicht);
      if (grund != null)
      {
        fehler.Add(new ExportFehler(liste, eintrag.Guid, eintrag.FormGuid, grund));
        continue;
      }

      gueltig.Add(eintrag);
    }

    return gueltig;
  }

  // Nur fuer bereits gepruefte Eintraege; Form ist null bei "Form beliebig"
  internal static (Pokemon Pokemon, PokemonForm? Form) Aufloesen(
    Katalog katalog,
    ListenEintrag eintrag)
  {
    var pokemon = katalog.PokemonZuGuid(eintrag.PokemonGuid)
      ?? throw new InvalidOperationException("Eintrag wurde vor dem Aufloesen nicht geprueft");
    var form = eintrag.FormBeliebig ? null : katalog.FormZuGuid(eintrag.FormGuid);
    return (pokemon, form);
  }

  // Nur Zeilen mit mindestens einem offenen Eintrag
  private static IEnumerable<ExportZeile> SuchZeilenFuerExport(
    Katalog katalog,
    IEnumerable<SucheEintrag> suche)
  {
    foreach (var zeile in SuchZeilen.Bilden(suche))
    {
      if (zeile.Offen == 0)
      {
        continue;
      }

      var (pokemon, form) = Aufloesen(katalog, zeile.Muster);
      yield return new ExportZeile(
        katalog.RegionZuDex(pokemon.DexNr),
        zeile.Muster.EinsZuEins ? AbschnittEinsZuEins : AbschnittFehlt,
        pokemon.DexNr,
        EintragText.Erzeugen(pokemon, form, zeile.Muster),
        Zusatz(zeile));
    }
  }

  // Gleiche Eintraege erscheinen nur einmal
  private static IEnumerable<ExportZeile> UeberZeilenFuerExport(
    Katalog katalog,
    IEnumerable<UeberEintrag> ueber)
  {
    var gesehen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var eintrag in ueber)
    {
      var (pokemon, form) = Aufloesen(katalog, eintrag);
      var text = EintragText.Erzeugen(pokemon, form, eintrag);
      if (!gesehen.Add(pokemon.DexNr + " " + text))
      {
        continue;
      }

      yield return new ExportZeile(
        katalog.RegionZuDex(pokemon.DexNr),
        AbschnittAbzugeben,
        pokemon.DexNr,
        text,
        "");
    }
  }

  private static string Formatieren(Katalog katalog, List<ExportZeile> zeilen)
  {
    var ausgabe = new StringBuilder();
    foreach (var region in katalog.RegionenInReihenfolge())
    {
      foreach (var abschnitt in Abschnitte)
      {
        var liste = zeilen
          .Where(z => z.Region == region && z.Abschnitt == abschnitt)
          .OrderBy(z => z.DexNr)
          .ThenBy(z => z.Text, StringComparer.OrdinalIgnoreCase)
          .ThenBy(z => z.Zusatz, StringComparer.Ordinal)
          .ToList();

        if (liste.Count == 0)
        {
          continue;
        }

        if (ausgabe.Length > 0)
        {
          ausgabe.Append('\n');
        }

        ausgabe.Append(region).Append(' ').Append(abschnitt).Append(":\n");
        foreach (var z in liste)
        {
          ausgabe
            .Append("- #").Append(z.DexNr).Append(' ')
            .Append(z.Text).Append(z.Zusatz).Append('\n');
        }
      }
    }

    return ausgabe.ToString();
  }

  // Ergibt z.B. " [Glueck, 100er] (1/3)"; nicht gesetzte Teile entfallen
  private static string Zusatz(SuchZeile zeile)
  {
    var bedingungen = new List<string>();
    if (zeile.Muster.Glueck)
    {
      bedingungen.Add(TextGlueck);
    }

    if (zeile.Muster.Hundert)
    {
      bedingungen.Add(TextHundert);
    }

    var teile = new List<string>();
    if (bedingungen.Count > 0)
    {
      teile.Add("[" + string.Join(", ", bedingungen) + "]");
    }

    if (zeile.Zaehler != "")
    {
      teile.Add(zeile.Zaehler);
    }

    return teile.Count == 0 ? "" : " " + string.Join(" ", teile);
  }

  private sealed record ExportZeile(
    string Region,
    string Abschnitt,
    int DexNr,
    string Text,
    string Zusatz);
}
