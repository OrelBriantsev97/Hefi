using ZXing.Net.Maui;            // BarcodeReaderOptions, BarcodeFormats, BarcodeDetectionEventArgs
using ZXing.Net.Maui.Controls;   // CameraBarcodeReaderView

namespace Hefi.Mobile.Pages;

/// <summary>
/// enables barcode scanning using the device camera. allowing users to scan
/// product barcodes and retrieve food data automatically.
/// </summary>
public partial class BarcodeScanPage : ContentPage
{
    private TaskCompletionSource<string?> _tcs = new();

    // Initializes the barcode scanning page, sets up reader options,
    public BarcodeScanPage()
    {
        InitializeComponent();

        CameraView.Options = new BarcodeReaderOptions
        {
            Formats    = BarcodeFormats.OneDimensional | BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple   = false
        };
    }
    public Task<string?> WaitForResultAsync() => _tcs.Task;

    // starts camera detaction when page appears
    protected override void OnAppearing()
    {
        base.OnAppearing();
        CameraView.IsDetecting = true;
    }

    // stop camera detection when page disappears
    protected override void OnDisappearing()
    {
        CameraView.IsDetecting = false;
        base.OnDisappearing();
    }

    // Triggered whenever a barcode is successfully detected by the camera.
    // retrieves the first barcode value, resolves the task, and closes the page.
    void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var value = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(value)) return;

        CameraView.IsDetecting = false;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _tcs.TrySetResult(value);
            await Navigation.PopAsync();
        });
    }

    // cancel scanning and close the page
    async void OnCancel(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopAsync();
    }

    // Toggles the device's flashlight to improve scanning in low-light
    async void OnTorch(object sender, EventArgs e)
    {
        CameraView.IsTorchOn = !CameraView.IsTorchOn;
        await Task.CompletedTask;
    }

    // Switches between the front and rear cameras.
    async void OnFlip(object sender, EventArgs e)
    {
        CameraView.CameraLocation =
            CameraView.CameraLocation == CameraLocation.Rear
            ? CameraLocation.Front
            : CameraLocation.Rear;
        await Task.CompletedTask;
    }
}
