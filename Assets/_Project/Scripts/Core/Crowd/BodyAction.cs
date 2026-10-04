using LastSeenWearing.Core.Layouts;

namespace LastSeenWearing.Core.Crowd
{
    /// <summary>
    /// A one-shot a body plays over its walk (PL.17, P1.25): what a camera sees someone do. The name is the clip's
    /// in the body's export JSON and the state's in the crowd animator's Action layer.
    /// </summary>
    public enum BodyAction : byte
    {
        None,
        WalletLift,
        PosterSwap,
        TentEnter,
        TentExit,
        ArrestOfficer,
        ArrestSuspect,
    }

    public static class BodyActions
    {
        public static string ClipName(BodyAction action) => action switch
        {
            BodyAction.WalletLift => "Act_WalletLift",
            BodyAction.PosterSwap => "Act_PosterSwap",
            BodyAction.TentEnter => "Tent_Enter",
            BodyAction.TentExit => "Tent_Exit",
            BodyAction.ArrestOfficer => "Arrest_Officer",
            BodyAction.ArrestSuspect => "Arrest_Suspect",
            _ => null,
        };

        /// <summary>
        /// What a target job looks like (GDD §04.4). Talking to a vendor and picking a safe have no clip yet (PL.38):
        /// the body stands, which is what an NPC at a stall does too.
        /// </summary>
        public static BodyAction ForTarget(TargetKind kind) => kind switch
        {
            TargetKind.Open => BodyAction.WalletLift,
            TargetKind.Fixed => BodyAction.PosterSwap,
            _ => BodyAction.None,
        };
    }
}
