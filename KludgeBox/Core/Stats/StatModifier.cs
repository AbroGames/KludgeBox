using CommunityToolkit.Mvvm.ComponentModel;

namespace KludgeBox.Core.Stats;

public partial class StatModifier<TStat> : ObservableObject
{
    public enum ModifierType { Additive, Multiplicative }
        
    public readonly TStat Stat;
    public readonly ModifierType Type;
    [ObservableProperty] private double _value;

    public StatModifier(TStat stat, ModifierType type, double value)
    {
        Stat = stat;
        Type = type;
        _value = value;
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