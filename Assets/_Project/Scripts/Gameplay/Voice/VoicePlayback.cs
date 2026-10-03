using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Voice;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Voice
{
    /// <summary>
    /// One speaker's voice on one channel, played on this client: decoded packets go into a
    /// <see cref="JitterBuffer"/> that a streaming clip reads on the audio thread. The radio plays flat (2D —
    /// heard anywhere) through a band-pass that makes it sound like a radio (GDD §04.2).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class VoicePlayback : MonoBehaviour
    {
        private const int StreamSeconds = 1;

        private JitterBuffer _buffer;
        private float[] _decoded;

        public int PacketsReceived { get; private set; }

        /// <summary>Peak of the last packet received, 0–1 (debug).</summary>
        public float LastPeak { get; private set; }

        public bool IsPlaying => _buffer != null && _buffer.IsPlaying;

        public static VoicePlayback CreateRadio(Transform parent, string speakerName, RadioConfig config)
        {
            var go = new GameObject($"Radio_{speakerName}", typeof(AudioSource));
            go.transform.SetParent(parent, false);
            var playback = go.AddComponent<VoicePlayback>();
            playback.Begin(config);

            var source = go.GetComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.volume = config.RadioVolume;
            go.AddComponent<AudioHighPassFilter>().cutoffFrequency = config.RadioLowCut;
            go.AddComponent<AudioLowPassFilter>().cutoffFrequency = config.RadioHighCut;
            return playback;
        }

        public void Receive(byte[] packet, int count)
        {
            if (_decoded.Length < count)
            {
                _decoded = new float[count];
            }

            MuLaw.Decode(packet, count, _decoded);
            var peak = 0f;
            for (var i = 0; i < count; i++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(_decoded[i]));
            }

            LastPeak = peak;
            PacketsReceived++;
            _buffer.Write(_decoded, count);
        }

        private void Begin(RadioConfig config)
        {
            _buffer = new JitterBuffer(config.MaxBufferSamples, config.PrebufferSamples);
            _decoded = new float[config.PacketSamples];
            var source = GetComponent<AudioSource>();
            source.clip = AudioClip.Create(name, config.SampleRate * StreamSeconds, 1, config.SampleRate, true, OnAudioRead);
            source.loop = true;
            source.playOnAwake = false;
            source.Play();
        }

        // Audio thread.
        private void OnAudioRead(float[] data)
        {
            _buffer?.Read(data);
        }
    }
}
