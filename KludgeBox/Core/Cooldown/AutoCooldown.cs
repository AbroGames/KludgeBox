namespace KludgeBox.Core.Cooldown;

/// <summary>
/// A looping timer that counts down and executes the specified action <c>actionWhenReady</c> at the end of each cycle.<br/>
/// Restarts automatically.
/// </summary>
public class AutoCooldown : Cooldown
{
    
    /// <param name="duration">The duration of the cooldown in seconds.</param>
    /// <param name="isActivated">If false, the cooldown starts only after manually calling <c>Start()</c>.</param>
    /// <param name="actionWhenReady">Invoked when the counter completes.</param>
    public AutoCooldown(double duration, bool isActivated = true, Action actionWhenReady = null) : 
        base(duration, isActivated, actionWhenReady) { }

    /// <summary>
    /// Updates the cooldown by a specified delta time 
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
    public void Update(double deltaTime)
    {
        if (!IsActivated) return;
		
        TimeLeft -= deltaTime;
        while (TimeLeft <= 0)
        {
            TimeLeft += Duration;
            ActivateAction();
        }
    }
	
    /// <summary>
    /// Resetting the elapsed time to 0 and stop cooldowner.
    /// </summary>
    public void Reset()
    {
        TimeLeft = Duration;
        IsActivated = false;
    }

    /// <summary>
    /// Resetting the elapsed time to 0 and activate cooldowner.
    /// </summary>
    public void Restart()
    {
        Reset();
        IsActivated = true;
    }

    public void Start()
    {
        IsActivated = true;
    }

    public void Pause()
    {
        IsActivated = false;
    }
}
