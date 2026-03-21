namespace KludgeBox.Core.Ping;

public class PingAnalyzer
{
    private record PingInfo(long AnalyzeTime, long PingTime);
    private record PacketLossInfo(long AnalyzeTime, long NumberOfSuccessPackets, long NumberOfLossesPackets);
    
    public readonly int MaxTimeOfAnalyticalSlidingWindowForPing = 60 * 1000;
    public readonly int MaxTimeOfAnalyticalSlidingWindowForPacketLoss = 60 * 1000;
    public readonly int MidTimeOfAnalyticalSlidingWindowForPacketLoss = 30 * 1000;
    public readonly int ShortTimeOfAnalyticalSlidingWindowForPacketLoss = 5 * 1000;
    
    // Финальные результаты аналитики
    public long CurrentPingTime { get; private set; }
    public long MinimumPingTime { get; private set; }
    public double AveragePingTime { get; private set; }
    public long MaximumPingTime { get; private set; }
    public double P50PingTime { get; private set; }
    public double P90PingTime { get; private set; }
    public double P99PingTime { get; private set; }
    public double AveragePacketLossInPercentForLongTime { get; private set; }
    public double AveragePacketLossInPercentForMidTime { get; private set; }
    public double AveragePacketLossInPercentForShortTime { get; private set; }

    // История промежуточной информации о пинге
    private readonly List<PingInfo> _pingsInfo = new();
    private readonly List<PacketLossInfo> _packetsLossInfo = new();

    public PingAnalyzer() { }

    public PingAnalyzer(
        int maxTimeOfAnalyticalSlidingWindowForPing, 
        int maxTimeOfAnalyticalSlidingWindowForPacketLoss, 
        int midTimeOfAnalyticalSlidingWindowForPacketLoss, 
        int shortTimeOfAnalyticalSlidingWindowForPacketLoss)
    {
        MaxTimeOfAnalyticalSlidingWindowForPing = maxTimeOfAnalyticalSlidingWindowForPing;
        MaxTimeOfAnalyticalSlidingWindowForPacketLoss = maxTimeOfAnalyticalSlidingWindowForPacketLoss;
        MidTimeOfAnalyticalSlidingWindowForPacketLoss = midTimeOfAnalyticalSlidingWindowForPacketLoss;
        ShortTimeOfAnalyticalSlidingWindowForPacketLoss = shortTimeOfAnalyticalSlidingWindowForPacketLoss;
    }

    // numberOfSuccessPackets и numberOfLossesPackets указываются не на текущий момент,
    // а от начала игры и до (CurrentTime - MaxPingTimeout), т.е. тут нет самых свежих данных за последнюю секунду
    public void Analyze(long pingTime, long numberOfSuccessPackets, long numberOfLossesPackets)
    {
        AddAttempt(pingTime, numberOfSuccessPackets, numberOfLossesPackets);
        DeleteOldAttempts();
        RecalculateStatistics();
    }

    private void AddAttempt(long pingTime, long numberOfSuccessPackets, long numberOfLossesPackets)
    {
        long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _pingsInfo.Add(new PingInfo(currentTime, pingTime));
        _packetsLossInfo.Add(new PacketLossInfo(currentTime, numberOfSuccessPackets, numberOfLossesPackets));
    }

    private void DeleteOldAttempts()
    {
        long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _pingsInfo.RemoveAll(pingInfo => pingInfo.AnalyzeTime < currentTime - MaxTimeOfAnalyticalSlidingWindowForPing);
        _packetsLossInfo.RemoveAll(packetLossInfo => packetLossInfo.AnalyzeTime < currentTime - MaxTimeOfAnalyticalSlidingWindowForPacketLoss);
    }

    private void RecalculateStatistics()
    {
        List<long> pingTimesSorted = _pingsInfo.ConvertAll(pingInfo => pingInfo.PingTime);
        pingTimesSorted.Sort();

        CurrentPingTime = _pingsInfo.Last().PingTime;
        MinimumPingTime = pingTimesSorted.First();
        AveragePingTime = (double) pingTimesSorted.Sum() / _pingsInfo.Count;
        MaximumPingTime = pingTimesSorted.Last();

        P50PingTime = CalculatePercentile(pingTimesSorted, 0.5);
        P90PingTime = CalculatePercentile(pingTimesSorted, 0.9);
        P99PingTime = CalculatePercentile(pingTimesSorted, 0.99);
        
        AveragePacketLossInPercentForLongTime = CalculatePacketLossPercent(_packetsLossInfo, MaxTimeOfAnalyticalSlidingWindowForPacketLoss);
        AveragePacketLossInPercentForMidTime = CalculatePacketLossPercent(_packetsLossInfo, MidTimeOfAnalyticalSlidingWindowForPacketLoss);
        AveragePacketLossInPercentForShortTime = CalculatePacketLossPercent(_packetsLossInfo, ShortTimeOfAnalyticalSlidingWindowForPacketLoss);
    }
    
    // Считает перцентили, percentile задается от 0 до 1
    private double CalculatePercentile(List<long> valuesSorted, double percentile) 
    {
        int count = valuesSorted.Count;
        if (count == 0) return 0d;
        if (percentile <= 0d || count == 1) return valuesSorted[0];
        if (percentile >= 1d) return valuesSorted[count - 1];
        
        double n = (count - 1) * percentile + 1;
        int k = (int) n;
        double d = n - k;
        return valuesSorted[k - 1] + d * (valuesSorted[k] - valuesSorted[k - 1]);
    }

    // Считает PacketLoss за определенный диапазон времени [slidingWindowTime; CurrentTime - PingChecker.MaxPingTimeout]
    private double CalculatePacketLossPercent(List<PacketLossInfo> packetsLossInfo, int slidingWindowTime)
    {
        if (packetsLossInfo.Count == 0) return 0;

        long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        
        PacketLossInfo fromInfo = packetsLossInfo.First(); // Информация о потерянных пакетах, полученная slidingWindowTime миллисекунд назад
        foreach (var packetLossInfo in packetsLossInfo)
        {
            if (packetLossInfo.AnalyzeTime >= currentTime - slidingWindowTime)
            {
                fromInfo = packetLossInfo;
                break;
            }
        }
        PacketLossInfo toInfo = packetsLossInfo.Last(); // Последняя полученная информация о потерянных пакетах
        
        long numberOfSuccessPacketsInSlidingWindow = toInfo.NumberOfSuccessPackets - fromInfo.NumberOfSuccessPackets;
        long numberOfLossesPacketsInSlidingWindow = toInfo.NumberOfLossesPackets - fromInfo.NumberOfLossesPackets;
        long numberOfAllPacketsInSlidingWindow = numberOfSuccessPacketsInSlidingWindow + numberOfLossesPacketsInSlidingWindow;
        if (numberOfLossesPacketsInSlidingWindow == 0) return 0;
        
        double packetLossesRate = (double) numberOfLossesPacketsInSlidingWindow / numberOfAllPacketsInSlidingWindow;
        return packetLossesRate * 100;
    }
}
