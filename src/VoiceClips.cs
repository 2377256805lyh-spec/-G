using System;
using System.IO;
using System.Media;
using System.Reflection;

namespace XiaoGDesktop
{
    // 配音提前做成单声道16位WAV并嵌入程序；运行时仅播放内存中的声音。
    internal sealed class VoiceClips : IDisposable
    {
        private readonly byte[][] originals;
        private readonly SoundPlayer[] players;
        private readonly MemoryStream[] streams;
        private readonly double[] durations;
        private int current = -1, volume;

        internal VoiceClips(int count, int initialVolume)
        {
            originals = new byte[count][];
            players = new SoundPlayer[count];
            streams = new MemoryStream[count];
            durations = new double[count];
            for (int i = 0; i < count; i++)
                using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("voice." + i.ToString("00") + ".wav"))
                {
                    if (resource == null) continue;
                    using (MemoryStream copy = new MemoryStream()) { resource.CopyTo(copy); originals[i] = copy.ToArray(); }
                    byte[] data = originals[i];
                    // 固定PCM头校验：避免错误素材被当成默认系统提示音播放。
                    if (data.Length < 44 || BitConverter.ToUInt16(data, 20) != 1 || BitConverter.ToUInt16(data, 22) != 1 ||
                        BitConverter.ToUInt16(data, 34) != 16 || BitConverter.ToUInt32(data, 36) != 0x61746164 ||
                        BitConverter.ToUInt32(data, 40) != data.Length - 44 || BitConverter.ToUInt32(data, 28) == 0)
                        throw new InvalidDataException("小G的配音文件格式不正确。");
                    durations[i] = (data.Length - 44) / (double)BitConverter.ToUInt32(data, 28);
                }
            SetVolume(initialVolume);
        }

        internal void SetVolume(int value)
        {
            Stop();
            volume = Math.Max(0, Math.Min(100, value));
            for (int i = 0; i < originals.Length; i++)
            {
                if (players[i] != null) players[i].Dispose();
                if (streams[i] != null) streams[i].Dispose();
                players[i] = null;
                streams[i] = null;
                if (originals[i] == null) continue;
                byte[] data = (byte[])originals[i].Clone();
                // 只改变这段配音的采样值，不改电脑或其他软件的总音量。
                for (int offset = 44; offset + 1 < data.Length; offset += 2)
                {
                    short sample = (short)(BitConverter.ToInt16(data, offset) * volume / 100);
                    data[offset] = (byte)sample;
                    data[offset + 1] = (byte)(sample >> 8);
                }
                streams[i] = new MemoryStream(data, false);
                players[i] = new SoundPlayer(streams[i]);
                players[i].Load();
            }
        }

        internal double Play(int index, bool enabled)
        {
            Stop();
            if (!enabled || volume == 0 || index < 0 || index >= players.Length || players[index] == null) return 0;
            try
            {
                players[index].Play();
                current = index;
                return durations[index];
            }
            catch (InvalidOperationException) { return 0; }
        }

        internal void Stop()
        {
            if (current >= 0) players[current].Stop();
            current = -1;
        }

        public void Dispose()
        {
            Stop();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null) players[i].Dispose();
                if (streams[i] != null) streams[i].Dispose();
            }
        }
    }
}
