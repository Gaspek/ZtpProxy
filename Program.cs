using System;
using System.Collections.Generic;
using System.Linq;

public interface INewsService
{
    Response AddMessage(string title, string content);
    Response ReadMessage(int id);
    Response EditMessage(int id, string newContent);
    Response DeleteMessage(int id);
}

public class Response
{
    public string Status { get; set; }
    public string Message { get; set; }

    public Response(string status, string message)
    {
        Status = status;
        Message = message;
    }
}

public class User
{
    public string Name { get; set; }
    public UserRole Role { get; set; }

    public User(string name, UserRole role)
    {
        Name = name;
        Role = role;
    }
}

public enum UserRole
{
    Guest,
    User,
    Moderator,
    Admin
}

public class Message
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }

    public Message(int id, string title, string content)
    {
        Id = id;
        Title = title;
        Content = content;
    }
}

public class NewsService : INewsService
{
    private List<Message> _messages;
    private int _nextId;

    public NewsService()
    {
        _messages = new List<Message>();
        _nextId = 1;
    }

    public Response AddMessage(string title, string content)
    {
        var message = new Message(_nextId++, title, content);
        _messages.Add(message);
        return new Response("Success", "Message added successfully.");
    }

    public Response ReadMessage(int id)
    {
        var message = _messages.FirstOrDefault(m => m.Id == id);
        if (message != null)
        {
            return new Response("Success", $"{message.Title}: {message.Content}");
        }
        return new Response("Error", "Message not found.");
    }

    public Response EditMessage(int id, string newContent)
    {
        var message = _messages.FirstOrDefault(m => m.Id == id);
        if (message == null)
        {
            return new Response("Error", "Message not found.");
        }

        message.Content = newContent;
        return new Response("Success", "Message edited successfully.");
    }

    public Response DeleteMessage(int id)
    {
        var message = _messages.FirstOrDefault(m => m.Id == id);
        if (message == null)
        {
            return new Response("Error", "Message not found.");
        }

        _messages.Remove(message);
        return new Response("Success", "Message deleted successfully.");
    }
}

public class NewsServiceProxy : INewsService
{
    private readonly INewsService _service;
    private readonly Lazy<Dictionary<int ,Response>> _cache;
    private readonly User _user;

    public NewsServiceProxy(User user, INewsService service)
    {
        _cache = new Lazy<Dictionary<int, Response>>();
        _user = user;
        _service = service;
    }

    public Response AddMessage(string title, string content)
    {
        if (_user.Role == UserRole.Guest)
        {
            return new Response("Error", "You do not have permissions to add messages");
        }
        //Zakomentowałem to bo nie widzę potrzeby czyszcznia całego cacha przy dodaniu wiadmomości natomiast było to w opisie zadania
        //ClearCache();
        return _service.AddMessage(title, content);
    }

    public Response ReadMessage(int id)
    {
        if (_cache.Value.ContainsKey(id))
        {
            Console.WriteLine($"Returning cached result for id: {id}");
            return _cache.Value[id];

        }
        Console.WriteLine($"Caching result for id: {id}");
        Response result = _service.ReadMessage(id);
        _cache.Value[id] = result;
        return result;
    }

    public Response EditMessage(int id, string newContent)
    {
        if (_user.Role == UserRole.Admin || _user.Role == UserRole.Moderator)
        {
            ClearCache(id);
            return _service.EditMessage(id, newContent);
        }
        return new Response("Error", "You do not have permissions to edit messages");
    }

    public Response DeleteMessage(int id)
    {
        if (_user.Role == UserRole.Admin)
        {
            ClearCache(id);
            return _service.DeleteMessage(id);
        }
        return new Response("Error", "You do not have permissions to delete messages");
    }

    private void ClearCache()
    {
        _cache.Value.Clear();
        Console.WriteLine("Czyszczenie cache");
    }
    private void ClearCache(int id)
    {
        if (_cache.Value.ContainsKey(id))
        {
            _cache.Value.Remove(id);
            Console.WriteLine($"Czyszczenie cache dla id: {id}");
        }
    }
}


public class Program
{
    public static void Main()
    {
        // Tworzymy instancję rzeczywistego serwisu
        INewsService newsService = new NewsService();
        User guset = new User("Norman", UserRole.Guest);
        User user = new User("Bartek", UserRole.User);
        User moderaor = new User("Inga", UserRole.Moderator);
        User admin = new User("Dawid", UserRole.Admin);

        INewsService guestProxy = new NewsServiceProxy(guset, newsService);
        INewsService userProxy = new NewsServiceProxy(user, newsService);
        INewsService modProxy = new NewsServiceProxy(moderaor, newsService);
        INewsService adminProxy = new NewsServiceProxy(admin, newsService);

        // Dodawanie wiadomości
        var addResponse = guestProxy.AddMessage("I like pancakes", "Pancakes are yummy :)");
        Console.WriteLine($"{addResponse.Status}: {addResponse.Message}");
        addResponse = userProxy.AddMessage("Breaking News", "New breakthrough in AI technology.");
        Console.WriteLine($"{addResponse.Status}: {addResponse.Message}");
        addResponse = modProxy.AddMessage("Market Update", "Stocks soar after positive earnings reports.");
        Console.WriteLine($"{addResponse.Status}: {addResponse.Message}");

        // Odczyt wiadomości
        var readResponse = guestProxy.ReadMessage(1);
        Console.WriteLine($"{readResponse.Status}: {readResponse.Message}");
        readResponse = userProxy.ReadMessage(2);
        Console.WriteLine($"{readResponse.Status}: {readResponse.Message}");

        // Próbujemy odczytać wiadomość, która nie istnieje
        readResponse = modProxy.ReadMessage(3);
        Console.WriteLine($"{readResponse.Status}: {readResponse.Message}");

        // Edycja wiadomości
        var editResponse = modProxy.EditMessage(1, "Updated content: AI technology is advancing rapidly.");
        Console.WriteLine($"{editResponse.Status}: {editResponse.Message}");
        // Odczyt wiadomości po edycji
        var readResponseAfterEdit = adminProxy.ReadMessage(1);
        Console.WriteLine($"{readResponseAfterEdit.Status}: {readResponseAfterEdit.Message}");
        //Próba edycji
        editResponse = userProxy.EditMessage(1, "Updated content: technology is advancing rapidly.");
        Console.WriteLine($"{editResponse.Status}: {editResponse.Message}");
        readResponseAfterEdit = adminProxy.ReadMessage(1);
        Console.WriteLine($"{readResponseAfterEdit.Status}: {readResponseAfterEdit.Message}");

        // Próba usunięcia wiadomości
        var deleteResponse = modProxy.DeleteMessage(2);
        Console.WriteLine($"{deleteResponse.Status}: {deleteResponse.Message}");
        var readResponseAfterDelete = modProxy.ReadMessage(2);
        Console.WriteLine($"{readResponseAfterDelete.Status}: {readResponseAfterDelete.Message}");

        // Próba odczytania usuniętej wiadomości
        deleteResponse = adminProxy.DeleteMessage(2);
        Console.WriteLine($"{deleteResponse.Status}: {deleteResponse.Message}");
        readResponseAfterDelete = adminProxy.ReadMessage(2);
        Console.WriteLine($"{readResponseAfterDelete.Status}: {readResponseAfterDelete.Message}");
    }
}
