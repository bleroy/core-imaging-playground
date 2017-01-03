using System.Drawing;
using System.Drawing.Drawing2D;

using BenchmarkDotNet.Attributes;

using ImageMagick;

using ImageSharp;
using ImageSharp.Formats;

using ImageSharpImage = ImageSharp.Image;
using ImageSharpSize = ImageSharp.Size;
using Size = System.Drawing.Size;

namespace ImageProcessing
{
    public class Resize
    {
        public Resize()
        {
            // Add ImageSharp Formats
            Configuration.Default.AddImageFormat(new JpegFormat());
            Configuration.Default.AddImageFormat(new PngFormat());
            Configuration.Default.AddImageFormat(new BmpFormat());
            Configuration.Default.AddImageFormat(new GifFormat());
        }

        [Benchmark(Baseline = true, Description = "System.Drawing Resize")]
        public Size ResizeSystemDrawing()
        {
            using (Bitmap source = new Bitmap(2000, 2000))
            {
                using (Bitmap destination = new Bitmap(400, 400))
                {
                    using (Graphics graphics = Graphics.FromImage(destination))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.CompositingQuality = CompositingQuality.HighSpeed;
                        graphics.DrawImage(source, 0, 0, 400, 400);
                    }

                    return destination.Size;
                }
            }
        }

        [Benchmark(Description = "ImageSharp Resize")]
        public ImageSharpSize ResizeImageSharp()
        {
            ImageSharpImage image = new ImageSharpImage(2000, 2000);
            image.Resize(400, 400);
            return new ImageSharpSize(400, 400);
        }

        [Benchmark(Description = "ImageMagick Resize")]
        public MagickGeometry MagickResize()
        {
            MagickGeometry size = new MagickGeometry(400, 400);
            using (MagickImage image = new MagickImage(MagickColor.FromRgba(0, 0, 0, 0), 2000, 2000))
            {
                image.Resize(size);
            }

            return size;
        }
    }
}