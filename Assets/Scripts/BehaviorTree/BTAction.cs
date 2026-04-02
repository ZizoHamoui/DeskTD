/// <summary>
/// Leaf node that executes a callback action.
/// Returns the BTStatus produced by the callback.
/// </summary>
public class BTAction : BTNode
{
    private readonly System.Func<BTStatus> action;

    public BTAction(System.Func<BTStatus> action)
    {
        this.action = action;
    }

    public override BTStatus Tick()
    {
        return action();
    }
}
