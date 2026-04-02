/// <summary>
/// Composite node that tries children left-to-right.
/// Returns Success on the first child that succeeds; Failure if all children fail.
/// </summary>
public class BTSelector : BTNode
{
    private readonly BTNode[] children;

    public BTSelector(params BTNode[] children)
    {
        this.children = children;
    }

    public override BTStatus Tick()
    {
        foreach (BTNode child in children)
        {
            BTStatus status = child.Tick();
            if (status != BTStatus.Failure)
                return status;
        }
        return BTStatus.Failure;
    }
}
