using Android.App;
using Android.Content.PM;
using Android.OS;
using Cattura.SDK.Test.Services;

namespace Cattura.SDK.Test;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Android.Util.Log.Info("Cattura.SDK.Test", $"MainActivity.OnCreate START — this={this.GetType().Name}");
        base.OnCreate(savedInstanceState);
        Android.Util.Log.Info("Cattura.SDK.Test", "MainActivity.OnCreate after base.OnCreate — chiamo InitializeIfNeeded(this)");
        // Inizializza il SDK Zebra AI passando QUESTA Activity (non Application.Context)
        // cosi' la SDK auto-precarica i decoder in background e il primo Scan apre
        // la camera istantaneamente — il classloader corretto e' garantito da
        // OnCreate dell'Activity (vincolo MAUI Mono).
        CatturaSdkServiceAndroid.InitializeIfNeeded(this);
        Android.Util.Log.Info("Cattura.SDK.Test", "MainActivity.OnCreate END");
    }
}
