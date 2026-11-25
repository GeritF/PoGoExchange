using System;
using System.Threading.Tasks;

namespace PoGoExchange;

public static class AppActions
{
  // Wird vom jeweiligen Plattformprojekt gesetzt
  public static Action? CloseApp { get; set; }

  // Text in die Zwischenablage kopieren; wird von der Oberflaeche (MainView) gesetzt
  public static Func<string, Task>? InZwischenablageKopieren { get; set; }

  // Text aus der Zwischenablage lesen; wird von der Oberflaeche (MainView) gesetzt
  public static Func<Task<string?>>? AusZwischenablageLesen { get; set; }
}
