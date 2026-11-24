using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PoGoExchange.ViewModels;
using Avalonia.Input.Platform;

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
