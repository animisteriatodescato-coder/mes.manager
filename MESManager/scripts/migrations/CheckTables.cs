using System;
using System.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connectionString = "Server=192.168.1.230\\SQLEXPRESS01;Database=MESManager_Prod;User Id=FAB;Password=password.123;TrustServerCertificate=True;";
        
        try
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                Console.WriteLine("Connessione riuscita!");
                
                // Verifica tabelle esistenti
                string checkQuery = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME IN ('ArticoliRicetta', 'AllegatiGantt', 'tbArticoliGantt')
                    ORDER BY TABLE_NAME";
                    
                using (var cmd = new SqlCommand(checkQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\nTabelle trovate:");
                    bool found = false;
                    while (reader.Read())
                    {
                        Console.WriteLine($"  - {reader.GetString(0)}");
                        found = true;
                    }
                    if (!found)
                    {
                        Console.WriteLine("  (nessuna tabella migrata trovata)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore: {ex.Message}");
        }
    }
}
