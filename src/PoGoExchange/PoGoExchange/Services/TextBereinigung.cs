using System.Globalization;

namespace PoGoExchange.Services;

public static class TextBereinigung
{
  // Entfernt am Anfang und Ende alle nicht druckbaren Zeichen:
  // Leerraum, Steuerzeichen und unsichtbare Formatzeichen (z.B. U+200B).
  // Zeichen innerhalb des Textes bleiben unveraendert.
  public static string Bereinigen(string? text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return "";
    }

    var start = 0;
    var ende = text.Length - 1;
    while (start <= ende && IstNichtDruckbar(text[start]))
    {
      start++;
    }

    while (ende >= start && IstNichtDruckbar(text[ende]))
    {
      ende--;
    }

    return text.Substring(start, ende - start + 1);
  }

  private static bool IstNichtDruckbar(char c) =>
    char.IsWhiteSpace(c)
    || char.IsControl(c)
    || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format;
}
