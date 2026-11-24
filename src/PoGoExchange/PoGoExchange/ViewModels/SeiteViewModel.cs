namespace PoGoExchange.ViewModels;

// Basis aller Seiten; der Titel erscheint in der Kopfleiste
public abstract class SeiteViewModel : ViewModelBase
{
  public abstract string Titel { get; }
}
