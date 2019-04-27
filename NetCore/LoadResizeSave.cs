using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using FreeImageAPI;
using ImageMagick;
using PhotoSauce.MagicScaler;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using ImageSharpSize = SixLabors.Primitives.Size;
using SystemDrawingImage = System.Drawing.Image;

namespace ImageProcessing
{
    public class LoadResizeSave
    {
        private const int ThumbnailSize = 150;
        private const int Quality = 75;
        private const string ImageSharp = nameof(ImageSharp);
        private const string SystemDrawing = nameof(SystemDrawing);
        private const string MagickNET = nameof(MagickNET);
        private const string FreeImage = nameof(FreeImage);
        private const string MagicScaler = nameof(MagicScaler);
        private const string SkiaSharpCanvas = nameof(SkiaSharpCanvas);
        private const string SkiaSharpBitmap = nameof(SkiaSharpBitmap);

        // Set the quality for ImagSharp
        private static readonly JpegEncoder imageSharpJpegEncoder = new JpegEncoder { Quality = Quality };
        private static readonly ImageCodecInfo systemDrawingJpegCodec = ImageCodecInfo.GetImageEncoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

        readonly IEnumerable<string> images;
        readonly string outputDirectory;

        static LoadResizeSave()
        {
            // Workaround ImageMagick issue
            OpenCL.IsEnabled = false;
        }

        public LoadResizeSave()
        {
            // Find the closest images directory
            string imageDirectory = Path.GetFullPath(".");
            while (!Directory.Exists(Path.Combine(imageDirectory, "images")))
            {
                imageDirectory = Path.GetDirectoryName(imageDirectory);
                if (imageDirectory == null)
                {
                    throw new FileNotFoundException("Could not find an image directory.");
                }
            }

            imageDirectory = Path.Combine(imageDirectory, "images");

            // Get at most 20 images from there
            this.images = Directory.EnumerateFiles(imageDirectory).Take(20);

            // Create the output directory next to the images directory
            this.outputDirectory = Path.Combine(Path.GetDirectoryName(imageDirectory), "output");
            if (!Directory.Exists(this.outputDirectory))
            {
                Directory.CreateDirectory(this.outputDirectory);
            }
        }

        private static string OutputPath(string inputPath, string outputDirectory, string postfix)
        {
            return Path.Combine(
                outputDirectory,
                Path.GetFileNameWithoutExtension(inputPath)
                + "-" + postfix
                + Path.GetExtension(inputPath));
        }

        private static (int width, int height) ScaledSize(int inWidth, int inHeight, int outSize)
        {
            int width, height;
            if (inWidth > inHeight)
            {
                width = outSize;
                height = (int)Math.Round(inHeight * outSize / (double)inWidth);
            }
            else
            {
                width = (int)Math.Round(inWidth * outSize / (double)inHeight);
                height = outSize;
            }

            return (width, height);
        }

        [Benchmark(Baseline = true, Description = "System.Drawing Load, Resize, Save")]
        public void SystemDrawingResizeBenchmark()
        {
            foreach (var image in this.images)
            {
                SystemDrawingResize(image, ThumbnailSize, this.outputDirectory);
            }
        }

