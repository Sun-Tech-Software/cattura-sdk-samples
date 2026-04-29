using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace Cattura.SDK.Test;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            })
#if ANDROID
            // Inizializza il SDK Zebra AI dall'OnCreate dell'Activity MAUI: cosi'
            // il context passato e' un'Activity reale (richiesto per il classloader
            // corretto su MAUI Mono → triggera l'auto-preload dei decoder).
            .ConfigureLifecycleEvents(lc => lc.AddAndroid(android =>
            {
                android.OnCreate((activity, _) =>
                {
                    Android.Util.Log.Info("Cattura.SDK.Test",
                        $"Lifecycle.OnCreate activity={activity.GetType().Name}");
                    Cattura.SDK.Test.Services.CatturaSdkServiceAndroid.InitializeIfNeeded(activity);
                });
            }))
#endif
            ;

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
