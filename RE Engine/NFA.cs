using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Tracing.Parsers.IIS_Trace;

namespace RE_Engine;

public class NFA
{
    public int UnionsCount { get; private set; } = 0;
    private Lazy<State[]> _currentStates;
    private Lazy<State[]> _nextStates;
    public (State start, State end) States { get; private set; }

    public NFA(State start, State end, int unions)
    {
        States = (start, end);
        UnionsCount = unions;
        _currentStates = new Lazy<State[]>(new State[UnionsCount + 1]);
        _nextStates = new Lazy<State[]>(new State[UnionsCount + 1]);
    }
    
    public bool Search(ReadOnlySpan<char> input)
    {
        bool found = false;
        bool terminate = false;
        int index = 0;

        for (int i = 0; i < input.Length; i++)
        {
            index = i;
            MatchState(States.start, input);
        }

        return found;
        
        void MatchState(State currentState, ReadOnlySpan<char> target)
        {
            if (!found || !terminate)
            {
                if (currentState.IsEnd)
                {
                    found = true;
                    return;
                }
            }
            else
            {
                return;
            }
            
            if (currentState.Transition.HasValue)
            {
                if (index >= target.Length)
                {
                    terminate = true;
                    return;
                }
                if (currentState.Transition.Value.symbol == target[index])
                {
                    index++;
                    MatchState(currentState.Transition.Value.to, target);
                }
            }
            else
            {
                if (currentState.EpsilonTransitions != null)
                {
                    int tempIndex = index;
                    foreach (var epsilon in currentState.EpsilonTransitions)
                    {
                        index = tempIndex;
                        MatchState(epsilon, target);
                    }
                }
            }
        }
    }
    
    public bool SearchParallel(ReadOnlySpan<char> input)
    {
        bool result = false;
        int i = 0;
        while (result != true)
        {
            if (i >= input.Length)
                return false;
            
            result = MatchStateParallel(input[i..]);
            i++;
        }
        
        return result;

        bool MatchStateParallel(ReadOnlySpan<char> target)
        {
            int nextCount = 0;
            int currentCount = 0;
            
            _currentStates.Value[currentCount] = States.start;
            currentCount++;
            
            for( int charIndex = 0; charIndex <= target.Length; )
            {
                if (currentCount == 0)
                    return false;
                
                nextCount = 0;
                
                bool incremented = false;
                for (int i = 0 ; i < currentCount; i++)
                {
                    if (_currentStates.Value[i].Transition.HasValue && charIndex != target.Length)
                    {
                        if (_currentStates.Value[i].Transition.Value.symbol == target[charIndex])
                        {
                            _nextStates.Value[nextCount] = _currentStates.Value[i].Transition.Value.to;
                            nextCount++;
                        }

                    }else if (_currentStates.Value[i].EpsilonTransitions != null)
                    {
                        foreach (var epsilonTo in _currentStates.Value[i].EpsilonTransitions)
                        {
                            var ep = ProcessEpsilon(epsilonTo);
                            if (ep.isEnd)
                                return true;
                            
                            incremented = ep.increment;
                        }
                    }
                }
                if (!incremented)
                    charIndex++;

                Array.Copy(_nextStates.Value, _currentStates.Value, nextCount);
                currentCount = nextCount;
            }

            return false;

            (bool isEnd, bool increment) ProcessEpsilon(State epsilonTarget)
            {
                if (epsilonTarget.IsEnd)
                {
                    return (true, false);
                }
                if (epsilonTarget.Transition.HasValue)
                {
                    _nextStates.Value[nextCount] = epsilonTarget;
                    nextCount++;
                    return (false, true); // Step to next character
                } 
                if(epsilonTarget.EpsilonTransitions != null)
                {
                    (bool isEnd, bool increment) result = (false, false);
                    foreach (var innerEpsilon in epsilonTarget.EpsilonTransitions)
                    {
                        result = ProcessEpsilon(innerEpsilon);
                        if (result.isEnd)
                            break; // return
                    }

                    return result;
                }

                return (false, false);
            }
        }
        
    }
    
    public static NFA FromSymbol(char symbol)
    {
        var start = new State(false);
        var end = new State(true);
        start.AddTransition(end, symbol);
        return new NFA(start, end, 0);
    }

    public static NFA FromEpsilon()
    {
        var start = new State(false);
        var end = new State(true);
        start.AddEpsilonTransition(end);
        return new NFA(start, end, 0);
    }

    public static NFA Concat(NFA first, NFA second)
    {
        first.States.end.AddEpsilonTransition(second.States.start);
        first.States.end.IsEnd = false;

        return new NFA( first.States.start, second.States.end, first.UnionsCount + second.UnionsCount );
    }

    public static NFA Union(NFA first, NFA second)
    {
        var start = new State(false);
        start.AddEpsilonTransition(first.States.start);
        start.AddEpsilonTransition(second.States.start);
        
        var end = new State(true);
        first.States.end.AddEpsilonTransition(end);
        second.States.end.AddEpsilonTransition(end);
        
        first.States.end.IsEnd = false;
        second.States.end.IsEnd = false;
        
        return new NFA( start, end, (first.UnionsCount + second.UnionsCount) + 1 );
    }

    public static NFA Closure(NFA nfa)
    {
        nfa.States.end.IsEnd = false;
        nfa.States.end.AddEpsilonTransition(nfa.States.start);
        
        var start = new State(false);
        start.AddEpsilonTransition(nfa.States.start);
        
        var end = new State(true);
        start.AddEpsilonTransition(end);
        nfa.States.end.AddEpsilonTransition(end);
        
        return new NFA(start, end, nfa.UnionsCount);
    }
}