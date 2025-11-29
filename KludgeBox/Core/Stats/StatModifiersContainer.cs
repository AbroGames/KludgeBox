namespace KludgeBox.Core.Stats;

public class StatModifiersContainer<TStat>
{
    private readonly HashSet<StatModifier<TStat>> _statsModifiers = new();
    
    private readonly Dictionary<TStat, double> _cacheAdditive = new();
    private readonly Dictionary<TStat, double> _cacheMultiplicative = new();
    private readonly HashSet<(TStat, StatModifier<TStat>.ModifierType)> _needToInvalidateCache = new();
    
    public void AddStatModifier(StatModifier<TStat> statModifier)
    {
        AddTaskToInvalidateCache(statModifier);
        statModifier.PropertyChanged += (stat, _) => AddTaskToInvalidateCache((StatModifier<TStat>) stat);
        
        _statsModifiers.Add(statModifier);
    }
    
    public bool RemoveStatModifier(StatModifier<TStat> statModifier)
    {
        AddTaskToInvalidateCache(statModifier);
        return _statsModifiers.Remove(statModifier);
    }

    public double GetStat(TStat stat, double baseValue = 0)
    {
        double additiveValue = GetStatValue(stat, StatModifier<TStat>.ModifierType.Additive);
        double multiplicativeValue = GetStatValue(stat, StatModifier<TStat>.ModifierType.Multiplicative);
        return (baseValue + additiveValue) * multiplicativeValue;
    }

    public double GetStatValue(TStat stat, StatModifier<TStat>.ModifierType type)
    {
        if (_needToInvalidateCache.Contains((stat, type))) RecalculateCache(stat, type);
        return GetCacheInfo(type).Dictionary.GetValueOrDefault(stat, GetCacheInfo(type).DefaultValue);
    }
    
    private void AddTaskToInvalidateCache(StatModifier<TStat> statModifier)
    {
        _needToInvalidateCache.Add((statModifier.Stat, statModifier.Type));
    }

    private void RecalculateCache(TStat stat, StatModifier<TStat>.ModifierType type)
    {
        GetCacheInfo(type).Dictionary[stat] = _statsModifiers
            .Where(sm => sm.Stat.Equals(stat))
            .Where(sm => sm.Type == type)
            .Select(sm => sm.Value)
            .Aggregate(GetCacheInfo(type).DefaultValue, GetCacheInfo(type).Operation);
        
        _needToInvalidateCache.Remove((stat, type));
    }
    
    private record CacheInfo( 
        Dictionary<TStat, double> Dictionary, 
        Func<double, double, double> Operation, 
        double DefaultValue);
    
    private CacheInfo GetCacheInfo(StatModifier<TStat>.ModifierType type)
    {
        return type switch
        {
            StatModifier<TStat>.ModifierType.Additive => 
                new CacheInfo(_cacheAdditive, (d1, d2) => d1 + d2, 0),
            StatModifier<TStat>.ModifierType.Multiplicative => 
                new CacheInfo(_cacheMultiplicative, (d1, d2) => d1 * d2, 1),
            _ => throw new ArgumentException("Unknown ModifierType: " + type)
        };
    }
    
}