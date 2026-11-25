using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PoGoExchange.ViewModels;

// Seitenstapel: Oeffnen legt eine Seite oben auf, Zurueck nimmt sie herunter.
// Die unterste Seite (Hauptseite) bleibt immer erhalten.
public sealed class Navigation : ObservableObject
{
  private readonly Stack<SeiteViewModel> _stapel = new();

  public SeiteViewModel? AktuelleSeite => _stapel.Count == 0 ? null : _stapel.Peek();

  public bool KannZurueck => _stapel.Count > 1;

  public void Oeffnen(SeiteViewModel seite)
  {
    _stapel.Push(seite);
    Geaendert();
  }

  // Leert den Stapel und zeigt nur noch die angegebene Seite (z.B. nach dem Neuladen)
  public void Ersetzen(SeiteViewModel seite)
  {
    _stapel.Clear();
    _stapel.Push(seite);
    Geaendert();
  }

  // Liefert false, wenn bereits die unterste Seite angezeigt wird
  public bool Zurueck()
  {
    if (!KannZurueck)
    {
      return false;
    }

    _stapel.Pop();
    Geaendert();
    return true;
  }

  private void Geaendert()
  {
    OnPropertyChanged(nameof(AktuelleSeite));
    OnPropertyChanged(nameof(KannZurueck));
  }
}
