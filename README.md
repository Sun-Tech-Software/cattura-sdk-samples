# Cattura Tester

A minimal .NET MAUI sample app that demonstrates how to integrate the **[Suntech.Cattura.SDK](https://www.nuget.org/packages/Suntech.Cattura.SDK)** NuGet package into an Android application.

This project is the reference integration example for Suntech's Cattura SDK — it shows the complete usage pattern from initialization to result handling in just a few files.

---

## Requirements

| | |
|---|---|
| Platform | Android 13+ (API 33) — Zebra AI DataCapture SDK 4.1 requirement; the SDK 1.x line covers Android 11/12 |
| Target device | Zebra With AI |
| .NET | .NET 10 (MAUI) |
| IDE | Visual Studio 2022 17.12+ or JetBrains Rider |

> The app runs only on Android. The project structure is cross-platform MAUI but the SDK is Android-only.

---

## Getting Started

### 1. Clone and open

```bash
git clone https://github.com/Sun-Tech-Software/cattura-sdk-samples.git
cd cattura-sdk-samples
```

Open `Cattura.SDK.Test.csproj` in Visual Studio or Rider.

### 2. Restore NuGet packages

```bash
dotnet restore
```

This pulls `Suntech.Cattura.SDK` (~67 MB). The two Zebra AI models are **not** NuGet
packages: the SDK package declares them as `AndroidMavenLibrary` items, so the **first
build** downloads them from Zebra's public Maven repository and caches them per machine:
- `com.zebra.ai.models.vision:barcode-decoder:5.0.4` (~154 MB)
- `com.zebra.ai.models.vision:text-ocr-recognizer:2.9.0` (~281 MB)

> The first build needs network access to `zebratech.jfrog.io` and may take a few minutes.
> Barcode-only apps can set `<CatturaBundleOcrModel>false</CatturaBundleOcrModel>` in the
> `.csproj` to skip the OCR model; see the package README for offline builds.

### 3. Configure the license

Open `Platforms/Android/Services/CatturaSdkServiceAndroid.cs` and set the `LicenseJwt` field:

```csharp
// Use "DEMO" for demo mode (results are partially masked with asterisks)
private static readonly string LicenseJwt = "DEMO";

// Use a real JWT for production (contact info@suntechonline.it)
// private static readonly string LicenseJwt = "eyJ...";
```

In **DEMO mode** the app works on any device without a license — scan results are partially replaced with `*` characters to indicate the demo limitation.

To obtain a production license, send your device serial numbers to [info@suntechonline.it](mailto:info@suntechonline.it):

```bash
adb shell getprop ro.serialno
```

### 4. Deploy to a Zebra device

Connect your Zebra device via USB and run:

```bash
dotnet build Cattura.SDK.Test.csproj -f net10.0-android36.0 -t:Run
```

---

## Project Structure

```
Cattura.SDK.Test/
├── MainPage.xaml              # UI — scan buttons and result display
├── MainPage.xaml.cs           # Button handlers and result formatting
├── Platforms/
│   └── Android/
│       └── Services/
│           └── CatturaSdkServiceAndroid.cs   # SDK wrapper (async/Task API)
└── Cattura.SDK.Test.csproj       # Project file with NuGet reference
```

The entire integration is contained in **`CatturaSdkServiceAndroid.cs`** — a single static helper class that wraps the Kotlin SDK callback model into a clean C# `Task<IList<CatturaResult>>` API.

---

## How It Works

### Initialization

The SDK is initialized once when the main page appears:

```csharp
CatturaScanner.Initialize(context, LicenseJwt);
```

### Starting a scan

A scan is started by calling `CatturaScanner.Scan()` and bridging the Kotlin callback to a `TaskCompletionSource`:

```csharp
var results = await CatturaSdkServiceAndroid.ScanAsync(
    activity,
    RecognitionMode.Ocr,
    SelectionMode.Automatic);
```

The scanner opens a fullscreen overlay on the device camera. The user points the camera at the target and the SDK returns results automatically (or after user confirmation, depending on the selection mode).

### Recognition modes

| Mode | Description |
|------|-------------|
| `Barcode` | Reads 1D/2D barcodes (Code 128, QR, Data Matrix, PDF417, and 40+ more) |
| `Ocr` | Reads printed or embossed text using the AI OCR model |
| `Both` | Reads barcodes and text simultaneously |

### Selection modes

| Mode | Description |
|------|-------------|
| `Automatic` | Returns the first recognized item immediately |
| `Accumulate` | Lets the user collect multiple items; confirms with "SEND ALL" |
| `Manual` | Freezes the frame and lets the user tap the item to send |
| `SceneCount` | Counts the objects in the scene (barcodes only), copies with the same code included; "SEND ALL" returns one result per counted copy |

### OCR confidence filter

The UI exposes a slider (0–100%) to set a minimum OCR confidence threshold. Items below the threshold are discarded before being returned to the app:

```csharp
cfg.OcrFilter.MinConfidencePercent = 75; // discard results below 75% confidence
```

Set to `0` to disable filtering and receive all results regardless of confidence.

### Result format

Each `CatturaResult` exposes:

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `string` | The decoded text or barcode value |
| `Type` | `ResultType` | `Barcode` or `Ocr` |
| `Symbology` | `string?` | Barcode symbology (e.g. `"Code128"`) — `null` for OCR |
| `Confidence` | `float` | Recognition confidence in [0, 1] |

---

## NuGet Package

This app consumes the SDK via NuGet. The only reference needed in your `.csproj` is:

```xml
<PackageReference Include="Suntech.Cattura.SDK" Version="2.0.1" />
```

The Zebra AI models are downloaded at the first build from Zebra's Maven repository (see above).

For more details on the full API, see the [Suntech.Cattura.SDK README](https://www.nuget.org/packages/Suntech.Cattura.SDK).

---

## License

This sample project is provided by [Suntech Srl](https://www.suntechonline.it) for integration reference purposes.  
The Cattura SDK itself is a commercial product — see the NuGet package license for details.

For support or licensing inquiries: [info@suntechonline.it](mailto:info@suntechonline.it)
