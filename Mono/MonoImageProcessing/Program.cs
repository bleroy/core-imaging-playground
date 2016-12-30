using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using SkiaSharp;

namespace MonoImageProcessing
{
    class MainClass
    {
        const int ThumbnailSize = 150;
        const string SkiaSharp = "SkiaSharp";

        public static void Main(string[] args)
        {
            // Find the closest images directory
            var imageDirectory = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
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
            var images = Directory.EnumerateFiles(imageDirectory).Take(20);
            // Create the output directory next to the images directory
            var outputDirectory = Path.Combine(Path.GetDirectoryName(imageDirectory), "output");
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            Console.WriteLine($"| {"Library",15} | {"Time (ms)",9} | {"Size (kB)",9} |");
            Console.WriteLine($"|-----------------|----------:|----------:|");
            // Resize the images using SkiaSharp
            var watch = new Stopwatch();
            watch.Start();
            foreach (var image in images)
            {
                SkiaResize(image, ThumbnailSize, outputDirectory);
            }
            watch.Stop();
            Report(SkiaSharp, outputDirectory, images.Count(), watch.ElapsedMilliseconds);
        }

        static string OutputPath(string inputPath, string outputDirectory, string postfix)
        {
            return Path.Combine(
                outputDirectory,
                Path.GetFileNameWithoutExtension(inputPath)
                + "-" + postfix
                + Path.GetExtension(inputPath));
        }

        static void Report(string library, string processedImagesDirectory, int count, long elapsed)
        {
            var size = Directory
                .GetFiles(processedImagesDirectory, "*-" + library + ".jpg")
                .Select(f => new FileInfo(f).Length)
                .Sum() / count;
            Console.WriteLine($"| {library,15} | {(float)elapsed / count,9: #.0} | {(float)size / 1024,9: #.0} |");
        }

        public static void SkiaResize(string path, int size, string outputDirectory)
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
