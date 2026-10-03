using System.Collections.Generic;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Core.Voice;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSeenWearing.Tests.Voice
{
    /// <summary>P1.15: the Watcher's radio reaches the field team anywhere — codec, buffer and who hears whom.</summary>
    public sealed class VoiceTests
    {
        [Test]
        public void MuLawRoundTripsWithinItsStep()
        {
            for (var x = -1f; x <= 1f; x += 0.001f)
            {
                var back = MuLaw.Decode(MuLaw.Encode(x));
                // µ-law error grows with level: ~1/16 of the value plus a small floor.
                Assert.That(back, Is.EqualTo(x).Within(System.Math.Abs(x) / 16f + 0.002f), x.ToString());
            }

            Assert.That(MuLaw.Decode(MuLaw.Encode(0f)), Is.EqualTo(0f).Within(0.001f), "silence stays silent");
            Assert.That(MuLaw.Decode(MuLaw.Encode(5f)), Is.EqualTo(1f).Within(0.02f), "clipped, not wrapped");
        }

        [Test]
        public void ResamplingKeepsLengthAndLevel()
        {
            var input = new float[1600];
            for (var i = 0; i < input.Length; i++)
            {
                input[i] = 0.5f;
            }

            var output = new float[800];
            Assert.That(Resampler.Resample(input, 1600, 16000, output, 8000), Is.EqualTo(800));
            Assert.That(output, Has.All.EqualTo(0.5f).Within(1e-6f));

            var up = new float[3200];
            Assert.That(Resampler.Resample(input, 1600, 16000, up, 32000), Is.EqualTo(3200));
            Assert.That(up, Has.All.EqualTo(0.5f).Within(1e-6f));
        }

        [Test]
        public void TheBufferWaitsThenPlaysInOrderThenGoesQuiet()
        {
            var buffer = new JitterBuffer(capacity: 100, prebuffer: 4);
            var out3 = new float[3];
            buffer.Write(new[] { 1f, 2f, 3f }, 3);
            buffer.Read(out3);
            Assert.That(out3, Is.EqualTo(new[] { 0f, 0f, 0f }), "below the prebuffer: silence");

            buffer.Write(new[] { 4f, 5f }, 2);
            buffer.Read(out3);
            Assert.That(out3, Is.EqualTo(new[] { 1f, 2f, 3f }));
            var out4 = new float[4];
            buffer.Read(out4);
            Assert.That(out4, Is.EqualTo(new[] { 4f, 5f, 0f, 0f }), "runs dry into silence");
            Assert.That(buffer.IsPlaying, Is.False, "and waits for the prebuffer again");
        }

        [Test]
        public void AFullBufferDropsTheOldest()
        {
            var buffer = new JitterBuffer(capacity: 3, prebuffer: 1);
            buffer.Write(new[] { 1f, 2f, 3f, 4f, 5f }, 5);
            var out3 = new float[3];
            buffer.Read(out3);
            Assert.That(out3, Is.EqualTo(new[] { 3f, 4f, 5f }));
        }

        [Test]
        public void TheRadioGoesFromTheWatcherToTheFieldTeamOnly()
        {
            Assert.That(VoiceRouting.MayTalk(Role.Watcher, VoiceChannel.Radio), Is.True);
            foreach (var role in new[] { Role.Patrol, Role.Plainclothes, Role.Dog, Role.Fugitive, Role.None })
            {
                Assert.That(VoiceRouting.MayTalk(role, VoiceChannel.Radio), Is.False, $"{role} listens only");
            }

            foreach (var role in new[] { Role.Patrol, Role.Plainclothes, Role.Dog })
            {
                Assert.That(VoiceRouting.Hears(Role.Watcher, VoiceChannel.Radio, role), Is.True, $"{role} hears it");
            }

            foreach (var role in new[] { Role.Fugitive, Role.Watcher, Role.None })
            {
                Assert.That(VoiceRouting.Hears(Role.Watcher, VoiceChannel.Radio, role), Is.False, $"{role} does not");
            }

            Assert.That(VoiceRouting.Hears(Role.Patrol, VoiceChannel.Radio, Role.Dog), Is.False, "no one else broadcasts");
        }

        [Test]
        public void ProximityIsBodyToBodyInRange()
        {
            var here = Vector3.zero;
            var near = new Vector3(0f, 0f, 11f);
            var far = new Vector3(0f, 0f, 13f);
            Assert.That(VoiceRouting.HearsProximity(Role.Patrol, here, Role.Fugitive, near, 12f), Is.True);
            Assert.That(VoiceRouting.HearsProximity(Role.Fugitive, here, Role.Patrol, near, 12f), Is.True, "the fugitive's voice carries too");
            Assert.That(VoiceRouting.HearsProximity(Role.Patrol, here, Role.Fugitive, far, 12f), Is.False, "out of range");
            Assert.That(VoiceRouting.HearsProximity(Role.Patrol, here, Role.Watcher, near, 12f), Is.False, "the Watcher has no body");
            Assert.That(VoiceRouting.MayTalk(Role.Watcher, VoiceChannel.Proximity), Is.False);
            Assert.That(VoiceRouting.MayTalk(Role.Patrol, VoiceChannel.RadioLeak), Is.False, "a leak is overheard, never spoken");
        }

        [Test]
        public void TheFugitiveOverhearsTheNearestOfficersRadio()
        {
            var officers = new List<Vector3> { new(10f, 0f, 0f), new(3f, 0f, 0f), new(0f, 0f, 4f) };
            Assert.That(VoiceRouting.LeakSource(Role.Fugitive, Vector3.zero, officers, 5f), Is.EqualTo(1), "nearest within range");
            Assert.That(VoiceRouting.LeakSource(Role.Fugitive, new Vector3(-20f, 0f, 0f), officers, 5f), Is.EqualTo(-1), "too far");
            Assert.That(VoiceRouting.LeakSource(Role.Patrol, Vector3.zero, officers, 5f), Is.EqualTo(-1), "the field team hears the radio itself");
            Assert.That(VoiceRouting.LeakSource(Role.Watcher, Vector3.zero, officers, 5f), Is.EqualTo(-1), "no body, nothing to overhear with");
            Assert.That(VoiceRouting.LeakSource(Role.Fugitive, Vector3.zero, new List<Vector3>(), 5f), Is.EqualTo(-1));
        }

        [Test]
        public void TheRadioIsOnAirUntilItGoesQuiet()
        {
            var air = new OnAir(0.3);
            Assert.That(air.IsOnAir(0d), Is.False, "silent at the start");
            air.Packet(10d);
            Assert.That(air.IsOnAir(10.1d), Is.True);
            Assert.That(air.IsOnAir(10.29d), Is.True);
            Assert.That(air.IsOnAir(10.31d), Is.False);
        }

        [Test]
        public void ThePatrolCarriesARadioLight()
        {
            var patrol = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Patrol.prefab");
            var light = patrol.GetComponentInChildren<LastSeenWearing.Gameplay.Voice.RadioLight>(true);
            Assert.That(light, Is.Not.Null);
            var lamp = new SerializedObject(light).FindProperty("_lamp").objectReferenceValue as Renderer;
            Assert.That(lamp, Is.Not.Null);
            Assert.That(lamp.name, Does.Contain("RadioLight"));
        }

        [Test]
        public void TheRadioConfigIsReachable()
        {
            var game = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/Config/GameConfig.asset");
            Assert.That(game.Radio, Is.Not.Null);
            Assert.That(game.Radio.PacketSamples, Is.GreaterThan(0));
            Assert.That(game.Radio.PacketSamples, Is.LessThan(1200), "a packet fits one datagram");
            Assert.That(game.Radio.PrebufferSamples, Is.LessThan(game.Radio.MaxBufferSamples));
        }
    }
}
