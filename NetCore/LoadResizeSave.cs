using BenchmarkDotNet.Attributes;
using ImageSharp;
using ImageSharp.Formats;
using ImageSharp.Processing;
using ImageMagick;
using FreeImageAPI;

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

        readonly IEnumerable<string> _images;
        readonly string _outputDirectory;

        public LoadResizeSave()
        {
            OpenCL.IsEnabled = false;
            // Add ImageSharp Formats
            Configuration.Default.AddImageFormat(new JpegFormat());

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

        //[Benchmark(Description = "ImageSharp Load, Resize, Save")]
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

        //[Benchmark(Description = "ImageMagick Load, Resize, Save")]
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

        [Benchmark(Description = "ImageFree Load, Resize, Save")]
        public void FreeImageResizeBenchmark()
        {
            foreach (var image in _images)
            {
                FreeImageResize(image, ThumbnailSize, _outputDirectory);
            }
        }

        static void FreeImageResize(string path, int size, string outputDirectory)
        {
            using (var original = new FreeImageBitmap(path))
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
                original.Rescale(width, height, FREE_IMAGE_FILTER.FILTER_BICUBIC);
                // JPEG_QUALITYGOOD is 75 JPEG.
                original.Save(OutputPath(path, outputDirectory, FreeImage), FREE_IMAGE_FORMAT.FIF_JPEG, FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYGOOD);
            }
        }
    }
}