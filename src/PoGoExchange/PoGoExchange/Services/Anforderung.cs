using PoGoExchange.Models;
using System;

namespace PoGoExchange.Services;

public static class Anforderung
{
  // Erfuellt das Exemplar alle gesetzten Anforderungen des Sucheintrags?
  // Nicht gesetzte Anforderungen gelten als "egal".
  public static bool Erfuellt(HabeEintrag exemplar, SucheEintrag suche)
  {
    if (exemplar.PokemonGuid != suche.PokemonGuid)
    {
      return false;
    }

    // "Form beliebig" in der Suche: jede Form des Pokemon erfuellt
    if (!suche.FormBeliebig && exemplar.FormGuid != suche.FormGuid)
    {
      return false;
    }

    if (
      (suche.Geschlecht != Geschlecht.Unbestimmt) &&
      (exemplar.Geschlecht != suche.Geschlecht)
    )
    {
      return false;
    }

    if (suche.Shiny && !exemplar.Shiny)
    {
      return false;
    }

    if (
      (suche.Dynamax != DynamaxStufe.Keins) &&
      (exemplar.Dynamax != suche.Dynamax)
    )
    {
      return false;
    }

    if (
      suche.KostuemAktiv &&
      (
        !exemplar.KostuemAktiv ||
        !TextMerkmalErfuellt(exemplar.KostuemText, suche.KostuemText)
      )
    )
    {
      return false;
    }

    if (
      suche.HintergrundAktiv &&
      (
        !exemplar.HintergrundAktiv ||
        !TextMerkmalErfuellt(exemplar.HintergrundText, suche.HintergrundText)
      )
    )
    {
      return false;
    }

    if (suche.Glueck && !exemplar.Glueck)
    {
      return false;
    }

    if (suche.Hundert && !exemplar.Hundert)
    {
      return false;
    }

    return true;
  }

  // Anforderung ohne Beschreibung = beliebig, sonst Vergleich ohne Gross-/Kleinschreibung
  private static bool TextMerkmalErfuellt(string exemplarText, string sucheText)
  {
    return
      sucheText == "" ||
      string.Equals(exemplarText, sucheText, StringComparison.OrdinalIgnoreCase);
  }
}
