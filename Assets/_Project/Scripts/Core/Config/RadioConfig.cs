using UnityEngine;

namespace LastSeenWearing.Core.Config
{
    /// <summary>
    /// The voice channels (docs/DATA.md §3, GDD §04.2, D-032): the radio's codec rate, packet length,
    /// playback buffer and radio sound. Stage noise, the proximity leak and the talk light join with P1.16.
    /// </summary>
    [CreateAssetMenu(fileName = "RadioConfig", menuName = "Last Seen Wearing/Config/Radio")]
    public sealed class RadioConfig : ScriptableObject
    {
        [Header("Codec")]
        [Tooltip("Samples per second on the wire (µ-law, one byte each). 8 kHz = telephone band. [PROVISIONAL]")]
        [SerializeField, Range(4000, 16000)] private int _sampleRate = 8000;
        [Tooltip("Milliseconds of voice per packet. [PROVISIONAL]")]
        [SerializeField, Range(20, 200)] private int _packetMilliseconds = 100;

        [Header("Playback")]
        [Tooltip("Milliseconds buffered before playback starts — absorbs network jitter. [PROVISIONAL]")]
        [SerializeField, Range(0, 1000)] private int _prebufferMilliseconds = 200;
        [Tooltip("Milliseconds held at most; older audio is dropped. [PROVISIONAL]")]
        [SerializeField, Range(200, 5000)] private int _maxBufferMilliseconds = 1000;
        [SerializeField, Range(0f, 2f)] private float _radioVolume = 1f;

        [Header("Radio sound")]
        [Tooltip("Band-pass low edge, Hz.")]
        [SerializeField, Range(50f, 1000f)] private float _radioLowCut = 300f;
        [Tooltip("Band-pass high edge, Hz.")]
        [SerializeField, Range(1000f, 8000f)] private float _radioHighCut = 3400f;

        public int SampleRate => _sampleRate;
        public int PacketSamples => _sampleRate * _packetMilliseconds / 1000;
        public int PrebufferSamples => _sampleRate * _prebufferMilliseconds / 1000;
        public int MaxBufferSamples => _sampleRate * _maxBufferMilliseconds / 1000;
        public float RadioVolume => _radioVolume;
        public float RadioLowCut => _radioLowCut;
        public float RadioHighCut => _radioHighCut;
    }
}
