using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using FreeImageAPI;
using ImageMagick;
using NetVips;
using PhotoSauce.MagicScaler;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using ImageFlow = Imageflow.Fluent;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using ImageSharpSize = SixLabors.ImageSharp.Size;
using NetVipsImage = NetVips.Image;
using SystemDrawingImage = System.Drawing.Image;

namespace ImageProcessing
{
    public class LoadResizeSave
    {
        private const int ThumbnailSize = 150;
        private const int Quality = 75;
        private const string ImageFlow = nameof(ImageFlow);
        private const string ImageSharp = nameof(ImageSharp);
        private readonly string ImageSharpTD = nameof(ImageSharp) + "TD";
        private const string SystemDrawing = nameof(SystemDrawing);
        private const string MagickNET = nameof(MagickNET);
        private const string NetVips = nameof(NetVips);
        private const string FreeImage = nameof(FreeImage);
        private const string MagicScaler = nameof(MagicScaler);
        private const string SkiaSharpBitmap = nameof(SkiaSharpBitmap);

        // Set the quality for ImagSharp
        private readonly JpegEncoder imageSharpJpegEncoder = new() { Quality = Quality, ColorType = JpegEncodingColor.YCbCrRatio420 };
        private readonly ImageCodecInfo systemDrawingJpegCodec =
            ImageCodecInfo.GetImageEncoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

        public IEnumerable<string> Images { get; }
        private readonly string outputDirectory;

        // TODO: https://github.com/dotnet/BenchmarkDotNet/issues/258
        public const int ImagesCount = 12;

        public LoadResizeSave()
        {
            if (RuntimeInformation.OSArchitecture is Architecture.X86 or Architecture.X64)
            {
                // Workaround ImageMagick issue
                OpenCL.IsEnabled = false;
            }

            // Disable libvips operations cache
            Cache.Max = 0;

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
            Images = Directory.EnumerateFiles(imageDirectory).Take(20);

            // Create the output directory next to the images directory
            this.outputDirectory = Path.Combine(Path.GetDirectoryName(imageDirectory), "output");
            if (!Directory.Exists(this.outputDirectory))
            {
                Directory.CreateDirectory(this.outputDirectory);
            }
        }

        private string OutputPath(string inputPath, string postfix)
        {
            return Path.Combine(
                this.outputDirectory,
                Path.GetFileNameWithoutExtension(inputPath)
                + "-" + postfix
                + Path.GetExtension(inputPath));
        }

        private (int width, int height) ScaledSize(int inWidth, int inHeight, int outSize)
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

