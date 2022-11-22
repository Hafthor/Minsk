using System;
namespace Minsk;

public interface IValue
{
	public string String { get; }
	public double Double { get; }
	public object Object { get; }
}

public class NullValue : IValue
{
	private NullValue() { }
	public static readonly NullValue Instance = new NullValue();
	public string String => null;
	public double Double => double.NaN;
	public object Object => null;
}

public class DoubleValue : IValue
{
	private readonly double value;
	private readonly Lazy<string> @string;
	public DoubleValue(double value) { this.value = value; @string = new Lazy<string>(() => value.ToString()); }
	public string String => @string.Value;
	public double Double => value;
	public object Object => value;
}

public class StringValue : IValue
{
	private readonly string value;
	private readonly Lazy<double> @double;
	public StringValue(string value) { this.value = value; @double = new Lazy<double>(() => double.Parse(value)); }
	public string String => value;
	public double Double => @double.Value;
	public object Object => value;
}

public class DictionaryValue : IValue
{
	private readonly Dictionary<string, IValue> value;
	public DictionaryValue() { value = new Dictionary<string, IValue>(); }
	public DictionaryValue(Dictionary<string, IValue> dict) { value = dict; }
	public string String => throw new Exception("cannot convert dictionary to string");
	public double Double => throw new Exception("cannot convert dictionary to double");
	public object Object => value;
	public IValue ObjectByKey(string key) => value[key];
	public void SetObjectByKey(string key, IValue val) => value[key] = val;
}

public class ArrayValue : IValue
{
	private readonly List<IValue> value;
	public ArrayValue() { value = new List<IValue>(); }
	public ArrayValue(List<IValue> arr) { value = arr; }
	public string String => throw new Exception("cannot convert array to string");
	public double Double => throw new Exception("cannot convert array to double");
	public object Object => value;
	public IValue ObjectByIndex(double index) => value[(int)index];
	public void SetObjectByIndex(double index, IValue val)
	{
		if (index == value.Count)
			value.Add(val);
		else if (index < value.Count && index >= 0)
			value[(int)index] = val;
		else
			throw new Exception("Index out of range");
	}
}

public class FunctionValue : IValue
{
    private readonly string parameterName;
    private readonly Func<IValue, IValue> value;
	public FunctionValue(string parameterName, Func<IValue, IValue> func)
	{
		this.parameterName = parameterName;
		value = func;
	}
	public string String => throw new Exception("cannot convert function to string");
	public double Double => throw new Exception("cannot convert function to double");
	public object Object => value;
	public IValue InvokeWith(IValue param, Variables vars)
	{
		vars.EnterScope();
		vars.Set(parameterName, param);
		var returnValue = value(param);
		vars.LeaveScope();
		return returnValue;
	}
}