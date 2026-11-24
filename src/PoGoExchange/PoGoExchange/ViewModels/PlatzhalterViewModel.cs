namespace PoGoExchange.ViewModels;

// Seite fuer Bereiche, die in spaeteren Bauschritten folgen
public sealed class PlatzhalterViewModel : SeiteViewModel
{
  private readonly string _titel;

  public PlatzhalterViewModel(string titel, string text)
  {
    _titel = titel;
    Text = text;
  }

  public override string Titel => _titel;

  public string Text { get; }
}
