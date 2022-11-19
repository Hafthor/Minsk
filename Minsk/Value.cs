﻿using System;
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
	public void SetObjectByIndex(double index, IValue val) => value[(int)index] = val;
}

public class FunctionValue : IValue
{
	private readonly Func<IValue, IValue> value;
	public FunctionValue() { value = (v) => v; }
	public FunctionValue(Func<IValue, IValue> func) { value = func; }
	public string String => throw new Exception("cannot convert function to string");
    public double Double => throw new Exception("cannot convert function to double");
	public object Object => value;
	public IValue InvokeWith(IValue param) => value(param);
}