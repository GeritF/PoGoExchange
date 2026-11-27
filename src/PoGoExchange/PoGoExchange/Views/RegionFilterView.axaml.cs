using Avalonia;
using Avalonia.Controls;

namespace PoGoExchange.Views;

public partial class RegionFilterView : UserControl
{
  // Breite der Beschriftung "Region:", z.B. um die Auswahl mit anderen Zeilen
  // der Seite auszurichten ("+ Mehrere"). Standard: nach Inhalt (NaN).
  public static readonly StyledProperty<double> BeschriftungsBreiteProperty =
    AvaloniaProperty.Register<RegionFilterView, double>(
      nameof(BeschriftungsBreite), double.NaN);

  public RegionFilterView()
  {
    InitializeComponent();
    Beschriftung.Width = BeschriftungsBreite;
  }

  public double BeschriftungsBreite
  {
    get => GetValue(BeschriftungsBreiteProperty);
    set => SetValue(BeschriftungsBreiteProperty, value);
  }

  protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
  {
    base.OnPropertyChanged(change);
    if (change.Property == BeschriftungsBreiteProperty)
    {
      Beschriftung.Width = BeschriftungsBreite;
    }
  }
}
