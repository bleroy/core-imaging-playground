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
        const int Width = 1280;
        const int Height = 853;
        const int ResizedWidth = 150;
        const int ResizedHeight = 99;

        public Resize()
        {
            // Add ImageSharp JPG Format
            Configuration.Default.AddImageFormat(new JpegFormat());
        }

        [Benchmark(Baseline = true, Description = "System.Drawing Resize")]
        public Size ResizeSystemDrawing()
        {
            using (Bitmap source = new Bitmap(Width, Height))
            {
                using (Bitmap destination = new Bitmap(ResizedWidth, ResizedHeight))
                {
                    using (Graphics graphics = Graphics.FromImage(destination))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.CompositingQuality = CompositingQuality.HighSpeed;
                        graphics.DrawImage(source, 0, 0, ResizedWidth, ResizedHeight);
                    }

                    return destination.Size;
                }
            }
        }

        [Benchmark(Description = "ImageSharp Resize")]
        public ImageSharpSize ResizeImageSharp()
        {
            ImageSharpImage image = new ImageSharpImage(Width, Height);
            image.Resize(ResizedWidth, ResizedHeight);
            return new ImageSharpSize(ResizedWidth, ResizedHeight);
        }

        [Benchmark(Description = "ImageMagick Resize")]
        public MagickGeometry MagickResize()
        {
            MagickGeometry size = new MagickGeometry(ResizedWidth, ResizedHeight);
            using (MagickImage image = new MagickImage(MagickColor.FromRgba(0, 0, 0, 0), Width, Height))
            {
                image.Resize(size);
            }

            return size;
        }
    }
}