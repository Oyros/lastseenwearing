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
    /// The voice network (GDD §04.2, P1.15–P1.16, D-032). A talking client sends its µ-law packets to the server;
    /// the server checks the speaker may use the channel and forwards each packet to exactly the clients who
    /// hear it (<see cref="VoiceRouting"/>, roles from the roster, positions from the player bodies) — the server
    /// owns who hears what. The Watcher holds PushToTalk for the radio, which the field team hears anywhere and
    /// anyone else overhears near an officer (the leak); a body holds PushToTalk for proximity. The server also
    /// tells everyone when the radio is on air, which lights every <see cref="RadioLight"/>.
    /// </summary>
    public sealed class VoiceRelay : MonoBehaviour
    {
        private const string ToServer = "lsw.voice.up";
        private const string ToListener = "lsw.voice.down";
        private const string AirState = "lsw.voice.air";

        // Header sizes: up = channel; down = source id (speaker, or the officer a leak comes from) + channel.
        private const int UpHeader = sizeof(byte);
        private const int DownHeader = sizeof(ulong) + sizeof(byte);

        [SerializeField] private RadioConfig _config;
        [SerializeField] private RoleRosterSync _roster;

        private readonly Dictionary<(ulong, VoiceChannel), VoicePlayback> _playbacks = new();
        private readonly List<Vector3> _officerPositions = new();
        private readonly List<ulong> _officerIds = new();
        private LastSeenWearingControls _controls;
        private VoiceCapture _capture;
        private VoiceChannel _talkChannel;
        private NetworkManager _network;
        private OnAir _onAir;
        private bool _broadcastOnAir;
        private bool _registered;

        public int PacketsSent { get; private set; }
        public int PacketsRelayed { get; private set; }
        public int PacketsRefused { get; private set; }
        public int LeaksRelayed { get; private set; }
        public VoiceCapture Capture => _capture;

        /// <summary>What this client is playing, by source and channel (debug).</summary>
        public IReadOnlyDictionary<(ulong, VoiceChannel), VoicePlayback> Playbacks => _playbacks;

        private void Awake()
        {
            _controls = new LastSeenWearingControls();
            _onAir = new OnAir(_config.OnAirHoldSeconds);
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
            if (network.IsServer)
            {
                UpdateOnAir();
            }

            var myRole = _roster.RoleOf(network.LocalClientId);
            OpenCapture(myRole);
            if (_capture == null)
            {
                return;
            }

            var talk = _talkChannel == VoiceChannel.Radio ? _controls.Watcher.PushToTalk : _controls.Field.PushToTalk;
            _capture.Pump(talk.IsPressed(), packet => Send(_talkChannel, packet));
        }

        // The microphone is open for whoever may talk: the Watcher on the radio, a body in proximity.
        private void OpenCapture(Role role)
        {
            var channel = VoiceRouting.MayTalk(role, VoiceChannel.Radio) ? VoiceChannel.Radio
                : VoiceRouting.MayTalk(role, VoiceChannel.Proximity) ? VoiceChannel.Proximity
                : (VoiceChannel)0;
            if (_capture != null && channel == _talkChannel)
            {
                return;
            }

            _capture?.Dispose();
            _capture = null;
            _controls.Disable();
            _talkChannel = channel;
            if (channel == 0)
            {
                return;
            }

            if (channel == VoiceChannel.Radio)
            {
                _controls.Watcher.Enable();
            }
            else
            {
                _controls.Field.Enable();
            }

            _capture = new VoiceCapture(_config.SampleRate, _config.PacketSamples);
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
            messages.RegisterNamedMessageHandler(AirState, OnAirState);
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
                messages.UnregisterNamedMessageHandler(AirState);
            }

            _registered = false;
            foreach (var playback in _playbacks.Values)
            {
                if (playback != null)
                {
                    Destroy(playback.gameObject);
                }
            }

            _playbacks.Clear();
            RadioLight.SetOnAir(false);
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

            if (channel == VoiceChannel.Radio)
            {
                _onAir.Packet(Time.unscaledTimeAsDouble);
                CollectOfficers();
            }

            var speakerBody = BodyOf(speaker);
            foreach (var listener in _network.ConnectedClientsIds)
            {
                if (listener == speaker)
                {
                    continue;
                }

                var listenerRole = _roster.RoleOf(listener);
                var listenerBody = BodyOf(listener);
                if (channel == VoiceChannel.Radio)
                {
                    if (VoiceRouting.Hears(speakerRole, channel, listenerRole))
                    {
                        Deliver(listener, speaker, VoiceChannel.Radio, packet, count);
                    }
                    else if (listenerBody != null)
                    {
                        var officer = VoiceRouting.LeakSource(listenerRole, listenerBody.position, _officerPositions, _config.LeakRadius);
                        if (officer >= 0)
                        {
                            LeaksRelayed++;
                            Deliver(listener, _officerIds[officer], VoiceChannel.RadioLeak, packet, count);
                        }
                    }
                }
                else if (speakerBody != null && listenerBody != null
                         && VoiceRouting.HearsProximity(speakerRole, speakerBody.position, listenerRole, listenerBody.position, _config.ProximityRadius))
                {
                    Deliver(listener, speaker, VoiceChannel.Proximity, packet, count);
                }
            }
        }

        private void CollectOfficers()
        {
            _officerPositions.Clear();
            _officerIds.Clear();
            foreach (var client in _network.ConnectedClientsIds)
            {
                var body = BodyOf(client);
                if (body != null && RoleRules.IsFieldTeam(_roster.RoleOf(client)))
                {
                    _officerIds.Add(client);
                    _officerPositions.Add(body.position);
                }
            }
        }

        private void Deliver(ulong listener, ulong source, VoiceChannel channel, byte[] packet, int count)
        {
            PacketsRelayed++;
            if (listener == _network.LocalClientId)
            {
                Play(source, channel, packet, count); // the host listens: no message to itself
                return;
            }

            using var writer = new FastBufferWriter(DownHeader + count, Allocator.Temp);
            writer.WriteValueSafe(source);
            writer.WriteValueSafe((byte)channel);
            writer.WriteBytesSafe(packet, count);
            _network.CustomMessagingManager.SendNamedMessage(ToListener, listener, writer, NetworkDelivery.ReliableSequenced);
        }

        // Server: tell everyone when the radio goes on or off air.
        private void UpdateOnAir()
        {
            var onAir = _onAir.IsOnAir(Time.unscaledTimeAsDouble);
            if (onAir == _broadcastOnAir)
            {
                return;
            }

            _broadcastOnAir = onAir;
            RadioLight.SetOnAir(onAir);
            using var writer = new FastBufferWriter(sizeof(bool), Allocator.Temp);
            writer.WriteValueSafe(onAir);
            _network.CustomMessagingManager.SendNamedMessageToAll(AirState, writer, NetworkDelivery.ReliableSequenced);
        }

        private void OnAirState(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId)
            {
                return;
            }

            reader.ReadValueSafe(out bool onAir);
            RadioLight.SetOnAir(onAir);
        }

        // Listener: a packet the server says this client hears.
        private void OnDown(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId)
            {
                return; // only the server routes voice
            }

            reader.ReadValueSafe(out ulong source);
            reader.ReadValueSafe(out byte channel);
            var count = reader.Length - reader.Position;
            var packet = new byte[count];
            reader.ReadBytesSafe(ref packet, count);
            Play(source, (VoiceChannel)channel, packet, count);
        }

        private void Play(ulong source, VoiceChannel channel, byte[] packet, int count)
        {
            var key = (source, channel);
            if (!_playbacks.TryGetValue(key, out var playback) || playback == null)
            {
                playback = Create(source, channel);
                if (playback == null)
                {
                    return; // a proximity or leak source without a body here yet
                }

                _playbacks[key] = playback;
            }

            playback.Receive(packet, count);
        }

        private VoicePlayback Create(ulong source, VoiceChannel channel)
        {
            if (channel == VoiceChannel.Radio)
            {
                return VoicePlayback.CreateRadio(transform, source.ToString(), _config);
            }

            var body = BodyOf(source);
            if (body == null)
            {
                return null;
            }

            return channel == VoiceChannel.Proximity
                ? VoicePlayback.CreateProximity(body, source.ToString(), _config)
                : VoicePlayback.CreateLeak(body, source.ToString(), _config);
        }

        // GetPlayerNetworkObject answers only for the local player on a client, so look the body up by owner.
        private Transform BodyOf(ulong clientId)
        {
            foreach (var spawned in _network.SpawnManager.SpawnedObjectsList)
            {
                if (spawned.IsPlayerObject && spawned.OwnerClientId == clientId)
                {
                    return spawned.transform;
                }
            }

            return null;
        }
    }
}
