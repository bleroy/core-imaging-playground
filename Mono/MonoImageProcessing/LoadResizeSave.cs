using BenchmarkDotNet.Attributes;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace MonoImageProcessing
{
    public class LoadResizeSave
    {
        const int ThumbnailSize = 150;
        const string SkiaSharp = nameof(SkiaSharp);

        readonly IEnumerable<string> _images;
        readonly string _outputDirectory;

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

        [Benchmark(Description = "SkiaSharp Load, Resize, Save")]
        public void SkiaLoadResizeSaveBenchmark()
        {
            foreach (var image in _images)
            {
                SkiaLoadResizeSave(image, ThumbnailSize, _outputDirectory);
            }
        }

        public static void SkiaLoadResizeSave(string path, int size, string outputDirectory)
        {
            using (var input = File.OpenRead(path))
            {
                using (var inputStream = new SKManagedStream(input))
                {
                    var original = SKBitmap.Decode(inputStream);
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

                    using (var output = File.OpenWrite(OutputPath(path, outputDirectory, SkiaSharp)))
                    {
                        surface.Snapshot()
                               .Encode(SKImageEncodeFormat.Jpeg, 85)
                               .SaveTo(output);
                    }
                }
            }
        }
    }
}