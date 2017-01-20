using BenchmarkDotNet.Attributes;

using ImageMagick;
using ImageSharp;
using ImageSharp.Formats;
using FreeImageAPI;

using ImageSharpImage = ImageSharp.Image;
using ImageSharpSize = ImageSharp.Size;

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
            OpenCL.IsEnabled = false;
            // Add ImageSharp JPG Format
            Configuration.Default.AddImageFormat(new JpegFormat());
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

        [Benchmark(Description = "FreeImage")]
        public void FreeImageResize()
        {
            using (var image = new FreeImageBitmap(Width, Height))
            {
                image.Rescale(ResizedWidth, ResizedHeight, FREE_IMAGE_FILTER.FILTER_BICUBIC);
            }
        }
    }
}