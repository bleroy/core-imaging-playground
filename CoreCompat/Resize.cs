using System.Drawing;
using System.Drawing.Drawing2D;

using BenchmarkDotNet.Attributes;

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
    }
}