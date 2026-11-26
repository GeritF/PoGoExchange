using Avalonia;
using Avalonia.Controls;
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
    if (_topLevel != null)
    {
      _topLevel.BackRequested += ZurueckAngefordert;
      var zwischenablage = _topLevel.Clipboard;
      AppActions.InZwischenablageKopieren = zwischenablage == null
        ? null
        : text => zwischenablage.SetTextAsync(text);
      AppActions.AusZwischenablageLesen = zwischenablage == null
        ? null
        : () => zwischenablage.TryGetTextAsync();

      var speicher = _topLevel.StorageProvider;
      var zipTyp = new FilePickerFileType("ZIP-Datei")
      {
        Patterns = new[] { "*.zip" },
        MimeTypes = new[] { "application/zip", "application/x-zip-compressed" }
      };
      AppActions.DateiSpeichern = async (name, inhalt) =>
      {
        // Android haengt die Endung selbst an (sonst "abc.zip.zip"),
        // daher dort ohne Endung vorschlagen
        if (OperatingSystem.IsAndroid() && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
          name = name[..^4];
        }
        var datei = await speicher.SaveFilePickerAsync(new FilePickerSaveOptions
        {
          SuggestedFileName = name,
          DefaultExtension = "zip",
          FileTypeChoices = new[] { zipTyp }
        });
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
        var dateien = await speicher.OpenFilePickerAsync(new FilePickerOpenOptions
        {
          AllowMultiple = false,
          FileTypeFilter = new[] { zipTyp }
        });
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
  }

  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
  {
    if (_topLevel != null)
    {
      _topLevel.BackRequested -= ZurueckAngefordert;
      _topLevel = null;
    }

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
}
