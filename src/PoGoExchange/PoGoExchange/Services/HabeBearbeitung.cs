using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Ein waehlbares Ziel fuer ein Exemplar: immer eine bestimmte Form
public sealed record FormZiel(Guid PokemonGuid, Guid FormGuid, int DexNr, string Text)
{
  public string Anzeige => $"#{DexNr} {Text}";
}

public sealed class HabeAnlegeErgebnis
{
  // Datensatz der Zeile (neu angelegt oder vorhandene Zeile mit erhoehter Anzahl)
  public required HabeEintrag Eintrag { get; init; }

  // Anzahl der Zeile vorher, oder null, wenn es sie noch nicht gab
  public int? Vorher { get; init; }

  public required int Nachher { get; init; }
}

// Anlegen und Bearbeiten von Zeilen in "Habe ich"
public static class HabeBearbeitung
{
  // Alle waehlbaren Formen (ohne archivierte), passend zum Suchfeld.
  // nurPokemon und nurForm schraenken die Auswahl ein (z.B. beim Erhalten).
  public static List<FormZiel> Ziele(
    Katalog katalog,
    string filter,
    Guid? nurPokemon = null,
    Guid? nurForm = null)
  {
    var ziele = new List<FormZiel>();
    foreach (var pokemon in katalog.AllePokemon())
    {
      if (nurPokemon != null && pokemon.Guid != nurPokemon)
      {
        continue;
      }

      if (!ListenAnzeige.FilterPasst(filter, pokemon))
      {
        continue;
      }

      var formen = katalog.FormenZuPokemon(pokemon.Guid)
        .Where(f => f.ArchiviertAm == null)
        .Where(f => nurForm == null || f.Guid == nurForm);
      foreach (var form in formen)
      {
        var text = EintragText.Erzeugen(pokemon, form, new HabeEintrag());
        ziele.Add(new FormZiel(pokemon.Guid, form.Guid, pokemon.DexNr, text));
      }
    }

    return ziele;
  }

  // Legt eine Zeile mit der Anzahl an. Gibt es dieselbe Zeile schon,
  // wird deren Anzahl erhoeht.
  public static HabeAnlegeErgebnis Anlegen(
    List<HabeEintrag> habe,
    HabeEintrag merkmale,
    FormZiel ziel,
    int anzahl)
  {
    if (anzahl < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(anzahl), "Anzahl muss mindestens 1 sein");
    }

    var neu = NeueZeile(merkmale, ziel.PokemonGuid, ziel.FormGuid, anzahl);
    var vorhanden = HabeZeilen.Aktive(habe, neu).FirstOrDefault();
    if (vorhanden == null)
    {
      habe.Add(neu);
      return new HabeAnlegeErgebnis { Eintrag = neu, Vorher = null, Nachher = anzahl };
    }

    var vorher = vorhanden.Anzahl;
    vorhanden.Anzahl += anzahl;
    return new HabeAnlegeErgebnis
    {
      Eintrag = vorhanden,
      Vorher = vorher,
      Nachher = vorhanden.Anzahl
    };
  }

  // Zeile, mit der die Zeile nach Aenderung der Merkmale zusammenfallen
  // wuerde, oder null, wenn es keine solche gibt
  public static HabeEintrag? Konflikt(
    IEnumerable<HabeEintrag> habe,
    HabeEintrag zeile,
    HabeEintrag neueMerkmale)
  {
    var probe = NeueZeile(neueMerkmale, zeile.PokemonGuid, zeile.FormGuid, 1);
    if (HabeZeilen.Schluessel(probe) == HabeZeilen.Schluessel(zeile))
    {
      return null;
    }

    return HabeZeilen.Aktive(habe, probe).FirstOrDefault();
  }

  // Uebernimmt die Merkmale fuer die ganze Zeile; Pokemon und Form bleiben.
  // Gibt es danach eine gleiche Zeile, wird sie in diese uebernommen.
  public static HabeEintrag MerkmaleAendern(
    List<HabeEintrag> habe,
    HabeEintrag zeile,
    HabeEintrag neueMerkmale)
  {
    MerkmaleUebernehmen(neueMerkmale, zeile);
    var weitere = HabeZeilen.Aktive(habe, zeile)
      .Where(e => !ReferenceEquals(e, zeile))
      .ToList();
    foreach (var weiterer in weitere)
    {
      zeile.Anzahl += weiterer.Anzahl;
      habe.Remove(weiterer);
    }

    return zeile;
  }

  // Setzt die Anzahl der Zeile; 0 entfernt die Zeile
  public static void AnzahlSetzen(List<HabeEintrag> habe, HabeEintrag zeile, int anzahl)
  {
    if (anzahl < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(anzahl), "Anzahl darf nicht negativ sein");
    }

    if (anzahl == 0)
    {
      habe.Remove(zeile);
      return;
    }

    zeile.Anzahl = anzahl;
  }

  // Merkmale fuer ein erhaltenes Exemplar, vorbelegt aus den Anforderungen
  // einer Zeile in "Suche ich" (Pokemon und Form setzt der Aufrufer)
  public static HabeEintrag VorlageAusSuche(SucheEintrag suche)
  {
    return new HabeEintrag
    {
      Geschlecht = suche.Geschlecht,
      Shiny = suche.Shiny,
      Dynamax = suche.Dynamax,
      KostuemAktiv = suche.KostuemAktiv,
      KostuemText = suche.KostuemText,
      HintergrundAktiv = suche.HintergrundAktiv,
      HintergrundText = suche.HintergrundText,
      Glueck = suche.Glueck,
      Hundert = suche.Hundert
    };
  }

  // Neue Zeile mit den Merkmalen der Vorlage fuer Pokemon und Form
  internal static HabeEintrag NeueZeile(
    HabeEintrag merkmale,
    Guid pokemonGuid,
    Guid formGuid,
    int anzahl)
  {
    var zeile = new HabeEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = pokemonGuid,
      FormGuid = formGuid,
      Anzahl = anzahl
    };
    MerkmaleUebernehmen(merkmale, zeile);
    return zeile;
  }

  // Freie Texte werden dabei getrimmt
  private static void MerkmaleUebernehmen(HabeEintrag quelle, HabeEintrag ziel)
  {
    ziel.Geschlecht = quelle.Geschlecht;
    ziel.Shiny = quelle.Shiny;
    ziel.Dynamax = quelle.Dynamax;
    ziel.KostuemAktiv = quelle.KostuemAktiv;
    ziel.KostuemText = TextBereinigung.Bereinigen(quelle.KostuemText);
    ziel.HintergrundAktiv = quelle.HintergrundAktiv;
    ziel.HintergrundText = TextBereinigung.Bereinigen(quelle.HintergrundText);
    ziel.Glueck = quelle.Glueck;
    ziel.Hundert = quelle.Hundert;
  }
}
