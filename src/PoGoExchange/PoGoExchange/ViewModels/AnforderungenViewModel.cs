using CommunityToolkit.Mvvm.ComponentModel;
using PoGoExchange.Models;
using PoGoExchange.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PoGoExchange.ViewModels;

// Eingabeblock fuer die Anforderungen einer Zeile in "Suche ich"
public sealed partial class AnforderungenViewModel : ViewModelBase
{
  // Reihenfolge entspricht den Enum-Werten (Unbestimmt/Keins = 0)
  public IReadOnlyList<string> GeschlechtAuswahl { get; } =
    ["egal", "m\u00e4nnlich", "weiblich"];

  public IReadOnlyList<string> DynamaxAuswahl { get; } =
    ["egal", "Dynamax", "Gigadynamax"];

  [ObservableProperty] private int _geschlechtIndex;

  [ObservableProperty] private bool _shiny;

  [ObservableProperty] private int _dynamaxIndex;

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

  [ObservableProperty] private bool _glueck;

  [ObservableProperty] private bool _hundert;

  [ObservableProperty] private bool _einsZuEins;

  // Wird bei jeder Aenderung einer Anforderung ausgeloest
  public event Action? Geaendert;

  // Fehler in den freien Texten; Texte zaehlen nur bei gesetztem Haken
  public string Fehler =>
    Freitext.MerkmaleFehler(KostuemAktiv, KostuemText, HintergrundAktiv, HintergrundText);

  public bool HatFehler => Fehler != "";

  // Neuer Eintrag mit den eingegebenen Anforderungen (ohne Pokemon und Form)
  public SucheEintrag Vorlage()
  {
    return new SucheEintrag
    {
      Geschlecht = (Geschlecht)GeschlechtIndex,
      Shiny = this.Shiny,
      Dynamax = (DynamaxStufe)DynamaxIndex,
      KostuemAktiv = this.KostuemAktiv,
      KostuemText = this.KostuemText,
      HintergrundAktiv = this.HintergrundAktiv,
      HintergrundText = this.HintergrundText,
      Glueck = this.Glueck,
      Hundert = this.Hundert,
      EinsZuEins = this.EinsZuEins
    };
  }

  // Uebernimmt die Anforderungen eines vorhandenen Eintrags in die Eingabe
  public void Uebernehmen(SucheEintrag eintrag)
  {
    GeschlechtIndex = (int)eintrag.Geschlecht;
    Shiny = eintrag.Shiny;
    DynamaxIndex = (int)eintrag.Dynamax;
    KostuemAktiv = eintrag.KostuemAktiv;
    KostuemText = eintrag.KostuemText;
    HintergrundAktiv = eintrag.HintergrundAktiv;
    HintergrundText = eintrag.HintergrundText;
    Glueck = eintrag.Glueck;
    Hundert = eintrag.Hundert;
    EinsZuEins = eintrag.EinsZuEins;
  }

  protected override void OnPropertyChanged(PropertyChangedEventArgs e)
  {
    base.OnPropertyChanged(e);
    if (e.PropertyName != nameof(Fehler) && e.PropertyName != nameof(HatFehler))
    {
      Geaendert?.Invoke();
    }
  }
}
