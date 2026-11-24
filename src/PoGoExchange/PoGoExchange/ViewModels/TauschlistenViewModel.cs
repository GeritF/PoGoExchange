using System.Collections.Generic;
using PoGoExchange.Services;

namespace PoGoExchange.ViewModels;

public sealed class TauschlistenViewModel : SeiteViewModel
{
  public TauschlistenViewModel(Datenbestand? bestand, string meldungshinweis)
  {
    Meldungshinweis = meldungshinweis;

    Suche = new ListenTabViewModel(true, (filter, mitErledigten) =>
      bestand == null
        ? new List<AnzeigeGruppe>()
        : ListenAnzeige.Suche(bestand.Katalog, bestand.Suche, mitErledigten, filter));

    Habe = new ListenTabViewModel(false, (filter, _) =>
      bestand == null
        ? new List<AnzeigeGruppe>()
        : ListenAnzeige.Habe(bestand.Katalog, bestand.Habe, filter));

    Ueber = new ListenTabViewModel(false, (filter, _) =>
      bestand == null
        ? new List<AnzeigeGruppe>()
        : ListenAnzeige.Ueber(bestand.Katalog, bestand.Ueber, filter));
  }

  public override string Titel => "Tauschlisten";

  public string Meldungshinweis { get; }

  public bool HatMeldungen => Meldungshinweis != "";

  public ListenTabViewModel Suche { get; }
  public ListenTabViewModel Habe { get; }
  public ListenTabViewModel Ueber { get; }
}
