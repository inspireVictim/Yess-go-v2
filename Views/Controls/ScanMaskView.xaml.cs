using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace YessGoFront.Views.Controls;

public partial class ScanMaskView : ContentView
{
    public ScanMaskView()
    {
        InitializeComponent();
    }

    private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
    {
        SKImageInfo info = args.Info;
        SKSurface surface = args.Surface;
        SKCanvas canvas = surface.Canvas;

        // First, draw the entire screen black
        canvas.Clear(SKColors.Black);

        // Get the scale factor to convert from MAUI logical units to pixels
        var view = sender as SKCanvasView;
        float scale = 1f;
        if (view != null && view.Width > 0)
        {
            scale = (float)(view.CanvasSize.Width / view.Width);
            if (float.IsNaN(scale) || scale <= 0) scale = 1f;
        }
        
        // Define the transparent hole in the center (matching the border size exactly)
        // Border has WidthRequest="280" and HeightRequest="280" in MAUI logical units
        // Convert to pixels using the scale factor
        const float holeSizeLogical = 280f;
        const float cornerRadiusLogical = 16f;
        
        float holeSize = holeSizeLogical * scale;
        float cornerRadius = cornerRadiusLogical * scale;
        
        float centerX = info.Width / 2f;
        float centerY = info.Height / 2f;
        
        // Create rectangle exactly matching the Border size
        SKRect holeRect = SKRect.Create(
            centerX - holeSize / 2f,
            centerY - holeSize / 2f,
            holeSize,
            holeSize
        );

        // Clear the hole with rounded corners using BlendMode.Clear
        using (var paint = new SKPaint 
        { 
            Color = SKColors.Transparent, 
            BlendMode = SKBlendMode.Clear,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        })
        {
            using (var path = new SKPath())
            {
                var rrect = new SKRoundRect(holeRect, cornerRadius, cornerRadius);
                path.AddRoundRect(rrect);
                canvas.DrawPath(path, paint);
            }
        }
    }
}
