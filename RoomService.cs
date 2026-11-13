using MySqlConnector;

static class RoomService
{
    public static void Search(int? guestId)
    {
        using var connection = Database.OpenConnection();

        // --- 1. Visa orterna med deras boenden och beskrivningar så operatören kan välja ---
        // LEFT JOIN gör att en ort visas även om den saknar boenden
        string citySql = @"
            SELECT l.id, l.city, a.name, a.rating, a.description
            FROM location l
            LEFT JOIN accommodations a ON a.city_id = l.id
            ORDER BY l.id, a.name";

        using (var cityCommand = new MySqlCommand(citySql, connection))
        using (var reader = cityCommand.ExecuteReader())
        {
            Console.WriteLine();
            Console.WriteLine("Orter:");
            int? lastCityId = null;
            while (reader.Read())
            {
                int cityIdInRow = reader.GetInt32("id");
                // En ort kan ha flera boenden, så ortnamnet skrivs bara ut första gången
                if (cityIdInRow != lastCityId)
                {
                    Console.WriteLine();
                    Console.WriteLine($"{cityIdInRow}. {reader.GetString("city")}");
                    lastCityId = cityIdInRow;
                }

                if (reader.IsDBNull(reader.GetOrdinal("name")))
                    continue; // orten har inga boenden

                string description = reader.IsDBNull(reader.GetOrdinal("description"))
                    ? "(ingen beskrivning)"
                    : reader.GetString("description");
                Console.WriteLine($"   - {reader.GetString("name")} (omdöme {reader.GetInt32("rating")}/5)");
                Console.WriteLine($"     {description}");
            }
            Console.WriteLine();
        }
        int cityId = Input.ReadInt("Välj ort (id): ");

        // --- 2. Obligatoriska uppgifter ---
        DateTime checkIn  = Input.ReadDate("Incheckning (ÅÅÅÅ-MM-DD): ");
        DateTime checkOut = Input.ReadDate("Utcheckning (ÅÅÅÅ-MM-DD): ");
        if (checkOut <= checkIn)
        {
            Console.WriteLine("Utcheckning måste vara efter incheckning.");
            return;
        }
        int guests = Input.ReadInt("Antal personer: ");
        if (guests < 1)
        {
            Console.WriteLine("Antal personer måste vara minst 1.");
            return;
        }

        // --- 3. Valfria filter ---
        bool needPool       = Input.ReadYesNo("Kräv pool? (j/n): ");
        bool needRestaurant = Input.ReadYesNo("Kräv restaurang? (j/n): ");
        int? maxBeach = Input.ReadOptionalInt("Max avstånd till strand i meter (Enter = valfritt): ");
        int? maxCity  = Input.ReadOptionalInt("Max avstånd till centrum i meter (Enter = valfritt): ");

        // --- 4. Sortering ---
        int sort = Input.ReadInt("Sortera på 1) pris lägst först  2) omdöme högst först: ");

        // --- 5. Själva sökningen: SQL gör allt filtrerings- och sorteringsarbete ---
        string sql = @"
            SELECT r.id, a.name, l.city, r.room_number, r.capacity, r.price_per_night, a.rating
            FROM rooms r
            JOIN accommodations a ON a.id = r.accommodation_id
            JOIN location l       ON l.id = a.city_id
            WHERE a.city_id = @cityId
              AND r.capacity >= @guests
              AND (@needPool = 0       OR a.pool = 1)
              AND (@needRestaurant = 0 OR a.restaurant = 1)
              AND (@maxBeach IS NULL   OR a.beach_distance <= @maxBeach)
              AND (@maxCity  IS NULL   OR a.city_distance  <= @maxCity)
              AND NOT EXISTS (
                  SELECT 1 FROM bookings b
                  WHERE b.rooms_id = r.id
                    AND b.check_in  < @checkOut
                    AND b.check_out > @checkIn
              )
            ORDER BY
              CASE WHEN @sort = 2 THEN a.rating END DESC,
              r.price_per_night ASC";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@cityId", cityId);
        command.Parameters.AddWithValue("@guests", guests);
        command.Parameters.AddWithValue("@checkIn", checkIn);
        command.Parameters.AddWithValue("@checkOut", checkOut);
        command.Parameters.AddWithValue("@needPool", needPool);
        command.Parameters.AddWithValue("@needRestaurant", needRestaurant);
        // null i C# måste skickas som DBNull.Value för att bli NULL i SQL
        command.Parameters.AddWithValue("@maxBeach", (object?)maxBeach ?? DBNull.Value);
        command.Parameters.AddWithValue("@maxCity",  (object?)maxCity  ?? DBNull.Value);
        command.Parameters.AddWithValue("@sort", sort);

        // --- 6. Visa resultatet ---
        using var result = command.ExecuteReader();
        Console.WriteLine();
        Console.WriteLine($"{"RumId",-6} {"Boende",-22} {"Ort",-14} {"Rum",-10} {"Plats",-6} {"Pris/natt",-10} Omdöme");

        var foundRoomIds = new List<int>();
        while (result.Read())
        {
            foundRoomIds.Add(result.GetInt32("id"));
            Console.WriteLine(
                $"{result.GetInt32("id"),-6} {result.GetString("name"),-22} {result.GetString("city"),-14} " +
                $"{result.GetString("room_number"),-10} {result.GetInt32("capacity"),-6} " +
                $"{result.GetDecimal("price_per_night").ToString("0") + " kr",-10} {result.GetInt32("rating")}");
        }

        if (foundRoomIds.Count == 0)
        {
            Console.WriteLine("Inga lediga rum matchar sökningen.");
            return;
        }

        // --- 7. Boka ett av rummen ---
        if (guestId is null)
        {
            Console.WriteLine("Välj en kund (menyval 2) för att kunna boka.");
            return;
        }

        int roomId = Input.ReadInt("Ange RumId att boka (0 = avbryt): ");
        if (roomId == 0)
            return;
        if (!foundRoomIds.Contains(roomId))
        {
            Console.WriteLine("Välj ett RumId från listan ovan.");
            return;
        }

        BookingService.Create(guestId.Value, roomId, checkIn, checkOut, guests);

    }
}
