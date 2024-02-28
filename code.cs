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

    // Сериализация XML
    public void XMLSave(string filePath)
    {
        using (var Writer = new StreamWriter(filePath))
        {
            var Serializer = new XmlSerializer(typeof(TextFile));
            Serializer.Serialize(Writer, this);
        }
    }

    // Десериализация XML
    public static TextFile XMLLoad(string filePath)
    {
        using (var Reader = new StreamReader(filePath))
        {
            var Serializer = new XmlSerializer(typeof(TextFile));
            return (TextFile)Serializer.Deserialize(Reader);
        }
    }

    // Бинарная сериализация
    public void BinarySave(string filePath)
    {
        using (var Writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
        {
            Writer.Write(FilePath);
            Writer.Write(Content);
        }
    }

    // бинарная десериализация 
    public static TextFile BinaryLoad(string filePath)
    {
        using (var Reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            var TextFile = new TextFile
            {
                FilePath = Reader.ReadString(),
                Content = Reader.ReadString()
            };
            return TextFile;
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
    string GetState();
}

public class TextEditorMemento : IOriginator
{
    private string Content;

    public TextEditorMemento(string content)
    {
        this.Content = content;
    }

    public string GetState()
    {
        return Content;
    }
}

public class TextEditor
{
    private Stack<IOriginator> History = new Stack<IOriginator>();
    private string Content;

    public void OpenFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл не был найден", filePath);
        }

        Content = File.ReadAllText(filePath);
        SaveState();
    }

    public void EditContent(string newContent)
    {
        Content = newContent;
        SaveState();
    }

    public void Undo()
    {
        if (History.Count > 0)
        {
            Content = History.Pop().GetState();
        }
    }

    public void SaveFile(string filePath)
    {
        File.WriteAllText(filePath, Content);
    }

    private void SaveState()
    {
        History.Push(new TextEditorMemento(Content));
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
