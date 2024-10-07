using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace RE_Engine;

class Program
{
    static void Main(string[] args)
    {
        
    } 

    public static NFA ToNFA(string postfixExpression)
    {
        if (postfixExpression.Length == 0)
            return NFA.FromEpsilon();

        var stack = new Stack<NFA>();
        
        foreach (var symbol in postfixExpression)
        {
            if (symbol == '|') // left | right
            {
                NFA right = stack.Pop();
                NFA left = stack.Pop();
                stack.Push(NFA.Union(left, right));
            }
            else if (symbol == '.') // left . right
            {
                NFA right = stack.Pop();
                NFA left = stack.Pop();
                stack.Push(NFA.Concat(left, right));
            }else if (symbol == '*') // nfa*
            {
                //NFA nfa = stack.Pop();
                stack.Push(NFA.Closure(stack.Pop()));
            }
            else // any symbol
            {
                stack.Push(NFA.FromSymbol(symbol));
            }
        }
        
        return stack.Pop();
    }
}