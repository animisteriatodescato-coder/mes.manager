using Microsoft.Data.SqlClient;

Console.WriteLine("Testing connection to Mago database...");

var connectionString = "Data Source=192.168.1.72\\SQLEXPRESS01;Initial Catalog=TODESCATO_NET;User Id=Gantt;Password=Gantt2019;TrustServerCertificate=True;Encrypt=False;";

try
{
    await using var conn = new SqlConnection(connectionString);
    Console.WriteLine("Opening connection...");
    await conn.OpenAsync();
    
    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT @@SERVERNAME AS Server, COUNT(*) AS TotalCommesse FROM MA_SaleOrd";
    
    Console.WriteLine("Executing query...");
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"Connected to: {reader["Server"]}");
        Console.WriteLine($"Total commesse in Mago: {reader["TotalCommesse"]}");
    }
    
    Console.WriteLine("Connection successful!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
}
