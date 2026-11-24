using System.Collections.Generic;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

public static class EintragText
{
  // Anzeigetexte (im Programm mit echten Umlauten)
  internal const string TextForm = "Form";
  internal const string TextMaennlich = "M\u00e4nnlich";
  internal const string TextWeiblich = "Weiblich";
  internal const string TextShiny = "Shiny";
  internal const string TextDynamax = "Dynamax";
  internal const string TextGigadynamax = "Gigadynamax";
  internal const string TextKostuem = "Kost\u00fcm";
  internal const string TextHintergrund = "Hintergrund";
  internal const string TextBeliebig = "(beliebig)";
  internal const string Trenner = " - ";

  // Aufbau: Name Form - Geschlecht - Shiny - Region - Dynamax - Kostuem - Hintergrund
  // Bei "Form beliebig" (form == null): Name - Form (beliebig) - Geschlecht - ...
  // Nicht gesetzte Bestandteile entfallen vollstaendig.
  public static string Erzeugen(Pokemon pokemon, PokemonForm? form, ListenEintrag eintrag)
  {
    var teile = new List<string>();
    if (form == null)
    {
      teile.Add(pokemon.Name);
      teile.Add(TextForm + " " + TextBeliebig);
    }
    else
    {
      teile.Add(form.Form == "" ? pokemon.Name : pokemon.Name + " " + form.Form);
    }

    if (eintrag.Geschlecht == Geschlecht.Maennlich)
    {
      teile.Add(TextMaennlich);
    }
    else if (eintrag.Geschlecht == Geschlecht.Weiblich)
    {
      teile.Add(TextWeiblich);
    }

    if (eintrag.Shiny)
    {
      teile.Add(TextShiny);
    }

    if (form != null && form.Region != "")
    {
      teile.Add(form.Region);
    }

    if (eintrag.Dynamax == DynamaxStufe.Dynamax)
    {
      teile.Add(TextDynamax);
    }
    else if (eintrag.Dynamax == DynamaxStufe.Gigadynamax)
    {
      teile.Add(TextGigadynamax);
    }

    if (eintrag.KostuemAktiv)
    {
      teile.Add(Merkmal(TextKostuem, eintrag.KostuemText));
    }

    if (eintrag.HintergrundAktiv)
    {
      teile.Add(Merkmal(TextHintergrund, eintrag.HintergrundText));
    }

    return string.Join(Trenner, teile);
  }

  // Mit Beschreibung: Kostuem: "Partyhut", ohne: Kostuem (beliebig)
  private static string Merkmal(string bezeichnung, string beschreibung) =>
    beschreibung == ""
      ? bezeichnung + " " + TextBeliebig
      : bezeichnung + ": \"" + beschreibung + "\"";
}
