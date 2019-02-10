using System;
using System.Buffers;
using System.Drawing;
using BenchmarkDotNet.Attributes;

using ImageMagick;
using FreeImageAPI;
using PhotoSauce.MagicScaler;
using SkiaSharp;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using ImageSharpImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using ImageSharpSize = SixLabors.Primitives.Size;

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
                image.Mutate(i => i.Resize(ResizedWidth, ResizedHeight));
                return image.Size();
            }
        }

        [Benchmark(Description = "ImageMagick Resize")]
        public MagickGeometry MagickResize()
        {
            MagickGeometry size = new MagickGeometry(ResizedWidth, ResizedHeight);
            using (MagickImage image = new MagickImage(MagickColor.FromRgba(0, 0, 0, 0), Width, Height))
            {
                image.Resize(size);
                return size;
            }
        }

        [Benchmark(Description = "FreeImage Resize")]
        public Size FreeImageResize()
        {
            using (var image = new FreeImageBitmap(Width, Height))
            {
                image.Rescale(ResizedWidth, ResizedHeight, FREE_IMAGE_FILTER.FILTER_BICUBIC);
                return image.Size;
            }
        }

        [Benchmark(Description = "MagicScaler Resize")]
        public Size MagicScalerResize()
        {
            // stride is width * bytes per pixel (3 for BGR), rounded up to the nearest multiple of 4
            const uint stride = ResizedWidth * 3u + 3u & ~3u;
            const uint bufflen = ResizedHeight * stride;

            var settings = new ProcessImageSettings
            {
                Width = ResizedWidth,
                Height = ResizedHeight,
                Sharpen = false
            };

            using (var pixels = new TestPatternPixelSource(Width, Height, PixelFormats.Bgr24bpp))
            using (var pipeline = MagicImageProcessor.BuildPipeline(pixels, settings))
            {
                var rect = new Rectangle(0, 0, ResizedWidth, ResizedHeight);
                var buffer = ArrayPool<byte>.Shared.Rent((int)bufflen);
                pipeline.PixelSource.CopyPixels(rect, (int)stride, new Span<byte>(buffer, 0, (int)bufflen));
                ArrayPool<byte>.Shared.Return(buffer);
                return rect.Size;
            }
        }

        [Benchmark(Description = "SkiaSharp Canvas Resize")]
        public SKSize SkiaCanvasResizeBenchmark()
        {
            using (var original = new SKBitmap(Width, Height))
            using (var surface = SKSurface.Create(new SKImageInfo(ResizedWidth, ResizedHeight)))
            using (var paint = new SKPaint())
            {
                var canvas = surface.Canvas;
                var scale = (float)ResizedWidth / Width;
                canvas.Scale(scale);

                paint.FilterQuality = SKFilterQuality.High;
                canvas.DrawBitmap(original, 0, 0, paint);
                canvas.Flush();

                return new SKSize(canvas.DeviceClipBounds.Width, canvas.DeviceClipBounds.Height);
            }
        }

        [Benchmark(Description = "SkiaSharp Bitmap Resize")]
        public SKSize SkiaBitmapResizeBenchmark()
        {
            using (var original = new SKBitmap(Width, Height))
            using (var resized = original.Resize(new SKImageInfo(ResizedWidth, ResizedHeight), SKBitmapResizeMethod.Lanczos3))
            using (var image = SKImage.FromBitmap(resized))
            {
                return new SKSize(image.Width, image.Height);
            }
        }
    }
}