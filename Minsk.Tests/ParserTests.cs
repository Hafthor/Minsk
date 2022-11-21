﻿namespace Minsk.Tests;

[TestClass]
public class ParserTests
{
    private readonly Dictionary<string, double> doubleTests = new Dictionary<string, double>() {
        {"123",123.0},
        {"1+2",3.0},
        {"2*3",6.0},
        {"3/2",1.5},
        {"1+2*3",7.0},
        {"3*2+1",7.0},
        {" 1 + 2 * 3 ",7.0},
        {"-1",-1.0},
        {"2*-3",-6.0},
        {"2.5",2.5},
        {"3*-(1-4)",9.0},
        {"1+(2*3)-(1+(2*3))",0.0},
        {"3 * -( // comment\r1-4) // more comment",9.0},
        {"3 * -(/* comment */1-4)/* more comment */",9.0},
        {"6=2*3",1.0},
        {"2*3>5",1.0},
        {"!(2*3>5)",0.0},
        {"!\"abc\"",0.0},
        {"-\"123\"",-123.0},
        {"123+\"456\"",579.0},
        {"\"456\"-\"123\"",333.0},
        {"\"abc\">\"def\"",0.0},
        {"\"abc\">=\"def\"",0.0},
        {"\"abc\"!=\"def\"",1.0},
        {"1+2*3^4",163.0},
        {"50.5%7",1.5},
    };

    private readonly Dictionary<string, string> stringTests = new Dictionary<string, string>()
    {
        {"\"abc\"","abc"},
        {"\" \\r \\\" \\\\ \""," \r \" \\ "},
        {"\"abc\"+\"def\"","abcdef"},
        {"\"abc\"+123","abc123"},
    };

    private Dictionary<string, IValue> dict = new Dictionary<string, IValue>();
    private IValue dictRead(string key) => dict[key];
    private void dictWrite(string key, IValue val) => dict[key] = val;

    [TestMethod]
    public void ParseDoubles()
    {
        foreach (var e in doubleTests)
        {
            var actual = Parser.LexParse(e.Key).Eval();
            Assert.IsTrue(actual is DoubleValue);
            Assert.AreEqual(e.Value, actual.Double);
        }
    }

    [TestMethod]
    public void ParseStrings()
    {
        foreach (var e in stringTests)
        {
            var actual = Parser.LexParse(e.Key).Eval();
            Assert.IsTrue(actual is StringValue);
            Assert.AreEqual(e.Value, actual.String);
        }
    }

    [TestMethod]
    public void ParseIdentifier()
    {
        dict.Add("abc", new DoubleValue(123));
        var actual = Parser.LexParse("abc").Eval((k) => dict[k]);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(123.0, actual.Double);
    }

    [TestMethod]
    public void ParseAssignment()
    {
        var actual = Parser.LexParse("abc:123").Eval(null, dictWrite);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(123.0, actual.Double);
        Assert.IsTrue(dict["abc"] is DoubleValue);
        Assert.AreEqual(123.0, dict["abc"].Double);
    }

    [TestMethod]
    public void ParseArrayDeref()
    {
        var arr = new ArrayValue(new List<IValue>() { new DoubleValue(3.14), new DoubleValue(2.718) });
        dict.Add("arr", arr);
        var actual = Parser.LexParse("arr[1]").Eval(dictRead);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(2.718, actual.Double);
    }

    [TestMethod]
    public void ParseArraySet()
    {
        var arr = new ArrayValue(new List<IValue>() { new DoubleValue(2.718) });
        dict.Add("arr", arr);
        var actual = Parser.LexParse("arr[0]:3.14").Eval(dictRead);
        var obj0 = arr.ObjectByIndex(0);
        Assert.IsTrue(obj0 is DoubleValue);
        Assert.AreEqual(3.14, obj0.Double);
    }

    [TestMethod]
    public void ParseDictDeref()
    {
        var d = new DictionaryValue(new Dictionary<string, IValue>() { { "pi", new DoubleValue(3.14) }, { "e", new DoubleValue(2.718) } });
        dict.Add("d", d);
        var actual = Parser.LexParse("d[\"e\"]").Eval(dictRead);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(2.718, actual.Double);
    }

