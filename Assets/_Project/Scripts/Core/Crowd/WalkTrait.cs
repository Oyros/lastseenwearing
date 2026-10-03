namespace LastSeenWearing.Core.Crowd
{
    /// <summary>
    /// The additive walk layers (LSW_WalkSystem.md §2) — each one a trait a player can say over the radio.
    /// The name is the crowd animator's layer name, part of the art contract. Two are two-sided, and a
    /// trait's signed weight picks the side: <see cref="Limp"/> −left / +right, <see cref="ArmSwing"/>
    /// −stiff / +big.
    /// </summary>
    public enum WalkTrait : byte
    {
        Limp = 0,
        Hunch = 1,
        Sway = 2,
        Bounce = 3,
        ArmSwing = 4,
    }
}
