using System;
using System.Runtime.InteropServices;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Runtime
{
    // Six independent WinMM waveOut devices, one per voice. This avoids the
    // Unity AudioClip path that throws native ArgumentException in this IL2CPP
    // build. API usage is standard WinMM; buffers are 48 kHz mono PCM16.
    internal sealed class WinMmPlayer : IDisposable
    {
        const int VoiceCount = 8;
        const int MaxSamples = 30720;
        const int BufferBytes = MaxSamples * 2;
        const uint WaveMapper = uint.MaxValue;
        const uint CallbackNull = 0;

        [StructLayout(LayoutKind.Sequential)]
        struct WaveFormatEx
        {
            internal ushort FormatTag;
            internal ushort ChannelCount;
            internal uint SamplesPerSecond;
            internal uint AverageBytesPerSecond;
            internal ushort BlockAlign;
            internal ushort BitsPerSample;
            internal ushort ExtraSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WaveHeader
        {
            internal IntPtr Data;
            internal uint BufferLength;
            internal uint BytesRecorded;
            internal IntPtr User;
            internal uint Flags;
            internal uint Loops;
            internal IntPtr Next;
            internal IntPtr Reserved;
        }

        sealed class Voice
        {
            internal IntPtr Handle;
            internal IntPtr Header;
            internal IntPtr Data;
            internal bool Ready;
            internal long EndsAt;
        }

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutOpen(out IntPtr handle, uint deviceId, ref WaveFormatEx format,
            IntPtr callback, IntPtr instance, uint flags);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutPrepareHeader(IntPtr handle, IntPtr header, int headerSize);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutWrite(IntPtr handle, IntPtr header, int headerSize);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutReset(IntPtr handle);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutUnprepareHeader(IntPtr handle, IntPtr header, int headerSize);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutClose(IntPtr handle);

        readonly Voice[] voices = new Voice[VoiceCount];
        readonly short[] mix = new short[MaxSamples];
        readonly object gate = new object();
        int nextVoice;
        int headerSize;
        bool unavailable;
        long retryAfter;

        internal bool Available => !unavailable;
        internal int Failures { get; private set; }

        internal bool Warmup()
        {
            lock (gate)
            {
                EnsureOpen();
                return !unavailable;
            }
        }

        void EnsureOpen()
        {
            if (voices[0] != null && voices[0].Ready) return;
            var now = Environment.TickCount64;
            if (unavailable && now < retryAfter) return;
            unavailable = false;
            Release();
            try
            {
                headerSize = Marshal.SizeOf<WaveHeader>();
                var format = new WaveFormatEx
                {
                    FormatTag = 1,
                    ChannelCount = 1,
                    SamplesPerSecond = 48000,
                    AverageBytesPerSecond = 96000,
                    BlockAlign = 2,
                    BitsPerSample = 16,
                    ExtraSize = 0
                };
                for (var i = 0; i < VoiceCount; i++)
                {
                    var voice = new Voice();
                    voices[i] = voice;
                    if (waveOutOpen(out voice.Handle, WaveMapper, ref format, IntPtr.Zero, IntPtr.Zero, CallbackNull) != 0)
                    {
                        Fail();
                        return;
                    }
                    voice.Data = Marshal.AllocHGlobal(BufferBytes);
                    voice.Header = Marshal.AllocHGlobal(headerSize);
                    var header = new WaveHeader
                    {
                        Data = voice.Data,
                        BufferLength = BufferBytes
                    };
                    Marshal.StructureToPtr(header, voice.Header, false);
                    if (waveOutPrepareHeader(voice.Handle, voice.Header, headerSize) != 0)
                    {
                        Fail();
                        return;
                    }
                    voice.Ready = true;
                }
            }
            catch
            {
                Fail();
            }
        }

        void Fail()
        {
            Failures++;
            unavailable = true;
            retryAfter = Environment.TickCount64 + 5000;
            Release();
        }

        void Release()
        {
            for (var i = 0; i < VoiceCount; i++)
            {
                var voice = voices[i];
                voices[i] = null;
                if (voice == null) continue;
                try
                {
                    if (voice.Handle != IntPtr.Zero)
                    {
                        waveOutReset(voice.Handle);
                        if (voice.Header != IntPtr.Zero)
                            waveOutUnprepareHeader(voice.Handle, voice.Header, headerSize);
                        waveOutClose(voice.Handle);
                        voice.Handle = IntPtr.Zero;
                    }
                }
                catch
                {
                }
                if (voice.Header != IntPtr.Zero)
                {
                    try { Marshal.FreeHGlobal(voice.Header); } catch { }
                    voice.Header = IntPtr.Zero;
                }
                if (voice.Data != IntPtr.Zero)
                {
                    try { Marshal.FreeHGlobal(voice.Data); } catch { }
                    voice.Data = IntPtr.Zero;
                }
                voice.Ready = false;
                voice.EndsAt = 0;
            }
            nextVoice = 0;
        }

        internal bool Play(short[] samples, int count, float volume)
        {
            if (samples == null || count <= 0) return false;
            lock (gate)
            {
                EnsureOpen();
                if (unavailable) return false;
                if (count > MaxSamples) count = MaxSamples;
                if (count > samples.Length) count = samples.Length;
                var voice = PickVoice(Environment.TickCount64);
                if (voice == null) return false;
                try
                {
                    waveOutReset(voice.Handle);
                    var gain = volume;
                    if (gain < 0f) gain = 0f;
                    if (gain > 1f) gain = 1f;
                    for (var i = 0; i < count; i++)
                    {
                        var scaled = samples[i] * gain / 32768f;
                        var limited = (float)Math.Tanh(scaled) * 32767f;
                        if (limited > 32767f) limited = 32767f;
                        else if (limited < -32768f) limited = -32768f;
                        mix[i] = (short)limited;
                    }
                    Marshal.Copy(mix, 0, voice.Data, count);
                    var header = Marshal.PtrToStructure<WaveHeader>(voice.Header);
                    header.BufferLength = (uint)(count * 2);
                    header.BytesRecorded = 0;
                    Marshal.StructureToPtr(header, voice.Header, false);
                    if (waveOutWrite(voice.Handle, voice.Header, headerSize) != 0)
                    {
                        Failures++;
                        return false;
                    }
                    voice.EndsAt = Environment.TickCount64 + (long)count * 1000L / 48000L + 40L;
                    return true;
                }
                catch
                {
                    Failures++;
                    return false;
                }
            }
        }

        Voice PickVoice(long now)
        {
            for (var i = 0; i < VoiceCount; i++)
            {
                var index = (nextVoice + i) % VoiceCount;
                var voice = voices[index];
                if (voice != null && voice.Ready && voice.EndsAt <= now)
                {
                    nextVoice = (index + 1) % VoiceCount;
                    return voice;
                }
            }
            Voice oldest = null;
            var oldestIndex = -1;
            for (var i = 0; i < VoiceCount; i++)
            {
                var voice = voices[i];
                if (voice == null || !voice.Ready) continue;
                if (oldest == null || voice.EndsAt < oldest.EndsAt)
                {
                    oldest = voice;
                    oldestIndex = i;
                }
            }
            if (oldestIndex >= 0) nextVoice = (oldestIndex + 1) % VoiceCount;
            return oldest;
        }

        public void Dispose()
        {
            lock (gate)
            {
                Release();
            }
            GC.SuppressFinalize(this);
        }
    }
}
