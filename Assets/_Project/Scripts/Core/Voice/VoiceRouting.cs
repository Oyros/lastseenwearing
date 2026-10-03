using System.Collections.Generic;
using LastSeenWearing.Core.Roles;
using UnityEngine;

namespace LastSeenWearing.Core.Voice
{
    /// <summary>
    /// Who may talk on a channel and who hears it (GDD §03, §04.2) — decided by the server for every packet.
    /// The radio is one way: the Watcher talks and the field team hears it anywhere; the field team cannot talk
    /// on it (GDD §03: "talk on radio (listen only)"). Proximity is every body talking to every body in range;
    /// the Watcher has none. The radio leaks: anyone with a body who is not on the field team hears it from the
    /// nearest officer within the leak radius (P1.16) — that is how the fugitive overhears it.
    /// </summary>
    public static class VoiceRouting
    {
        /// <summary>Roles with a body in the festival — all but the Watcher (GDD §03).</summary>
        public static bool HasBody(Role role) => role is not (Role.None or Role.Watcher);

        public static bool MayTalk(Role speaker, VoiceChannel channel) => channel switch
        {
            VoiceChannel.Radio => speaker == Role.Watcher,
            VoiceChannel.Proximity => HasBody(speaker),
            _ => false, // a leak is never spoken, only overheard
        };

        /// <summary>The radio itself: no positions involved.</summary>
        public static bool Hears(Role speaker, VoiceChannel channel, Role listener) =>
            channel == VoiceChannel.Radio && MayTalk(speaker, channel) && RoleRules.IsFieldTeam(listener);

        public static bool HearsProximity(Role speaker, Vector3 speakerAt, Role listener, Vector3 listenerAt, float radius) =>
            MayTalk(speaker, VoiceChannel.Proximity) && HasBody(listener)
            && (listenerAt - speakerAt).sqrMagnitude <= radius * radius;

        /// <summary>
        /// Index of the officer whose radio <paramref name="listener"/> overhears — the nearest within
        /// <paramref name="leakRadius"/> — or -1. The field team hears the radio itself, never a leak.
        /// </summary>
        public static int LeakSource(Role listener, Vector3 listenerAt, IReadOnlyList<Vector3> officers, float leakRadius)
        {
            if (!HasBody(listener) || RoleRules.IsFieldTeam(listener))
            {
                return -1;
            }

            var best = -1;
            var bestDistance = leakRadius * leakRadius;
            for (var i = 0; i < officers.Count; i++)
            {
                var distance = (officers[i] - listenerAt).sqrMagnitude;
                if (distance <= bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
