using LastSeenWearing.Core.Roles;

namespace LastSeenWearing.Core.Voice
{
    /// <summary>
    /// Who may talk on a channel and who hears it (GDD §03, §04.2) — decided by the server for every packet.
    /// The radio is one way: the Watcher talks and the field team hears it anywhere; the field team cannot talk
    /// on it (GDD §03: "talk on radio (listen only)"), and the fugitive never hears it directly — only its
    /// proximity leak (P1.16).
    /// </summary>
    public static class VoiceRouting
    {
        public static bool MayTalk(Role speaker, VoiceChannel channel) => channel switch
        {
            VoiceChannel.Radio => speaker == Role.Watcher,
            _ => false, // proximity arrives with P1.16
        };

        public static bool Hears(Role speaker, VoiceChannel channel, Role listener) =>
            MayTalk(speaker, channel) && channel switch
            {
                VoiceChannel.Radio => RoleRules.IsFieldTeam(listener),
                _ => false,
            };
    }
}
