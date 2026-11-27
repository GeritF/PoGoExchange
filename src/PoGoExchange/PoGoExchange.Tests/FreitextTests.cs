using PoGoExchange.Services;

namespace PoGoExchange.Tests;

public class FreitextTests
{
  [Theory]
  [InlineData("Partyhut", true)]
  [InlineData("Party \"Hut\"", false)]
  [InlineData("Party\nHut", false)]
  public void Fehler_NurAnfuehrungszeichenUndZeilenumbruchVerboten(string text, bool erlaubt)
  {
    Assert.Equal(erlaubt, Freitext.Fehler(text) == null);
  }

  [Theory]
  [InlineData(false, "Party \"Hut\"", false, "Strand\nNacht", "")]
  [InlineData(true, "Partyhut", true, "Strand", "")]
  [InlineData(true, "Party \"Hut\"", false, "",
    "Kost\u00fcm: Anf\u00fchrungszeichen sind nicht erlaubt")]
  [InlineData(false, "", true, "Strand\rNacht",
    "Hintergrund: Zeilenumbr\u00fcche sind nicht erlaubt")]
  [InlineData(true, "Party \"Hut\"", true, "Strand\nNacht",
    "Kost\u00fcm: Anf\u00fchrungszeichen sind nicht erlaubt")]
  public void MerkmaleFehler_PrueftNurAktiveTexteUndMeldetDenErsten(
    bool kostuemAktiv,
    string kostuemText,
    bool hintergrundAktiv,
    string hintergrundText,
    string erwartet)
  {
    var fehler = Freitext.MerkmaleFehler(
      kostuemAktiv, kostuemText, hintergrundAktiv, hintergrundText);
    Assert.Equal(erwartet, fehler);
  }
}
