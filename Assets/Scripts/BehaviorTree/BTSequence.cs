/// <summary>
/// Composite node that runs children left-to-right.
/// Returns Failure on the first child that fails; Success if all children succeed.
/// </summary>
public class BTSequence : BTNode
{
    private readonly BTNode[] children;

    public BTSequence(params BTNode[] children)
    {
        this.children = children;
    }

    public override BTStatus Tick()
    {
        foreach (BTNode child in children)
        {
            BTStatus status = child.Tick();
            if (status != BTStatus.Success)
                return status;
        }
        return BTStatus.Success;
    }
}
