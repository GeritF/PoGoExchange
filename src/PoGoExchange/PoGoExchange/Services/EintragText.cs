using System.Collections.Generic;
using PoGoExchange.Models;

namespace PoGoExchange.Services;

public static class EintragText
{
  // Anzeigetexte (im Programm mit echten Umlauten)
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
  // Nicht gesetzte Bestandteile entfallen vollstaendig.
  public static string Erzeugen(Pokemon pokemon, PokemonForm form, ListenEintrag eintrag)
  {
    var kopf = form.Form == "" ? pokemon.Name : pokemon.Name + " " + form.Form;
    var teile = new List<string> { kopf };

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

    if (form.Region != "")
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
