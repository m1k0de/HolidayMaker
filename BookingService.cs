using MySqlConnector;

static class BookingService
{
    public static void Create(int guestId, int roomId, DateTime checkIn, DateTime checkOut, int numberOfGuests)
    {
        using var connection = Database.OpenConnection();

        // --- 1. Hämta rummets pris och kapacitet ---
        decimal pricePerNight;
        int capacity;

        using (var roomCommand = new MySqlCommand(
            "SELECT price_per_night, capacity FROM rooms WHERE id = @roomId", connection))
        {
            roomCommand.Parameters.AddWithValue("@roomId", roomId);
            using var reader = roomCommand.ExecuteReader();
            if (!reader.Read())
            {
                Console.WriteLine("Det finns inget rum med det id:t.");
                return;
            }
            pricePerNight = reader.GetDecimal("price_per_night");
            capacity      = reader.GetInt32("capacity");
        }

        // --- 2. Kontrollera kapaciteten ---
        if (numberOfGuests > capacity)
        {
            Console.WriteLine($"Rummet har bara plats för {capacity} personer.");
            return;
        }

        // --- 3. Kontrollera tillgängligheten IGEN precis innan vi sparar ---
        string overlapSql = @"
            SELECT COUNT(*) FROM bookings
            WHERE rooms_id  = @roomId
              AND check_in  < @checkOut
              AND check_out > @checkIn";

        using (var overlapCommand = new MySqlCommand(overlapSql, connection))
        {
            overlapCommand.Parameters.AddWithValue("@roomId", roomId);
            overlapCommand.Parameters.AddWithValue("@checkIn", checkIn);
            overlapCommand.Parameters.AddWithValue("@checkOut", checkOut);

            long overlapping = Convert.ToInt64(overlapCommand.ExecuteScalar());
            if (overlapping > 0)
            {
                Console.WriteLine("Rummet är tyvärr redan bokat under den perioden.");
                return;
            }
        }

        // --- 4. Spara bokningen ---
        string insertSql = @"
            INSERT INTO bookings (guests_id, rooms_id, number_of_guests, check_in, check_out)
            VALUES (@guestId, @roomId, @numberOfGuests, @checkIn, @checkOut)";

        using var insertCommand = new MySqlCommand(insertSql, connection);
        insertCommand.Parameters.AddWithValue("@guestId", guestId);
        insertCommand.Parameters.AddWithValue("@roomId", roomId);
        insertCommand.Parameters.AddWithValue("@numberOfGuests", numberOfGuests);
        insertCommand.Parameters.AddWithValue("@checkIn", checkIn);
        insertCommand.Parameters.AddWithValue("@checkOut", checkOut);
        insertCommand.ExecuteNonQuery();
        long bookingId = insertCommand.LastInsertedId;

        // --- 5. Bekräftelse med totalpris ---
        int nights = (checkOut - checkIn).Days;
        decimal total = nights * pricePerNight;

        Console.WriteLine();
        Console.WriteLine("=== Bokningsbekräftelse ===");
        Console.WriteLine($"Bokningsnummer: {bookingId}");
        Console.WriteLine($"Period:         {checkIn:yyyy-MM-dd} – {checkOut:yyyy-MM-dd} ({nights} nätter)");
        Console.WriteLine($"Antal personer: {numberOfGuests}");
        Console.WriteLine($"Totalpris:      {nights} × {pricePerNight:0} kr = {total:0} kr");
    }

    // Visar alla bokningar för en kund. Returnerar bokningsnumren (används vid avbokning).
    public static List<int> ShowForGuest(int guestId)
    {
        using var connection = Database.OpenConnection();

        string sql = @"
            SELECT b.id, a.name, r.room_number, b.check_in, b.check_out, b.number_of_guests,
                   DATEDIFF(b.check_out, b.check_in) * r.price_per_night AS total_price
            FROM bookings b
            JOIN rooms r          ON r.id = b.rooms_id
            JOIN accommodations a ON a.id = r.accommodation_id
            WHERE b.guests_id = @guestId
            ORDER BY b.check_in";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@guestId", guestId);

        using var reader = command.ExecuteReader();
        var bookingIds = new List<int>();

        Console.WriteLine();
        Console.WriteLine($"{"BokNr",-6} {"Boende",-22} {"Rum",-13} {"Incheckning",-12} {"Utcheckning",-12} {"Pers",-5} Totalpris");

