using PoGoExchange.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PoGoExchange.Services;

// Eine eingelesene Zeile einer fremden Tauschliste.
// Die Merkmale stehen in den geerbten Spalten von ListenEintrag.
public sealed class ImportEintrag : ListenEintrag
{
  public string Abschnitt { get; set; } = "";
  public int DexNr { get; set; }
  public int ZeilenNummer { get; set; }
  public string Zeile { get; set; } = "";
}

// Eine Zeile, die nicht zugeordnet werden konnte
public sealed record ImportFehler(int ZeilenNummer, string Zeile, string Grund);

public sealed class ImportErgebnis
{
  public required IReadOnlyList<ImportEintrag> Eintraege { get; init; }
  public required IReadOnlyList<ImportFehler> Fehler { get; init; }
}

public static class TauschlistenImport
{
  // Eintragszeile: optionales Aufzaehlungszeichen, dann #DexNr und Rest
  private static readonly Regex EintragMuster = new(
    @"^(?:[-*]\s*)?#(\d{1,5})\s+(.+)$",
    RegexOptions.CultureInvariant);

  // Zaehler "(1/3)" am Zeilenende
  private static readonly Regex ZaehlerMuster = new(
    @"\s*\(\d+/\d+\)$",
    RegexOptions.CultureInvariant);

  // Zaehlbedingungen "[Glueck, 100er]" am Zeilenende
  private static readonly Regex BedingungenMuster = new(
    "\\s*\\[[^\\[\\]\"]*\\]$",
    RegexOptions.CultureInvariant);

  private static readonly string[] Abschnitte =
  [
    Tauschliste.AbschnittEinsZuEins,
    Tauschliste.AbschnittFehlt,
    Tauschliste.AbschnittAbzugeben
  ];

  public static ImportErgebnis Einlesen(Katalog katalog, string text)
  {
    var eintraege = new List<ImportEintrag>();
    var fehler = new List<ImportFehler>();
    string? abschnitt = null;

    var zeilen = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    for (var i = 0; i < zeilen.Length; i++)
    {
      var nummer = i + 1;
      var zeile = TextBereinigung.Bereinigen(zeilen[i]);
      if (zeile == "")
      {
        continue;
      }

      var treffer = EintragMuster.Match(zeile);
      if (!treffer.Success)
      {
        if (zeile.EndsWith(':'))
        {
          // Ueberschrift; eine unbekannte beendet den aktuellen Abschnitt
          abschnitt = AbschnittAusUeberschrift(zeile);
        }

        // Sonstiger Text (z.B. Begruessung) wird ignoriert
        continue;
      }

      if (abschnitt == null)
      {
        fehler.Add(new ImportFehler(nummer, zeile, "Eintrag steht in keinem bekannten Abschnitt"));
        continue;
      }

      var eintrag = new ImportEintrag
      {
        Abschnitt = abschnitt,
        DexNr = int.Parse(treffer.Groups[1].Value, CultureInfo.InvariantCulture),
        ZeilenNummer = nummer,
        Zeile = zeile
      };

      var grund = Auswerten(katalog, eintrag, treffer.Groups[2].Value);
      if (grund != null)
      {
        fehler.Add(new ImportFehler(nummer, zeile, grund));
        continue;
      }

      eintraege.Add(eintrag);
    }

    return new ImportErgebnis { Eintraege = eintraege, Fehler = fehler };
  }

  // "Kanto 1:1:" ergibt "1:1"; unbekannter Abschnitt ergibt null
  private static string? AbschnittAusUeberschrift(string zeile)
  {
    var ohneDoppelpunkt = zeile.Substring(0, zeile.Length - 1).TrimEnd();
    var leerzeichen = ohneDoppelpunkt.LastIndexOf(' ');
    var letztesWort = leerzeichen < 0
      ? ohneDoppelpunkt
      : ohneDoppelpunkt.Substring(leerzeichen + 1);

    return Abschnitte.FirstOrDefault(a =>
      string.Equals(a, letztesWort, StringComparison.OrdinalIgnoreCase));
  }

