using MySqlConnector;

int? selectedGuestId = null; // den kund operatören arbetar med just nu
bool running = true;

while (running)
{
    Console.WriteLine();
    Console.WriteLine("===Holiday Maker===");
    Console.WriteLine(selectedGuestId is null
        ? "Ingen kund vald"
        : $"Vald kund: id {selectedGuestId}");
    Console.WriteLine("1. Registrera kund");
    Console.WriteLine("2. Välj kund");
    Console.WriteLine("3. Sök lediga rum och boka");
    Console.WriteLine("4. Visa kundens bokningar");
    Console.WriteLine("5. Ändra antal personer");
    Console.WriteLine("6. Avboka");
    Console.WriteLine("0. Avsluta");
    Console.Write("Val: ");

    string? choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1": GuestService.Register(); break;
            case "2": selectedGuestId = GuestService.Select() ?? selectedGuestId; break;
            case "3": RoomService.Search(selectedGuestId); break;
            case "4":
                if (selectedGuestId is null) Console.WriteLine("Välj en kund först (menyval 2).");
                else BookingService.ShowForGuest(selectedGuestId.Value);
                break;
            case "5":
                if (selectedGuestId is null) Console.WriteLine("Välj en kund först (menyval 2).");
                else BookingService.ChangeNumberOfGuests(selectedGuestId.Value);
                break;
            case "6":
                if (selectedGuestId is null) Console.WriteLine("Välj en kund först (menyval 2).");
                else BookingService.Cancel(selectedGuestId.Value);
                break;
            case "0": running = false; break;
            default:  Console.WriteLine("Ogiltigt val, försök igen."); break;
        }
    }
    catch (MySqlException ex)
    {
        // Ett databasfel ska inte avsluta programmet
        Console.WriteLine($"Databasfel: {ex.Message}");
    }
}

Console.WriteLine("Trevlig Resa!");
