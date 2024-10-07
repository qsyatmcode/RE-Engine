namespace RE_Engine;

public class State
{
    public bool IsEnd { get; set; }
    public (char symbol, State to)? Transition { get; private set; }
    public List<State>? EpsilonTransitions { get; private set; }
    
    public State(bool isEnd)
    {
        IsEnd = isEnd;
        Transition = null;
        EpsilonTransitions = null;
    }

    public void AddTransition(State to, char symbol)
    {
        EpsilonTransitions = null;
        Transition = (symbol, to);
    }

    public void AddEpsilonTransition(State to)
    {
        Transition = null;
        if (EpsilonTransitions == null)
        {
            EpsilonTransitions = new List<State>();
            EpsilonTransitions.Add(to);
        }
        else
        {
            if(EpsilonTransitions.Count < 2)
                EpsilonTransitions.Add(to);
            else
                throw new Exception("EpsilonTransitions must be at least 2");
        }
    }
}