namespace OSLearningSystem.Services;

using OSLearningSystem.Models;

public class DeadlockService
{
    // Алгоритм банкіра (Banker's Algorithm) для перевірки безпечного стану системи
    public BankerSimulationResult CheckSafeState(List<BankerProcess> processes, int[] totalAvailable)
    {
        var result = new BankerSimulationResult();
        int n = processes.Count;
        if (n == 0 || totalAvailable == null || totalAvailable.Length == 0) return result;

        int numResources = totalAvailable.Length;
        int[] work = (int[])totalAvailable.Clone();
        bool[] finish = new bool[n];
        var safeSeq = new List<string>();

        result.ExecutionSteps.Add($"Початковий стан: Доступно ресурсів Work = [{string.Join(", ", work)}]");

        int count = 0;
        while (count < n)
        {
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                if (!finish[i])
                {
                    // Перевіряємо, чи Need[i] <= Work
                    bool canAllocate = true;
                    for (int j = 0; j < numResources; j++)
                    {
                        if (processes[i].Need[j] > work[j])
                        {
                            canAllocate = false;
                            break;
                        }
                    }

                    if (canAllocate)
                    {
                        // Процес може отримати ресурси, завершитись та звільнити свої
                        for (int j = 0; j < numResources; j++)
                        {
                            work[j] += processes[i].Allocation[j];
                        }

                        safeSeq.Add(processes[i].Name);
                        finish[i] = true;
                        found = true;
                        count++;

                        result.ExecutionSteps.Add(
                            $"✅ Процес {processes[i].Name} може виконатися (Потреба: [{string.Join(", ", processes[i].Need)}] <= Доступно). " +
                            $"Після звільнення ресурсів Work = [{string.Join(", ", work)}]");
                    }
                }
            }

            if (!found)
            {
                // Жоден процес не може задовольнити потреби -> Тупик (Unsafe State)
                result.IsSafe = false;
                result.ExecutionSteps.Add("❌ УВАГА: Безпечної послідовності не існує! Система перебуває в небезпечному стані (Deadlock / Unsafe State).");
                result.FinalAvailable = work;
                return result;
            }
        }

        result.IsSafe = true;
        result.SafeSequence = safeSeq;
        result.FinalAvailable = work;
        result.ExecutionSteps.Add($"🎉 СИСТЕМА В БЕЗПЕЧНОМУ СТАНІ (SAFE)! Послідовність: < {string.Join(" ➔ ", safeSeq)} >");
        return result;
    }
}
