/// <summary>
/// Leaf node that evaluates a boolean predicate.
/// Returns Success if the predicate is true, Failure otherwise.
/// </summary>
public class BTCondition : BTNode
{
    private readonly System.Func<bool> predicate;

    public BTCondition(System.Func<bool> predicate)
    {
        this.predicate = predicate;
    }

    public override BTStatus Tick()
    {
        return predicate() ? BTStatus.Success : BTStatus.Failure;
    }
}