        internal static void SystemDrawingResize(string path, int size, string outputDirectory)
        {
            using (var image = SystemDrawingImage.FromFile(path, true))
            {
                var scaled = ScaledSize(image.Width, image.Height, size);
                var resized = new Bitmap(scaled.width, scaled.height);
                using (var graphics = Graphics.FromImage(resized))
                using (var attributes = new ImageAttributes())
                {
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.AssumeLinear;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(image, Rectangle.FromLTRB(0, 0, resized.Width, resized.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);

                    // Save the results
                    using (var output = File.Open(OutputPath(path, outputDirectory, SystemDrawing), FileMode.Create))
                    using (var encoderParams = new EncoderParameters(1))
                    using (var qualityParam = new EncoderParameter(Encoder.Quality, (long)Quality))
                    {
                        encoderParams.Param[0] = qualityParam;
                        resized.Save(output, systemDrawingJpegCodec, encoderParams);
                    }
                }
            }
        }

        [Benchmark(Description = "ImageSharp Load, Resize, Save")]
        public void ImageSharpResizeBenchmark()
        {
            foreach (string image in this.images)
            {
                ImageSharpResize(image, ThumbnailSize, this.outputDirectory);
            }
        }

        internal static void ImageSharpResize(string path, int size, string outputDirectory)
        {
            using (FileStream input = File.OpenRead(path))
            {
                using (FileStream output = File.Open(OutputPath(path, outputDirectory, ImageSharp), FileMode.Create))
                {
                    // Resize it to fit a 150x150 square
                    using (var image = ImageSharpImage.Load(input))
                    {
                        image.Mutate(i => i.Resize(new ResizeOptions
                        {
                            Size = new ImageSharpSize(size, size),
                            Mode = ResizeMode.Max
                        }));

                        // Reduce the size of the file
                        image.Metadata.ExifProfile = null;

                        // Save the results
                        image.Save(output, imageSharpJpegEncoder);
                    }
                }
            }
        }

        [Benchmark(Description = "ImageMagick Load, Resize, Save")]
        public void MagickResizeBenchmark()
        {
            foreach (string image in this.images)
            {
                MagickResize(image, ThumbnailSize, this.outputDirectory);
            }
        }

        internal static void MagickResize(string path, int size, string outputDirectory)
        {
            using (var image = new MagickImage(path))
            {
                // Resize it to fit a 150x150 square
                image.Resize(size, size);

                // Reduce the size of the file
                image.Strip();

                // Set the quality
                image.Quality = Quality;

                // Save the results
                image.Write(OutputPath(path, outputDirectory, MagickNET));
            }
        }

        [Benchmark(Description = "ImageFree Load, Resize, Save")]
        public void FreeImageResizeBenchmark()
        {
            foreach (string image in this.images)
            {
                FreeImageResize(image, ThumbnailSize, this.outputDirectory);
            }
        }

        internal static void FreeImageResize(string path, int size, string outputDirectory)
        {
            using (var original = FreeImageBitmap.FromFile(path))
            {
                (int width, int height) = ScaledSize(original.Width, original.Height, size);
                var resized = new FreeImageBitmap(original, width, height);
                // JPEG_QUALITYGOOD is 75 JPEG.
                // JPEG_BASELINE strips metadata (EXIF, etc.)
                resized.Save(OutputPath(path, outputDirectory, FreeImage), FREE_IMAGE_FORMAT.FIF_JPEG,
                    FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYGOOD
                    | FREE_IMAGE_SAVE_FLAGS.JPEG_BASELINE);
            }
        }

        [Benchmark(Description = "MagicScaler Load, Resize, Save")]
        public void MagicScalerResizeBenchmark()
        {
            foreach (string image in this.images)
            {
                MagicScalerResize(image, ThumbnailSize, this.outputDirectory);
            }
        }

        internal static void MagicScalerResize(string path, int size, string outputDirectory)
        {
            var settings = new ProcessImageSettings()
            {
                Width = size,
                Height = size,
                ResizeMode = CropScaleMode.Max,
                SaveFormat = FileFormat.Jpeg,
                JpegQuality = Quality,
                JpegSubsampleMode = ChromaSubsampleMode.Subsample420
            };

            using (var output = new FileStream(OutputPath(path, outputDirectory, MagicScaler), FileMode.Create))
            {
                MagicImageProcessor.ProcessImage(path, output, settings);
            }
        }

        [Benchmark(Description = "SkiaSharp Canvas Load, Resize, Save")]
        public void SkiaCanvasLoadResizeSaveBenchmark()
        {
            foreach (string image in this.images)
            {
                SkiaCanvasLoadResizeSave(image, ThumbnailSize, this.outputDirectory);
            }
        }

        internal static void SkiaCanvasLoadResizeSave(string path, int size, string outputDirectory)
        {
            using (var input = File.OpenRead(path))
            using (var inputStream = new SKManagedStream(input))
            using (var original = SKBitmap.Decode(inputStream))
            {
                var scaled = ScaledSize(original.Width, original.Height, size);
                var info = new SKImageInfo(scaled.width, scaled.height, original.ColorType, original.AlphaType);

                using (var surface = SKSurface.Create(info))
                using (var paint = new SKPaint() { FilterQuality = SKFilterQuality.High })
                {
                    var canvas = surface.Canvas;
                    canvas.Scale((float)scaled.width / original.Width);
                    canvas.DrawBitmap(original, 0, 0, paint);
                    canvas.Flush();

                    using (var output = File.OpenWrite(OutputPath(path, outputDirectory, SkiaSharpCanvas)))
                    {
                        surface.Snapshot()
                            .Encode(SKEncodedImageFormat.Jpeg, Quality)
                            .SaveTo(output);
                    }
                }
            }
        }

        [Benchmark(Description = "SkiaSharp Bitmap Load, Resize, Save")]
        public void SkiaBitmapLoadResizeSaveBenchmark()
        {
            foreach (string image in this.images)
            {
                SkiaBitmapLoadResizeSave(image, ThumbnailSize, this.outputDirectory);
            }
        }

        internal static void SkiaBitmapLoadResizeSave(string path, int size, string outputDirectory)
        {
            using (var input = File.OpenRead(path))
            using (var inputStream = new SKManagedStream(input))
            using (var original = SKBitmap.Decode(inputStream))
            {
                var scaled = ScaledSize(original.Width, original.Height, size);
                using (var resized = original.Resize(new SKImageInfo(scaled.width, scaled.height), SKFilterQuality.High))
                {
                    if (resized == null)
                    {
                        return;
                    }

                    using (var image = SKImage.FromBitmap(resized))
                    using (var output = File.OpenWrite(OutputPath(path, outputDirectory, SkiaSharpBitmap)))
                    {
                        image.Encode(SKEncodedImageFormat.Jpeg, Quality)
                             .SaveTo(output);
                    }
                }
            }
        }
    }
}