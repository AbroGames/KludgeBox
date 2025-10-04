namespace KludgeBox.Core.Cooldown;

public abstract class Cooldown
{
    //Duration of the cooldown in seconds.
    public double Duration { get; } = 0;
	
    //Gets elapsed time in seconds
    public double ElapsedTime => Duration - _timeLeft;
    //Gets time left in seconds
    public double TimeLeft => _timeLeft;

    //Gets the fraction of the cooldown completed, ranging from 0 to 1.
    public double FractionElapsedTime => ElapsedTime / Duration;
	
    public event Action ActionWhenReady;
	
    protected double _timeLeft = 0;
    protected bool _isActivated = false;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Cooldown"/> class with the specified duration.
    /// </summary>
    /// <param name="duration">The duration of the cooldown in seconds.</param>
    public Cooldown(double duration, bool isActivated = true, Action actionWhenReady = null) 
    {
	    if (duration <= 0)
	    {
		    throw new ArgumentException("Duration must be more than zero.");
	    }
		
	    Duration = duration;
	    _timeLeft = duration;
	    _isActivated = isActivated;
	    ActionWhenReady = actionWhenReady;
    }

    protected void ActivateAction()
    {
	    ActionWhenReady?.Invoke();
    }
}
