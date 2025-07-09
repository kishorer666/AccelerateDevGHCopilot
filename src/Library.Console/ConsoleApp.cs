using Library.ApplicationCore;
using Library.ApplicationCore.Entities;
using Library.ApplicationCore.Enums;
using Library.Console;
using Library.Infrastructure.Data;

public class ConsoleApp
{
    ConsoleState _currentState = ConsoleState.PatronSearch;
    List<Patron> matchingPatrons = new List<Patron>();
    Patron? selectedPatronDetails = null;
    Loan selectedLoanDetails = null!;
    IPatronRepository _patronRepository;
    ILoanRepository _loanRepository;
    ILoanService _loanService;
    IPatronService _patronService;
    JsonData _jsonData;

    public ConsoleApp(
        ILoanService loanService,
        IPatronService patronService,
        IPatronRepository patronRepository,
        ILoanRepository loanRepository,
        JsonData jsonData)
    {
        _patronRepository = patronRepository;
        _loanRepository = loanRepository;
        _loanService = loanService;
        _patronService = patronService;
        _jsonData = jsonData;
    }

    public async Task Run()
    {
        while (true)
        {
            switch (_currentState)
            {
                case ConsoleState.PatronSearch:
                    _currentState = await PatronSearch();
                    break;
                case ConsoleState.PatronSearchResults:
                    _currentState = await PatronSearchResults();
                    break;
                case ConsoleState.PatronDetails:
                    _currentState = await PatronDetails();
                    break;
                case ConsoleState.LoanDetails:
                    _currentState = await LoanDetails();
                    break;
            }
        }
    }

    async Task<ConsoleState> PatronSearch()
    {
        string searchInput = ReadPatronName();

        matchingPatrons = await _patronRepository.SearchPatrons(searchInput);

        // Guard-style clauses for edge cases
        if (matchingPatrons.Count > 20)
        {
            Console.WriteLine("More than 20 patrons satisfy the search, please provide more specific input...");
            return ConsoleState.PatronSearch;
        }
        else if (matchingPatrons.Count == 0)
        {
            Console.WriteLine("No matching patrons found.");
            return ConsoleState.PatronSearch;
        }

        Console.WriteLine("Matching Patrons:");
        PrintPatronsList(matchingPatrons);
        return ConsoleState.PatronSearchResults;
    }

    static string ReadPatronName()
    {
        string? searchInput = null;
        while (String.IsNullOrWhiteSpace(searchInput))
        {
            Console.Write("Enter a string to search for patrons by name: ");

            searchInput = Console.ReadLine();
        }
        return searchInput;
    }

    static void PrintPatronsList(List<Patron> matchingPatrons)
    {
        int patronNumber = 1;
        foreach (Patron patron in matchingPatrons)
        {
            Console.WriteLine($"{patronNumber}) {patron.Name}");
            patronNumber++;
        }
    }

    async Task<ConsoleState> PatronSearchResults()
    {
        CommonActions options = CommonActions.Select | CommonActions.SearchPatrons | CommonActions.Quit;
        CommonActions action = ReadInputOptions(options, out int selectedPatronNumber);
        if (action == CommonActions.Select)
        {
            if (selectedPatronNumber >= 1 && selectedPatronNumber <= matchingPatrons.Count)
            {
                var selectedPatron = matchingPatrons.ElementAt(selectedPatronNumber - 1);
                selectedPatronDetails = await _patronRepository.GetPatron(selectedPatron.Id)!;
                return ConsoleState.PatronDetails;
            }
            else
            {
                Console.WriteLine("Invalid patron number. Please try again.");
                return ConsoleState.PatronSearchResults;
            }
        }
        else if (action == CommonActions.Quit)
        {
            return ConsoleState.Quit;
        }
        else if (action == CommonActions.SearchPatrons)
        {
            return ConsoleState.PatronSearch;
        }

        throw new InvalidOperationException("An input option is not handled.");
    }

    static CommonActions ReadInputOptions(CommonActions options, out int optionNumber)
    {
        CommonActions action;
        optionNumber = 0;
        do
        {
            Console.WriteLine();
            WriteInputOptions(options);
            string? userInput = Console.ReadLine();

            action = userInput switch
            {
                "q" when options.HasFlag(CommonActions.Quit) => CommonActions.Quit,
                "s" when options.HasFlag(CommonActions.SearchPatrons) => CommonActions.SearchPatrons,
                "m" when options.HasFlag(CommonActions.RenewPatronMembership) => CommonActions.RenewPatronMembership,
                "e" when options.HasFlag(CommonActions.ExtendLoanedBook) => CommonActions.ExtendLoanedBook,
                "r" when options.HasFlag(CommonActions.ReturnLoanedBook) => CommonActions.ReturnLoanedBook,
                "b" when options.HasFlag(CommonActions.SearchBooks) => CommonActions.SearchBooks,
                _ when int.TryParse(userInput, out optionNumber) => CommonActions.Select,
                _ => CommonActions.Repeat
            };

            if (action == CommonActions.Repeat)
            {
                Console.WriteLine("Invalid input. Please try again.");
            }
        } while (action == CommonActions.Repeat);
        return action;
    }

