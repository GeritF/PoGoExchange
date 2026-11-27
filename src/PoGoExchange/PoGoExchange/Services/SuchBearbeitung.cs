using PoGoExchange.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PoGoExchange.Services;

public enum ZielKategorie
{
  Verfuegbar,
  HastDuSchon,
  NichtVerfuegbar
}

// Ein waehlbares Ziel: eine bestimmte Form oder ein Pokemon mit "Form beliebig"
public sealed record SuchZiel(
  Guid PokemonGuid,
  Guid FormGuid,
  int DexNr,
  string Text,
  ZielKategorie Kategorie)
{
  public bool FormBeliebig => FormGuid == Guid.Empty;
  public string Anzeige => $"#{DexNr} {Text}";
}

// Stand einer Zeile in "Suche ich", z.B. 1/3
public sealed record ZeilenStand(int Erledigt, int Ziel)
{
  public override string ToString()
  {
    return $"{Erledigt}/{Ziel}";
  }
}

public sealed class AnlegeErgebnis
{
  // Datensatz der Zeile (neu angelegt oder vorhandene Zeile mit erhoehter Anzahl)
  public required SucheEintrag Eintrag { get; init; }

  // Stand der Zeile vorher, oder null, wenn es sie noch nicht gab
  public ZeilenStand? Vorher { get; init; }

  public required ZeilenStand Nachher { get; init; }
}

// Anlegen und Bearbeiten von Zeilen in "Suche ich".
public static class SuchBearbeitung
{
  // Alle waehlbaren Ziele mit Kategorie, passend zu den Anforderungen.
  // "Form beliebig" wird nur bei Pokemon mit mehreren Formen angeboten.
  public static List<SuchZiel> Ziele(
    Katalog katalog,
    IEnumerable<HabeEintrag> habe,
    SucheEintrag anforderung,
    string filter)
  {
    var exemplare = habe.ToList();
    var ziele = new List<SuchZiel>();
    foreach (var pokemon in katalog.AllePokemon())
    {
      if (!ListenAnzeige.FilterPasst(filter, pokemon))
      {
        continue;
      }

      var formen = katalog.FormenZuPokemon(pokemon.Guid)
        .Where(f => f.ArchiviertAm == null)
        .ToList();
      if (formen.Count == 0)
      {
        continue;
      }

      if (formen.Count > 1)
      {
        var irgendeineVerfuegbar = formen.Any(f => f.ShinyVerfuegbar);
        ziele.Add(Ziel(pokemon, null, irgendeineVerfuegbar, anforderung, exemplare));
      }

      foreach (var form in formen)
      {
        ziele.Add(Ziel(pokemon, form, form.ShinyVerfuegbar, anforderung, exemplare));
      }
    }

    return ziele;
  }

  // Legt eine Zeile mit der Anzahl an. Gibt es dieselbe Zeile schon,
  // wird deren Anzahl erhoeht.
  public static AnlegeErgebnis Anlegen(
    List<SucheEintrag> suche,
    IEnumerable<HabeEintrag> habe,
    SucheEintrag anforderung,
    SuchZiel ziel,
    int anzahl)
  {
    if (anzahl < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(anzahl), "Anzahl muss mindestens 1 sein");
    }

    var neu = NeuerEintrag(anforderung, ziel.PokemonGuid, ziel.FormGuid, anzahl);
    var vorhanden = Aktive(suche, neu).FirstOrDefault();
    if (vorhanden == null)
    {
      suche.Add(neu);
      return new AnlegeErgebnis
      {
        Eintrag = neu,
        Vorher = null,
        Nachher = Stand(suche, habe, neu)
      };
    }

