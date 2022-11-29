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
	public static readonly DoubleValue Zero = new DoubleValue(0);
    public static readonly DoubleValue One = new DoubleValue(1);
    public static readonly DoubleValue NaN = new DoubleValue(double.NaN);
	public string String => @string.Value;
	public double Double => value;
	public object Object => value;
}

public class StringValue : IValue
{
	public static readonly StringValue Empty = new StringValue("");
	private readonly string value;
	private readonly Lazy<double> @double;
	public StringValue(string value) { this.value = value; @double = new Lazy<double>(() => double.Parse(value)); }
	public string String => value;
	public double Double => @double.Value;
	public object Object => value;
}

public class DictionaryValue : IValue
{
	public static readonly DictionaryValue Empty = new DictionaryValue();
	private readonly Dictionary<string, IValue> value;
	public DictionaryValue() { value = new Dictionary<string, IValue>(); }
	public DictionaryValue(Dictionary<string, IValue> dict) { value = dict; }
	public string String => "[Dictionary]";
	public double Double => double.NaN;
	public object Object => value;
	public IValue ObjectByKey(string key) => value[key];
	public void SetObjectByKey(string key, IValue val) => value[key] = val;
}

public class ArrayValue : IValue
{
	public static readonly ArrayValue Empty = new ArrayValue();
	private readonly List<IValue> value;
	public ArrayValue() { value = new List<IValue>(); }
	public ArrayValue(List<IValue> arr) { value = arr; }
	public string String => "[Array]";
	public double Double => double.NaN;
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
	public static readonly FunctionValue Identity = new FunctionValue("x", (x) => x);
    private readonly string parameterName;
    private readonly Func<IValue, IValue> value;
	public FunctionValue(string parameterName, Func<IValue, IValue> func)
	{
		this.parameterName = parameterName;
		value = func;
	}
	public string String => "[Function]";
	public double Double => double.NaN;
	public object Object => value;
	public IValue InvokeWith(IValue param, Context ctx)
	{
		ctx.EnterScope();
		ctx.Set(parameterName, param);
		var returnValue = value(param);
		if (ctx.EscapeFunc)
		{
			returnValue = ctx.EscapeFuncValue;
			ctx.EscapeFunc = false;
		}
        ctx.LeaveScope();
		return returnValue;
	}
}