    static void WriteInputOptions(CommonActions options)
    {
        Console.WriteLine("Input Options:");
        if (options.HasFlag(CommonActions.ReturnLoanedBook))
        {
            Console.WriteLine(" - \"r\" to mark as returned");
        }
        if (options.HasFlag(CommonActions.ExtendLoanedBook))
        {
            Console.WriteLine(" - \"e\" to extend the book loan");
        }
        if (options.HasFlag(CommonActions.RenewPatronMembership))
        {
            Console.WriteLine(" - \"m\" to extend patron's membership");
        }
        if (options.HasFlag(CommonActions.SearchPatrons))
        {
            Console.WriteLine(" - \"s\" for new search");
        }
        if (options.HasFlag(CommonActions.SearchBooks))
        {
            Console.WriteLine(" - \"b\" to check book availability");
        }
        if (options.HasFlag(CommonActions.Quit))
        {
            Console.WriteLine(" - \"q\" to quit");
        }
        if (options.HasFlag(CommonActions.Select))
        {
            Console.WriteLine("Or type a number to select a list item.");
        }
    }

    async Task<ConsoleState> PatronDetails()
    {
        if (selectedPatronDetails == null)
        {
            Console.WriteLine("No patron selected.");
            return ConsoleState.PatronSearch;
        }
        Console.WriteLine($"Name: {selectedPatronDetails.Name}");
        Console.WriteLine($"Membership Expiration: {selectedPatronDetails.MembershipEnd}");
        Console.WriteLine();
        Console.WriteLine("Book Loans:");
        int loanNumber = 1;
        foreach (Loan loan in selectedPatronDetails.Loans)
        {
            var bookTitle = loan.BookItem?.Book?.Title ?? "Unknown";
            Console.WriteLine($"{loanNumber}) {bookTitle} - Due: {loan.DueDate} - Returned: {(loan.ReturnDate != null).ToString()}");
            loanNumber++;
        }

        CommonActions options = CommonActions.SearchPatrons | CommonActions.Quit | CommonActions.Select | CommonActions.RenewPatronMembership | CommonActions.SearchBooks;
        CommonActions action = ReadInputOptions(options, out int selectedLoanNumber);
        if (action == CommonActions.Select)
        {
            if (selectedLoanNumber >= 1 && selectedLoanNumber <= selectedPatronDetails.Loans.Count())
            {
                var selectedLoan = selectedPatronDetails.Loans.ElementAt(selectedLoanNumber - 1);
                selectedLoanDetails = selectedPatronDetails.Loans.Where(l => l.Id == selectedLoan.Id).Single();
                return ConsoleState.LoanDetails;
            }
            else
            {
                Console.WriteLine("Invalid book loan number. Please try again.");
                return ConsoleState.PatronDetails;
            }
        }
        else if (action == CommonActions.Quit)
        {
            return ConsoleState.Quit;
        }
        else if (action == CommonActions.SearchPatrons)
        {
            return ConsoleState.PatronSearch;
        }
        else if (action == CommonActions.RenewPatronMembership)
        {
            var status = await _patronService.RenewMembership(selectedPatronDetails.Id);
            Console.WriteLine(EnumHelper.GetDescription(status));
            // reloading after renewing membership
            var refreshed = await _patronRepository.GetPatron(selectedPatronDetails.Id);
            if (refreshed != null)
                selectedPatronDetails = refreshed;
            else
                Console.WriteLine("Warning: Patron could not be reloaded.");
            return ConsoleState.PatronDetails;
        }
        else if (action == CommonActions.SearchBooks)
        {
            await SearchBooks();
            return ConsoleState.PatronDetails;
        }

        throw new InvalidOperationException("An input option is not handled.");
    }

