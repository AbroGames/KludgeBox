namespace KludgeBox.Core.Cooldown;

public class AutoCooldown : Cooldown
{
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoCooldown"/> class with the specified duration.
    /// </summary>
    /// <param name="duration">The duration of the cooldown in seconds.</param>
    public AutoCooldown(double duration, bool isActivated = true, Action actionWhenReady = null) : 
        base(duration, isActivated, actionWhenReady) { }

    /// <summary>
    /// Updates the cooldown by a specified delta time 
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
    public void Update(double deltaTime)
    {
        if (!_isActivated) return;
		
        _timeLeft -= deltaTime;
        while (_timeLeft <= 0)
        {
            _timeLeft += Duration;
            ActivateAction();
        }
    }
	
    /// <summary>
    /// Resetting the elapsed time to 0 and stop cooldowner.
    /// </summary>
    public void Reset()
    {
        _timeLeft = Duration;
        _isActivated = false;
    }

    /// <summary>
    /// Resetting the elapsed time to 0 and activate cooldowner.
    /// </summary>
    public void Restart()
    {
        Reset();
        _isActivated = true;
    }

    public void Start()
    {
        _isActivated = true;
    }

    public void Pause()
    {
        _isActivated = false;
    }
}
