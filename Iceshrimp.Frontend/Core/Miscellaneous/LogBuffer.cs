namespace Iceshrimp.Frontend.Core.Miscellaneous;

public class LogBuffer
{
	private readonly Queue<string> _buffer = new();
	private          int           _usedCapacity;
	private readonly int           _maxCapacity;

	public LogBuffer(int capacity)
	{
		_maxCapacity = capacity;
	}

	public void Add(string input)
	{
		if (input.Length > _maxCapacity)
			throw new
				ArgumentException($"Log message size ({input.Length}) exceeds total buffer capacity of {_maxCapacity}");
		while (_usedCapacity + input.Length > _maxCapacity)
		{
			var x = _buffer.Dequeue();
			_usedCapacity -= x.Length;
		}

		_buffer.Enqueue(input);
		_usedCapacity += input.Length;
	}

	public List<string> AsList()
	{
		return _buffer.ToList();
	}
}