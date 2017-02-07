using BenchmarkDotNet.Attributes;
using ImageSharp;
using ImageSharp.Formats;
using ImageSharp.Processing;
using ImageMagick;
using FreeImageAPI;
using PhotoSauce.MagicScaler;
using SkiaSharp;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using ImageSharpImage = ImageSharp.Image;
using ImageSharpSize = ImageSharp.Size;

namespace ImageProcessing
{
    public class LoadResizeSave
    {
        const int ThumbnailSize = 150;
        const int Quality = 75;
        const string ImageSharp = nameof(ImageSharp);
        const string SystemDrawing = nameof(SystemDrawing);
        const string MagickNET = nameof(MagickNET);
        const string FreeImage = nameof(FreeImage);
        const string MagicScaler = nameof(MagicScaler);
        const string SkiaSharpCanvas = nameof(SkiaSharpCanvas);
        const string SkiaSharpBitmap = nameof(SkiaSharpBitmap);

        readonly IEnumerable<string> _images;
        readonly string _outputDirectory;

        static LoadResizeSave()
        {
            OpenCL.IsEnabled = false;
            // Add ImageSharp Formats
            Configuration.Default.AddImageFormat(new JpegFormat());
        }

        public LoadResizeSave()
        {
            // Find the closest images directory
            var imageDirectory = Path.GetFullPath(".");
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
            _images = Directory.EnumerateFiles(imageDirectory).Take(20);
            // Create the output directory next to the images directory
            _outputDirectory = Path.Combine(Path.GetDirectoryName(imageDirectory), "output");
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        static string OutputPath(string inputPath, string outputDirectory, string postfix)
        {
            return Path.Combine(
                outputDirectory,
                Path.GetFileNameWithoutExtension(inputPath)
                + "-" + postfix
                + Path.GetExtension(inputPath));
        }

        [Benchmark(Description = "ImageSharp Load, Resize, Save")]
        public void ImageSharpBenchmark()
        {
            foreach (var image in _images)
            {
                ImageSharpResize(image, ThumbnailSize, _outputDirectory);
            }
        }

        internal static void ImageSharpResize(string path, int size, string outputDirectory)
        {
            using (var input = File.OpenRead(path))
            {
                using (var output = File.Open(OutputPath(path, outputDirectory, ImageSharp), FileMode.Create))
                {
                    // Resize it to fit a 150x150 square
                    using (var image = new ImageSharpImage(input))
                    {
                        image.Resize(new ResizeOptions
                        {
                            Size = new ImageSharpSize(size, size),
                            Mode = ResizeMode.Max
                        });

                        // Reduce the size of the file
                        image.MetaData.ExifProfile = null;

                        // Set the quality
                        image.MetaData.Quality = Quality;

                        // Save the results
                        image.Save(output);
                    }
                }
            }
        }

        [Benchmark(Description = "ImageMagick Load, Resize, Save")]
        public void MagickResizeBenchmark()
        {
            foreach (var image in _images)
            {
                MagickResize(image, ThumbnailSize, _outputDirectory);
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
            foreach (var image in _images)
            {
                FreeImageResize(image, ThumbnailSize, _outputDirectory);
            }
        }

        internal static void FreeImageResize(string path, int size, string outputDirectory)
        {
            using (var original = FreeImageBitmap.FromFile(path))
            {
                int width, height;
                if (original.Width > original.Height)
                {
                    width = size;
                    height = original.Height * size / original.Width;
                }
                else
                {
                    width = original.Width * size / original.Height;
                    height = size;
                }
                var resized = new FreeImageBitmap(original, width, height);
                // JPEG_QUALITYGOOD is 75 JPEG.
                // JPEG_BASELINE strips metadata (EXIF, etc.)
               resized.Save(OutputPath(path, outputDirectory, FreeImage), FREE_IMAGE_FORMAT.FIF_JPEG,
                   FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYGOOD |
                   FREE_IMAGE_SAVE_FLAGS.JPEG_BASELINE);
            }
        }

        [Benchmark(Description = "MagicScaler Load, Resize, Save")]
        public void MagicScalerResizeBenchmark()
        {
            foreach (var image in _images)
            {
                MagicScalerResize(image, ThumbnailSize, _outputDirectory);
            }
        }

        internal static void MagicScalerResize(string path, int size, string outputDirectory)
        {
            var settings = new ProcessImageSettings() {
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
            foreach (var image in _images)
            {
                SkiaCanvasLoadResizeSave(image, ThumbnailSize, _outputDirectory);
            }
        }

        static void SkiaCanvasLoadResizeSave(string path, int size, string outputDirectory)
        {
            using (var input = File.OpenRead(path))
            {
                using (var inputStream = new SKManagedStream(input))
                {
                    using (var original = SKBitmap.Decode(inputStream))
                    {
                        int width, height;
                        if (original.Width > original.Height)
                        {
                            width = size;
                            height = original.Height * size / original.Width;
                        }
                        else
                        {
                            width = original.Width * size / original.Height;
                            height = size;
                        }
                        var surface = SKSurface.Create(width, height, original.ColorType, original.AlphaType);
                        var canvas = surface.Canvas;
                        var scale = (float)width / original.Width;
                        canvas.Scale(scale);
                        var paint = new SKPaint();
                        paint.FilterQuality = SKFilterQuality.High;
                        canvas.DrawBitmap(original, 0, 0, paint);
                        canvas.Flush();

                        using (var output = File.OpenWrite(OutputPath(path, outputDirectory, SkiaSharpCanvas)))
                        {
                            surface.Snapshot()
                                .Encode(SKImageEncodeFormat.Jpeg, Quality)
                                .SaveTo(output);
                        }
                    }
                }
            }
        }

        [Benchmark(Description = "SkiaSharp Bitmap Load, Resize, Save")]
        public void SkiaBitmapLoadResizeSaveBenchmark()
        {
            foreach (var image in _images)
            {
                SkiaBitmapLoadResizeSave(image, ThumbnailSize, _outputDirectory);
            }
        }

        static void SkiaBitmapLoadResizeSave(string path, int size, string outputDirectory)
        {
            using (var input = File.OpenRead(path))
            {
                using (var inputStream = new SKManagedStream(input))
                {
                    using (var original = SKBitmap.Decode(inputStream))
                    {
                        int width, height;
                        if (original.Width > original.Height)
                        {
                            width = size;
                            height = original.Height * size / original.Width;
                        }
                        else
                        {
                            width = original.Width * size / original.Height;
                            height = size;
                        }

                        using (var resized = original.Resize(new SKImageInfo(width, height), SKBitmapResizeMethod.Lanczos3))
                        {
                            if (resized == null)
                                return;

                            using (var image = SKImage.FromBitmap(resized))
                            {
                                using (var output = File.OpenWrite(OutputPath(path, outputDirectory, SkiaSharpBitmap)))
                                {
                                    image.Encode(SKImageEncodeFormat.Jpeg, Quality)
                                        .SaveTo(output);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}