/// <summary>
/// Status returned by behavior tree nodes after evaluation.
/// </summary>
public enum BTStatus
{
    Success,
    Failure,
    Running
}

/// <summary>
/// Abstract base class for all behavior tree nodes.
/// </summary>
public abstract class BTNode
{
    public abstract BTStatus Tick();
}
