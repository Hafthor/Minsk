namespace Minsk;

public class Context
{
	private readonly Variables vars;
	public Context(Variables vars) => this.vars = vars;

	public bool EscapeLoop { get; set; }
	public bool EscapeFunc { get; set; }
	public IValue EscapeFuncValue { get; set; }
	public bool Escape => EscapeFunc || EscapeLoop;

	public IValue Get(string variableName) => vars.Get(variableName);
	public void Set(string variableName, IValue value) => vars.Set(variableName, value);
	public void EnterScope() => vars.EnterScope();
	public void LeaveScope() => vars.LeaveScope();
}

