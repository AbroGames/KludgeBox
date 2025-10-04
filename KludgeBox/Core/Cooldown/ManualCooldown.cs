namespace KludgeBox.Core.Cooldown;

public class ManualCooldown : Cooldown
{
	
	//Cooldown ended, time left and actions executed
	public bool IsCompleted => _isCompleted;
	
	private bool _isCompleted = false;

	/// <summary>
	/// Initializes a new instance of the <see cref="ManualCooldown"/> class with the specified duration.
	/// </summary>
	/// <param name="duration">The duration of the cooldown in seconds.</param>
	public ManualCooldown(double duration, bool isCompleted = false, bool isActivated = true, Action actionWhenReady = null) :
		base(duration, isActivated, actionWhenReady)
	{
		_isCompleted = isCompleted;
		
		if (isCompleted)
		{
			_timeLeft = 0;
		}
	}

	/// <summary>
	/// Updates the cooldown by a specified delta time 
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
	public void Update(double deltaTime)
	{
		if (!_isActivated) return;
		
		_timeLeft -= deltaTime;
	    if (_timeLeft <= 0)
	    {
		    _timeLeft = 0;
		    _isCompleted = true;
		    _isActivated = false;
		    ActivateAction();
	    }
	}
	
	/// <summary>
	/// Resetting the elapsed time to 0 and stop cooldowner.
	/// </summary>
	public void Reset()
	{
		_timeLeft = Duration;
		_isCompleted = false;
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
		if (_isCompleted) return;
		_isActivated = true;
	}

	public void Pause()
	{
		if (_isCompleted) return;
		_isActivated = false;
	}
}
