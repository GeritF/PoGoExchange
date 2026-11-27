using PoGoExchange.Models;
using System;
using System.Collections.Generic;

namespace PoGoExchange.Services;

public static class ListenNamen
{
  public const string Habe = "Habe ich";
  public const string Suche = "Suche ich";
  public const string Ueber = "Habe ich \u00fcber";
}

public sealed record KonsistenzFehler(string Liste, Guid EintragGuid, string Grund);

// Prueft die Verweise der persoenlichen Listen auf Pokemon und Formen
public static class Konsistenz
{
  // Liefert null, wenn der Eintrag gueltig ist, sonst den Grund.
  // formPflicht: in "Habe ich" und "Habe ich ueber" muss eine Form gesetzt sein.
  public static string? EintragPruefen(Katalog katalog, ListenEintrag eintrag, bool formPflicht)
  {
    if (katalog.PokemonZuGuid(eintrag.PokemonGuid) == null)
    {
      return $"Pokemon {eintrag.PokemonGuid} nicht gefunden";
    }

    if (eintrag.FormBeliebig)
    {
      return formPflicht ? "Form fehlt (nur in \"Suche ich\" erlaubt)" : null;
    }

    var form = katalog.FormZuGuid(eintrag.FormGuid);
    if (form == null)
    {
      return $"Form {eintrag.FormGuid} nicht gefunden";
    }

    if (form.PokemonGuid != eintrag.PokemonGuid)
    {
      return $"Form {eintrag.FormGuid} geh\u00f6rt nicht zu Pokemon {eintrag.PokemonGuid}";
    }

    return null;
  }

  // Prueft alle Eintraege der drei Listen und liefert die Fehler gesammelt
  // (leere Liste, wenn alles gueltig ist)
  public static List<KonsistenzFehler> ListenPruefen(
    Katalog katalog,
    IEnumerable<HabeEintrag> habe,
    IEnumerable<SucheEintrag> suche,
    IEnumerable<UeberEintrag> ueber)
  {
    var fehler = new List<KonsistenzFehler>();
    Sammeln(katalog, habe, ListenNamen.Habe, true, fehler);
    Sammeln(katalog, suche, ListenNamen.Suche, false, fehler);
    Sammeln(katalog, ueber, ListenNamen.Ueber, true, fehler);
    return fehler;
  }

  private static void Sammeln<T>(
    Katalog katalog,
    IEnumerable<T> eintraege,
    string liste,
    bool formPflicht,
    List<KonsistenzFehler> fehler)
    where T : ListenEintrag
  {
    foreach (var eintrag in eintraege)
    {
      var grund = EintragPruefen(katalog, eintrag, formPflicht);
      if (grund != null)
      {
        fehler.Add(new KonsistenzFehler(liste, eintrag.Guid, grund));
      }
    }
  }
}