  // Wertet den Text nach "#DexNr" aus.
  // Liefert null bei Erfolg, sonst den Grund fuer die Fehlerliste.
  private static string? Auswerten(Katalog katalog, ImportEintrag eintrag, string rest)
  {
    rest = ZaehlerMuster.Replace(rest, "");
    rest = BedingungenMuster.Replace(rest, "");
    var teile = AnTrennernZerlegen(rest);

    var pokemon = katalog.PokemonZuDex(eintrag.DexNr);
    if (pokemon == null)
    {
      return $"Dex-Nummer {eintrag.DexNr} unbekannt";
    }

    // Erster Teil: Name, optional gefolgt von der Form
    var kopf = teile[0];
    string formText;
    if (string.Equals(kopf, pokemon.Name, StringComparison.OrdinalIgnoreCase))
    {
      formText = "";
    }
    else if (kopf.StartsWith(pokemon.Name + " ", StringComparison.OrdinalIgnoreCase))
    {
      formText = TextBereinigung.Bereinigen(kopf.Substring(pokemon.Name.Length));
    }
    else
    {
      return $"Name passt nicht zu #{eintrag.DexNr} ({pokemon.Name})";
    }

    var region = "";
    var formBeliebig = false;
    foreach (var teil in teile.Skip(1))
    {
      if (!MerkmalUebernehmen(katalog, eintrag, teil, ref region, ref formBeliebig))
      {
        return $"Unbekannter Bestandteil: {teil}";
      }
    }

    eintrag.PokemonGuid = pokemon.Guid;

    if (formBeliebig)
    {
      if (formText != "" || region != "")
      {
        return "Form (beliebig) zusammen mit Form oder Region angegeben";
      }

      eintrag.FormGuid = Guid.Empty;
      return null;
    }

    var form = katalog.FormSuchen(pokemon.Guid, formText, region);
    if (form == null)
    {
      return $"Form nicht gefunden: {pokemon.Name}, Form '{formText}', Region '{region}'";
    }

    eintrag.FormGuid = form.Guid;
    return null;
  }

  // Erkennt einen Bestandteil und setzt das passende Merkmal, die Region
  // oder "Form beliebig"
  private static bool MerkmalUebernehmen(
    Katalog katalog,
    ImportEintrag eintrag,
    string teil,
    ref string region,
    ref bool formBeliebig)
  {
    if (Gleich(teil, EintragText.TextForm + " " + EintragText.TextBeliebig))
    {
      formBeliebig = true;
      return true;
    }

    if (Gleich(teil, EintragText.TextMaennlich))
    {
      eintrag.Geschlecht = Geschlecht.Maennlich;
      return true;
    }

    if (Gleich(teil, EintragText.TextWeiblich))
    {
      eintrag.Geschlecht = Geschlecht.Weiblich;
      return true;
    }

    if (Gleich(teil, EintragText.TextShiny))
    {
      eintrag.Shiny = true;
      return true;
    }

    if (Gleich(teil, EintragText.TextDynamax))
    {
      eintrag.Dynamax = DynamaxStufe.Dynamax;
      return true;
    }

    if (Gleich(teil, EintragText.TextGigadynamax))
    {
      eintrag.Dynamax = DynamaxStufe.Gigadynamax;
      return true;
    }

    if (TextMerkmalLesen(teil, EintragText.TextKostuem, out var kostuem))
    {
      eintrag.KostuemAktiv = true;
      eintrag.KostuemText = kostuem;
      return true;
    }

    if (TextMerkmalLesen(teil, EintragText.TextHintergrund, out var hintergrund))
    {
      eintrag.HintergrundAktiv = true;
      eintrag.HintergrundText = hintergrund;
      return true;
    }

    var regionsname = katalog.RegionsnameFinden(teil);
    if (regionsname != null)
    {
      region = regionsname;
      return true;
    }

    return false;
  }

  // "Kostuem (beliebig)" ergibt "", "Kostuem: \"Partyhut\"" ergibt "Partyhut"
  private static bool TextMerkmalLesen(string teil, string bezeichnung, out string beschreibung)
  {
    beschreibung = "";
    if (Gleich(teil, bezeichnung + " " + EintragText.TextBeliebig))
    {
      return true;
    }

    var praefix = bezeichnung + ": \"";
    if (
      teil.Length > praefix.Length &&
      teil.StartsWith(praefix, StringComparison.OrdinalIgnoreCase) &&
      teil.EndsWith('"'))
    {
      var innen = teil.Substring(praefix.Length, teil.Length - praefix.Length - 1);
      beschreibung = TextBereinigung.Bereinigen(innen);
      return true;
    }

    return false;
  }

  // Zerlegt an " - ", aber nicht innerhalb von Anfuehrungszeichen
  private static List<string> AnTrennernZerlegen(string text)
  {
    var trenner = EintragText.Trenner;
    var teile = new List<string>();
    var aktuell = new StringBuilder();
    var inAnfuehrung = false;

    for (var i = 0; i < text.Length; i++)
    {
      if (!inAnfuehrung && string.CompareOrdinal(text, i, trenner, 0, trenner.Length) == 0)
      {
        teile.Add(TextBereinigung.Bereinigen(aktuell.ToString()));
        aktuell.Clear();
        i += trenner.Length - 1;
        continue;
      }

      if (text[i] == '"')
      {
        inAnfuehrung = !inAnfuehrung;
      }

      aktuell.Append(text[i]);
    }

    teile.Add(TextBereinigung.Bereinigen(aktuell.ToString()));
    return teile;
  }

  private static bool Gleich(string a, string b)
  {
    return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
  }
}
