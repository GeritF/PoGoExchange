using PoGoExchange.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PoGoExchange.Services;

// Ein waehlbares Pokemon (z.B. beim Anlegen einer eigenen Form)
public sealed record PokemonZiel(Guid PokemonGuid, int DexNr, string Name)
{
  public string Anzeige => $"#{DexNr} {Name}";
}

// Anlegen, Aendern und Loeschen eigener Formen (eigene_formen.csv).
// Nach jeder Aenderung muss der Katalog neu aufgebaut werden
// (Datenbestand.KatalogAufbauen).
public static class FormVerwaltung
{
  internal const string MeldungZeichen =
    "Im Form-Text nicht erlaubt: Anf\u00fchrungszeichen, eckige und runde Klammern " +
    "sowie \" - \" (Bindestrich mit Leerzeichen). " +
    "Beispiel: \"Toll, und gro\u00df\" statt \"Toll (und gro\u00df)\".";

  // Liefert null, wenn der Text als Form erlaubt ist. Die Zeichen wuerden
  // sonst Export und Import der Tauschliste durcheinanderbringen.
  public static string? FormTextFehler(string text)
  {
    if (text.IndexOfAny(['"', '[', ']', '(', ')', '\n', '\r']) >= 0 || text.Contains(" - "))
    {
      return MeldungZeichen;
    }

    return null;
  }

  // Alle Pokemon, passend zum Suchfeld
  public static List<PokemonZiel> Pokemon(Katalog katalog, string filter)
  {
    return
    [
      .. katalog.AllePokemon()
        .Where(p => ListenAnzeige.FilterPasst(filter, p))
        .Select(p => new PokemonZiel(p.Guid, p.DexNr, p.Name))
    ];
  }

  // Vorhandene Form desselben Pokemon mit gleicher Form und Region (ohne
  // Gross-/Kleinschreibung, Raender getrimmt); ausser wird nicht beachtet
  public static PokemonForm? Konflikt(
    Katalog katalog,
    Guid pokemonGuid,
    string form,
    string region,
    Guid? ausser = null)
  {
    var f = Normal(form);
    var r = Normal(region);
    return katalog.FormenZuPokemon(pokemonGuid)
      .Where(x => ausser == null || x.Guid != ausser)
      .FirstOrDefault(x => Normal(x.Form) == f && Normal(x.Region) == r);
  }

  // Legt eine eigene Form an. Liefert die neue Form oder eine Fehlermeldung.
  public static (PokemonForm? Form, string? Fehler) Anlegen(
    List<PokemonForm> eigene,
    Katalog katalog,
    Guid pokemonGuid,
    string form,
    string region,
    bool shinyVerfuegbar)
  {
    var fehler = Pruefen(katalog, pokemonGuid, form, region, null);
    if (fehler != null)
    {
      return (null, fehler);
    }

    var neu = new PokemonForm
    {
      Guid = Guid.NewGuid(),
      PokemonGuid = pokemonGuid,
      Form = TextBereinigung.Bereinigen(form),
      Region = TextBereinigung.Bereinigen(region),
      ShinyVerfuegbar = shinyVerfuegbar
    };
    eigene.Add(neu);
    return (neu, null);
  }

  // Aendert eine eigene Form. Liefert null oder eine Fehlermeldung.
  public static string? Aendern(
    Katalog katalog,
    PokemonForm eigene,
    string form,
    string region,
    bool shinyVerfuegbar)
  {
    var fehler = Pruefen(katalog, eigene.PokemonGuid, form, region, eigene.Guid);
    if (fehler != null)
    {
      return fehler;
    }

    eigene.Form = TextBereinigung.Bereinigen(form);
    eigene.Region = TextBereinigung.Bereinigen(region);
    eigene.ShinyVerfuegbar = shinyVerfuegbar;
    return null;
  }

  // Namen der Listen, die die Form verwenden; leer, wenn unbenutzt
  public static List<string> Verwendung(
    PokemonForm form,
    IEnumerable<HabeEintrag> habe,
    IEnumerable<SucheEintrag> suche,
    IEnumerable<UeberEintrag> ueber)
  {
    var listen = new List<string>();
    if (habe.Any(e => e.FormGuid == form.Guid))
    {
      listen.Add(ListenNamen.Habe);
    }

    if (suche.Any(e => e.FormGuid == form.Guid))
    {
      listen.Add(ListenNamen.Suche);
    }

    if (ueber.Any(e => e.FormGuid == form.Guid))
    {
      listen.Add(ListenNamen.Ueber);
    }

    return listen;
  }

  // Entfernt eine eigene Form, sofern keine Liste sie verwendet.
  // Liefert null oder eine Fehlermeldung.
  public static string? Loeschen(
    List<PokemonForm> eigene,
    PokemonForm form,
    IEnumerable<HabeEintrag> habe,
    IEnumerable<SucheEintrag> suche,
    IEnumerable<UeberEintrag> ueber)
  {
    var verwendung = Verwendung(form, habe, suche, ueber);
    if (verwendung.Count > 0)
    {
      return "Die Form wird noch verwendet in: " + string.Join(", ", verwendung) +
        ". Erst dort entfernen oder \u00e4ndern.";
    }

    eigene.Remove(form);
    return null;
  }

  private static string? Pruefen(
    Katalog katalog,
    Guid pokemonGuid,
    string form,
    string region,
    Guid? ausser)
  {
    var zeichen = FormTextFehler(form);
    if (zeichen != null)
    {
      return zeichen;
    }

    if (Konflikt(katalog, pokemonGuid, form, region, ausser) != null)
    {
      return "Diese Form gibt es f\u00fcr dieses Pokemon bereits.";
    }

    return null;
  }

  private static string Normal(string text)
  {
    return TextBereinigung.Bereinigen(text).ToLowerInvariant();
  }
}