        while (reader.Read())
        {
            bookingIds.Add(reader.GetInt32("id"));
            Console.WriteLine(
                $"{reader.GetInt32("id"),-6} {reader.GetString("name"),-22} {reader.GetString("room_number"),-13} " +
                $"{reader.GetDateTime("check_in"):yyyy-MM-dd}   {reader.GetDateTime("check_out"):yyyy-MM-dd}   " +
                $"{reader.GetInt32("number_of_guests"),-5} {reader.GetDecimal("total_price"):0} kr");
        }

        if (bookingIds.Count == 0)
            Console.WriteLine("Kunden har inga bokningar.");

        return bookingIds;
    }

    // Visar kundens bokningar och tar bort den bokning operatören väljer.
    public static void Cancel(int guestId)
    {
        List<int> bookingIds = ShowForGuest(guestId);
        if (bookingIds.Count == 0)
            return;

        int bookingId = Input.ReadInt("Ange bokningsnummer att avboka (0 = avbryt): ");
        if (bookingId == 0)
            return;
        if (!bookingIds.Contains(bookingId))
        {
            Console.WriteLine("Välj ett bokningsnummer från listan ovan.");
            return;
        }
        if (!Input.ReadYesNo($"Vill du verkligen avboka bokning {bookingId}? (j/n): "))
        {
            Console.WriteLine("Avbokningen avbröts.");
            return;
        }

        using var connection = Database.OpenConnection();

        // guests_id i WHERE: man kan bara avboka den valda kundens egna bokningar
        string sql = "DELETE FROM bookings WHERE id = @bookingId AND guests_id = @guestId";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@bookingId", bookingId);
        command.Parameters.AddWithValue("@guestId", guestId);

        int rowsDeleted = command.ExecuteNonQuery();
        Console.WriteLine(rowsDeleted == 1
            ? $"Bokning {bookingId} är avbokad. Rummet är nu ledigt för perioden."
            : "Bokningen kunde inte hittas.");
    }

    // Ändrar antal personer i en av kundens bokningar. Nya antalet måste rymmas i rummet.
    public static void ChangeNumberOfGuests(int guestId)
    {
        List<int> bookingIds = ShowForGuest(guestId);
        if (bookingIds.Count == 0)
            return;

        int bookingId = Input.ReadInt("Ange bokningsnummer att ändra (0 = avbryt): ");
        if (bookingId == 0)
            return;
        if (!bookingIds.Contains(bookingId))
        {
            Console.WriteLine("Välj ett bokningsnummer från listan ovan.");
            return;
        }

        int newCount = Input.ReadInt("Nytt antal personer: ");
        if (newCount < 1)
        {
            Console.WriteLine("Antal personer måste vara minst 1.");
            return;
        }

        using var connection = Database.OpenConnection();

        // --- 1. Hämta kapaciteten för rummet som bokningen gäller ---
        string capacitySql = @"
            SELECT r.capacity
            FROM bookings b
            JOIN rooms r ON r.id = b.rooms_id
            WHERE b.id = @bookingId";

        int capacity;
        using (var capacityCommand = new MySqlCommand(capacitySql, connection))
        {
            capacityCommand.Parameters.AddWithValue("@bookingId", bookingId);
            capacity = Convert.ToInt32(capacityCommand.ExecuteScalar());
        }

        if (newCount > capacity)
        {
            Console.WriteLine($"Rummet har bara plats för {capacity} personer. Ingen ändring gjordes.");
            return;
        }

        // --- 2. Uppdatera bokningen ---
        string updateSql = @"
            UPDATE bookings
            SET number_of_guests = @newCount
            WHERE id = @bookingId AND guests_id = @guestId";

        using var updateCommand = new MySqlCommand(updateSql, connection);
        updateCommand.Parameters.AddWithValue("@newCount", newCount);
        updateCommand.Parameters.AddWithValue("@bookingId", bookingId);
        updateCommand.Parameters.AddWithValue("@guestId", guestId);

        int rowsUpdated = updateCommand.ExecuteNonQuery();
        Console.WriteLine(rowsUpdated == 1
            ? $"Bokning {bookingId} gäller nu {newCount} personer."
            : "Bokningen kunde inte hittas.");
    }

}
