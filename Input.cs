using System.Globalization;

static class Input
{
    // Frågar tills användaren skriver ett giltigt heltal
    public static int ReadInt(string prompt)
    {
        while (true)
        {
            Console.WriteLine(prompt);
            if (int.TryParse(Console.ReadLine(), out int value))
                return value;
            Console.WriteLine("Skriv ett heltal.");
        }
    }

    // Frågar tills användaren skriver något som inte är tomt
    public static string ReadText(string prompt)
    {
        while (true)
        {
            Console.WriteLine(prompt);
            string? input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(input))
                return input;
            Console.WriteLine("Fältet får inte vara tomt.");
        }
    }

    // Frågar tills användaren skriver ett datum i formatet ÅÅÅÅ-MM-DD
    public static DateTime ReadDate(string prompt)
    {
        while (true)
        {
            Console.WriteLine(prompt);
            if (DateTime.TryParseExact(Console.ReadLine(), "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                return date;
            Console.WriteLine("Skriv datumet som ÅÅÅÅ-MM-DD, t.ex. 2026-12-10.");
        }
    }

    // Frågar tills användaren svarar j eller n
    public static bool ReadYesNo(string prompt)
    {
        while (true)
        {
            Console.WriteLine(prompt);
            string? input = Console.ReadLine()?.Trim().ToLower();
            if (input == "j") return true;
            if (input == "n") return false;
            Console.WriteLine("Svara j eller n.");
        }
    }

    // Som ReadInt, men tomt svar (bara Enter) betyder "inget värde" och ger null
    public static int? ReadOptionalInt(string prompt)
    {
        while (true)
        {
            Console.WriteLine(prompt);
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) return null;
            if (int.TryParse(input, out int value)) return value;
            Console.WriteLine("Skriv ett heltal, eller tryck Enter för att hoppa över.");
        }
    }

}
