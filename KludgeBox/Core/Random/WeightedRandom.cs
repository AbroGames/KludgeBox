using System.Collections;

namespace KludgeBox.Core.Random;

/// <summary>
/// Represents a weighted random picker that can randomly select stored items based on their weight.
/// </summary>
/// <typeparam name="T">The type of the stored items.</typeparam>
public class WeightedRandom<T> : IEnumerable
{
	public int Count => _items.Count;
		
	private readonly List<WeightedItem<T>> _items = [];
	private List<double> _prefixSumOfWeights;

	/// <summary>
	/// Adds an item with the specified weight to the picker. If the item already exists, its weight is increased.
	/// </summary>
	/// <param name="item">The item to add.</param>
	/// <param name="weight">The weight of the item. Must be more than 0.</param>
	public void Add(T item, double weight)
	{
		if (item is null) return;
		if (weight < 0) throw new ArgumentException("Weight must be more than 0");

		// Check if the item already exists in the list
		var existingItem = _items.Find(i => i.Item.Equals(item));

		if (existingItem != null)
		{
			// Increase the weight of the existing item
			existingItem.Weight += weight;
		}
		else
		{
			// Add a new item to the list
			_items.Add(new WeightedItem<T>(item, weight));
		}

		_prefixSumOfWeights = GeneratePrefixSumOfWeights();
	}

	/// <summary>
	/// Picks a random item from the picker based on their weights.
	/// </summary>
	/// <returns>The randomly selected item.</returns>
	public T PickRandom()
	{
		var weightedRandomIndex = GetWeightedRandomIndexFromPrefixSumOfWeights();
		return _items[weightedRandomIndex].Item;
	}

	/// <summary>
	/// Adjusts the weight of an existing item in the picker.
	/// </summary>
	/// <param name="item">The item whose weight needs to be adjusted.</param>
	/// <param name="weight">The new weight of the item.</param>
	public void ChangeWeight(T item, double weight)
	{
		var existingItem = _items.Find(i => i.Item.Equals(item));
			
		if (existingItem != null)
		{
			existingItem.Weight = weight;
		}
	}

	/// <summary>
	/// Removes an item from the picker.
	/// </summary>
	/// <param name="item">The item to remove.</param>
	public void Remove(T item)
	{
		var existingItem = _items.Find(i => i.Item.Equals(item));

		if (existingItem != null)
		{
			_items.Remove(existingItem);
		}
	}

	public IEnumerator GetEnumerator()
	{
		return _items.GetEnumerator();
	}

	private List<double> GeneratePrefixSumOfWeights()
	{
		if (_items.Count == 0) return [];
			
		var prefixSum = new List<double>(_items.Count)
		{
			[0] = _items[0].Weight
		};

		for (var i = 1; i < prefixSum.Count; i++) {
			prefixSum[i] = prefixSum[i - 1] + _items[i].Weight;
		}
		return prefixSum;
	}

	private int GetWeightedRandomIndexFromPrefixSumOfWeights()
	{
		var totalWeight = _prefixSumOfWeights[^1];
		var randomValue = KludgeBoxServices.Rand.Range(totalWeight);

		for (var i = 0; i < _prefixSumOfWeights.Count; i++) {
			if (randomValue < _prefixSumOfWeights[i]) {
				return i;
			}
		}
		return _prefixSumOfWeights.Count - 1;
	}

	private class WeightedItem<TItem>(TItem item, double weight)
	{
		public TItem Item { get; set; } = item;
		public double Weight { get; set; } = weight;
	}
}