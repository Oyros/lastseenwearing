using System;
using LastSeenWearing.Core.Voice;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Voice
{
    /// <summary>
    /// The local microphone, cut into µ-law packets at the radio's rate (D-032). The microphone runs while the
    /// capture is open; <see cref="Pump"/> collects what it recorded since the last call and, while
    /// <c>transmitting</c>, hands out a packet for every <c>packetSamples</c> it has. Audio recorded while
    /// not transmitting is thrown away, so a press never sends the past.
    /// </summary>
    public sealed class VoiceCapture : IDisposable
    {
        private const int LoopSeconds = 1;
        private const int PreferredMicrophoneRate = 16000;

        private readonly int _rate;
        private readonly int _packetSamples;
        private readonly int _microphoneRate;
        private readonly AudioClip _clip;
        private readonly float[] _raw;
        private readonly float[] _resampled;
        private readonly float[] _pending;
        private int _pendingCount;
        private int _lastPosition;

        public VoiceCapture(int rate, int packetSamples)
        {
            _rate = rate;
            _packetSamples = packetSamples;
            Microphone.GetDeviceCaps(null, out var min, out var max);
            _microphoneRate = min == 0 && max == 0 ? PreferredMicrophoneRate : Mathf.Clamp(PreferredMicrophoneRate, min, max);
            _clip = Microphone.Start(null, true, LoopSeconds, _microphoneRate);
            _raw = new float[_microphoneRate * LoopSeconds];
            _resampled = new float[Resampler.OutputCount(_raw.Length, _microphoneRate, rate) + 1];
            _pending = new float[_resampled.Length + packetSamples];
            Debug.Log($"[Voice] Microphone '{(Microphone.devices.Length > 0 ? Microphone.devices[0] : "none")}' at {_microphoneRate} Hz → {rate} Hz.");
        }

        public bool IsRecording => _clip != null && Microphone.IsRecording(null);

        /// <summary>Peak level of the last packet handed out, 0–1 (debug).</summary>
        public float LastPeak { get; private set; }

        /// <summary>Calls <paramref name="send"/> with each full packet recorded since the last pump.</summary>
        public void Pump(bool transmitting, Action<byte[]> send)
        {
            if (!IsRecording)
            {
                return;
            }

            var position = Microphone.GetPosition(null);
            var available = (position - _lastPosition + _raw.Length) % _raw.Length;
            if (available == 0)
            {
                return;
            }

            if (!transmitting)
            {
                _lastPosition = position;
                _pendingCount = 0;
                return;
            }

            ReadLoop(_lastPosition, available);
            _lastPosition = position;
            var produced = Resampler.Resample(_raw, available, _microphoneRate, _resampled, _rate);
            Array.Copy(_resampled, 0, _pending, _pendingCount, produced);
            _pendingCount += produced;

            while (_pendingCount >= _packetSamples)
            {
                var packet = new byte[_packetSamples];
                var peak = 0f;
                for (var i = 0; i < _packetSamples; i++)
                {
                    peak = Mathf.Max(peak, Mathf.Abs(_pending[i]));
                }

                MuLaw.Encode(_pending, _packetSamples, packet);
                LastPeak = peak;
                _pendingCount -= _packetSamples;
                Array.Copy(_pending, _packetSamples, _pending, 0, _pendingCount);
                send(packet);
            }
        }

        public void Dispose()
        {
            Microphone.End(null);
        }

        // The clip is a ring: read `count` samples from `start`, wrapping, into the front of _raw.
        private void ReadLoop(int start, int count)
        {
            var firstPart = Mathf.Min(count, _raw.Length - start);
            var buffer = new float[firstPart];
            _clip.GetData(buffer, start);
            Array.Copy(buffer, _raw, firstPart);
            if (count > firstPart)
            {
                var rest = new float[count - firstPart];
                _clip.GetData(rest, 0);
                Array.Copy(rest, 0, _raw, firstPart, rest.Length);
            }
        }
    }
}
