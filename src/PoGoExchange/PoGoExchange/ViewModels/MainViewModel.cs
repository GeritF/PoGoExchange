using CommunityToolkit.Mvvm.ComponentModel;

namespace PoGoExchange.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] public partial string Greeting { get; set; } = "Welcome to PoGoExchange!";
}