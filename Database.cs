using MySqlConnector;

static class Database
{
    // Lösenordet läses från en miljövariabel, så att det inte hamnar i källkoden
    private static readonly string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=holidaymaker;User ID=root;" +
        $"Password={Environment.GetEnvironmentVariable("HOLIDAYMAKER_DB_PASSWORD")};";

    // Skapar och öppnar en ny anslutning. Används med "using var conn = Database.OpenConnection();"
    public static MySqlConnection OpenConnection()
    {
        var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
}
