namespace PoGoExchange.Services;

// Pruefung der freien Texte fuer Kostuem und Hintergrund,
// gemeinsam genutzt von "Habe ich" und "Suche ich"
public static class Freitext
{
  // Liefert null, wenn der Text als Kostuem- oder Hintergrundbeschreibung erlaubt ist
  public static string? Fehler(string text)
  {
    if (text.Contains('"'))
    {
      return "Anf\u00fchrungszeichen sind nicht erlaubt";
    }

    if (text.Contains('\n') || text.Contains('\r'))
    {
      return "Zeilenumbr\u00fcche sind nicht erlaubt";
    }

    return null;
  }

  // Erster Fehler in den freien Texten mit vorangestelltem Merkmal,
  // "" wenn alles erlaubt ist. Texte zaehlen nur bei gesetztem Haken.
  public static string MerkmaleFehler(
    bool kostuemAktiv,
    string kostuemText,
    bool hintergrundAktiv,
    string hintergrundText)
  {
    if (kostuemAktiv)
    {
      var fehler = Fehler(kostuemText);
      if (fehler != null)
      {
        return "Kost\u00fcm: " + fehler;
      }
    }

    if (hintergrundAktiv)
    {
      var fehler = Fehler(hintergrundText);
      if (fehler != null)
      {
        return "Hintergrund: " + fehler;
      }
    }

    return "";
  }
}
