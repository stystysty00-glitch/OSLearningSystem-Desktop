namespace OSLearningSystem.Services;

using OSLearningSystem.Models;

public class CpuSchedulingService
{
    public CpuSimulationResult Simulate(List<ProcessItem> originalProcesses, CpuAlgorithm algorithm, int timeQuantum = 2)
    {
        var result = new CpuSimulationResult();
        if (originalProcesses == null || originalProcesses.Count == 0) return result;

        var processes = originalProcesses.Select(p => p.Clone()).OrderBy(p => p.ArrivalTime).ToList();

        switch (algorithm)
        {
            case CpuAlgorithm.FCFS:
                SimulateFCFS(processes, result);
                break;
            case CpuAlgorithm.SJF_NonPreemptive:
                SimulateSJF(processes, result);
                break;
            case CpuAlgorithm.SJF_Preemptive:
                SimulateSRTF(processes, result);
                break;
            case CpuAlgorithm.RoundRobin:
                SimulateRoundRobin(processes, result, Math.Max(1, timeQuantum));
                break;
            case CpuAlgorithm.Priority_NonPreemptive:
                SimulatePriority(processes, result);
                break;
        }

        // Розрахунок підсумкових метрик
        result.Processes = processes.OrderBy(p => p.Id).ToList();
        if (result.Processes.Count > 0)
        {
            result.AverageWaitingTime = Math.Round(result.Processes.Average(p => p.WaitingTime), 2);
            result.AverageTurnaroundTime = Math.Round(result.Processes.Average(p => p.TurnaroundTime), 2);
        }

        if (result.GanttChart.Count > 0)
        {
            result.TotalExecutionTime = result.GanttChart.Max(g => g.EndTime);
            var busyTime = result.GanttChart.Where(g => !g.IsIdle).Sum(g => g.Duration);
            result.CpuUtilization = result.TotalExecutionTime > 0 
                ? Math.Round((double)busyTime / result.TotalExecutionTime * 100, 1) 
                : 0;
        }

        return result;
    }

    private void SimulateFCFS(List<ProcessItem> processes, CpuSimulationResult result)
    {
        int currentTime = 0;
        foreach (var p in processes)
        {
            if (currentTime < p.ArrivalTime)
            {
                result.GanttChart.Add(new GanttBlock
                {
                    ProcessName = "Idle",
                    StartTime = currentTime,
                    EndTime = p.ArrivalTime,
                    Color = "#94a3b8"
                });
                result.LogSteps.Add($"[Час {currentTime}-{p.ArrivalTime}]: Процесор простоює (Idle)");
                currentTime = p.ArrivalTime;
            }

            p.ResponseTime = currentTime - p.ArrivalTime;
            int start = currentTime;
            currentTime += p.BurstTime;
            p.CompletionTime = currentTime;
            p.TurnaroundTime = p.CompletionTime - p.ArrivalTime;
            p.WaitingTime = p.TurnaroundTime - p.BurstTime;

            result.GanttChart.Add(new GanttBlock
            {
                ProcessName = p.Name,
                Color = p.Color,
                StartTime = start,
                EndTime = currentTime
            });
            result.LogSteps.Add($"[Час {start}-{currentTime}]: Виконується {p.Name} (Завершено о {p.CompletionTime})");
        }
    }

    private void SimulateSJF(List<ProcessItem> processes, CpuSimulationResult result)
    {
        int currentTime = 0;
        var remaining = new List<ProcessItem>(processes);
        var completed = new List<ProcessItem>();

        while (remaining.Count > 0)
        {
            var available = remaining.Where(p => p.ArrivalTime <= currentTime).ToList();
            if (available.Count == 0)
            {
                int nextArrival = remaining.Min(p => p.ArrivalTime);
                result.GanttChart.Add(new GanttBlock
                {
                    ProcessName = "Idle",
                    StartTime = currentTime,
                    EndTime = nextArrival,
                    Color = "#94a3b8"
                });
                currentTime = nextArrival;
                continue;
            }

            var nextProcess = available.OrderBy(p => p.BurstTime).First();
            remaining.Remove(nextProcess);

            int start = currentTime;
            currentTime += nextProcess.BurstTime;
            nextProcess.CompletionTime = currentTime;
            nextProcess.TurnaroundTime = nextProcess.CompletionTime - nextProcess.ArrivalTime;
            nextProcess.WaitingTime = nextProcess.TurnaroundTime - nextProcess.BurstTime;

            result.GanttChart.Add(new GanttBlock
            {
                ProcessName = nextProcess.Name,
                Color = nextProcess.Color,
                StartTime = start,
                EndTime = currentTime
            });
            result.LogSteps.Add($"[Час {start}-{currentTime}]: Виконано {nextProcess.Name} (Burst: {nextProcess.BurstTime})");
            completed.Add(nextProcess);
        }
    }

