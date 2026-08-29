namespace MEC.Domain.Common
{
    public readonly record struct SaturdayLeavePolicy(DateTime EffectiveFrom, bool CountsAsLeaveDay);
}
