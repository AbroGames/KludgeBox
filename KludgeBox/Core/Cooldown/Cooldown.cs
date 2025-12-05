namespace KludgeBox.Core.Cooldown;

public abstract class Cooldown
{
    //Duration of the cooldown in seconds.
    public double Duration { get; } = 0;
	
    //Gets elapsed time in seconds
    public double ElapsedTime => Duration - TimeLeft;
    //Gets time left in seconds
    public double TimeLeft { get; protected set; } = 0;
    public bool IsActivated { get; protected set; } = false;

    //Gets the fraction of the cooldown completed, ranging from 0 to 1.
    public double FractionElapsedTime => ElapsedTime / Duration;
	
    public event Action ActionWhenReady;

    /// <param name="duration">The duration of the cooldown in seconds.</param>
    /// <param name="isActivated">If false, the cooldown starts only after manually calling <c>Start()</c>.</param>
    /// <param name="actionWhenReady">Invoked when the counter completes.</param>
    public Cooldown(double duration, bool isActivated = true, Action actionWhenReady = null) 
    {
	    if (duration <= 0)
	    {
		    throw new ArgumentException("Duration must be more than zero.");
	    }
		
	    Duration = duration;
	    TimeLeft = duration;
	    IsActivated = isActivated;
	    ActionWhenReady = actionWhenReady;
    }

    protected void ActivateAction()
    {
	    ActionWhenReady?.Invoke();
    }
}
