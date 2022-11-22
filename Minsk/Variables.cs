namespace Minsk;

public class Variables
{
	private readonly Stack<Dictionary<string, IValue>> scopeStack = new Stack<Dictionary<string, IValue>>();

	public Variables() => scopeStack.Push(new Dictionary<string, IValue>());

	public IValue Get(string variableName)
	{
		foreach (var scope in scopeStack)
			if (scope.TryGetValue(variableName, out IValue value))
				return value;
		return NullValue.Instance;
	}

	public void Set(string variableName, IValue value)
	{
		var scope = scopeStack.Peek();
		if (scope.ContainsKey(variableName))
			scope[variableName] = value;
		else
			scope.Add(variableName, value);
	}

	public void EnterScope() => scopeStack.Push(new Dictionary<string, IValue>());

	public void LeaveScope() => scopeStack.Pop();
}