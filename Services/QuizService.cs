namespace OSLearningSystem.Services;

using OSLearningSystem.Models;

public class QuizService
{
    public List<QuizQuestion> GetQuestions() => GetDefaultQuestions();

    public QuizResult EvaluateQuiz(List<QuizQuestion> questions)
    {
        int correct = questions.Count(q => q.SelectedOptionIndex.HasValue && q.SelectedOptionIndex.Value == q.CorrectOptionIndex);
        return new QuizResult
        {
            TotalQuestions = questions.Count,
            CorrectAnswers = correct
        };
    }

    public List<QuizQuestion> GetDefaultQuestions()
    {
        return new List<QuizQuestion>
        {
            new QuizQuestion
            {
                Id = 1,
                Category = "Планування процесора",
                QuestionText = "Який алгоритм планування процесора мінімізує середній час очікування для набору процесів?",
                Options = new List<string>
                {
                    "FCFS (First-Come, First-Served)",
                    "SJF (Shortest Job First).",
                    "Round Robin з великим квантом",
                    "Пріоритетне планування без витіснення"
                },
                CorrectOptionIndex = 1,
                Explanation = "SJF є математично оптимальним алгоритмом щодо мінімізації середнього часу очікування, якщо тривалість усіх завдань відома заздалегідь."
            },
            new QuizQuestion
            {
                Id = 2,
                Category = "Планування процесора",
                QuestionText = "Що станеться в алгоритмі Round Robin, якщо квант часу (Time Quantum) буде прагнути до нескінченності?",
                Options = new List<string>
                {
                    "Система перетвориться на алгоритм FCFS.",
                    "Виникне стан Deadlock",
                    "Середній час очікування впаде до 0",
                    "Процесор зупинить роботу"
                },
                CorrectOptionIndex = 0,
                Explanation = "Якщо квант часу перевищує тривалість будь-якого процесу, кожен процес виконується до кінця без переривань, що еквівалентно First-Come First-Served."
            },
            new QuizQuestion
            {
                Id = 3,
                Category = "Керування пам'яттю",
                QuestionText = "Яка різниця між внутрішньою та зовнішньою фрагментацією?",
                Options = new List<string>
                {
                    "Внутрішня виникає на диску, зовнішня — в RAM",
                    "Внутрішня — це невикористане місце всередині виділеного блоку; зовнішня — наявність сумарно достатньої пам'яті, але розбитої на замалі шматки.",
                    "Зовнішня фрагментація притаманна лише сторінковій організації",
                    "Між ними немає жодної різниці"
                },
                CorrectOptionIndex = 1,
                Explanation = "Внутрішня фрагментація виникає, коли виділений блок більший за запитаний. Зовнішня — коли сумарно пам'яті вистачає, але немає одного суміжного блоку потрібного розміру."
            },
            new QuizQuestion
            {
                Id = 4,
                Category = "Керування пам'яттю",
                QuestionText = "Який принцип лежить в основі алгоритму заміщення сторінок LRU (Least Recently Used)?",
                Options = new List<string>
                {
                    "Заміщується сторінка, яка потрапила в пам'ять найпершою",
                    "Заміщується сторінка, до якої найдовше не було звернень у минулому.",
                    "Заміщується випадкова сторінка",
                    "Заміщується сторінка з найменшим розміром"
                },
                CorrectOptionIndex = 1,
                Explanation = "LRU (Least Recently Used) спирається на часову локальність і вивантажує ту сторінку, до якої не зверталися найдовший період часу."
            },
            new QuizQuestion
            {
                Id = 5,
                Category = "Синхронізація та тупики",
                QuestionText = "Для чого призначений алгоритм банкіра (Banker's Algorithm) Едсгера Дейкстри?",
                Options = new List<string>
                {
                    "Для оптимізації роботи кешу процесора",
                    "Для уникнення тупиків (Deadlock Avoidance) шляхом аналізу безпечності стану перед виділенням ресурсів.",
                    "Для шифрування системних паролів",
                    "Для розрахунку швидкості комп'ютерної мережі"
                },
                CorrectOptionIndex = 1,
                Explanation = "Алгоритм банкіра моделює розподіл обмежених ресурсів і видає їх тільки тоді, коли після виділення гарантовано залишається хоча б одна безпечна послідовність завершення процесів."
            },
            new QuizQuestion
            {
                Id = 6,
                Category = "Архітектура ОС",
                QuestionText = "У чому полягає різниця між режимом користувача (User Mode) та режимом ядра (Kernel Mode)?",
                Options = new List<string>
                {
                    "У User Mode програми мають прямий доступ до апаратного забезпечення",
                    "У Kernel Mode виконується лише графічний інтерфейс",
                    "У Kernel Mode код має необмежений доступ до процесора і пам'яті; у User Mode доступ обмежено задля стабільності та безпеки.",
                    "User Mode використовується лише для мережевих операцій"
                },
                CorrectOptionIndex = 2,
                Explanation = "Дворівневий режим роботи апаратного забезпечення захищає систему: користувацькі додатки не можуть безпосередньо впливати на системні критичні ресурси без системних викликів (system calls)."
            }
        };
    }
}
