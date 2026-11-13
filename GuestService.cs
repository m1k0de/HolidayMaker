using MySqlConnector;

static class GuestService
{
    public static void Register()
    {
        // 1. Läs in uppgifterna från operatören
        string firstName = Input.ReadText("Förnamn: ");
        string lastName = Input.ReadText("Efternamn: ");
        string email = Input.ReadText("E-post: ");
        string phone = Input.ReadText("Telefon: ");

        // 2. Öppna en anslutning (stängs automatiskt med "using")
        using var connection = Database.OpenConnection();

        // 3. Sql med platshållare(@...) istället för de riktiga värdena
        string sql = @"
            INSERT INTO guests (first_name, last_name, email, phone)
            VALUES (@firstName, @lastName, @email, @phone)";

        using var command = new MySqlCommand(sql, connection);

        // 4. Kopplar varje platshållare till ett värde
        command.Parameters.AddWithValue("@firstName", firstName);
        command.Parameters.AddWithValue("@lastName", lastName);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@phone", phone);

        // 5. Kör INSERT och hämta id:t som databasen gav den nya kunden
        try
        {
            command.ExecuteNonQuery();
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            Console.WriteLine("Det finns redan en kund med den e-postadressen.");
            return;
        }

        long newId = command.LastInsertedId;

        Console.WriteLine($"Kunden {firstName} {lastName} registrerades med id {newId}.");
    }

    // Listar alla kunder och låter operatören välja en. Returnerar id, eller null om valet är ogiltigt.
    public static int? Select()
    {
        using var connection = Database.OpenConnection();

        // 1. Listar alla kunder
        string listSql = @"
            SELECT id, first_name, last_name, email
            FROM guests
            ORDER BY last_name, first_name";

        using (var listCommand = new MySqlCommand(listSql, connection))
        using (var reader = listCommand.ExecuteReader())
        {
            Console.WriteLine();
            Console.WriteLine("Id   Namn                          E-post");
            while (reader.Read())   // en varv per rad i resultatet
            {
                int id = reader.GetInt32("id");
                string name = $"{reader.GetString("first_name")} {reader.GetString("last_name")}";
                string email = reader.GetString("email");
                Console.WriteLine($"{id,-4} {name,-29} {email}");
            }
        }   // reader stängs här, så att anslutningen kan användas igen

        // 2. Låt operatören välja ett id
        int chosenId = Input.ReadInt("Ange kund-id: ");

        // 3. Kontrollera att kunden finns (parameteriserad fråga)
        string checkSql = "SELECT first_name, last_name FROM guests WHERE id = @id";
        using var checkCommand = new MySqlCommand(checkSql, connection);
        checkCommand.Parameters.AddWithValue("@id", chosenId);

        using var checkReader = checkCommand.ExecuteReader();
        if (!checkReader.Read())
        {
            Console.WriteLine("Ingen kund med det id:t.");
            return null;
        }

        Console.WriteLine($"Vald kund: {checkReader.GetString("first_name")} {checkReader.GetString("last_name")}");
        return chosenId;
    }
}
