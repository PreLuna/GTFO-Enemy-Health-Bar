using System;
using System.Collections.Generic;
using System.IO;
using PreLuna.EnemyHealthBar.BFVFeedback.Core;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Runtime
{
    internal sealed class BfvAudio : IDisposable
    {
        readonly Dictionary<string, short[]> cues = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, short[]> filtered = new(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> missing = new(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> readFailed = new(StringComparer.OrdinalIgnoreCase);
        readonly List<string> roots = new();
        readonly WinMmPlayer player = new();
        BfvConfig config;
        short[] killChime;
        bool failed;
        bool warmed;
        bool warmupWarned;
        long nextWarmupAt;

        internal bool Ready => !failed;
        internal bool Warmed => warmed;

        internal void Initialize(BfvConfig settings, string pluginRoot)
        {
            config = settings;
            BuildRoots(pluginRoot, settings.CustomDirectory);
            killChime = BuildKillChime();
            var opened = player.Warmup();
            Log("audio backend=winmm; opened=" + opened + "; roots=" + roots.Count);
            Warmup();
        }

        internal void PlayHit(BfvHitCue cue, float channelVolume)
            => Play(cue, BfvKillTier.Minor, BfvFlight.None, channelVolume,
                config == null ? 1f : config.HitGain, false);

        internal void PlayKill(BfvKillTier tier, BfvFlight flight, float channelVolume)
        {
            Play(BfvHitCue.Kill, tier, flight, channelVolume,
                config == null ? 1.6f : config.KillGain, true);
            PlayKillChime();
        }

        void Play(BfvHitCue cue, BfvKillTier tier, BfvFlight flight, float channelVolume,
            float cueGain, bool kill)
        {
            if (!Ready || config == null) return;
            var volume = BfvPolicy.Clamp(config.Volume * channelVolume * config.Gain * cueGain, 0f, 3f, 0f);
            if (volume <= 0f) return;
            var candidates = BfvPolicy.CueCandidates(config.SoundStyle, cue, tier, flight);
            for (var i = 0; i < candidates.Length; i++)
            {
                var samples = kill ? LoadFiltered(candidates[i]) : Load(candidates[i]);
                if (samples == null) continue;
                if (player.Play(samples, samples.Length, volume)) return;
            }
        }

        void PlayKillChime()
        {
            if (config == null || !config.KillChimeEnabled || killChime == null || killChime.Length == 0)
                return;
            var volume = BfvPolicy.Clamp(
                config.Volume * config.KillChimeVolume * config.Gain, 0f, 2.5f, 0f);
            if (volume <= 0f) return;
            player.Play(killChime, killChime.Length, volume);
        }

        internal void Warmup()
        {
            if (warmed || failed || config == null) return;
            var now = Environment.TickCount64;
            if (now < nextWarmupAt) return;
            nextWarmupAt = now + 2000L;
            if (!player.Available && !player.Warmup())
            {
                if (!warmupWarned)
                {
                    warmupWarned = true;
                    Log("winmm unavailable; sound disabled until it can reopen (game audio is unaffected)");
                }
                return;
            }
            var baseCues = new[]
            {
                "audio/bf5/hit.wav",
                "audio/bf5/weakspot.wav",
                "audio/bf5/armor.wav",
                "audio/bf5/kill_minor.wav",
                "audio/bf5/kill_elite.wav",
                "audio/bf5/kill_boss.wav"
            };
            var loaded = 0;
            for (var i = 0; i < baseCues.Length; i++)
                if (Load(baseCues[i]) != null) loaded++;
            if (loaded > 0)
            {
                warmed = true;
                Log("audio warmup; base cues=" + loaded + "/" + baseCues.Length +
                    "; roots=" + roots.Count + "; style=" + config.SoundStyle);
            }
            else if (!warmupWarned)
            {
                warmupWarned = true;
                Log("audio warmup found no cues; will keep retrying quietly");
            }
        }

        short[] Load(string relative)
        {
            if (cues.TryGetValue(relative, out var cached)) return cached;
            if (missing.Contains(relative)) return null;
            var found = false;
            for (var i = 0; i < roots.Count; i++)
            {
                var path = Path.Combine(roots[i], relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path)) continue;
                found = true;
                try
                {
                    var bytes = File.ReadAllBytes(path);
                    var samples = ParsePcm16Mono(Path.GetFileNameWithoutExtension(path), bytes);
                    if (samples == null)
                    {
                        if (readFailed.Add(relative)) Log("cue unreadable: " + relative);
                        return null;
                    }
                    cues[relative] = samples;
                    return samples;
                }
                catch (Exception error)
                {
                    if (readFailed.Add(relative))
                        Log("read failed (" + relative + "): " + error.GetType().Name);
                    return null;
                }
            }
            if (found) return null;
            missing.Add(relative);
            Log("cue missing: " + relative + " roots=" + string.Join(";", roots));
            return null;
        }

        short[] LoadFiltered(string relative)
        {
            var hz = config == null ? 0f : config.KillHighPassHz;
            if (hz <= 0f) return Load(relative);
            var key = relative + "|hp" + (int)hz;
            if (filtered.TryGetValue(key, out var cached)) return cached;
            var source = Load(relative);
            if (source == null) return null;
            var result = HighPass(source, hz);
            filtered[key] = result;
            return result;
        }

        static short[] HighPass(short[] input, float hz)
        {
            if (input == null || input.Length == 0 || hz <= 0f) return input;
            var dt = 1f / 48000f;
            var rc = 1f / (2f * (float)Math.PI * hz);
            var alpha = rc / (rc + dt);
            var output = new short[input.Length];
            var previousInput = 0f;
            var previousOutput = 0f;
            for (var i = 0; i < input.Length; i++)
            {
                var current = input[i] / 32768f;
                var value = alpha * (previousOutput + current - previousInput);
                previousInput = current;
                previousOutput = value;
                var sample = value * 32768f;
                if (sample > 32767f) sample = 32767f;
                else if (sample < -32768f) sample = -32768f;
                output[i] = (short)sample;
            }
            return output;
        }

        static short[] BuildKillChime()
        {
            const int sampleRate = 48000;
            const int milliseconds = 90;
            var count = sampleRate * milliseconds / 1000;
            var samples = new short[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)sampleRate;
                var attack = 1f - (float)Math.Exp(-t * 400f);
                var decay = (float)Math.Exp(-t * 48f);
                var wave = .55f * (float)Math.Sin(2 * Math.PI * 1700f * t)
                    + .30f * (float)Math.Sin(2 * Math.PI * 2550f * t)
                    + .15f * (float)Math.Sin(2 * Math.PI * 3400f * t);
                var value = wave * attack * decay * .8f;
                if (value > 1f) value = 1f;
                else if (value < -1f) value = -1f;
                samples[i] = (short)(value * 32767f);
            }
            return samples;
        }

        static short[] ParsePcm16Mono(string name, byte[] bytes)
        {
            if (bytes == null || bytes.Length < 44) return null;
            if (bytes[0] != 'R' || bytes[1] != 'I' || bytes[2] != 'F' || bytes[3] != 'F') return null;
            if (bytes[8] != 'W' || bytes[9] != 'A' || bytes[10] != 'V' || bytes[11] != 'E') return null;

            var format = 0;
            var channels = 0;
            var sampleRate = 0;
            var bits = 0;
            var dataOffset = -1;
            var dataLength = 0;
            var position = 12;
            while (position + 8 <= bytes.Length)
            {
                var size = BitConverter.ToInt32(bytes, position + 4);
                if (size < 0 || position + 8 + size > bytes.Length) break;
                if (bytes[position] == 'f' && bytes[position + 1] == 'm' &&
                    bytes[position + 2] == 't' && bytes[position + 3] == ' ')
                {
                    if (size < 16) return null;
                    format = BitConverter.ToUInt16(bytes, position + 8);
                    channels = BitConverter.ToUInt16(bytes, position + 10);
                    sampleRate = BitConverter.ToInt32(bytes, position + 12);
                    bits = BitConverter.ToUInt16(bytes, position + 22);
                }
                else if (bytes[position] == 'd' && bytes[position + 1] == 'a' &&
                         bytes[position + 2] == 't' && bytes[position + 3] == 'a')
                {
                    dataOffset = position + 8;
                    dataLength = size;
                    break;
                }
                position += 8 + size + (size & 1);
            }

            if (dataOffset < 0 || dataLength < 2) return null;
            if (format != 1 || channels != 1 || sampleRate != 48000 || bits != 16)
            {
                Log("unsupported wav: " + name + " tag=" + format + " ch=" + channels +
                    " rate=" + sampleRate + " bits=" + bits);
                return null;
            }
            var count = dataLength / 2;
            if (count <= 0) return null;
            var samples = new short[count];
            for (var i = 0; i < count; i++)
                samples[i] = BitConverter.ToInt16(bytes, dataOffset + i * 2);
            return samples;
        }

        void BuildRoots(string pluginRoot, string customDirectory)
        {
            roots.Clear();
            var custom = (customDirectory ?? "").Trim();
            if (custom.Length > 0)
            {
                var path = Path.IsPathRooted(custom) ? custom : Path.Combine(pluginRoot, custom);
                if (Directory.Exists(path)) roots.Add(path);
                else Log("custom directory missing: " + path);
            }
            var assets = Path.Combine(pluginRoot, "Assets");
            if (Directory.Exists(assets)) roots.Add(assets);
            else Log("asset directory missing: " + assets);
        }

        public void Dispose()
        {
            try { player.Dispose(); } catch { }
            cues.Clear();
            filtered.Clear();
            missing.Clear();
            readFailed.Clear();
            roots.Clear();
            killChime = null;
            config = null;
            failed = false;
            warmed = false;
        }

        static void Log(string message)
            => Plugin.Instance?.LogSource?.LogInfo("[BFV] " + message);
    }
}
