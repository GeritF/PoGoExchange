using System;
using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

// Anlegen und Bearbeiten von Eintraegen in "Habe ich ueber".
// Die Liste hat keine Anzahl; gleiche Eintraege gibt es nur einmal.
public static class UeberBearbeitung
{
  // Freie Texte zaehlen nur bei gesetztem Haken, Vergleich in Kleinbuchstaben
  internal static UeberSchluessel Schluessel(UeberEintrag e) => new(
    e.PokemonGuid,
    e.FormGuid,
    e.Geschlecht,
    e.Shiny,
    e.Dynamax,
    e.KostuemAktiv,
    e.KostuemAktiv ? e.KostuemText.ToLowerInvariant() : "",
    e.HintergrundAktiv,
    e.HintergrundAktiv ? e.HintergrundText.ToLowerInvariant() : ""
  );

  // Legt einen Eintrag an. Gibt es denselben schon, wird nichts angelegt;
  // neu ist dann false und Eintrag der vorhandene.
  public static (UeberEintrag Eintrag, bool Neu) Anlegen(
    List<UeberEintrag> ueber,
    ListenEintrag merkmale,
    FormZiel ziel)
  {
    var neu = NeuerEintrag(merkmale, ziel.PokemonGuid, ziel.FormGuid);
    var vorhanden = Gleiche(ueber, neu).FirstOrDefault();
    if (vorhanden != null)
    {
      return (vorhanden, false);
    }

    ueber.Add(neu);
    return (neu, true);
  }

  // Eintrag, mit dem der Eintrag nach Aenderung der Merkmale gleich waere,
  // oder null, wenn es keinen solchen gibt
  public static UeberEintrag? Konflikt(
    IEnumerable<UeberEintrag> ueber,
    UeberEintrag eintrag,
    ListenEintrag neueMerkmale)
  {
    var probe = NeuerEintrag(neueMerkmale, eintrag.PokemonGuid, eintrag.FormGuid);
    if (Schluessel(probe) == Schluessel(eintrag))
    {
      return null;
    }

    return Gleiche(ueber, probe).FirstOrDefault();
  }

  // Uebernimmt die Merkmale; Pokemon und Form bleiben. Gibt es danach einen
  // gleichen Eintrag, wird dieser entfernt (es bleibt einer uebrig).
  public static UeberEintrag MerkmaleAendern(
    List<UeberEintrag> ueber,
    UeberEintrag eintrag,
    ListenEintrag neueMerkmale)
  {
    MerkmaleUebernehmen(neueMerkmale, eintrag);
    var weitere = Gleiche(ueber, eintrag).Where(e => !ReferenceEquals(e, eintrag)).ToList();
    foreach (var weiterer in weitere)
    {
      ueber.Remove(weiterer);
    }

    return eintrag;
  }

  public static void Loeschen(List<UeberEintrag> ueber, UeberEintrag eintrag)
  {
    ueber.Remove(eintrag);
  }

  private static List<UeberEintrag> Gleiche(IEnumerable<UeberEintrag> ueber, UeberEintrag muster)
  {
    var schluessel = Schluessel(muster);
    return ueber.Where(e => Schluessel(e) == schluessel).ToList();
  }

  private static UeberEintrag NeuerEintrag(ListenEintrag merkmale, Guid pokemonGuid, Guid formGuid)
  {
    var eintrag = new UeberEintrag
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = pokemonGuid,
      FormGuid = formGuid
    };
    MerkmaleUebernehmen(merkmale, eintrag);
    return eintrag;
  }

  // Freie Texte werden dabei getrimmt
  private static void MerkmaleUebernehmen(ListenEintrag quelle, UeberEintrag ziel)
  {
    ziel.Geschlecht = quelle.Geschlecht;
    ziel.Shiny = quelle.Shiny;
    ziel.Dynamax = quelle.Dynamax;
    ziel.KostuemAktiv = quelle.KostuemAktiv;
    ziel.KostuemText = TextBereinigung.Bereinigen(quelle.KostuemText);
    ziel.HintergrundAktiv = quelle.HintergrundAktiv;
    ziel.HintergrundText = TextBereinigung.Bereinigen(quelle.HintergrundText);
  }
}

internal readonly record struct UeberSchluessel(
  Guid PokemonGuid,
  Guid FormGuid,
  Geschlecht Geschlecht,
  bool Shiny,
  DynamaxStufe Dynamax,
  bool KostuemAktiv,
  string KostuemText,
  bool HintergrundAktiv,
  string HintergrundText
);
