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
            using (var writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                writer.Write(Content);
            }
        }
        else
        {
            using (var streamWriter = new StreamWriter(filePath))
            {
                var serializer = new XmlSerializer(typeof(TextEditor));
                serializer.Serialize(streamWriter, this);
            }
        }
    }

    public static TextEditor Load(string filePath, bool useBinaryDeserialization)
    {
        if (useBinaryDeserialization)
        {
            using (var reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                return new TextEditor { Content = reader.ReadString() };
            }
        }
        else
        {
            using (var streamReader = new StreamReader(filePath))
            {
                var serializer = new XmlSerializer(typeof(TextEditor));
                return (TextEditor)serializer.Deserialize(streamReader);
            }
        }
    }
}

public class FileSearcher
{
    public IEnumerable<string> SearchFiles(string directoryPath, string keyword)
    {
        var files = Directory.GetFiles(directoryPath, "*.txt", SearchOption.AllDirectories);
        return files.Where(FileUser => File.ReadAllText(FileUser).Contains(keyword));
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
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл не найден", filePath);
        }

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
    private Dictionary<string, List<string>> index = new Dictionary<string, List<string>>();

    public void IndexDirectory(string directoryPath, string keyword)
    {
        var files = Directory.GetFiles(directoryPath, "*.txt", SearchOption.AllDirectories);
        foreach (var FileUser in files)
        {
            var Content = File.ReadAllText(FileUser);
            if (Content.Contains(keyword))
            {
                if (!index.ContainsKey(keyword))
                {
                    index[keyword] = new List<string>();
                }
                index[keyword].Add(FileUser);
            }
        }
    }

    public void PrintIndex()
    {
        foreach (var Entry in index)
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
        Caretaker caretake = new Caretaker();
        TextEditor editor = null;
        var searcher = new FileSearcher();
        var indexer = new FileIndexer();
        Console.Write("Укажите директорию для работы: ");
        string directoryPath = Console.ReadLine();
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

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.Write("Введите путь до файла: ");
                    string filePath = Console.ReadLine();
                    editor = new TextEditor(filePath);
                    caretake.SaveState(editor);
                    Console.WriteLine("Файл открыт.");
                    break;
                case "2":
                    if (editor == null)
                    {
                        Console.WriteLine("Файл не открыт.");
                        break;
                    }
                    Console.WriteLine("Введите новое содержимое (введите 'exit' для завершения):");
                    string newContent = Console.ReadLine();
                    if (newContent.ToLower() == "exit")
                    {
                        break;
                    }
                    editor.EditContent(newContent);
                    caretake.SaveState(editor);
                    Console.WriteLine("Содержимое редактировано.");
                    break;
                case "3":
                    Console.Write("Введите путь для сохранения файла: ");
                    string saveFilePath = Console.ReadLine();
                    editor.Save(saveFilePath);
                    Console.WriteLine("Файл сохранен.");
                    break;
                case "4":
                    caretake.RestoreState(editor);
                    Console.WriteLine("Последнее изменение откачено.");
                    break;
                case "5":
                    Console.Write("Введите ключевое слово для поиска: ");
                    string searchKeyword = Console.ReadLine();
                    var searchResults = searcher.SearchFiles(directoryPath, searchKeyword);
                    foreach (var fileUser in searchResults)
                    {
                        Console.WriteLine(fileUser);
                    }
                    break;
                case "6":
                    Console.Write("Введите ключевое слово для индексации: ");
                    string indexKeyword = Console.ReadLine();
                    indexer.IndexDirectory(directoryPath, indexKeyword);
                    Console.WriteLine("Директория индексирована.");
                    break;
                case "7":
                    indexer.PrintIndex();
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
