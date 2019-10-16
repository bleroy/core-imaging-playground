using System;
using System.Buffers;
using System.Drawing;
using System.Drawing.Drawing2D;
using BenchmarkDotNet.Attributes;
using FreeImageAPI;
using ImageMagick;
using NetVips;
using PhotoSauce.MagicScaler;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using ImageSharpImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using ImageSharpSize = SixLabors.Primitives.Size;

namespace ImageProcessing
{
    public class Resize
    {
        private const int Width = 1280;
        private const int Height = 853;
        private const int ResizedWidth = 150;
        private const int ResizedHeight = 99;

        public Resize() => OpenCL.IsEnabled = false;

        [Benchmark(Baseline = true, Description = "System.Drawing Resize")]
        public Size ResizeSystemDrawing()
        {
            using (var source = new Bitmap(Width, Height))
            using (var destination = new Bitmap(ResizedWidth, ResizedHeight))
            {
                using (var graphics = Graphics.FromImage(destination))
                {
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.AssumeLinear;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(source, 0, 0, ResizedWidth, ResizedHeight);
                }

                return destination.Size;
            }
        }

        [Benchmark(Description = "ImageSharp Resize")]
        public ImageSharpSize ResizeImageSharp()
        {
            using (var image = new ImageSharpImage(Width, Height))
            {
                image.Mutate(i => i.Resize(ResizedWidth, ResizedHeight, KnownResamplers.Bicubic));
                return image.Size();
            }
        }

        [Benchmark(Description = "ImageMagick Resize")]
        public MagickGeometry MagickResize()
        {
            var size = new MagickGeometry(ResizedWidth, ResizedHeight);
            using (var image = new MagickImage(MagickColor.FromRgba(0, 0, 0, 0), Width, Height))
            {
                image.FilterType = FilterType.Cubic;

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
            const int stride = ResizedWidth * 3 + 3 & ~3;
            const int bufflen = ResizedHeight * stride;

            var settings = new ProcessImageSettings
            {
                Width = ResizedWidth,
                Height = ResizedHeight,
                Sharpen = false,
                Interpolation = InterpolationSettings.Cubic
            };

            using (var pixels = new TestPatternPixelSource(Width, Height, PixelFormats.Bgr24bpp))
            using (var pipeline = MagicImageProcessor.BuildPipeline(pixels, settings))
            {
                var rect = new Rectangle(0, 0, ResizedWidth, ResizedHeight);
                var buffer = ArrayPool<byte>.Shared.Rent(bufflen);
                pipeline.PixelSource.CopyPixels(rect, stride, new Span<byte>(buffer, 0, bufflen));
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
            using (var resized = original.Resize(new SKImageInfo(ResizedWidth, ResizedHeight), SKFilterQuality.High))
            using (var image = SKImage.FromBitmap(resized))
            {
                return new SKSize(image.Width, image.Height);
            }
        }

        [Benchmark(Description = "NetVips Resize")]
        public (int width, int height) NetVipsResize()
        {
            // Scaling calculations
            const double xFactor = (double)ResizedWidth / Width;
            const double yFactor = (double)ResizedHeight / Height;

            using (var original = NetVips.Image.Black(Width, Height))
            using (var resized = original.Resize(xFactor, vscale: yFactor, kernel: Enums.Kernel.Cubic))
            {
                // libvips is "lazy" and will not process pixels
                // until you write to an output file, buffer or memory
                var _ = resized.WriteToMemory();

                return (resized.Width, resized.Height);
            }
        }
    }
}