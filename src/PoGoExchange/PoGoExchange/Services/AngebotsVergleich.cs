using PoGoExchange.Models;
using System;
using System.Collections.Generic;

namespace PoGoExchange.Services;

public enum Passung
{
  Passt,
  Pruefen,
  PasstNicht
}

public sealed class Vergleichsergebnis
{
  public required Passung Passung { get; init; }
  public required IReadOnlyList<string> Hinweise { get; init; }
}

// Vergleicht ein Angebot mit einer Anforderung.
// Angebot: nicht gesetzte Merkmale bedeuten "hat es nicht" (Geschlecht: "nicht angegeben").
// Anforderung: nicht gesetzte Merkmale bedeuten "egal".
// Glueck und 100er werden nicht verglichen, sie entstehen erst durch den Tausch.
public static class AngebotsVergleich
{
  internal const string HinweisForm = "Form beim Angebot nicht angegeben";
  internal const string HinweisGeschlecht = "Geschlecht beim Angebot nicht angegeben";
  internal const string HinweisKostuem = "Kost\u00fcm beim Angebot nicht beschrieben";
  internal const string HinweisHintergrund = "Hintergrund beim Angebot nicht beschrieben";

  public static Vergleichsergebnis Vergleichen(ListenEintrag angebot, ListenEintrag anforderung)
  {
    var hinweise = new List<string>();
    if (!IstVereinbar(angebot, anforderung, hinweise))
    {
      return new Vergleichsergebnis
      {
        Passung = Passung.PasstNicht,
        Hinweise = []
      };
    }

    return new Vergleichsergebnis
    {
      Passung = hinweise.Count == 0 ? Passung.Passt : Passung.Pruefen,
      Hinweise = hinweise
    };
  }

  // Liefert false bei "passt nicht"; Hinweise fuer "pruefen" werden gesammelt
  private static bool IstVereinbar(
    ListenEintrag angebot,
    ListenEintrag anforderung,
    List<string> hinweise)
  {
    if (angebot.PokemonGuid != anforderung.PokemonGuid)
    {
      return false;
    }

    // "Form beliebig" in der Anforderung: jede Form passt.
    // "Form beliebig" im Angebot: unklar, welche Form er hat -> pruefen.
    if (!anforderung.FormBeliebig)
    {
      if (angebot.FormBeliebig)
      {
        hinweise.Add(HinweisForm);
      }
      else if (angebot.FormGuid != anforderung.FormGuid)
      {
        return false;
      }
    }

    if (
      anforderung.Geschlecht != Geschlecht.Unbestimmt &&
      angebot.Geschlecht != anforderung.Geschlecht)
    {
      if (angebot.Geschlecht != Geschlecht.Unbestimmt)
      {
        return false;
      }

      hinweise.Add(HinweisGeschlecht);
    }

    if (anforderung.Shiny && !angebot.Shiny)
    {
      return false;
    }

    if (
      anforderung.Dynamax != DynamaxStufe.Keins &&
      angebot.Dynamax != anforderung.Dynamax)
    {
      return false;
    }

    var kostuem = TextMerkmal(
      angebot.KostuemAktiv,
      angebot.KostuemText,
      anforderung.KostuemAktiv,
      anforderung.KostuemText);
    if (kostuem == Passung.PasstNicht)
    {
      return false;
    }

    if (kostuem == Passung.Pruefen)
    {
      hinweise.Add(HinweisKostuem);
    }

    var hintergrund = TextMerkmal(
      angebot.HintergrundAktiv,
      angebot.HintergrundText,
      anforderung.HintergrundAktiv,
      anforderung.HintergrundText);
    if (hintergrund == Passung.PasstNicht)
    {
      return false;
    }

    if (hintergrund == Passung.Pruefen)
    {
      hinweise.Add(HinweisHintergrund);
    }

    return true;
  }

  // Kostuem bzw. Hintergrund:
  // nicht gefordert oder beliebig gefordert = passt,
  // bestimmter Text gefordert, Angebot ohne Beschreibung = pruefen,
  // gefordert aber keins, oder anderer Text = passt nicht
  private static Passung TextMerkmal(
    bool angebotAktiv,
    string angebotText,
    bool anforderungAktiv,
    string anforderungText)
  {
    if (!anforderungAktiv)
    {
      return Passung.Passt;
    }

    if (!angebotAktiv)
    {
      return Passung.PasstNicht;
    }

    if (anforderungText == "")
    {
      return Passung.Passt;
    }

    if (angebotText == "")
    {
      return Passung.Pruefen;
    }

    return string.Equals(angebotText, anforderungText, StringComparison.OrdinalIgnoreCase)
      ? Passung.Passt
      : Passung.PasstNicht;
  }
}
