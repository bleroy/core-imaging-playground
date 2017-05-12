using BenchmarkDotNet.Attributes;

using ImageMagick;
using ImageSharp;
using FreeImageAPI;
using PhotoSauce.MagicScaler;
using SkiaSharp;
using System.Runtime.InteropServices;

using ImageSharpImage = ImageSharp.Image<ImageSharp.Rgba32>;
using ImageSharpSize = ImageSharp.Size;

namespace ImageProcessing
{
    public class Resize
    {
        const int Width = 1280;
        const int Height = 853;
        const int ResizedWidth = 150;
        const int ResizedHeight = 99;

        public Resize()
        {
            OpenCL.IsEnabled = false;
        }

        [Benchmark(Description = "ImageSharp Resize")]
        public ImageSharpSize ResizeImageSharp()
        {
            using (var image = new ImageSharpImage(Width, Height))
            {
                image.Resize(ResizedWidth, ResizedHeight);
            }
            return new ImageSharpSize(ResizedWidth, ResizedHeight);
        }

        [Benchmark(Description = "ImageMagick Resize")]
        public MagickGeometry MagickResize()
        {
            MagickGeometry size = new MagickGeometry(ResizedWidth, ResizedHeight);
            using (MagickImage image = new MagickImage(MagickColor.FromRgba(0, 0, 0, 0), Width, Height))
            {
                image.Resize(size);
            }

            return size;
        }

        [Benchmark(Description = "FreeImage Resize")]
        public void FreeImageResize()
        {
            using (var image = new FreeImageBitmap(Width, Height))
            {
                image.Rescale(ResizedWidth, ResizedHeight, FREE_IMAGE_FILTER.FILTER_BICUBIC);
            }
        }

        [Benchmark(Description = "MagicScaler Resize")]
        public void MagicScalerResize()
        {
            // stride is width * bytes per pixel (3 for BGR), rounded up to the nearest multiple of 4
            const uint stride = ResizedWidth * 3u + 3u & ~3u;
            const uint bufflen = ResizedHeight * stride;

            var pixels = new TestPatternPixelSource(Width, Height, PixelFormats.Bgr24bpp);
            var settings = new ProcessImageSettings {
                Width = ResizedWidth,
                Height = ResizedHeight
            };

            using (var pipeline = MagicImageProcessor.BuildPipeline(pixels, settings))
            {
                var rect = new System.Drawing.Rectangle(0, 0, ResizedWidth, ResizedHeight);
                var buffer = Marshal.AllocHGlobal((int)bufflen);
                pipeline.PixelSource.CopyPixels(rect, stride, bufflen, buffer);
                Marshal.FreeHGlobal(buffer);
            }
        }

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