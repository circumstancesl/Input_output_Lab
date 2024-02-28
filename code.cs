using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

[Serializable]
public class TextFile
{
    public string FilePath { get; set; }
    public string Content { get; set; }

    public TextFile() { }

    public TextFile(string FilePath)
    {
        FilePath = FilePath;
        Content = File.ReadAllText(FilePath);
    }

    public void Save(string FilePath)
    {
        using (var Writer = new StreamWriter(FilePath))
        {
            var Serializer = new XmlSerializer(typeof(TextFile));
            Serializer.Serialize(Writer, this);
        }
    }

    public static TextFile Load(string FilePath)
    {
        using (var Reader = new StreamReader(FilePath))
        {
            var Serializer = new XmlSerializer(typeof(TextFile));
            return (TextFile)Serializer.Deserialize(Reader);
        }
    }
}

public class FileSearcher
{
    public IEnumerable<string> SearchFiles(string DirectoryPath, string Keyword)
    {
        var Files = Directory.GetFiles(DirectoryPath, "*.txt", SearchOption.AllDirectories);
        return Files.Where(FileUser => File.ReadAllText(FileUser).Contains(Keyword));
    }
}

public interface IMemento
{
    string GetState();
}

public class TextEditorMemento : IMemento
{
    private string Content;

    public TextEditorMemento(string Content)
    {
        this.Content = Content;
    }

    public string GetState()
    {
        return Content;
    }
}

public class TextEditor
{
    private Stack<IMemento> History = new Stack<IMemento>();
    private string Content;

    public void OpenFile(string FilePath)
    {
        Content = File.ReadAllText(FilePath);
        SaveState();
    }

    public void EditContent(string NewContent)
    {
        Content = NewContent;
        SaveState();
    }

    public void Undo()
    {
        if (History.Count > 0)
        {
            Content = History.Pop().GetState();
        }
    }

    public void SaveFile(string FilePath)
    {
        File.WriteAllText(FilePath, Content);
    }

    private void SaveState()
    {
        History.Push(new TextEditorMemento(Content));
    }
}

public class FileIndexer
{
    private Dictionary<string, List<string>> Index = new Dictionary<string, List<string>>();

    public void IndexDirectory(string DirectoryPath, string Keyword)
    {
        var Files = Directory.GetFiles(DirectoryPath, "*.txt", SearchOption.AllDirectories);
        foreach (var FileUser in Files)
        {
            var Content = File.ReadAllText(FileUser);
            if (Content.Contains(Keyword))
            {
                if (!Index.ContainsKey(Keyword))
                {
                    Index[Keyword] = new List<string>();
                }
                Index[Keyword].Add(FileUser);
            }
        }
    }

    public void PrintIndex()
    {
        foreach (var Entry in Index)
        {
            Console.WriteLine($"Keyword: {Entry.Key}");
            foreach (var FileUser in Entry.Value)
            {
                Console.WriteLine($"  {FileUser}");
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        var Editor = new TextEditor();
        var Searcher = new FileSearcher();
        var Indexer = new FileIndexer();
        Console.Write("Укажите директорию для работы: ");
        string DirectoryPath = Console.ReadLine();

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1. Открыть файл");
            Console.WriteLine("2. Редактировать содержимое");
            Console.WriteLine("3. Откат");
            Console.WriteLine("4. Сохранить файл");
            Console.WriteLine("5. Поиск файлов");
            Console.WriteLine("6. Индекс директории");
            Console.WriteLine("7. Вывести файлы по индексу");
            Console.WriteLine("8. Выход");
            Console.WriteLine("Введите номер выбора: ");

            string Choice = Console.ReadLine();

            switch (Choice)
            {
                case "1":
                    Console.Write("Введите путь файла: ");
                    string FilePath = Console.ReadLine();
                    Editor.OpenFile(FilePath);
                    Console.WriteLine("Файл открыт.");
                    break;
                case "2":
                    Console.WriteLine("Введите новое содержимое (введите 'exit' для завершения):");
                    string newContent = Console.ReadLine();
                    Editor.EditContent(newContent);
                    Console.WriteLine("Содержимое редактировано.");
                    break;
                case "3":
                    Editor.Undo();
                    Console.WriteLine("Последнее изменение отменено.");
                    break;
                case "4":
                    Console.Write("Введите путь для сохранения файла: ");
                    string SaveFilePath = Console.ReadLine();
                    Editor.SaveFile(SaveFilePath);
                    Console.WriteLine("Файл сохранен.");
                    break;
                case "5":
                    Console.Write("Введите ключевое слово для поиска: ");
                    string SearchKeyword = Console.ReadLine();
                    var SearchResults = Searcher.SearchFiles(DirectoryPath, SearchKeyword);
                    foreach (var FileUser in SearchResults)
                    {
                        Console.WriteLine(FileUser);
                    }
                    break;
                case "6":
                    Console.Write("Введите ключевое слово для индексации: ");
                    string IndexKeyword = Console.ReadLine();
                    Indexer.IndexDirectory(DirectoryPath, IndexKeyword);
                    Console.WriteLine("Директория индексирована.");
                    break;
                case "7":
                    Indexer.PrintIndex();
                    break;
                case "8":
                    return;
                default:
                    Console.WriteLine("Недопустимое значение.");
                    break;
            }
        }
    }
}
