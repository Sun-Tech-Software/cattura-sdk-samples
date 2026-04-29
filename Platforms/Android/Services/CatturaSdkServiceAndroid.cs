using Android.App;
using Android.Content;
using IT.Suntech.Cattura.SDK;
using Java.Interop;
using AndroidObject = Java.Lang.Object;
using ZebraRecognitionMode = IT.Suntech.Cattura.SDK.RecognitionMode;
using ZebraSelectionMode = IT.Suntech.Cattura.SDK.SelectionMode;

namespace Cattura.SDK.Test.Services;

/// <summary>
/// C# helper that exposes an idiomatic async/Task API for calling the Kotlin
/// Cattura SDK library from cross-platform MAUI code.
///
/// The Kotlin library exposes static methods on the CatturaScanner object.
/// C# sees them via binding as static methods of the generated class.
/// </summary>
public static class CatturaSdkServiceAndroid
{
    private static bool _initialized;

    // =========================================================================
    // LICENSE: replace with the real JWT provided by Suntech.
    // Leave null for internal use/testing (no device verification).
    //
    // Obtain the JWT by sending your device serials to Suntech:
    //   adb shell getprop ro.serialno
    //   or: adb shell settings get secure android_id
    // =========================================================================
    // Use "DEMO" for demo mode (results with random asterisks),
    // or the real JWT provided by Suntech for production use.
    private static readonly string LicenseJwt = "DEMO";
    // =========================================================================

    /// <summary>
    /// Initializes the SDK with license verification if <see cref="LicenseJwt"/> is set.
    /// Pass an Activity context so the SDK can auto-pre-load the AI decoders
    /// in background (the Activity classloader is required for JNI on MAUI Mono).
    /// </summary>
    /// <exception cref="IT.Suntech.Cattura.SDK.LicenseException">
    /// Thrown if the JWT is invalid, expired, or the device is not authorized.
    /// </exception>
    public static void InitializeIfNeeded(Activity? activity = null)
    {
        if (_initialized) return;
        _initialized = true;
        // Prefer Activity context: triggers auto pre-load of decoders.
        // Fallback to Application.Context (no auto pre-load — first Scan
        // will show the "Initializing AI engine" overlay).
        Android.Content.Context context = activity ?? (Android.Content.Context)Android.App.Application.Context;

        CatturaScanner.Initialize(context, LicenseJwt);
    }

    /// <summary>
    /// Pre-loads the AI decoders in background so the first scan starts the
    /// camera immediately. Must be called from an Activity context (not from
    /// Application) so JNI gets the correct classloader.
    /// </summary>
    public static void PreloadDecoders(Activity activity)
    {
        if (!_initialized) InitializeIfNeeded(activity);
        CatturaScanner.PreloadDecoders(activity);
    }

    /// <summary>
    /// Starts a scan. Always returns a list of results:
    /// - 1 item for AUTOMATIC/MANUAL or a single selection in ACCUMULATE mode
    /// - N items for ACCUMULATE when the user taps "SEND ALL"
    ///
    /// Throws <see cref="OperationCanceledException"/> if the user cancels,
    /// and <see cref="Exception"/> for SDK errors.
    /// </summary>
    public static Task<IList<CatturaResult>> ScanAsync(
        Activity activity,
        ZebraRecognitionMode recognitionMode,
        ZebraSelectionMode selectionMode,
        Action<CatturaConfig>? configureMore = null)
    {
        InitializeIfNeeded();

        var config = new CatturaConfig
        {
            RecognitionMode = recognitionMode,
            SelectionMode = selectionMode
        };
        configureMore?.Invoke(config);

        CatturaScanner.Configure(config);

        var tcs = new TaskCompletionSource<IList<CatturaResult>>();
        CatturaScanner.Scan(activity, new ScanCallback(tcs));
        return tcs.Task;
    }

    /// <summary>Cancels an ongoing scan.</summary>
    public static void Cancel() => CatturaScanner.Cancel();

    /// <summary>
    /// Returns the device identifier used by the SDK for license validation.
    /// Send this value to Suntech when requesting a license activation.
    /// On Android 14 / Zebra TC53 this is typically the ANDROID_ID.
    /// Verify via ADB: <c>adb shell settings get secure android_id</c>
    /// </summary>
    public static string GetDeviceId()
    {
        var context = Android.App.Application.Context;
        return CatturaScanner.GetDeviceId(context);
    }

    /// <summary>
    /// Shows an AlertDialog with device model and device ID, plus a Copy button.
    /// Also logs both values to logcat (tag: Cattura.SDK).
    /// </summary>
    public static void ShowDeviceId(Activity activity)
    {
        CatturaScanner.ShowDeviceId(activity);
    }

    /// <summary>Releases SDK resources.</summary>
    public static void Dispose()
    {
        CatturaScanner.Dispose();
        _initialized = false;
    }

    /// <summary>
    /// Thrown when the calling device does not support the Zebra AI DataCapture SDK
    /// (e.g. TC22 with a QCM5430 SoC — no Hexagon HTP). Detected upfront in
    /// <see cref="CatturaScanner.Scan"/> by inspecting <c>ro.soc.model</c>.
    /// </summary>
    public class SdkNotAvailableException : Exception
    {
        public SdkNotAvailableException(string message) : base(message) { }
    }

    /// <summary>
    /// Adapter that implements <see cref="ICatturaCallback"/> and propagates the result
    /// to a <see cref="TaskCompletionSource{TResult}"/>.
    /// </summary>
    private class ScanCallback : AndroidObject, ICatturaCallback
    {
        private readonly TaskCompletionSource<IList<CatturaResult>> _tcs;
        public ScanCallback(TaskCompletionSource<IList<CatturaResult>> tcs) => _tcs = tcs;

        public void OnResult(CatturaResult result)
            => _tcs.TrySetResult(new List<CatturaResult> { result });

        public void OnResults(IList<CatturaResult>? results)
            => _tcs.TrySetResult(results ?? new List<CatturaResult>());

        public void OnError(CatturaError? error)
        {
            // Distinguishes "device not supported" from generic SDK errors so the UI
            // can show a tailored message ("Use a TC53/TC58/TC73/TC78") instead of
            // a vague "Error".
            if (error?.Code == CatturaError.CodeSdkNotAvailable)
                _tcs.TrySetException(new SdkNotAvailableException(error.Message));
            else
                _tcs.TrySetException(new Exception(error?.Message ?? "Unknown error"));
        }

        public void OnCancelled() => _tcs.TrySetCanceled();
    }
}
