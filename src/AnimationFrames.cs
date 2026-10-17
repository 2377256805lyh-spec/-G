using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XiaoGDesktop
{
    // 经审核的动作提前绘制为高清帧；播放只解压取图，不在界面线程重新拉伸人物。
    internal sealed class AnimationFrames
    {
        internal const int Rate = 60;
        private readonly ZipArchive archive;
        private readonly Dictionary<string, string[]> timelines = new Dictionary<string, string[]>();
        private readonly Dictionary<string, BitmapSource> cache = new Dictionary<string, BitmapSource>();
        private readonly Queue<string> order = new Queue<string>();

        internal AnimationFrames()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("animation-frames.zip");
            // 未制作帧包的源码构建沿用原画过渡，供正常制作新动作时使用。
            if (stream == null) return;
            archive = new ZipArchive(stream, ZipArchiveMode.Read);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (!entry.FullName.StartsWith("timelines/", StringComparison.Ordinal) || !entry.FullName.EndsWith(".txt", StringComparison.Ordinal)) continue;
                string name = Path.GetFileNameWithoutExtension(entry.FullName);
                using (StreamReader reader = new StreamReader(entry.Open()))
                    timelines.Add(name, reader.ReadToEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        internal bool TryGet(string name, double elapsed, out BitmapSource image)
        {
            image = null;
            string[] timeline;
            if (!timelines.TryGetValue(name, out timeline)) return false;
            int index = Math.Max(0, Math.Min(timeline.Length - 1, (int)Math.Round(elapsed * Rate)));
            string key = timeline[index];
            if (cache.TryGetValue(key, out image)) return true;
            ZipArchiveEntry entry = archive.GetEntry("frames/" + key + ".bgra");
            int length = SourceArtwork.Width * SourceArtwork.Height * 4;
            if (entry == null || entry.Length != length) throw new InvalidDataException("动作画面损坏：" + name);
            byte[] pixels = new byte[length];
            using (Stream source = entry.Open())
            {
                int read = 0;
                while (read < pixels.Length)
                {
                    int count = source.Read(pixels, read, pixels.Length - read);
                    if (count == 0) throw new EndOfStreamException("动作画面没有读取完整：" + name);
                    read += count;
                }
            }
            image = BitmapSource.Create(SourceArtwork.Width, SourceArtwork.Height, 96 * SourceArtwork.Scale, 96 * SourceArtwork.Scale,
                PixelFormats.Pbgra32, null, pixels, SourceArtwork.Width * 4);
            image.Freeze();
            if (cache.Count >= 48) cache.Remove(order.Dequeue());
            cache.Add(key, image); order.Enqueue(key);
            return true;
        }
    }
}
