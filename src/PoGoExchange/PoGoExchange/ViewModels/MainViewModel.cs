using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PoGoExchange.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] public partial string Greeting { get; set; } = "Welcome to PoGoExchange!";
    
    [RelayCommand]
    private void Close() => AppActions.CloseApp?.Invoke();
}