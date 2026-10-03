using System.Collections.Generic;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Core.Voice;
using LastSeenWearing.Gameplay.Player;
using LastSeenWearing.Gameplay.Roles;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace LastSeenWearing.Gameplay.Voice
{
    /// <summary>
    /// The voice network (GDD §04.2, P1.15, D-032). A talking client sends its µ-law packets to the server; the
    /// server checks the speaker may use the channel and forwards each packet to exactly the clients who hear
    /// it (<see cref="VoiceRouting"/>, roles from the roster) — the server owns who hears what. Listeners play
    /// each speaker through a <see cref="VoicePlayback"/>. P1.15 is the radio: the Watcher holds PushToTalk,
    /// the field team hears it anywhere.
    /// </summary>
    public sealed class VoiceRelay : MonoBehaviour
    {
        private const string ToServer = "lsw.voice.up";
        private const string ToListener = "lsw.voice.down";

        // Header sizes: up = channel; down = speaker id + channel.
        private const int UpHeader = sizeof(byte);
        private const int DownHeader = sizeof(ulong) + sizeof(byte);

        [SerializeField] private RadioConfig _config;
        [SerializeField] private RoleRosterSync _roster;

        private readonly Dictionary<ulong, VoicePlayback> _radios = new();
        private LastSeenWearingControls _controls;
        private VoiceCapture _capture;
        private NetworkManager _network;
        private bool _registered;

        public int PacketsSent { get; private set; }
        public int PacketsRelayed { get; private set; }
        public int PacketsRefused { get; private set; }
        public VoiceCapture Capture => _capture;
        public IReadOnlyDictionary<ulong, VoicePlayback> Radios => _radios;

        private void Awake()
        {
            _controls = new LastSeenWearingControls();
        }

        private void OnDestroy()
        {
            Unregister();
            _capture?.Dispose();
            _controls.Dispose();
        }

        private void Update()
        {
            var network = NetworkManager.Singleton;
            if (network == null || !network.IsListening || !_roster.IsSpawned)
            {
                Unregister();
                return;
            }

            Register(network);
            var myRole = _roster.RoleOf(network.LocalClientId);
            var mayTalk = VoiceRouting.MayTalk(myRole, VoiceChannel.Radio);
            OpenCapture(mayTalk);
            if (_capture == null)
            {
                return;
            }

            var transmitting = _controls.Watcher.PushToTalk.IsPressed();
            _capture.Pump(transmitting, packet => Send(VoiceChannel.Radio, packet));
        }

        private void OpenCapture(bool open)
        {
            if (open && _capture == null)
            {
                _controls.Watcher.Enable();
                _capture = new VoiceCapture(_config.SampleRate, _config.PacketSamples);
            }
            else if (!open && _capture != null)
            {
                _controls.Watcher.Disable();
                _capture.Dispose();
                _capture = null;
            }
        }

        private void Register(NetworkManager network)
        {
            if (_registered && _network == network)
            {
                return;
            }

            _network = network;
            var messages = network.CustomMessagingManager;
            if (network.IsServer)
            {
                messages.RegisterNamedMessageHandler(ToServer, OnUp);
            }

            messages.RegisterNamedMessageHandler(ToListener, OnDown);
            _registered = true;
        }

        private void Unregister()
        {
            if (!_registered)
            {
                return;
            }

            var messages = _network != null ? _network.CustomMessagingManager : null;
            if (messages != null)
            {
                messages.UnregisterNamedMessageHandler(ToServer);
                messages.UnregisterNamedMessageHandler(ToListener);
            }

            _registered = false;
            foreach (var radio in _radios.Values)
            {
                Destroy(radio.gameObject);
            }

            _radios.Clear();
        }

        private void Send(VoiceChannel channel, byte[] packet)
        {
            PacketsSent++;
            if (_network.IsServer)
            {
                Route(_network.LocalClientId, channel, packet, packet.Length); // the host talks: route in place
                return;
            }

            using var writer = new FastBufferWriter(UpHeader + packet.Length, Allocator.Temp);
            writer.WriteValueSafe((byte)channel);
            writer.WriteBytesSafe(packet);
            _network.CustomMessagingManager.SendNamedMessage(ToServer, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
        }

        // Server: a client's packet arrives.
        private void OnUp(ulong sender, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte channel);
            var count = reader.Length - reader.Position;
            var packet = new byte[count];
            reader.ReadBytesSafe(ref packet, count);
            Route(sender, (VoiceChannel)channel, packet, count);
        }

        // Server: forward a packet to everyone who hears it.
        private void Route(ulong speaker, VoiceChannel channel, byte[] packet, int count)
        {
            var speakerRole = _roster.RoleOf(speaker);
            if (!VoiceRouting.MayTalk(speakerRole, channel))
            {
                PacketsRefused++;
                return;
            }

            foreach (var listener in _network.ConnectedClientsIds)
            {
                if (listener == speaker || !VoiceRouting.Hears(speakerRole, channel, _roster.RoleOf(listener)))
                {
                    continue;
                }

                PacketsRelayed++;
                if (listener == _network.LocalClientId)
                {
                    Play(speaker, channel, packet, count); // the host listens: no message to itself
                    continue;
                }

                using var writer = new FastBufferWriter(DownHeader + count, Allocator.Temp);
                writer.WriteValueSafe(speaker);
                writer.WriteValueSafe((byte)channel);
                writer.WriteBytesSafe(packet, count);
                _network.CustomMessagingManager.SendNamedMessage(ToListener, listener, writer, NetworkDelivery.ReliableSequenced);
            }
        }

        // Listener: a packet the server says this client hears.
        private void OnDown(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId)
            {
                return; // only the server routes voice
            }

            reader.ReadValueSafe(out ulong speaker);
            reader.ReadValueSafe(out byte channel);
            var count = reader.Length - reader.Position;
            var packet = new byte[count];
            reader.ReadBytesSafe(ref packet, count);
            Play(speaker, (VoiceChannel)channel, packet, count);
        }

        private void Play(ulong speaker, VoiceChannel channel, byte[] packet, int count)
        {
            if (channel != VoiceChannel.Radio)
            {
                return; // proximity playback arrives with P1.16
            }

            if (!_radios.TryGetValue(speaker, out var radio))
            {
                radio = VoicePlayback.CreateRadio(transform, speaker.ToString(), _config);
                _radios.Add(speaker, radio);
            }

            radio.Receive(packet, count);
        }
    }
}
