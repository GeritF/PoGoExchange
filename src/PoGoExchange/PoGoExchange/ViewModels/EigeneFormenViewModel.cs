using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PoGoExchange.Models;
using PoGoExchange.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace PoGoExchange.ViewModels;

// Eine eigene Form in der Liste; Antippen oeffnet sie
public sealed partial class EigeneFormEintrag : ViewModelBase
{
  private readonly System.Action<PokemonForm> _oeffnen;

  public EigeneFormEintrag(PokemonForm form, string text, System.Action<PokemonForm> oeffnen)
  {
    Form = form;
    Text = text;
    _oeffnen = oeffnen;
  }

  public PokemonForm Form { get; }

  public string Text { get; }

  [RelayCommand]
  private void Oeffnen()
  {
    _oeffnen(Form);
  }
}

// Seite "Eigene Formen" (aus der Verwaltung)
public sealed partial class EigeneFormenViewModel : SeiteViewModel
{
  private readonly Navigation _navigation;
  private readonly Datenbestand _bestand;

  public EigeneFormenViewModel(Navigation navigation, Datenbestand bestand)
  {
    _navigation = navigation;
    _bestand = bestand;
    Aktualisieren();
  }

  public override string Titel => "Eigene Formen";

  public ObservableCollection<EigeneFormEintrag> Liste { get; } = [];

  [ObservableProperty] private bool _keineVorhanden;

  [ObservableProperty] [NotifyPropertyChangedFor(nameof(HatRueckmeldung))]
  private string _rueckmeldung = "";

  public bool HatRueckmeldung => Rueckmeldung != "";

  [RelayCommand]
  private void Neu()
  {
    _navigation.Oeffnen(new EigeneFormViewModel(_navigation, _bestand, Geaendert));
  }

  private void Oeffnen(PokemonForm form)
  {
    _navigation.Oeffnen(new EigeneFormViewModel(_navigation, _bestand, Geaendert, form));
  }

  // Nach Anlegen, Aendern oder Loeschen: zurueck zur Liste
  private void Geaendert(PokemonForm? form, string rueckmeldung)
  {
    _navigation.Zurueck();
    Aktualisieren();
    Rueckmeldung = rueckmeldung;
  }

  private void Aktualisieren()
  {
    Liste.Clear();
    var eintraege = _bestand.EigeneFormen
      .Select(f => (Form: f, Pokemon: _bestand.Katalog.PokemonZuGuid(f.PokemonGuid)))
      .OrderBy(x => x.Pokemon?.DexNr ?? int.MaxValue)
      .ThenBy(x => x.Form.Form)
      .ThenBy(x => x.Form.Region);
    foreach (var (form, pokemon) in eintraege)
    {
      Liste.Add(new EigeneFormEintrag(form, EigeneFormViewModel.Text(pokemon, form), Oeffnen));
    }

    KeineVorhanden = Liste.Count == 0;
  }
}