    async Task<ConsoleState> LoanDetails()
    {
        if (selectedLoanDetails == null)
        {
            Console.WriteLine("No loan selected.");
            return ConsoleState.PatronDetails;
        }
        string bookTitle = "Unknown";
        string bookAuthor = "Unknown";
        DateTime? dueDate = null;
        // Suppress false positive nullability warning for value type
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604
        bool returned = false;
#pragma warning restore CS8600, CS8601, CS8602, CS8603, CS8604
        if (selectedLoanDetails != null)
        {
            bookTitle = selectedLoanDetails.BookItem?.Book?.Title ?? "Unknown";
            bookAuthor = selectedLoanDetails.BookItem?.Book?.Author?.Name ?? "Unknown";
            dueDate = selectedLoanDetails.DueDate;
            returned = selectedLoanDetails.ReturnDate != null;
        }
        Console.WriteLine($"Book title: {bookTitle}");
        Console.WriteLine($"Book Author: {bookAuthor}");
        Console.WriteLine($"Due date: {(dueDate.HasValue ? dueDate.Value.ToString() : "Unknown")}");
        Console.WriteLine($"Returned: {(selectedLoanDetails != null ? returned.ToString() : "Unknown")}");
        Console.WriteLine();

        CommonActions options = CommonActions.SearchPatrons | CommonActions.Quit | CommonActions.ReturnLoanedBook | CommonActions.ExtendLoanedBook;
        CommonActions action = ReadInputOptions(options, out int selectedLoanNumber);

        if (action == CommonActions.ExtendLoanedBook)
        {
            if (selectedLoanDetails != null)
            {
                var status = await _loanService.ExtendLoan(selectedLoanDetails.Id);
                Console.WriteLine(EnumHelper.GetDescription(status));

                // reload loan after extending
                if (selectedPatronDetails != null)
                {
                    var refreshedPatron = await _patronRepository.GetPatron(selectedPatronDetails.Id);
                    if (refreshedPatron != null)
                        selectedPatronDetails = refreshedPatron;
                }
                var refreshedLoan = await _loanRepository.GetLoan(selectedLoanDetails.Id);
                if (refreshedLoan != null)
                    selectedLoanDetails = refreshedLoan;
            }
            return ConsoleState.LoanDetails;
        }
        else if (action == CommonActions.ReturnLoanedBook)
        {
            if (selectedLoanDetails != null)
            {
                var status = await _loanService.ReturnLoan(selectedLoanDetails.Id);

                Console.WriteLine(EnumHelper.GetDescription(status));
                _currentState = ConsoleState.LoanDetails;
                // reload loan after returning
                var refreshedLoan = await _loanRepository.GetLoan(selectedLoanDetails.Id);
                if (refreshedLoan != null)
                    selectedLoanDetails = refreshedLoan;
            }
            return ConsoleState.LoanDetails;
        }
        else if (action == CommonActions.Quit)
        {
            return ConsoleState.Quit;
        }
        else if (action == CommonActions.SearchPatrons)
        {
            return ConsoleState.PatronSearch;
        }

        throw new InvalidOperationException("An input option is not handled.");
    }

    async Task SearchBooks()
    {
        Console.Write("Enter the book title to check availability: ");
        string? bookTitle = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(bookTitle))
        {
            Console.WriteLine("No title entered.");
            return;
        }

        await _jsonData.EnsureDataLoaded();

        var books = _jsonData.Books ?? new List<Book>();
        var bookItems = _jsonData.BookItems ?? new List<BookItem>();
        var loans = _jsonData.Loans ?? new List<Loan>();

        // Find the book by title (case-insensitive)
        var book = books.FirstOrDefault(b => b.Title.Equals(bookTitle, StringComparison.OrdinalIgnoreCase));
        if (book == null)
        {
            Console.WriteLine($"No book found with title '{bookTitle}'.");
            return;
        }

        // Retrieve the corresponding BookItem using BookId
        var bookItem = bookItems.FirstOrDefault(bi => bi.BookId == book.Id);
        if (bookItem == null)
        {
            Console.WriteLine($"No copies of '{book.Title}' exist in the library.");
            return;
        }

        // Check for an active loan for the BookItem
        var activeLoan = loans.FirstOrDefault(l => l.BookItemId == bookItem.Id && l.ReturnDate == null);

        if (activeLoan == null)
        {
            Console.WriteLine($"{book.Title} is available for loan");
        }
        else
        {
            Console.WriteLine($"{book.Title} is on loan to another patron. The return due date is {activeLoan.DueDate:yyyy-MM-dd}.");
        }
    }

    // Helper to get JsonData from the repositories
    JsonData GetJsonData()
    {
        if (_loanRepository is Library.Infrastructure.Data.JsonLoanRepository loanRepo)
        {
            var field = typeof(Library.Infrastructure.Data.JsonLoanRepository).GetField("_jsonData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var value = field.GetValue(loanRepo);
                if (value is JsonData jsonData)
                    return jsonData;
            }
        }
        if (_patronRepository is Library.Infrastructure.Data.JsonPatronRepository patronRepo)
        {
            var field = typeof(Library.Infrastructure.Data.JsonPatronRepository).GetField("_jsonData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var value = field.GetValue(patronRepo);
                if (value is JsonData jsonData)
                    return jsonData;
            }
        }
        throw new InvalidOperationException("JsonData could not be accessed.");
    }
}
