using BenchmarkDotNet.Attributes;
using ImageSharp;
using ImageSharp.Formats;
using ImageMagick;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

using ImageSharpImage = ImageSharp.Image;
using ImageSharpSize = ImageSharp.Size;
using SystemDrawingImage = System.Drawing.Image;

namespace ImageProcessing
{
    public class LoadResizeSave
    {
        const int ThumbnailSize = 150;
        const int Quality = 75;
        const string ImageSharp = nameof(ImageSharp);
        const string SystemDrawing = nameof(SystemDrawing);
        const string MagickNET = nameof(MagickNET);

        readonly IEnumerable<string> _images;
        readonly string _outputDirectory;
        readonly ImageCodecInfo _codec;
        readonly EncoderParameters _encoderParameters;

        public LoadResizeSave()
        {
            // Add ImageSharp Formats
            Configuration.Default.AddImageFormat(new JpegFormat());

            // Initialize the encoder and parameters for System.Drawing
            var qualityParamId = Encoder.Quality;
            _encoderParameters = new EncoderParameters(1);
            _encoderParameters.Param[0] = new EncoderParameter(qualityParamId, Quality);
            _codec = ImageCodecInfo.GetImageDecoders().FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

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

        static void ImageSharpResize(string path, int size, string outputDirectory)
        {
            using (var input = File.OpenRead(path))
            {
                using (var output = File.Open(OutputPath(path, outputDirectory, ImageSharp), FileMode.Create))
                {
                    // Resize it to fit a 150x150 square
                    var image = new ImageSharpImage(input)
                        .Resize(new ResizeOptions
                        {
                            Size = new ImageSharpSize(size, size),
                            Mode = ResizeMode.Max
                        });

                    // Reduce the size of the file
                    image.ExifProfile = null;

                    // Set the quality
                    image.Quality = Quality;

                    // Save the results
                    image.Save(output);
                }
            }
        }

        [Benchmark(Baseline = true, Description = "System.Drawing Load, Resize, Save")]
        public void SystemDrawingResizeBenchmark()
        {
            foreach (var image in _images)
            {
                SystemDrawingResize(image, ThumbnailSize, _outputDirectory);
            }
        }

        void SystemDrawingResize(string path, int size, string outputDirectory)
        {
            using (var image = new Bitmap(SystemDrawingImage.FromFile(path)))
            {
                int width, height;
                if (image.Width > image.Height)
                {
                    width = size;
                    height = Convert.ToInt32(image.Height * size / (double)image.Width);
                }
                else
                {
                    width = Convert.ToInt32(image.Width * size / (double)image.Height);
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
                    using (var output = File.Open(OutputPath(path, outputDirectory, SystemDrawing), FileMode.Create))
                    {
                        resized.Save(output, _codec, _encoderParameters);
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

        static void MagickResize(string path, int size, string outputDirectory)
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
    }
}