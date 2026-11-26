namespace OSLearningSystem.Models;

public class ProcessItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ArrivalTime { get; set; }
    public int BurstTime { get; set; }
    public int Priority { get; set; } = 1;
    public string Color { get; set; } = "#3b82f6";

    // Результати симуляції
    public int CompletionTime { get; set; }
    public int TurnaroundTime { get; set; }
    public int WaitingTime { get; set; }
    public int ResponseTime { get; set; } = -1;

    public ProcessItem Clone()
    {
        return new ProcessItem
        {
            Id = Id,
            Name = Name,
            ArrivalTime = ArrivalTime,
            BurstTime = BurstTime,
            Priority = Priority,
            Color = Color,
            CompletionTime = CompletionTime,
            TurnaroundTime = TurnaroundTime,
            WaitingTime = WaitingTime,
            ResponseTime = ResponseTime
        };
    }
}

public class GanttBlock
{
    public string ProcessName { get; set; } = string.Empty;
    public string Color { get; set; } = "#94a3b8";
    public int StartTime { get; set; }
    public int EndTime { get; set; }
    public int Duration => EndTime - StartTime;
    public bool IsIdle => string.IsNullOrEmpty(ProcessName) || ProcessName == "Idle";
}

public class CpuSimulationResult
{
    public List<ProcessItem> Processes { get; set; } = new();
    public List<GanttBlock> GanttChart { get; set; } = new();
    public double AverageWaitingTime { get; set; }
    public double AverageTurnaroundTime { get; set; }
    public double CpuUtilization { get; set; }
    public int TotalExecutionTime { get; set; }
    public List<string> LogSteps { get; set; } = new();
}

public enum CpuAlgorithm
{
    FCFS,
    SJF_NonPreemptive,
    SJF_Preemptive, // SRTF
    RoundRobin,
    Priority_NonPreemptive
}
