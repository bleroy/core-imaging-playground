using ImageMagick;
using ImageSharp;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using ImageSharpImage = ImageSharp.Image;
using ImageSharpSize = ImageSharp.Size;
// using ImageSharpExifTag = ImageSharp.ExifTag;
using SystemDrawingImage = System.Drawing.Image;
using System.Linq;
using System.Diagnostics;

namespace ImageProcessing
{
    public class Program
    {
        const int ThumbnailSize = 150;
        const string ImageSharp = "ImageSharp";
        const string SystemDrawing = "SystemDrawing";
        const string ImageMagick = "MagickNET";

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
            Console.WriteLine($"{"Library", 15} | {"Time (ms)", 9} | {"Size (kB)", 9} |");
            Console.WriteLine("---------------:|----------:|----------:|");
            // Resize the images using ImageSharp
            var watch = new Stopwatch();
            watch.Start();
            foreach(var image in images)
            {
                ImageSharpResize(image, ThumbnailSize, outputDirectory);
            }
            watch.Stop();
            Report(ImageSharp, outputDirectory, images.Count(), watch.ElapsedMilliseconds);
            // Resize the images using System.Drawing
            watch.Reset();
            watch.Start();
            foreach (var image in images)
            {
                SystemDrawingResize(image, ThumbnailSize, outputDirectory);
            }
            watch.Stop();
            Report(SystemDrawing, outputDirectory, images.Count(), watch.ElapsedMilliseconds);
            // Resize the images using Magick.NET
            watch.Reset();
            watch.Start();
            foreach (var image in images)
            {
                MagickResize(image, ThumbnailSize, outputDirectory);
            }
            watch.Stop();
            Report(ImageMagick, outputDirectory, images.Count(), watch.ElapsedMilliseconds);
        }

        static string OutputPath(string inputPath, string outputDirectory, string postfix)
        {
            return Path.Combine(
                outputDirectory,
                Path.GetFileNameWithoutExtension(inputPath)
                + "-" + postfix
                +Path.GetExtension(inputPath));
        }

        static void Report(string library, string processedImagesDirectory, int count, long elapsed)
        {
            var size = Directory
                .GetFiles(processedImagesDirectory, "*-" + library + ".jpg")
                .Select(f => new FileInfo(f).Length)
                .Sum() / count;
            Console.WriteLine($"{library, 15} | {elapsed / count, 9} | {size / 1024, 9} |");
        }

        public static void ImageSharpResize(string path, int size, string outputDirectory)
        {
            using (var input = File.OpenRead(path))
            {
                using (var output = File.OpenWrite(OutputPath(path, outputDirectory, ImageSharp)))
                {
                    var image = new ImageSharpImage(input)
                    // Resize it to fit a 150x150 square
                        .Resize(new ResizeOptions
                        {
                            Size = new ImageSharpSize(size, size),
                            Mode = ResizeMode.Max
                        });
                    /*
                    // Dial down the saturation
                        .Saturation(-50);
                    // Add a watermark composed from EXIT data
                    var exif = image.ExifProfile;
                    var description = exif.GetValue(ImageSharpExifTag.ImageDescription);
                    var yearTaken = DateTime.ParseExact(
                        (string)exif.GetValue(ImageSharpExifTag.DateTimeOriginal).Value,
                        "yyyy:MM:dd HH:mm:ss",
                        CultureInfo.InvariantCulture)
                        .Year;
                    var author = exif.GetValue(ImageSharpExifTag.Artist);
                    var copyright = $"{description} (c) {yearTaken} {author}";
                    exif.SetValue(ImageSharpExifTag.Copyright, copyright);
                    */
                    // Save the results
                    image.Save(output);
                }
            }
        }

        public static void SystemDrawingResize(string path, int size, string outputDirectory)
        {
            var image = new Bitmap(SystemDrawingImage.FromFile(path));
            // Resize it to fit a 150x150 square
            int width, height;
            if (image.Width > image.Height)
            {
                width = size;
                height = image.Height * size / image.Width;
            }
            else
            {
                width = image.Width * size / image.Height;
                height = size;
            }
            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(image, 0, 0, width, height);
                // Save the results
                using (var output = File.OpenWrite(OutputPath(path, outputDirectory, SystemDrawing)))
                {
                    resized.Save(output, ImageFormat.Jpeg);
                }
            }
        }

        public static void MagickResize(string path, int size, string outputDirectory)
        {
            using (var image = new MagickImage(path))
            {
                // Resize it to fit a 150x150 square
                image.Resize(size, size);
                // Reduce the size of the file
                image.Strip();
                // Save the results
                image.Write(OutputPath(path, outputDirectory, ImageMagick));
            }
        }
    }
}
