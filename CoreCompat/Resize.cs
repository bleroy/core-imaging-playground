using System.Drawing;
using System.Drawing.Drawing2D;

using BenchmarkDotNet.Attributes;

using Size = System.Drawing.Size;

namespace ImageProcessing
{
    public class Resize
    {
        private const int Width = 1280;
        private const int Height = 853;
        private const int ResizedWidth = 150;
        private const int ResizedHeight = 99;

        [Benchmark(Baseline = true, Description = "System.Drawing Resize")]
        public Size ResizeSystemDrawing()
        {
            using (var source = new Bitmap(Width, Height))
            {
                using (var destination = new Bitmap(ResizedWidth, ResizedHeight))
                {
                    using (var graphics = Graphics.FromImage(destination))
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
    }
}