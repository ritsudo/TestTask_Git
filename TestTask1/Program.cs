// TODO:  полировка программы

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace TestTask1
{
    // Класс и конструктор элемента базы
    [Serializable]
    public class NoteTask
    {

        public int taskKey; // идентификатор задания - добавляется автоматически с наибольшим значением
        public string taskName; // описание задания
        public string taskDescription; // полный текст задания
        public DateTime startDate; // дата
        public bool isFinished; // закончена ли
        public NoteTask() { }
        public NoteTask(int key) { // конструктор принимает только ключ, остальные параметры устанавливаются стандартные и редактируются напрямую
            taskKey = key;
            taskName = "Name";
            taskDescription = "Description";
            startDate = new DateTime(2020, 01, 01);
            isFinished = false;
         }

        public string GetShort(string inputStr) // форматирует сокращенные данные для вывода под консольную таблицу
        {
            if (inputStr.Length > 12) return (inputStr.Substring(0, 9) + "...");
            if (inputStr.Length > 7) return inputStr;
            return (inputStr) + "\t";
        }
        public string CheckFinished()
        {
            if(isFinished) return "Да";
            return "Нет";
        }
        public string CheckOld() // проверка на просроченность
        {
            DateTime dateCurrent = new DateTime();
            dateCurrent = DateTime.Today;
            System.TimeSpan diff0 = new TimeSpan(0); // нулевой интервал
            System.TimeSpan diff1 = dateCurrent.Subtract(startDate);
            // вычесть прошедшую дату из текущей, если результатат больше нуля - да
            if (TimeSpan.Compare(diff1, diff0) == 1) return "Да";
            return "Нет";
        }
        public void SetFullData() { } //установка данных одним методом при создании объекта
        public void GetShortInfo() { // возвращает краткую информацию
            Console.Write($"{taskKey}\t" +
                $"{GetShort(taskName)}" +
                $"\t{GetShort(taskDescription)}\t{startDate:dd.MM.yy} {CheckOld()}\t{CheckFinished()}\n");
        }
        public void GetFullInfo() // возвращает полную информацию
        {
            Console.Write($"# ID элемента: {taskKey}\n# Имя элемента: {taskName}\n# Описание элемента: {taskDescription}\n" +
                $"# Планируемая дата: {startDate.ToShortDateString()}\n# Просрочено: {CheckOld()}\n# Отмечено как выполенное: {CheckFinished()}\n");
        }
    }

    class Program
    {

        static List<NoteTask> noteList = new List<NoteTask>(); // создаём пустую коллекцию данных
        static string titleHeader = "№ пп\tID\tИМЯ\t\tОПИСАНИЕ\tДАТА\t БЫЛО\tВЫПОЛН.\n"; // шапка таблицы
        static bool isDbInitialized = false; // флаг инициализации БД
        static bool isEdited = false; // предотвращает закрытие редактора
        static XmlSerializer formatter = new XmlSerializer(typeof(List<NoteTask>));

        //
        // Символьно-графические и информационные вспомогательные методы консоли
        //

        static void anyKey() 
        {
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }
        static void drawBar()
        {
            for (int i = 0; i < Console.WindowWidth; i++) {
                Console.Write("#");
            }
        }
        static void helpScreen()
        {
            Console.Clear();
            drawBar();
            Console.WriteLine("#\n# Тестовое задание №1 для SolarLab (Уровень 2 - консольное приложение)\n" +
                "# Автор: Виктор Костылев. \n# Реализация началась: 21.12.2020, Реализация закончена: 23.12.2020");
            Console.WriteLine("# Приветствую Вас в редакторе заметок!\n#");
            drawBar();
            Console.WriteLine("#\n# Управление с клавиатуры: \n" +
                "# W - добавить пустую заметку в текущий список \n# E - изменить данные заметки \n# R - удаление заметки \n#\n" +
                "# O - загрузить состояние из файла \n# U - сохранить состояние в файл \n#\n# H - вызов справки (текущее окно) \n# X - выход из программы\n#");
            drawBar();
            anyKey();
        }
        static void newScreen()
        {
            Console.Clear();
            viewNoteList();
        }

        //
        // Вспомогательные методы работы с ID для "БД"
        //

        static int GetLastKey() {                            // показывает последний имеющийся ключ
            var curCount = noteList.Count;                   // получает текущее кол-во эл-в в списке
            var lastKey = (noteList[curCount - 1].taskKey);     // получает новое значение ключа идентификатора (с учетом удалённых ранее элементов)
            return lastKey;
        }
        static int GetNewKey() {                // показывает ключ будущего элемента
            var newKey = GetLastKey() + 1;      // получает новое значение ключа идентификатора (с учетом удалённых ранее элементов)
            return newKey;
        }
        static int GetNumberByID(int ID)
        {
            for (int i = 0; i < noteList.Count; i++)
            {
                // остановка только на нужном элементе
                if (noteList[i].taskKey == ID)  return i;
            }

            return -1; // -1 если элемент не найден
        }

        //
        // Методы для работы с ФС
        //
        static void saveFile() {
        // передаем в конструктор тип класса
        
            string newName = $"{DateTime.Now:yyyyMMdd_HHmm}.xml";
            using (FileStream fs = new FileStream(newName, FileMode.OpenOrCreate))
            {
                formatter.Serialize(fs, noteList);
                Console.WriteLine($"XML файл сохранён под именем {newName}.");
            }
            anyKey();
        }
        static void openFile(string fileName)
        {
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
                {
                    noteList = (List<NoteTask>)formatter.Deserialize(fs);
                    Console.WriteLine($"Файл {fileName} загружен.");
                }
            } catch
            {
                Console.WriteLine("Ошибка при открытии файла. Файл не обнаружен. Проверьте вводимые данные");
            }
            anyKey();
        }

        static void loadLastState() // загрузка последнего состояния в БД
        {
            string[] fileEntries = Directory.GetFiles("./");
            string lastFileName = "";
            foreach (string file in fileEntries)
            {
                if (file.Contains("xml")) lastFileName = file;
            }
                openFile(lastFileName);
        }
        static void loadFile()
        {
            Console.WriteLine("Введите имя нужного Вам файла. Директория содержит следующие файлы:");
            string[] fileEntries = Directory.GetFiles("./");
            foreach (string file in fileEntries) {
                if(file.Contains("xml")) Console.WriteLine(file);
            }
            Console.Write("\n@ ");
            openFile(Console.ReadLine());
        }

        //
        // Методы UI редактора
        //

        static void viewNoteList() {
            Console.WriteLine($"Текущая дата: {DateTime.Now:dd.MM.yyyy} \tТекущее содержимое списка:\n");
            Console.Write(titleHeader);
            drawBar();
            for (int n = 0; n < noteList.Count; n++)
            {
                Console.Write($"{n + 1} \t");
                noteList[n].GetShortInfo();
            }
            Console.Write("\n\n");
        }
        static void createNote() {
            //получение последнего идентификатора
            noteList.Add(new NoteTask(GetNewKey()));
            Console.Write($"Новый элемент с идентификатором {GetLastKey()} создан.");
            anyKey();

        }
        static void deleteNote()
        {
            Console.WriteLine("Введите числовой ID удаляемого элемента");
            int delNumber = -1;
            while (!Int32.TryParse(Console.ReadLine(), out delNumber)) Console.WriteLine("Требуется ввести число");
            int delID = GetNumberByID(delNumber);
            if (delID != -1)
            {
                noteList.RemoveAt(GetNumberByID(delNumber));
                Console.Write($"Элемент с идентификатором {delNumber} и порядковым номером {delID + 1} был удалён.");
            } else
            {
                Console.Write($"Искомый элемент с идентификатором {delNumber} не найден. Удаление не выполнено.");
            }

            anyKey();
        }
        static void editor(int edID, int edParameter, string NewValue) // метод, непосредственно редактирующий данные
        {
            switch (edParameter)
            {
                // 0 - ID не меняется, 1 - имя, 2 - описание, 3 - дата, 4 - выполнено ли
                case 1:
                    noteList[edID].taskName = NewValue;
                    break;
                case 2:
                    noteList[edID].taskDescription = NewValue;
                    break;
                case 3:
                    int ny, nm, nd = 0;
                    Console.WriteLine("Введите новое число. Например: 01");
                    while (!Int32.TryParse(Console.ReadLine(), out nd)) Console.WriteLine("Требуется ввести число");
                    Console.WriteLine("Введите новый месяц. Например: 12");
                    while (!Int32.TryParse(Console.ReadLine(), out nm)) Console.WriteLine("Требуется ввести число");
                    Console.WriteLine("Введите новый год. Например: 2020");
                    while (!Int32.TryParse(Console.ReadLine(), out ny)) Console.WriteLine("Требуется ввести число");
                    try { 
                        noteList[edID].startDate = new DateTime(ny, nm, nd);
                    } catch
                    {
                        Console.WriteLine("Дата была введены неверно. Проверьте данные.");
                        Console.ReadLine();
                    }
                    break;
                case 4:
                    noteList[edID].isFinished = !noteList[edID].isFinished;
                    break;
            }
         //   noteList[edID].taskName = "Edited";
        }
        static void editNote(int edID) 
        {

            //вывод полной информации через метод класса

            //меню выбора действия с клавиатуры: W - ЗАКРЫТЬ, E - ИЗМЕНИТЬ, R - просмотр истории (персистентность)
            Console.Clear();
            drawBar();
            Console.WriteLine("");
            noteList[edID].GetFullInfo();
            Console.WriteLine("");
            drawBar();
            Console.WriteLine("\n| W - ЗАКРЫТЬ | A - ИЗМ. ИМЯ | S - ИЗМ. ОПИСАНИЕ \n" +
                "| D - ИЗМ. ДАТУ | F - ОТМЕТИТЬ КАК ВЫПОЛЕННОЕ / СНЯТЬ ФЛАГ\n");

            var input = Console.ReadKey();

            switch (input.Key)
            {
                case ConsoleKey.W:
                    isEdited = true;
                    Console.WriteLine("\nДанные сохранены.");
                    break;
                case ConsoleKey.A:
                    Console.WriteLine("\nВведите новое имя:");
                    editor(edID, 1, Console.ReadLine());
                    break;
                case ConsoleKey.S:
                    Console.WriteLine("\nВведите новое описание:");
                    editor(edID, 2, Console.ReadLine());
                    break;
                case ConsoleKey.D:
                    Console.WriteLine("\nВведите новую дату:");
                    editor(edID, 3, "");
                    break;
                case ConsoleKey.F:
                    Console.WriteLine("\nСостояние изменено.");
                    editor(edID, 4, "");
                    break;
            }
        } // окно редактора
        static void viewNote() {
            Console.WriteLine("Введите числовой ID просматриваемого элемента");
            int edNumber = -1;
            while(!Int32.TryParse(Console.ReadLine(), out edNumber)) Console.WriteLine("Требуется ввести число");
            Console.Write(edNumber);
            int edID = GetNumberByID(edNumber);
            if (edID != -1)
            {
                while(!isEdited)
                {
                    editNote(edID);
                }
                isEdited = false;
//                editor(edID); // если элемент есть и нажата E - открывает редактор
//                Console.Write($"Данные элемента с идентификатором {edNumber} и порядковым номером {edID + 1} изменены");
            } else
            {
                Console.Write($"Искомый элемент с идентификатором {edNumber} не найден в таблице.");
            }
            anyKey();
        } // селектор элементов
        static bool startupScreen() // основное окно программы
        {
            newScreen();
            Console.Write("\n W - СОЗДАТЬ | E - ОТКРЫТЬ | R - УДАЛИТЬ | H - СПРАВКА | X - ВЫХОД\n" +
                " U - СОХРАНИТЬ ТАБЛИЦУ В ФАЙЛ | O - ЗАГРУЗИТЬ ТАБЛИЦУ ИЗ ФАЙЛА\n@ ");
            var input = Console.ReadKey();
            newScreen();

            switch (input.Key) // управление с клавиатуры
            {
                case ConsoleKey.W: createNote();
                    break;
                case ConsoleKey.E: viewNote();
                    break;
                case ConsoleKey.R: deleteNote();
                    break;
                case ConsoleKey.H: helpScreen();
                    break;
                case ConsoleKey.U: saveFile();
                    break;
                case ConsoleKey.O: loadFile();
                    break;
                case ConsoleKey.X: return true;
                default: break;
            }

            return false;
        }

    //
    // Основной метод приложения
    //
        static void Main(string[] args)
        {
            // забивка базы из 5 объектов в коллекцию. Имитация загрузки из БД. Загрузочный экран

            if (!isDbInitialized)
            {
                for (int i = 0; i < 5; i++) //создали 5 объектов ключи 1 - 5
                {
                    noteList.Add(new NoteTask(i));
                }
                isDbInitialized = true;
                helpScreen();
                Console.WriteLine("Сейчас будет загружено последнее состояние БД");
                loadLastState();
            }

            // создание программной петли

            while (!startupScreen()) {} 

            // закрытие приложения

            Console.Clear();
            Console.WriteLine("Программа завершена.");
            Environment.Exit(0);


        }
    }
}
