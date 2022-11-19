using System;
namespace Minsk;

public interface IValue
{
	public string String { get; }
	public double Double { get; }
	public object Object { get; }
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
