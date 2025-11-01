namespace BeselerNet.Web.Testing;

public class WorkflowContext
{
    public WorkflowStep CurrentStep { get; set; } = WorkflowStep.One;
    public event Action? OnChanged;

    public void AdvanceStep()
    {
        CurrentStep = CurrentStep switch
        {
            WorkflowStep.One => WorkflowStep.Two,
            WorkflowStep.Two => WorkflowStep.Three,
            WorkflowStep.Three => WorkflowStep.One,
            _ => throw new ArgumentOutOfRangeException()
        };
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChanged?.Invoke();
}

public enum WorkflowStep
{
    One,
    Two,
    Three
}