        [Benchmark(Baseline = true, Description = "System.Drawing Load, Resize, Save"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void SystemDrawingBenchmark()
        {
            foreach (string image in Images)
            {
                SystemDrawingResize(image);
            }
        }

        public void SystemDrawingResize(string input)
        {
            using (var image = SystemDrawingImage.FromFile(input, true))
            {
                var scaled = ScaledSize(image.Width, image.Height, ThumbnailSize);
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
                    using (var encoderParams = new EncoderParameters(1))
                    using (var qualityParam = new EncoderParameter(Encoder.Quality, (long)Quality))
                    {
                        encoderParams.Param[0] = qualityParam;
                        resized.Save(OutputPath(input, SystemDrawing), systemDrawingJpegCodec, encoderParams);
                    }
                }
            }
        }

        [Benchmark(Description = "ImageFlow Load, Resize, Save"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public async Task ImageFlowBenchmark()
        {
            foreach (string image in Images)
            {
                await ImageFlowResize(image);
            }
        }

        public async Task ImageFlowResize(string input)
        {
            using (var output = File.Open(OutputPath(input, ImageFlow), FileMode.Create))
            {
                var imageBytes = await File.ReadAllBytesAsync(input);
                // Resize it to fit a 150x150 square
                using (var image = new ImageFlow.ImageJob())
                {
                    var o =
                        await image
                            .Decode(imageBytes)
                            .ResizerCommands($"width={ThumbnailSize}&height={ThumbnailSize}&mode=max")
                            .EncodeToBytes(new ImageFlow.MozJpegEncoder(Quality, true))
                            .Finish()
                            .InProcessAsync();

                    // Don't throw, bad for benchmark.
                    if (o.First.TryGetBytes().HasValue)
                    {
                        var b = o.First.TryGetBytes().Value.ToArray();
                        await output.WriteAsync(b, 0, b.Length);
                    }
                }
            }
        }

        [Benchmark(Description = "ImageSharp Load, Resize, Save"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void ImageSharpBenchmark()
        {
            foreach (string image in Images)
            {
                ImageSharpResize(image);
            }
        }

        public void ImageSharpResize(string input)
        {
            using (var output = File.Open(OutputPath(input, ImageSharp), FileMode.Create))
            {
                // Resize it to fit a 150x150 square
                using (var image = ImageSharpImage.Load(input))
                {
                    image.Mutate(i => i.Resize(new ResizeOptions
                    {
                        Size = new ImageSharpSize(ThumbnailSize, ThumbnailSize),
                        Mode = ResizeMode.Max
                    }));

                    // Reduce the size of the file
                    image.Metadata.ExifProfile = null;
                    image.Metadata.IptcProfile = null;
                    image.Metadata.XmpProfile = null;

                    // Save the results.
                    image.Save(output, imageSharpJpegEncoder);
                }
            }
        }

        [Benchmark(Description = "ImageSharp TD Load, Resize, Save"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void ImageSharpTargetedDecodeBenchmark()
        {
            foreach (string image in Images)
            {
                ImageSharpTargetedDecodeResize(image);
            }
        }

        public void ImageSharpTargetedDecodeResize(string input)
        {
            DecoderOptions options = new()
            {
                // Resize it to fit a 150x150 square
                TargetSize = new(ThumbnailSize, ThumbnailSize),
            };

            using var output = File.Open(OutputPath(input, ImageSharpTD), FileMode.Create);
            using var image = ImageSharpImage.Load(options, input);

            // Reduce the size of the file
            image.Metadata.ExifProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            // Save the results.
            image.Save(output, imageSharpJpegEncoder);
        }

        [Benchmark(Description = "ImageMagick Load, Resize, Save"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void MagickBenchmark()
        {
            foreach (string image in Images)
            {
                MagickResize(image);
            }
        }

        public void MagickResize(string input)
        {
            using (var image = new MagickImage(input))
            {
                // Resize it to fit a 150x150 square
                image.Resize(ThumbnailSize, ThumbnailSize);

                // Reduce the size of the file
                image.Strip();

                // Set the quality
                image.Quality = Quality;

                // Save the results
                image.Write(OutputPath(input, MagickNET));
            }
        }

        [Benchmark(Description = "ImageFree Load, Resize, Save"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void FreeImageBenchmark()
        {
            foreach (string image in Images)
            {
                FreeImageResize(image);
            }
        }

        public void FreeImageResize(string input)
        {
            using (var original = FreeImageBitmap.FromFile(input))
            {
                var scaled = ScaledSize(original.Width, original.Height, ThumbnailSize);
                using (var resized = new FreeImageBitmap(original, scaled.width, scaled.height))
                {
                    // JPEG_QUALITYGOOD is 75 JPEG.
                    // JPEG_BASELINE strips metadata (EXIF, etc.)
                    resized.Save(OutputPath(input, FreeImage), FREE_IMAGE_FORMAT.FIF_JPEG,
                        FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYGOOD |
                        FREE_IMAGE_SAVE_FLAGS.JPEG_BASELINE);
                }
            }
        }

        [Benchmark(Description = "MagicScaler Load, Resize, Save"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void MagicScalerBenchmark()
        {
            foreach (string image in Images)
            {
                MagicScalerResize(image);
            }
        }

        public void MagicScalerResize(string input)
        {
            var settings = new ProcessImageSettings()
            {
                Width = ThumbnailSize,
                Height = ThumbnailSize,
                ResizeMode = CropScaleMode.Max,
                EncoderOptions = new JpegEncoderOptions(Quality, ChromaSubsampleMode.Subsample420, true)
            };

            MagicImageProcessor.ProcessImage(input, OutputPath(input, MagicScaler), settings);
        }

        [Benchmark(Description = "SkiaSharp Load, Resize, Save"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void SkiaBitmapBenchmark()
        {
            foreach (string image in Images)
            {
                SkiaBitmapResize(image);
            }
        }

        public void SkiaBitmapResize(string input)
        {
            using (var original = SKBitmap.Decode(input))
            {
                var scaled = ScaledSize(original.Width, original.Height, ThumbnailSize);
                using (var resized = original.Resize(new SKImageInfo(scaled.width, scaled.height), SKFilterQuality.High))
                {
                    if (resized == null)
                    {
                        return;
                    }

                    using (var image = SKImage.FromBitmap(resized))
                    using (var output = File.OpenWrite(OutputPath(input, SkiaSharpBitmap)))
                    {
                        image.Encode(SKEncodedImageFormat.Jpeg, Quality)
                             .SaveTo(output);
                    }
                }
            }
        }

        [Benchmark(Description = "NetVips Load, Resize, Save"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void NetVipsBenchmark()
        {
            foreach (string image in Images)
            {
                NetVipsResize(image);
            }
        }

        public void NetVipsResize(string input)
        {
            // Thumbnail to fit a 150x150 square
            using (var thumb = NetVipsImage.Thumbnail(input, ThumbnailSize, ThumbnailSize))
            {
                // Remove all metadata except color profile
                using var mutated = thumb.Mutate(mutable =>
                {
                    foreach (var field in mutable.GetFields())
                    {
                        if (field == "icc-profile-data")
                            continue;

                        mutable.Remove(field);
                    }
                });

                // Save the results
                mutated.Jpegsave(OutputPath(input, NetVips), q: Quality);
            }
        }
    }
}