    var vorher = Stand(suche, habe, vorhanden);
    vorhanden.Anzahl += anzahl;
    return new AnlegeErgebnis
    {
      Eintrag = vorhanden,
      Vorher = vorher,
      Nachher = Stand(suche, habe, vorhanden)
    };
  }

  // Erledigt/Ziel der Zeile, zu der das Muster gehoert; 0/0, wenn es sie nicht gibt
  public static ZeilenStand Stand(
    IEnumerable<SucheEintrag> suche,
    IEnumerable<HabeEintrag> habe,
    SucheEintrag muster)
  {
    var schluessel = SuchZeilen.Schluessel(muster);
    var zeile = SuchZeilen.Bilden(suche, habe)
      .FirstOrDefault(z => SuchZeilen.Schluessel(z.Muster) == schluessel);
    return zeile == null ? new ZeilenStand(0, 0) : new ZeilenStand(zeile.Erledigt, zeile.Ziel);
  }

  // Zeile, mit der die Zeile nach Aenderung der Anforderungen zusammenfallen
  // wuerde, oder null, wenn es keine solche gibt
  public static SuchZeile? Konflikt(
    IEnumerable<SucheEintrag> suche,
    IEnumerable<HabeEintrag> habe,
    SuchZeile zeile,
    SucheEintrag neueAnforderung)
  {
    var probe = NeuerEintrag(
      neueAnforderung, zeile.Muster.PokemonGuid, zeile.Muster.FormGuid, 1);
    var neuerSchluessel = SuchZeilen.Schluessel(probe);
    if (neuerSchluessel == SuchZeilen.Schluessel(zeile.Muster))
    {
      return null;
    }

    return SuchZeilen.Bilden(suche, habe)
      .FirstOrDefault(z => SuchZeilen.Schluessel(z.Muster) == neuerSchluessel);
  }

  // Uebernimmt die Anforderungen fuer die Zeile; Pokemon und Form bleiben.
  // Faellt die Zeile danach mit einer anderen zusammen, werden beide
  // zusammengefuehrt. Liefert den Datensatz der resultierenden Zeile.
  public static SucheEintrag AnforderungenAendern(
    List<SucheEintrag> suche,
    SuchZeile zeile,
    SucheEintrag neueAnforderung)
  {
    var eintraege = Aktive(suche, zeile.Muster);
    if (eintraege.Count == 0)
    {
      throw new InvalidOperationException("Die Zeile ist nicht mehr vorhanden");
    }

    foreach (var eintrag in eintraege)
    {
      MerkmaleUebernehmen(neueAnforderung, eintrag);
    }

    return Zusammenfuehren(suche, eintraege[0]);
  }

  // Setzt die Anzahl der Zeile; 0 entfernt die Zeile
  public static void AnzahlSetzen(
    List<SucheEintrag> suche,
    SuchZeile zeile,
    int anzahl)
  {
    if (anzahl < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(anzahl), "Anzahl darf nicht negativ sein");
    }

    var eintraege = Aktive(suche, zeile.Muster);
    if (eintraege.Count == 0)
    {
      throw new InvalidOperationException("Die Zeile ist nicht mehr vorhanden");
    }

    // Sollte es mehrere Datensaetze fuer die Zeile geben, bleibt hoechstens einer
    foreach (var weiterer in eintraege.Skip(1))
    {
      suche.Remove(weiterer);
    }

    if (anzahl == 0)
    {
      suche.Remove(eintraege[0]);
      return;
    }

    eintraege[0].Anzahl = anzahl;
  }

  // Neuer Datensatz mit den Anforderungen der Vorlage fuer Pokemon und Form
  internal static SucheEintrag NeuerEintrag(
    SucheEintrag vorlage,
    Guid pokemonGuid,
    Guid formGuid,
    int anzahl)
  {
    var eintrag = new SucheEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = pokemonGuid,
      FormGuid = formGuid,
      Anzahl = anzahl
    };
    MerkmaleUebernehmen(vorlage, eintrag);
    return eintrag;
  }

  // Datensaetze mit demselben Schluessel wie das Muster
  internal static List<SucheEintrag> Aktive(IEnumerable<SucheEintrag> suche, SucheEintrag muster)
  {
    var schluessel = SuchZeilen.Schluessel(muster);
    return [.. suche.Where(e => SuchZeilen.Schluessel(e) == schluessel)];
  }

  // Alle weiteren Datensaetze derselben Zeile gehen in "behalten" auf
  private static SucheEintrag Zusammenfuehren(List<SucheEintrag> suche, SucheEintrag behalten)
  {
    var weitere = Aktive(suche, behalten).Where(e => !ReferenceEquals(e, behalten)).ToList();
    foreach (var weiterer in weitere)
    {
      behalten.Anzahl += weiterer.Anzahl;
      suche.Remove(weiterer);
    }

    return behalten;
  }

  private static SuchZiel Ziel(
    Pokemon pokemon,
    PokemonForm? form,
    bool shinyVerfuegbar,
    SucheEintrag anforderung,
    List<HabeEintrag> exemplare)
  {
    var formGuid = form?.Guid ?? Guid.Empty;
    var probe = NeuerEintrag(anforderung, pokemon.Guid, formGuid, 1);

    ZielKategorie kategorie;
    if (exemplare.Any(h => Anforderung.Erfuellt(h, probe)))
    {
      kategorie = ZielKategorie.HastDuSchon;
    }
    else if (anforderung.Shiny && !shinyVerfuegbar)
    {
      kategorie = ZielKategorie.NichtVerfuegbar;
    }
    else
    {
      kategorie = ZielKategorie.Verfuegbar;
    }

    var text = EintragText.Erzeugen(pokemon, form, new SucheEintrag());
    return new SuchZiel(pokemon.Guid, formGuid, pokemon.DexNr, text, kategorie);
  }

  // Freie Texte werden dabei getrimmt
  private static void MerkmaleUebernehmen(SucheEintrag quelle, SucheEintrag ziel)
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
    ziel.EinsZuEins = quelle.EinsZuEins;
  }
}
