using System.Collections.Generic;
using System.Linq;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

// Ein Treffer fuer die Anzeige: seine Originalzeile, mein Eintrag und
// gegebenenfalls Hinweise ("pruefen: ...", "1:1 gewuenscht")
public sealed record TrefferAnzeige(string Er, string Ich, string Zusatz, bool IstPruefen)
{
  public bool HatZusatz => Zusatz != "";
}

public sealed record TrefferAbschnitt(string Titel, IReadOnlyList<TrefferAnzeige> Treffer);

// Ergebnisseite des Abgleichs mit einer fremden Tauschliste.
// Leere Abschnitte werden nicht angezeigt.
public sealed class AbgleichErgebnisViewModel : SeiteViewModel
{
  private readonly Datenbestand _bestand;
  private readonly List<SuchZeile> _suchZeilen;

  public AbgleichErgebnisViewModel(Datenbestand bestand, AbgleichErgebnis ergebnis)
  {
    _bestand = bestand;
    _suchZeilen = SuchZeilen.Bilden(bestand.Suche, bestand.Habe);

    var abschnitte = new List<TrefferAbschnitt>();
    Hinzufuegen(abschnitte, "1:1 passt exakt", ergebnis.EinsZuEins);
    Hinzufuegen(abschnitte, "Er hat, was ich suche", ergebnis.ErHat);
    Hinzufuegen(abschnitte, "Er sucht, was ich \u00fcbrig habe", ergebnis.ErSucht);
    Abschnitte = abschnitte;

    Fehler = ergebnis.Fehler
      .Select(f => $"Zeile {f.ZeilenNummer}: {f.Zeile.Trim()} ({f.Grund})")
      .ToList();
  }

  public override string Titel => "Abgleich";

  public IReadOnlyList<TrefferAbschnitt> Abschnitte { get; }

  public bool KeineTreffer => Abschnitte.Count == 0;

  public IReadOnlyList<string> Fehler { get; }

  public bool HatFehler => Fehler.Count > 0;

  public string FehlerTitel => $"Nicht erkannte Zeilen ({Fehler.Count})";

  private void Hinzufuegen(
    List<TrefferAbschnitt> abschnitte,
    string titel,
    IReadOnlyList<AbgleichTreffer> treffer)
  {
    if (treffer.Count == 0)
    {
      return;
    }

    var anzeigen = treffer.Select(Anzeige).ToList();
    abschnitte.Add(new TrefferAbschnitt($"{titel} ({treffer.Count})", anzeigen));
  }

  private TrefferAnzeige Anzeige(AbgleichTreffer treffer)
  {
    var teile = new List<string>();
    var pruefen = treffer.Passung == Passung.Pruefen;
    if (pruefen)
    {
      teile.Add("pr\u00fcfen: " + string.Join(", ", treffer.Hinweise));
    }

    if (treffer.EinsZuEinsGewuenscht)
    {
      teile.Add("1:1 gew\u00fcnscht");
    }

    return new TrefferAnzeige(
      treffer.Fremd.Zeile.Trim(),
      EigenText(treffer.Eigen),
      string.Join(" \u00b7 ", teile),
      pruefen);
  }

  // Mein Eintrag in der gewohnten Darstellung; bei "Suche ich" mit
  // Bedingungen und Zaehler wie in der Liste
  private string EigenText(ListenEintrag eigen)
  {
    var (pokemon, form) = Tauschliste.Aufloesen(_bestand.Katalog, eigen);
    var text = $"#{pokemon.DexNr} {EintragText.Erzeugen(pokemon, form, eigen)}";
    if (eigen is SucheEintrag suche)
    {
      var schluessel = SuchZeilen.Schluessel(suche);
      var zeile = _suchZeilen.FirstOrDefault(z => SuchZeilen.Schluessel(z.Muster) == schluessel);
      if (zeile != null)
      {
        text += ListenAnzeige.SuchZusatz(zeile);
      }
    }

    return text;
  }
}