    [TestMethod]
    public void ParseDictSet()
    {
        var d = new DictionaryValue(new Dictionary<string, IValue>() { { "pi", new DoubleValue(3.14) } });
        dict.Add("d", d);
        var actual = Parser.LexParse("d[\"e\"]:2.718").Eval(dictRead);
        var objE = d.ObjectByKey("e");
        Assert.IsTrue(objE is DoubleValue);
        Assert.AreEqual(2.718, objE.Double);
    }

    [TestMethod]
    public void ParseDotDictDeref()
    {
        var d = new DictionaryValue(new Dictionary<string, IValue>() { { "pi", new DoubleValue(3.14) }, { "e", new DoubleValue(2.718) } });
        dict.Add("d", d);
        var actual = Parser.LexParse("d.pi").Eval(dictRead);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(3.14, actual.Double);
    }

    [TestMethod]
    public void ParseDotDictSet()
    {
        var d = new DictionaryValue(new Dictionary<string, IValue>() { { "pi", new DoubleValue(3.14) } });
        dict.Add("d", d);
        var actual = Parser.LexParse("d.e:2.718").Eval(dictRead);
        var objE = d.ObjectByKey("e");
        Assert.IsTrue(objE is DoubleValue);
        Assert.AreEqual(2.718, objE.Double);
    }

    [TestMethod]
    public void ParseMethodInvoke()
    {
        var upper = new FunctionValue((v) =>
        {
            if (v is StringValue sv) return new StringValue(sv.String.ToUpper());
            throw new Exception("unexpected value type");
        });
        dict.Add("upper", upper);
        var actual = Parser.LexParse("upper(\"pi\")").Eval(dictRead);
        Assert.IsTrue(actual is StringValue);
        Assert.AreEqual("PI", actual.String);
    }

    [TestMethod]
    public void ParseStatements()
    {
        var actual = Parser.LexParse("a:1;b:2;c:3;d:a+b*c").Eval(dictRead, dictWrite);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(7.0, actual.Double);
        Assert.IsTrue(dict["a"] is DoubleValue);
        Assert.AreEqual(1.0, dict["a"].Double);
        Assert.IsTrue(dict["b"] is DoubleValue);
        Assert.AreEqual(2.0, dict["b"].Double);
        Assert.IsTrue(dict["c"] is DoubleValue);
        Assert.AreEqual(3.0, dict["c"].Double);
        Assert.IsTrue(dict["d"] is DoubleValue);
        Assert.AreEqual(7.0, dict["d"].Double);
    }

    [TestMethod]
    public void ParseNewObjects()
    {
        var actual = Parser.LexParse("a:[];b:{};a[0]:3.14;b[\"e\"]:2.718").Eval(dictRead, dictWrite);
        Assert.IsTrue(dict["a"] is ArrayValue);
        Assert.AreEqual(3.14, dict["a"] is ArrayValue av ? av.ObjectByIndex(0).Double : 0);
        Assert.IsTrue(dict["b"] is DictionaryValue);
        Assert.AreEqual(2.718, dict["b"] is DictionaryValue dv ? dv.ObjectByKey("e").Double : 0);
    }

    [TestMethod]
    public void ParseIfFalse()
    {
        var actual = Parser.LexParse("a:3.14;a=3.2??a:2.718").Eval(dictRead, dictWrite);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(0.0, actual.Double);
        Assert.IsTrue(dict["a"] is DoubleValue);
        Assert.AreEqual(3.14, dict["a"].Double);
    }

    [TestMethod]
    public void ParseIfTrue()
    {
        var actual = Parser.LexParse("a:3.14;a=3.14??a:2.718").Eval(dictRead, dictWrite);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(2.718, actual.Double);
        Assert.IsTrue(dict["a"] is DoubleValue);
        Assert.AreEqual(2.718, dict["a"].Double);
    }

    [TestMethod]
    public void ParseWhile()
    {
        var actual = Parser.LexParse("a:0;a<10?a:a+3").Eval(dictRead, dictWrite);
        Assert.IsTrue(actual is DoubleValue);
        Assert.AreEqual(0.0, actual.Double);
        Assert.IsTrue(dict["a"] is DoubleValue);
        Assert.AreEqual(12.0, dict["a"].Double);
    }
}