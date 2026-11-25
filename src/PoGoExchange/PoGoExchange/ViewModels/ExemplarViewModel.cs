using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using PoGoExchange.Models;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

// Eingabeblock fuer die Merkmale eines Exemplars ("Habe ich", "Habe ich ueber").
// Anders als bei Anforderungen beschreiben die Werte das Exemplar:
// "unbekannt" statt "egal", "keins" statt "egal", leerer Text = nicht beschrieben.
public sealed partial class ExemplarViewModel : ViewModelBase
{
  public ExemplarViewModel(bool mitGlueckUndHundert = true)
  {
    MitGlueckUndHundert = mitGlueckUndHundert;
  }

  // In "Habe ich ueber" gibt es kein Glueck und keinen 100er
  public bool MitGlueckUndHundert { get; }

  // Reihenfolge entspricht den Enum-Werten (Unbestimmt/Keins = 0)
  public IReadOnlyList<string> GeschlechtAuswahl { get; } =
    new[] { "unbekannt", "m\u00e4nnlich", "weiblich" };

  public IReadOnlyList<string> DynamaxAuswahl { get; } =
    new[] { "keins", "Dynamax", "Gigadynamax" };

  [ObservableProperty]
  private int _geschlechtIndex;

  [ObservableProperty]
  private bool _shiny;

  [ObservableProperty]
  private int _dynamaxIndex;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(Fehler))]
  [NotifyPropertyChangedFor(nameof(HatFehler))]
  private bool _kostuemAktiv;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(Fehler))]
  [NotifyPropertyChangedFor(nameof(HatFehler))]
  private string _kostuemText = "";

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(Fehler))]
  [NotifyPropertyChangedFor(nameof(HatFehler))]
  private bool _hintergrundAktiv;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(Fehler))]
  [NotifyPropertyChangedFor(nameof(HatFehler))]
  private string _hintergrundText = "";

  [ObservableProperty]
  private bool _glueck;

  [ObservableProperty]
  private bool _hundert;

  // Fehler in den freien Texten; Texte zaehlen nur bei gesetztem Haken
  public string Fehler
  {
    get
    {
      if (KostuemAktiv)
      {
        var fehler = SuchBearbeitung.FreitextFehler(KostuemText);
        if (fehler != null)
        {
          return "Kost\u00fcm: " + fehler;
        }
      }

      if (HintergrundAktiv)
      {
        var fehler = SuchBearbeitung.FreitextFehler(HintergrundText);
        if (fehler != null)
        {
          return "Hintergrund: " + fehler;
        }
      }

      return "";
    }
  }

  public bool HatFehler => Fehler != "";

  // Neue Zeile mit den eingegebenen Merkmalen (ohne Pokemon und Form)
  public HabeEintrag Vorlage()
  {
    return new HabeEintrag
    {
      Geschlecht = (Geschlecht)GeschlechtIndex,
      Shiny = this.Shiny,
      Dynamax = (DynamaxStufe)DynamaxIndex,
      KostuemAktiv = this.KostuemAktiv,
      KostuemText = this.KostuemText,
      HintergrundAktiv = this.HintergrundAktiv,
      HintergrundText = this.HintergrundText,
      Glueck = MitGlueckUndHundert && this.Glueck,
      Hundert = MitGlueckUndHundert && this.Hundert
    };
  }

  // Uebernimmt die Merkmale eines vorhandenen Eintrags in die Eingabe
  public void Uebernehmen(ListenEintrag eintrag)
  {
    GeschlechtIndex = (int)eintrag.Geschlecht;
    Shiny = eintrag.Shiny;
    DynamaxIndex = (int)eintrag.Dynamax;
    KostuemAktiv = eintrag.KostuemAktiv;
    KostuemText = eintrag.KostuemText;
    HintergrundAktiv = eintrag.HintergrundAktiv;
    HintergrundText = eintrag.HintergrundText;
    if (eintrag is HabeEintrag habe)
    {
      Glueck = habe.Glueck;
      Hundert = habe.Hundert;
    }
  }
}
