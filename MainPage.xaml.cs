#if ANDROID
using Cattura.SDK.Test.Services;
using ZebraRecognitionMode = IT.Suntech.Cattura.SDK.RecognitionMode;
using ZebraSelectionMode = IT.Suntech.Cattura.SDK.SelectionMode;
#endif

namespace Cattura.SDK.Test;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
#if ANDROID
        // SDK già inizializzata da MainActivity.OnCreate(this) con Activity context
        // (triggera l'auto pre-load dei decoder in background). Qui solo refresh UI.
        DeviceIdLabel.Text = CatturaSdkServiceAndroid.GetDeviceId();
#else
        DeviceIdLabel.Text = "N/A (Android only)";
#endif
    }

    private void OnShowDeviceIdClicked(object sender, EventArgs e)
    {
#if ANDROID
        var activity = Platform.CurrentActivity!;
        CatturaSdkServiceAndroid.ShowDeviceId(activity);
#endif
    }

    // Minimum OCR confidence threshold set by the slider (0-100).
    // 0 = no filter. Applied to OCR and "Both" scans.
    private int _minOcrConfidence = 0;

    // Overlay label sizes (sp) — applied to every scan via CatturaConfig.
    private int _barcodeLabelSize = 18;
    private int _ocrLabelSize = 18;

    private void OnOcrConfidenceChanged(object sender, ValueChangedEventArgs e)
    {
        _minOcrConfidence = (int)Math.Round(e.NewValue);
        OcrConfLabel.Text = $"OCR confidence threshold: {_minOcrConfidence}%";
    }

    private void OnBarcodeSizeChanged(object sender, ValueChangedEventArgs e)
    {
        _barcodeLabelSize = (int)Math.Round(e.NewValue);
        BarcodeSizeLabel.Text = $"Barcode label: {_barcodeLabelSize} sp";
    }

    private void OnOcrSizeChanged(object sender, ValueChangedEventArgs e)
    {
        _ocrLabelSize = (int)Math.Round(e.NewValue);
        OcrSizeLabel.Text = $"OCR label: {_ocrLabelSize} sp";
    }

    // ---------- BARCODE ----------
    private async void OnBarcodeAutoClicked(object sender, EventArgs e) =>
        await Scan(ZebraRecognitionMode.Barcode, ZebraSelectionMode.Automatic);

    private async void OnBarcodeAccumulateClicked(object sender, EventArgs e) =>
        await Scan(ZebraRecognitionMode.Barcode, ZebraSelectionMode.Accumulate);

    private async void OnBarcodeManualClicked(object sender, EventArgs e) =>
        await Scan(ZebraRecognitionMode.Barcode, ZebraSelectionMode.Manual);

    // Counts the objects in the scene (copies with the same code included):
    // "SEND ALL" returns one CatturaResult per counted copy.
    private async void OnBarcodeSceneCountClicked(object sender, EventArgs e) =>
        await Scan(ZebraRecognitionMode.Barcode, ZebraSelectionMode.SceneCount);

    // ---------- OCR ----------
    private async void OnOcrAutoClicked(object sender, EventArgs e) =>
        await Scan(ZebraRecognitionMode.Ocr, ZebraSelectionMode.Automatic);

    private async void OnOcrAccumulateClicked(object sender, EventArgs e) =>
        await Scan(ZebraRecognitionMode.Ocr, ZebraSelectionMode.Accumulate);

    private async void OnOcrManualClicked(object sender, EventArgs e) =>
        await Scan(ZebraRecognitionMode.Ocr, ZebraSelectionMode.Manual);

    // ---------- BOTH ----------
    private async void OnBothAutoClicked(object sender, EventArgs e) =>
        await Scan(ZebraRecognitionMode.Both, ZebraSelectionMode.Automatic);

    private async void OnBothAccumulateClicked(object sender, EventArgs e) =>
        await Scan(ZebraRecognitionMode.Both, ZebraSelectionMode.Accumulate);

    private async void OnBothManualClicked(object sender, EventArgs e) =>
        await Scan(ZebraRecognitionMode.Both, ZebraSelectionMode.Manual);

#if ANDROID
    private async Task Scan(ZebraRecognitionMode recog, ZebraSelectionMode selection)
    {
        ResultLabel.Text = "Scanning...";
        try
        {
            var activity = Platform.CurrentActivity!;
            int minConf = _minOcrConfidence;
            var results = await CatturaSdkServiceAndroid.ScanAsync(
                activity,
                recog,
                selection,
                configureMore: cfg =>
                {
                    // Apply OCR confidence threshold only when OCR is actually in use
                    // (Ocr or Both mode).
                    if (recog == ZebraRecognitionMode.Ocr || recog == ZebraRecognitionMode.Both)
                    {
                        cfg.OcrFilter.MinConfidencePercent = minConf;
                    }
                    // Overlay label sizes — barcode green boxes vs OCR blue boxes
                    cfg.BarcodeOverlayLabelSizeSp = _barcodeLabelSize;
                    cfg.OcrOverlayLabelSizeSp = _ocrLabelSize;
                });

            if (results.Count == 0)
            {
                ResultLabel.Text = "No results";
                return;
            }

            if (results.Count == 1)
            {
                var r = results[0];
                ResultLabel.Text =
                    $"✓ Single result\n\n" +
                    $"Type: {r.Type}\n" +
                    $"Symbology: {r.Symbology ?? "-"}\n" +
                    $"Value: {r.Value}\n" +
                    $"Confidence: {(int)(r.Confidence * 100)}%";
            }
            else
            {
                var lines = new List<string> { $"✓ {results.Count} results (SEND ALL)\n" };
                for (int i = 0; i < results.Count; i++)
                {
                    var r = results[i];
                    var conf = $"{(int)(r.Confidence * 100)}%";
                    var tag = r.Symbology ?? "OCR";
                    lines.Add($"{i + 1}. [{tag}] {r.Value}  ({conf})");
                }
                ResultLabel.Text = string.Join("\n", lines);
            }
        }
        catch (OperationCanceledException)
        {
            ResultLabel.Text = "Scan cancelled by user";
        }
        catch (CatturaSdkServiceAndroid.SdkNotAvailableException ex)
        {
            ResultLabel.Text =
                "⚠ Device not supported\n\n" +
                "This device does not have the Zebra AI DataCapture SDK runtime " +
                "(Hexagon HTP). Use a TC53, TC58, TC73 or TC78.\n\n" +
                $"Details: {ex.Message}";
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"⚠ Error: {ex.Message}";
        }
    }
#else
    private Task Scan(int recog, int selection)
    {
        ResultLabel.Text = "Cattura SDK is only available on Android";
        return Task.CompletedTask;
    }

    // Stub enums for shared code (used on non-Android only)
    private static class ZebraRecognitionMode { public const int Barcode = 0, Ocr = 1, Both = 2; }
    private static class ZebraSelectionMode { public const int Automatic = 0, Accumulate = 1, Manual = 2, SceneCount = 3; }
#endif
}
