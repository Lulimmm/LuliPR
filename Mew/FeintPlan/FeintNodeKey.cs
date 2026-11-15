namespace Mew.FeintPlan;

public readonly record struct FeintNodeKey(string Phase, uint NodeId)
{
    public override string ToString()
        => string.IsNullOrEmpty(Phase) ? NodeId.ToString() : $"{Phase}:{NodeId}";

    public static FeintNodeKey Parse(string value)
    {
        var separator = value.LastIndexOf(':');
        if (separator < 0)
            return new FeintNodeKey(string.Empty, uint.Parse(value));

        return new FeintNodeKey(value[..separator], uint.Parse(value[(separator + 1)..]));
    }
}
