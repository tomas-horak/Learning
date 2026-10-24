using System;
using System.Collections.Generic;
using System.Linq;

class LibraryManager
{
    private const int MaximumBooks = 5;
    private const int MaximumBorrowedBooks = 3;

    static void Main()
    {
        List<Book> books = new();
        bool isRunning = true;

        while (isRunning)
        {
            Console.WriteLine();
            Console.WriteLine("Commands: add, remove, view, search, borrow, checkin, exit");
            Console.Write("Enter a command: ");
            string action = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

            switch (action)
            {
                case "add":
                    AddBook(books);
                    break;
                case "remove":
                    RemoveBook(books);
                    break;
                case "view":
                    DisplayBooks(books);
                    break;
                case "search":
                    SearchBook(books);
                    break;
                case "borrow":
                    BorrowBook(books);
                    break;
                case "checkin":
                    CheckInBook(books);
                    break;
                case "exit":
                    isRunning = false;
                    break;
                default:
                    Console.WriteLine("Invalid command. Please try again.");
                    break;
            }
        }

        Console.WriteLine("Goodbye!");
    }

    private static void AddBook(List<Book> books)
    {
        if (books.Count >= MaximumBooks)
        {
            Console.WriteLine($"The library is full. It can hold up to {MaximumBooks} books.");
            return;
        }

        string? title = ReadTitle("Enter the title to add: ");
        if (title == null)
        {
            return;
        }

        books.Add(new Book(title));
        Console.WriteLine($"Added \"{title}\" to the library.");
    }

    private static void RemoveBook(List<Book> books)
    {
        string? title = ReadTitle("Enter the title to remove: ");
        if (title == null)
        {
            return;
        }

        Book? book = FindBook(books, title);
        if (book == null)
        {
            Console.WriteLine($"\"{title}\" is not in the collection.");
            return;
        }

        if (book.IsCheckedOut)
        {
            Console.WriteLine("Check in this book before removing it.");
            return;
        }

        books.Remove(book);
        Console.WriteLine($"Removed \"{book.Title}\" from the library.");
    }

    private static void DisplayBooks(List<Book> books)
    {
        if (books.Count == 0)
        {
            Console.WriteLine("The library is empty.");
            return;
        }

        Console.WriteLine("Books in the library:");
        foreach (Book book in books)
        {
            string status = book.IsCheckedOut ? "checked out" : "available";
            Console.WriteLine($"- {book.Title} ({status})");
        }
    }

    // SEARCH FEATURE: look up a title without case sensitivity and report its status.
    private static void SearchBook(List<Book> books)
    {
        string? title = ReadTitle("Enter the title to search for: ");
        if (title == null)
        {
            return;
        }

        Book? book = FindBook(books, title);
        if (book == null)
        {
            Console.WriteLine($"\"{title}\" is not in the collection.");
            return;
        }

        string status = book.IsCheckedOut ? "currently checked out" : "available";
        Console.WriteLine($"\"{book.Title}\" is in the collection and is {status}.");
    }

    // BORROWING LIMIT FEATURE: count checked-out books and allow at most three.
    private static void BorrowBook(List<Book> books)
    {
        int borrowedCount = books.Count(book => book.IsCheckedOut);
        if (borrowedCount >= MaximumBorrowedBooks)
        {
            Console.WriteLine($"You have reached the limit of {MaximumBorrowedBooks} borrowed books.");
            return;
        }

        string? title = ReadTitle("Enter the title to borrow: ");
        if (title == null)
        {
            return;
        }

        Book? book = FindBook(books, title);
        if (book == null)
        {
            Console.WriteLine($"\"{title}\" is not in the collection.");
            return;
        }

        if (book.IsCheckedOut)
        {
            Console.WriteLine($"\"{book.Title}\" is already checked out.");
            return;
        }

        book.IsCheckedOut = true;
        Console.WriteLine($"You borrowed \"{book.Title}\". You have borrowed {borrowedCount + 1} of {MaximumBorrowedBooks} books.");
    }

    private static void CheckInBook(List<Book> books)
    {
        string? title = ReadTitle("Enter the title to check in: ");
        if (title == null)
        {
            return;
        }

        Book? book = FindBook(books, title);
        if (book == null)
        {
            Console.WriteLine($"\"{title}\" is not in the collection.");
            return;
        }

        if (!book.IsCheckedOut)
        {
            Console.WriteLine($"\"{book.Title}\" is not checked out.");
            return;
        }

        book.IsCheckedOut = false;
        Console.WriteLine($"\"{book.Title}\" has been checked in.");
    }

    private static string? ReadTitle(string prompt)
    {
        Console.Write(prompt);
        string title = (Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine("The title cannot be empty.");
            return null;
        }

        return title;
    }

    private static Book? FindBook(List<Book> books, string title)
    {
        return books.FirstOrDefault(book =>
            string.Equals(book.Title, title, StringComparison.OrdinalIgnoreCase));
    }
}

class Book
{
    public string Title { get; }

    // CHECKED-OUT FLAG: true while this book is borrowed; false when it is available.
    public bool IsCheckedOut { get; set; }

    public Book(string title)
    {
        Title = title;
        IsCheckedOut = false;
    }
}
