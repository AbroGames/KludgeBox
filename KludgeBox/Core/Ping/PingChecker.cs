using System.Diagnostics;
using KludgeBox.Core.Cooldown;

namespace KludgeBox.Core.Ping;

public class PingChecker
{
    private record PingInfo(long PingId, Stopwatch SentTimer);
    
    public readonly double PingCooldown = 1;
    public readonly double MaxPingTimeout = 1;

    public readonly PingAnalyzer PingAnalyzer = new();
    
    private readonly AutoCooldown _pingSendCooldown;
    private readonly IDictionary<long, Stopwatch> _pingIdToSentTime = new Dictionary<long, Stopwatch>(); // Мапа для быстрого доступа к Stopwatch (хранит ссылку на таймер из _orderedPingInfo.SentTimer)
    private readonly LinkedList<PingInfo> _orderedPingInfo = new(); // Список информации о пакетах пинга, отсортированы по PingId (т.е. в порядке отправки)
    private readonly ISet<long> _successPingIdInCollections = new HashSet<long>(); // Список успешных ответов на пинги (содержит PingId). Отдельно от _orderedPingInfo, чтобы не делать каждый раз поиск элемента в LinkedList.
    private long _nextPingId = 0; // Следующее уникальное значение для пакета пинга
    private long _numberOfSuccessPackets = 0; // Количество полученных ответных пакетов пинга (отправлены более чем MaxPingTimeout миллисекунд назад)
    private long _numberOfLossesPackets = 0; // Количество потерянных пакетов пинга (отправлены более чем MaxPingTimeout миллисекунд назад)

    private readonly Action<long> _pingSender; // Вызывается каждый раз, когда надо отправить пакет пинга (пакет должен быть Unreliable!)

    public PingChecker(Action<long> pingSender)
    {
        _pingSender = pingSender;
        _pingSendCooldown = new(PingCooldown);
    }
    
    public PingChecker(Action<long> pingSender, double pingCooldown, double maxPingTimeout, PingAnalyzer pingAnalyzer)
    {
        PingCooldown = pingCooldown;
        MaxPingTimeout = maxPingTimeout;
        PingAnalyzer = pingAnalyzer;
        
        _pingSender = pingSender;
        _pingSendCooldown = new(PingCooldown);
    }

    public void Start()
    {
        _pingSendCooldown.ActionWhenReady += SendPingPacket;
    }
    
    public void OnProcess(double delta)
    {
        _pingSendCooldown.Update(delta);
    }
    
    public void OnReceivedPingPacket(long pingId)
    {
        if (!_pingIdToSentTime.TryGetValue(pingId, out var stopwatch)) 
        {
            return; //Сообщение уже было посчитано как потерянное
        }
        
        long pingTime = stopwatch.ElapsedMilliseconds;
        _successPingIdInCollections.Add(pingId);

        CheckAndDeleteOldAttempts();
        PingAnalyzer.Analyze(pingTime, _numberOfSuccessPackets, _numberOfLossesPackets);
    }
    
    private void SendPingPacket()
    {
        long pingId = _nextPingId++;
        Stopwatch stopwatch = new();
        _pingIdToSentTime.Add(pingId, stopwatch);
        _orderedPingInfo.AddLast(new PingInfo(pingId, stopwatch));
        
        stopwatch.Start();
        _pingSender.Invoke(pingId);
        
        CheckAndDeleteOldAttempts();
    }

    private void CheckAndDeleteOldAttempts()
    {
        var currentElement = _orderedPingInfo.First;
        while (currentElement != null && currentElement.Value.SentTimer.ElapsedMilliseconds > MaxPingTimeout * 1000)
        {
            long pingId = currentElement.Value.PingId;
            if (_successPingIdInCollections.Contains(pingId))
            {
                _numberOfSuccessPackets++;
            }
            else
            {
                _numberOfLossesPackets++;
            }
            
            _pingIdToSentTime.Remove(pingId);
            _orderedPingInfo.Remove(currentElement);
            _successPingIdInCollections.Remove(pingId);
            currentElement = currentElement.Next;
        }
    }
}
