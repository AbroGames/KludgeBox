using Godot;

namespace KludgeBox.Core.Random;

/// <summary>
/// The random distribution allows achieving a more reliable, yet still quite unpredictable randomness.
/// It helps reduce the frequency of situations where the chance succeeds or fails too many times in a row.
/// Suitable for crit-like random.
/// </summary>
public class RandomDistribution
{
	// Average proc chance.
	public double ProcChance
	{
		get => _procChance;
		set
		{
			_procChance = Mathf.Clamp(value, 0, 1);
			_constant = GetConstant(_procChance);
		}
	}

	/// <summary>
	/// Get current proc chance.
	/// </summary>
	public double CurrentChance { get; private set; }

	// Base proc chance.
	private double _procChance;
	// Constant, used to calculate chance.
	private double _constant;

	// Cached probability constants. Another values is being interpolated between cached ones.
	private static double[] _constants;
	// This flag indicates state of _constants cache.
	private static bool _ready = false;

	public RandomDistribution(double procChance = 0)
	{
		if (!_ready)
			Initialize();

		ProcChance = procChance;
		CurrentChance = _constant;
	}


	public bool TryProc()
	{
		// Generate random value for proc.
		var random = KludgeBoxServices.Rand.Double;

		if (KludgeBoxServices.Rand.Chance(CurrentChance))
		{
			// if this execution is proc'd, then reset proc chance to base and return true.
			CurrentChance = _constant;
			return true;
		}
		// If it's not proc'd, then adjust the chance and return false.
		CurrentChance += _constant;
		return false;
	}

	private double GetConstant(double probability)
	{
		// Clamp the probability value between 0 and 1
		probability = Mathf.Clamp(probability, 0, 1);

		// Calculate the lower bound by truncating the decimal part of probability
		double lowerBound = (int)(probability * 100);

		// Calculate the upper bound by adding 1 to the lower bound
		double upperBound = (int)(probability * 100 + 1);

		// Retrieve the constant value associated with the lower bound from the _constants array
		double lowerConst = _constants[(int)(lowerBound)];

		// Retrieve the constant value associated with the upper bound from the _constants array
		double upperConst = _constants[(int)(Mathf.Min(upperBound, 100))];

		// Calculate the weight based on the relative position of the probability within the bounds
		double weight = (probability - lowerBound / 100) / (upperBound / 100 - lowerBound / 100);

		// Interpolate between the lower and upper constant values based on the weight
		return Mathf.Lerp(lowerConst, upperConst, weight);
	}

	public static void Initialize()
	{
		if (_ready)
			return;

		_constants = new double[101];
		_constants[0] = 0;

		for (int i = 1; i <= 100; i++)
		{
			_constants[i] = FindCFromProbability(i / 100.0);
		}
		_ready = true;
	}
	// Function to find the constant 'C' from the given target probability 'targetProbability'
	private static double FindCFromProbability(double targetProbability)
	{
		// Initialize upper and lower bounds for 'C' search
		double upperBound = targetProbability;
		double lowerBound = 0.0;
		double currentMidpoint;

		// Variables to track previous and current probability values during the search
		double previousProbability = 0, currentProbability = 1;

		// Variable to count the number of steps taken in the binary search process
		int steps = 0;

		// Perform binary search to find 'C' with the desired precision
		while (true)
		{
			// Increment the steps counter for each iteration
			steps++;

			// Calculate the midpoint between the upper and lower bounds
			currentMidpoint = (upperBound + lowerBound) / 2;

			// Calculate the probability corresponding to the current 'C' value
			currentProbability = ProbabilityFromC(currentMidpoint);

			// Check if the difference between the current and previous probabilities
			// is within the desired precision, if so, break the loop
			if (Mathf.Abs(currentProbability - previousProbability) <= 0.000000000001)
			{
				break;
			}

			// Adjust the upper and lower bounds based on the comparison with the target probability
			if (currentProbability > targetProbability)
			{
				upperBound = currentMidpoint;
			}
			else
			{
				lowerBound = currentMidpoint;
			}

			// Update the previous probability for the next iteration
			previousProbability = currentProbability;
		}

		// Return the found constant 'C'
		return currentMidpoint;
	}


	// Function to calculate the probability 'P' from the given constant 'C'
	private static double ProbabilityFromC(double constantC)
	{
		// Variables to track previous and current probability outcomes
		double previousOutcome = 0;
		double currentOutcome = 0;

		// Variable to calculate the cumulative sum of 'n * currentOutcome'
		double sumN = 0;

		// Calculate the maximum number of iterations based on the desired precision
		double maxTries = Mathf.Ceil(1 / constantC);

		// Variable to count the number of steps taken during the probability calculation
		int steps = 0;

		// Iterate from 1 to the calculated maxTries to find the probability 'P'
		for (int n = 1; n <= maxTries; n++)
		{
			// Increment the steps counter for each iteration
			steps++;

			// Calculate the probability outcome for the current iteration 'n'
			currentOutcome = Mathf.Min(1, constantC * n) * (1 - previousOutcome);

			// Update the cumulative probability by adding the current outcome
			previousOutcome += currentOutcome;

			// Update the sumN by accumulating 'n * currentOutcome'
			sumN += n * currentOutcome;
		}

		// Return the calculated probability 'P'
		return 1 / sumN;
	}
}
