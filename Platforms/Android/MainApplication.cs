using Android;
using Android.App;
using Android.Content.PM;
using Android.Runtime;

// Явно объявляем разрешение и возможности камеры в манифесте,
// чтобы в Release Android/ZXing могли корректно инициализировать камеру
[assembly: UsesPermission(Manifest.Permission.Camera)]
[assembly: UsesFeature("android.hardware.camera", Required = false)]
[assembly: UsesFeature("android.hardware.camera.autofocus", Required = false)]

namespace YessGoFront;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
