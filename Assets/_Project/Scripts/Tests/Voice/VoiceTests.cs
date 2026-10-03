using LastSeenWearing.Core.Config;
using LastSeenWearing.Core.Roles;
using LastSeenWearing.Core.Voice;
using NUnit.Framework;
using UnityEditor;

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
