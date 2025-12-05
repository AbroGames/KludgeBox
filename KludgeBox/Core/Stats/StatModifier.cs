namespace KludgeBox.Core.Stats;

public class StatModifier<TStat>
{
    public enum ModifierType { Additive, Multiplicative }
        
    public readonly TStat Stat;
    public readonly ModifierType Type;
    public readonly double Value;

    public StatModifier(TStat stat, ModifierType type, double value)
    {
        Stat = stat;
        Type = type;
        Value = value;
    }

    public static StatModifier<TStat> CreateAdditive(TStat stat, double value)
    {
        return new StatModifier<TStat>(stat, ModifierType.Additive, value);
    }
        
    public static StatModifier<TStat> CreateMultiplicative(TStat stat, double value)
    {
        return new StatModifier<TStat>(stat, ModifierType.Multiplicative, value);
    }
}