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

    public TextFile(string filePath)
    {
        this.FilePath = filePath;
        Content = File.ReadAllText(filePath);
    }

    public void Save(string filePath, bool useBinarySerialization)
    {
        if (useBinarySerialization)
        {
            using (var Writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                Writer.Write(Content);
            }
        }
        else
        {
            using (var StreamWriter = new StreamWriter(filePath))
            {
                var Serializer = new XmlSerializer(typeof(TextEditor));
                Serializer.Serialize(StreamWriter, this);
            }
        }
    }

    public static TextEditor Load(string filePath, bool useBinaryDeserialization)
    {
        if (useBinaryDeserialization)
        {
            using (var Reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                return new TextEditor { Content = Reader.ReadString() };
            }
        }
        else
        {
            using (var StreamReader = new StreamReader(filePath))
            {
                var Serializer = new XmlSerializer(typeof(TextEditor));
                return (TextEditor)Serializer.Deserialize(StreamReader);
            }
        }
    }
}

public class FileSearcher
{
    public IEnumerable<string> SearchFiles(string directoryPath, string keyword)
    {
        var Files = Directory.GetFiles(directoryPath, "*.txt", SearchOption.AllDirectories);
        return Files.Where(FileUser => File.ReadAllText(FileUser).Contains(keyword));
    }
}

public interface IOriginator
{
    object GetState();
    void SetState(object state);
}

public class Memento
{
    private object state;

    public Memento(object state)
    {
        this.state = state;
    }

    public object GetState()
    {
        return state;
    }
}

public class Caretaker
{
    private Stack<Memento> history = new Stack<Memento>();

    public void SaveState(IOriginator originator)
    {
        history.Push(new Memento(originator.GetState()));
    }

    public void RestoreState(IOriginator originator)
    {
        if (history.Count > 0)
        {
            originator.SetState(history.Pop().GetState());
        }
    }
}

[Serializable]
public class TextEditor : IOriginator
{
    public string FilePath { get; set; }
    public string Content { get; set; }

    public TextEditor() { }

    public TextEditor(string filePath)
    {
        FilePath = filePath;
        Content = File.ReadAllText(filePath);
    }

    public void EditContent(string newContent)
    {
        Content = newContent;
    }

    public object GetState()
    {
        return new TextEditorMemento(Content);
    }

    public void SetState(object state)
    {
        if (state is TextEditorMemento memento)
        {
            Content = memento.GetState();
        }
    }

    public void Save(string filePath)
    {
        File.WriteAllText(filePath, Content);
    }
}

[Serializable]
public class TextEditorMemento
{
    private string content;

    public TextEditorMemento(string content)
    {
        this.content = content;
    }

    public string GetState()
    {
        return content;
    }
}


public class FileIndexer
{
    private Dictionary<string, List<string>> Index = new Dictionary<string, List<string>>();

    public void IndexDirectory(string directoryPath, string keyword)
    {
        var Files = Directory.GetFiles(directoryPath, "*.txt", SearchOption.AllDirectories);
        foreach (var FileUser in Files)
        {
            var Content = File.ReadAllText(FileUser);
            if (Content.Contains(keyword))
            {
                if (!Index.ContainsKey(keyword))
                {
                    Index[keyword] = new List<string>();
                }
                Index[keyword].Add(FileUser);
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
        Caretaker Caretaker = new Caretaker();
        TextEditor Editor = null;
        var Searcher = new FileSearcher();
        var Indexer = new FileIndexer();
        Console.Write("Укажите директорию для работы: ");
        string DirectoryPath = Console.ReadLine();
        bool useBinarySerialization = false;

        while (true)
        {
            Console.WriteLine("1. Открыть файл");
            Console.WriteLine("2. Редактировать содержимое");
            Console.WriteLine("3. Сохранить файл");
            Console.WriteLine("4. Откат");
            Console.WriteLine("5. Поиск файлов");
            Console.WriteLine("6. Индекс директории");
            Console.WriteLine("7. Вывести файлы по индексу");
            Console.WriteLine("8. Сменить тип сериализации");
            Console.WriteLine("9. Выход");
            Console.Write("Введите номер выбора: ");

            string Choice = Console.ReadLine();

            switch (Choice)
            {
                case "1":
                    Console.Write("Введите путь до файла: ");
                    string FilePath = Console.ReadLine();
                    Editor = new TextEditor(FilePath);
                    Caretaker.SaveState(Editor);
                    Console.WriteLine("Файл открыт.");
                    break;
                case "2":
                    if (Editor == null)
                    {
                        Console.WriteLine("Файл не открыт.");
                        break;
                    }
                    Console.WriteLine("Введите новое содержимое (введите 'exit' для завершения):");
                    string NewContent = Console.ReadLine();
                    if (NewContent.ToLower() == "exit")
                    {
                        break;
                    }
                    Editor.EditContent(NewContent);
                    Caretaker.SaveState(Editor);
                    Console.WriteLine("Содержимое редактировано.");
                    break;
                case "3":
                    Console.Write("Введите путь для сохранения файла: ");
                    string SaveFilePath = Console.ReadLine();
                    Editor.Save(SaveFilePath);
                    Console.WriteLine("Файл сохранен.");
                    break;
                case "4":
                    Caretaker.RestoreState(Editor);
                    Console.WriteLine("Последнее изменение откачено.");
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
                    Console.WriteLine("Выберите тип сериализации:");
                    Console.WriteLine("1. XML");
                    Console.WriteLine("2. Binary");
                    Console.Write("Введите номер выбора: ");
                    string serializationChoice = Console.ReadLine();
                    useBinarySerialization = serializationChoice == "2";
                    Console.WriteLine("Тип сериализации был сменен.");
                    break;
                case "9":
                    return;
                default:
                    Console.WriteLine("Неверный выбор.");
                    break;
            }
        }
    }
}
