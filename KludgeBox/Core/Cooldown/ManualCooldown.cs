namespace KludgeBox.Core.Cooldown;

/// <summary>
/// A timer that counts down once and executes the specified <c>actionWhenReady</c> upon completion.<br/>
/// To run it again, you must manually call <c>Restart()</c>.
/// </summary>
public class ManualCooldown : Cooldown
{
	
	//Cooldown ended, time left and actions executed
	public bool IsCompleted { get; private  set; } = false;

	/// <summary>
	/// Initializes a new instance of the <see cref="ManualCooldown"/> class with the specified duration.
	/// </summary>
	/// <param name="duration">The duration of the cooldown in seconds.</param>
	/// <param name="isCompleted">If true, the cooldown is immediately set to the completed state upon creation.<br/>
	/// If <c>isActivated</c> is also true, <c>actionWhenReady</c> will be executed on the first <c>Update()</c> call.</param>
	/// <param name="isActivated">If false, the cooldown starts only after manually calling <c>Start()</c>.</param>
	/// <param name="actionWhenReady">Invoked when the counter completes.</param>
	public ManualCooldown(double duration, bool isCompleted = false, bool isActivated = true, Action actionWhenReady = null) :
		base(duration, isActivated, actionWhenReady)
	{
		IsCompleted = isCompleted;
		
		if (isCompleted)
		{
			TimeLeft = 0;
		}
	}

	/// <summary>
	/// Updates the cooldown by a specified delta time 
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
	public void Update(double deltaTime)
	{
		if (!IsActivated) return;
		
		TimeLeft -= deltaTime;
	    if (TimeLeft <= 0)
	    {
		    TimeLeft = 0;
		    IsCompleted = true;
		    IsActivated = false;
		    ActivateAction();
	    }
	}
	
	/// <summary>
	/// Resetting the elapsed time to 0 and stop cooldowner.
	/// </summary>
	public void Reset()
	{
		TimeLeft = Duration;
		IsCompleted = false;
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
		if (IsCompleted) return;
		IsActivated = true;
	}

	public void Pause()
	{
		if (IsCompleted) return;
		IsActivated = false;
	}
}