    private void SimulateSRTF(List<ProcessItem> processes, CpuSimulationResult result)
    {
        int n = processes.Count;
        int[] remainingBurst = processes.Select(p => p.BurstTime).ToArray();
        int completed = 0;
        int currentTime = 0;
        int? lastProcessIdx = null;
        int blockStart = 0;

        while (completed < n)
        {
            int shortestIdx = -1;
            int minRemaining = int.MaxValue;

            for (int i = 0; i < n; i++)
            {
                if (processes[i].ArrivalTime <= currentTime && remainingBurst[i] > 0)
                {
                    if (remainingBurst[i] < minRemaining)
                    {
                        minRemaining = remainingBurst[i];
                        shortestIdx = i;
                    }
                }
            }

            if (shortestIdx == -1)
            {
                // Idle
                if (lastProcessIdx.HasValue)
                {
                    result.GanttChart.Add(new GanttBlock
                    {
                        ProcessName = processes[lastProcessIdx.Value].Name,
                        Color = processes[lastProcessIdx.Value].Color,
                        StartTime = blockStart,
                        EndTime = currentTime
                    });
                    lastProcessIdx = null;
                }
                currentTime++;
                continue;
            }

            if (lastProcessIdx != shortestIdx)
            {
                if (lastProcessIdx.HasValue)
                {
                    result.GanttChart.Add(new GanttBlock
                    {
                        ProcessName = processes[lastProcessIdx.Value].Name,
                        Color = processes[lastProcessIdx.Value].Color,
                        StartTime = blockStart,
                        EndTime = currentTime
                    });
                }
                blockStart = currentTime;
                lastProcessIdx = shortestIdx;
            }

            remainingBurst[shortestIdx]--;
            currentTime++;

            if (remainingBurst[shortestIdx] == 0)
            {
                completed++;
                var p = processes[shortestIdx];
                p.CompletionTime = currentTime;
                p.TurnaroundTime = p.CompletionTime - p.ArrivalTime;
                p.WaitingTime = p.TurnaroundTime - p.BurstTime;
                result.LogSteps.Add($"[Час {currentTime}]: Процес {p.Name} завершив виконання");
            }
        }

        if (lastProcessIdx.HasValue)
        {
            result.GanttChart.Add(new GanttBlock
            {
                ProcessName = processes[lastProcessIdx.Value].Name,
                Color = processes[lastProcessIdx.Value].Color,
                StartTime = blockStart,
                EndTime = currentTime
            });
        }
    }

    private void SimulateRoundRobin(List<ProcessItem> processes, CpuSimulationResult result, int quantum)
    {
        var readyQueue = new Queue<ProcessItem>();
        int currentTime = 0;
        int completed = 0;
        int n = processes.Count;
        var remainingBurst = processes.ToDictionary(p => p.Id, p => p.BurstTime);
        var inQueue = new HashSet<int>();

        // Додаємо ті, що прибули о 0
        foreach (var p in processes.Where(p => p.ArrivalTime <= currentTime))
        {
            readyQueue.Enqueue(p);
            inQueue.Add(p.Id);
        }

        while (completed < n)
        {
            if (readyQueue.Count == 0)
            {
                var nextP = processes.Where(p => remainingBurst[p.Id] > 0).OrderBy(p => p.ArrivalTime).FirstOrDefault();
                if (nextP != null)
                {
                    result.GanttChart.Add(new GanttBlock
                    {
                        ProcessName = "Idle",
                        StartTime = currentTime,
                        EndTime = nextP.ArrivalTime,
                        Color = "#94a3b8"
                    });
                    currentTime = nextP.ArrivalTime;
                    readyQueue.Enqueue(nextP);
                    inQueue.Add(nextP.Id);
                }
            }

            if (readyQueue.Count == 0) break;

            var current = readyQueue.Dequeue();
            inQueue.Remove(current.Id);

            int execution = Math.Min(quantum, remainingBurst[current.Id]);
            int start = currentTime;
            currentTime += execution;
            remainingBurst[current.Id] -= execution;

            result.GanttChart.Add(new GanttBlock
            {
                ProcessName = current.Name,
                Color = current.Color,
                StartTime = start,
                EndTime = currentTime
            });

            // Додаємо нові процеси, що надійшли під час цього кванту
            foreach (var p in processes.Where(p => p.ArrivalTime <= currentTime && remainingBurst[p.Id] > 0 && !inQueue.Contains(p.Id) && p.Id != current.Id))
            {
                readyQueue.Enqueue(p);
                inQueue.Add(p.Id);
            }

            if (remainingBurst[current.Id] > 0)
            {
                readyQueue.Enqueue(current);
                inQueue.Add(current.Id);
            }
            else
            {
                completed++;
                current.CompletionTime = currentTime;
                current.TurnaroundTime = current.CompletionTime - current.ArrivalTime;
                current.WaitingTime = current.TurnaroundTime - current.BurstTime;
                result.LogSteps.Add($"[Час {currentTime}]: {current.Name} повністю завершено");
            }
        }
    }

    private void SimulatePriority(List<ProcessItem> processes, CpuSimulationResult result)
    {
        int currentTime = 0;
        var remaining = new List<ProcessItem>(processes);

        while (remaining.Count > 0)
        {
            var available = remaining.Where(p => p.ArrivalTime <= currentTime).ToList();
            if (available.Count == 0)
            {
                int nextArrival = remaining.Min(p => p.ArrivalTime);
                result.GanttChart.Add(new GanttBlock
                {
                    ProcessName = "Idle",
                    StartTime = currentTime,
                    EndTime = nextArrival,
                    Color = "#94a3b8"
                });
                currentTime = nextArrival;
                continue;
            }

            // Менший номер пріоритету = вищий пріоритет
            var nextProcess = available.OrderBy(p => p.Priority).First();
            remaining.Remove(nextProcess);

            int start = currentTime;
            currentTime += nextProcess.BurstTime;
            nextProcess.CompletionTime = currentTime;
            nextProcess.TurnaroundTime = nextProcess.CompletionTime - nextProcess.ArrivalTime;
            nextProcess.WaitingTime = nextProcess.TurnaroundTime - nextProcess.BurstTime;

            result.GanttChart.Add(new GanttBlock
            {
                ProcessName = nextProcess.Name,
                Color = nextProcess.Color,
                StartTime = start,
                EndTime = currentTime
            });
            result.LogSteps.Add($"[Час {start}-{currentTime}]: Виконано {nextProcess.Name} (Пріоритет: {nextProcess.Priority})");
        }
    }
}
