using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using SystemDrawingImage = System.Drawing.Image;

namespace ImageProcessing
{
    public class LoadResizeSave
    {
        private const int ThumbnailSize = 150;
        private const int Quality = 75;
        private const string SystemDrawing = nameof(SystemDrawing);

        private readonly IEnumerable<string> images;
        private readonly string outputDirectory;
        private static readonly ImageCodecInfo codec;
        private static readonly EncoderParameters encoderParameters;

        static LoadResizeSave()
        {
            // Initialize the encoder and parameters for System.Drawing
            Encoder qualityParamId = Encoder.Quality;
            encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(qualityParamId, Quality);
            codec = Array.Find(ImageCodecInfo.GetImageDecoders(), codec => codec.FormatID == ImageFormat.Jpeg.Guid);
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

        [Benchmark(Baseline = true, Description = "System.Drawing Load, Resize, Save")]
        public void SystemDrawingResizeBenchmark()
        {
            foreach (string image in this.images)
            {
                SystemDrawingResize(image, ThumbnailSize, this.outputDirectory);
            }
        }

        internal static void SystemDrawingResize(string path, int size, string outputDirectory)
        {
            using (var image = SystemDrawingImage.FromFile(path, true))
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

                using (var resized = new Bitmap(width, height))
                using (var graphics = Graphics.FromImage(resized))
                using (var attributes = new ImageAttributes())
                {
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighSpeed;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.DrawImage(image, Rectangle.FromLTRB(0, 0, width, height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);

                    // Save the results
                    using (FileStream output = File.Open(OutputPath(path, outputDirectory, SystemDrawing), FileMode.Create))
                    {
                        // This works
                        // resized.Save(output, ImageFormat.Jpeg);
                        //
                        // This fails
                        // resized.Save(output, codec, encoderParameters);
                        // {System.ArgumentException: Parameter is not valid.
                        // at System.Drawing.Image.Save(Stream stream, ImageCodecInfo encoder, EncoderParameters encoderParams)
                        // See https://github.com/dotnet/corefx/issues/34156
                        // This means we can only compare performance not quality with the System.Drawing output.
                        resized.Save(output, ImageFormat.Jpeg);
                    }
                }
            }
        }
    }
}