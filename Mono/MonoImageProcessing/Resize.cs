using BenchmarkDotNet.Attributes;
using SkiaSharp;

namespace MonoImageProcessing
{
    public class Resize
    {
        const int Width = 1280;
        const int Height = 853;
        const int ResizedWidth = 150;
        const int ResizedHeight = 99;

        [Benchmark(Description = "SkiaSharp Canvas Resize")]
        public void SkiaCanvasResizeBenchmark()
        {
            var original = new SKBitmap(Width, Height);
            var surface = SKSurface.Create(new SKImageInfo(ResizedWidth, ResizedHeight));
            var canvas = surface.Canvas;
            var scale = (float)ResizedWidth / Width;
            canvas.Scale(scale);
            var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            canvas.DrawBitmap(original, 0, 0, paint);
            canvas.Flush();
        }

        [Benchmark(Description = "SkiaSharp Bitmap Resize")]
        public void SkiaBitmapResizeBenchmark()
        {
            var original = new SKBitmap(Width, Height);
            var resized = original.Resize(new SKImageInfo(ResizedWidth, ResizedHeight), SKBitmapResizeMethod.Lanczos3);
            var image = SKImage.FromBitmap(resized);
        }
    }
}