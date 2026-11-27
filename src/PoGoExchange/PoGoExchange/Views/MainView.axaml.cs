using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PoGoExchange.ViewModels;
using System;
using System.IO;

namespace PoGoExchange.Views;

public partial class MainView : UserControl
{
  private TopLevel? _topLevel;

  public MainView()
  {
    InitializeComponent();
  }

  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnAttachedToVisualTree(e);
    _topLevel = TopLevel.GetTopLevel(this);
    if (_topLevel == null)
    {
      return;
    }

    _topLevel.BackRequested += ZurueckAngefordert;
    if (_topLevel.InputPane != null)
    {
      _topLevel.InputPane.StateChanged += TastaturGeaendert;
    }

    var zwischenablage = _topLevel.Clipboard;
    AppActions.InZwischenablageKopieren = zwischenablage == null
      ? null
      : zwischenablage.SetTextAsync;
    AppActions.AusZwischenablageLesen = zwischenablage == null
      ? null
      : zwischenablage.TryGetTextAsync;

    var speicher = _topLevel.StorageProvider;
    var zipTyp = new FilePickerFileType("ZIP-Datei")
    {
      Patterns = ["*.zip"],
      MimeTypes = ["application/zip", "application/x-zip-compressed"]
    };
    AppActions.DateiSpeichern = async (name, inhalt) =>
    {
      // Android haengt die Endung selbst an (sonst "abc.zip.zip"),
      // daher dort ohne Endung vorschlagen
      if (
        OperatingSystem.IsAndroid() &&
        name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
      )
      {
        name = name[..^4];
      }

      var datei =
        await speicher.SaveFilePickerAsync(
          new FilePickerSaveOptions {
            SuggestedFileName = name,
            DefaultExtension = "zip",
            FileTypeChoices = [zipTyp]
          }
        );
      if (datei == null)
      {
        return false;
      }

      await using var strom = await datei.OpenWriteAsync();
      await strom.WriteAsync(inhalt);
      return true;
    };
    AppActions.DateiOeffnen = async () =>
    {
      var dateien =
        await speicher.OpenFilePickerAsync(
          new FilePickerOpenOptions {
            AllowMultiple = false,
            FileTypeFilter = [zipTyp]
          }
        );
      if (dateien.Count == 0)
      {
        return null;
      }

      await using var strom = await dateien[0].OpenReadAsync();
      using var puffer = new MemoryStream();
      await strom.CopyToAsync(puffer);
      return puffer.ToArray();
    };
  }

  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
  {
    if (_topLevel != null)
    {
      _topLevel.BackRequested -= ZurueckAngefordert;
      if (_topLevel.InputPane != null)
      {
        _topLevel.InputPane.StateChanged -= TastaturGeaendert;
      }

      _topLevel = null;
    }

    Inhalt.Margin = default;

    base.OnDetachedFromVisualTree(e);
  }

  // Android-Zurueck-Taste bzw. -Geste: wie der Zurueck-Button. Auf der
  // Hauptseite wird sie abgefangen, damit die App nicht versehentlich
  // verlassen wird (dafuer gibt es den Beenden-Button).
  private void ZurueckAngefordert(object? sender, RoutedEventArgs e)
  {
    if (DataContext is MainViewModel vm)
    {
      vm.Navigation.Zurueck();
    }

    e.Handled = true;
  }

  // Bildschirmtastatur: Avalonia verkleinert die Ansicht nicht selbst. Deshalb
  // bekommt der Inhalt unten einen Abstand in Hoehe des verdeckten Teils. So
  // liegen alle Seiten oberhalb der Tastatur und lassen sich bis zum Ende
  // scrollen. Der Abstand wird gegen die Unterkante des Inhalts gerechnet,
  // damit eine Navigationsleiste nicht doppelt zaehlt.
  // Wichtig: Nicht Padding der MainView setzen. Dort liegt auf Android der
  // Abstand fuer Status- und Navigationsleiste; ueberschrieben rutscht der
  // Inhalt unter die Statusleiste (beobachtet auf dem S24).
  // Gemessen auf Samsung Galaxy S24 (Android 17, One UI 9.0): Der verdeckte
  // Bereich endet ueber der Navigationsleiste, wie der Inhalt.
  private void TastaturGeaendert(object? sender, InputPaneStateEventArgs e)
  {
    Inhalt.Margin = new Thickness(0, 0, 0, TastaturAbstand(e.NewState, e.EndRect));
  }

  private double TastaturAbstand(InputPaneState zustand, Rect verdeckt)
  {
    // Leerer Bereich: schwebende Tastatur ueber der Ansicht, kein Abstand
    if (zustand != InputPaneState.Open || verdeckt.Height <= 0 || _topLevel == null)
    {
      return 0;
    }

    // Unterkante des Inhalts; liegt bei schon gesetztem Abstand um diesen
    // hoeher (die Margin liegt ausserhalb von Bounds)
    var unten = Inhalt.TranslatePoint(new Point(0, Inhalt.Bounds.Height), _topLevel);
    if (unten == null)
    {
      return 0;
    }

    // Bisherigen Abstand dazurechnen: Unterkante, wie sie ohne Abstand waere
    return Math.Max(0, unten.Value.Y + Inhalt.Margin.Bottom - verdeckt.Y);
  }
}
