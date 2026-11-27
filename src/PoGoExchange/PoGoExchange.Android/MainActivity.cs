using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace PoGoExchange.Android;

[Activity(
  Label = "PoGoExchange.Android",
  Theme = "@style/MyTheme.NoActionBar",
  Icon = "@drawable/icon",
  MainLauncher = true,
  ConfigurationChanges =
    ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
  protected override void OnCreate(global::Android.OS.Bundle? savedInstanceState)
  {
    base.OnCreate(savedInstanceState);
    AppActions.CloseApp = () => FinishAndRemoveTask();
  }
}
