using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace ImageProcessing
{
    public class LoadResizeSaveParallel : LoadResizeSave
    {
        [Benchmark(Baseline = true, Description = "System.Drawing Load, Resize, Save - Parallel"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void SystemDrawingBenchmarkParallel()
        {
            Parallel.ForEach(Images, SystemDrawingResize);
        }

        [Benchmark(Description = "ImageSharp Load, Resize, Save - Parallel"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void ImageSharpBenchmarkParallel()
        {
            Parallel.ForEach(Images, ImageSharpResize);
        }

        [Benchmark(Description = "ImageMagick Load, Resize, Save - Parallel"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void MagickBenchmarkParallel()
        {
            Parallel.ForEach(Images, MagickResize);
        }

        [Benchmark(Description = "ImageFree Load, Resize, Save - Parallel"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void FreeImageBenchmarkParallel()
        {
            Parallel.ForEach(Images, FreeImageResize);
        }

        [Benchmark(Description = "MagicScaler Load, Resize, Save - Parallel"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void MagicScalerBenchmarkParallel()
        {
            Parallel.ForEach(Images, MagickResize);
        }

        [Benchmark(Description = "SkiaSharp Canvas Load, Resize, Save - Parallel"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void SkiaCanvasBenchmarkParallel()
        {
            Parallel.ForEach(Images, SkiaCanvasResize);
        }

        [Benchmark(Description = "SkiaSharp Bitmap Load, Resize, Save - Parallel"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void SkiaBitmapBenchmarkParallel()
        {
            Parallel.ForEach(Images, SkiaBitmapResize);
        }

        [Benchmark(Description = "NetVips Load, Resize, Save - Parallel"/*,
            OperationsPerInvoke = ImagesCount*/)]
        public void NetVipsBenchmarkParallel()
        {
            Parallel.ForEach(Images, NetVipsResize);
        }
    }
}