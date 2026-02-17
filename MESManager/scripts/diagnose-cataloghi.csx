#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Data.SqlClient, 5.1.5"

using Microsoft.Data.SqlClient;
using System;
using System.Data;

// Script diagnostico per verificare conteggi cataloghi nel database

var connectionString = "Server=localhost\\SQLEXPRESS01;Database=MESManager_Dev;Trusted_Connection=True;TrustServerCertificate=True;";

Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║  DIAGNOSTICA CATALOGHI - MESManager                      ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.WriteLine();

try
{
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    Console.WriteLine($"✅ Connesso a: {conn.Database} su {conn.DataSource}");
    Console.WriteLine();
    
    // Query conteggi
    var queries = new Dictionary<string, string>
    {
        { "Anime", "SELECT COUNT(*) FROM Anime" },
        { "Operatori", "SELECT COUNT(*) FROM Operatori" },
        { "Macchine", "SELECT COUNT(*) FROM Macchine" },
        { "Articoli", "SELECT COUNT(*) FROM Articoli" },
        { "Ricette", "SELECT COUNT(*) FROM Ricette" },
        { "Clienti", "SELECT COUNT(*) FROM Clienti" },
        { "Commesse", "SELECT COUNT(*) FROM Commesse" }
    };
    
    Console.WriteLine("📊 CONTEGGI TABELLE:");
    Console.WriteLine("─────────────────────────────────────────────────────────");
    
    foreach (var (tabella, query) in queries)
    {
        using var cmd = new SqlCommand(query, conn);
        var count = (int)cmd.ExecuteScalar();
        var status = count > 0 ? "✅" : "❌";
        Console.WriteLine($"{status} {tabella,-15} : {count,6} record");
    }
    
    Console.WriteLine();
    Console.WriteLine("🔍 VERIFICA DUPLICATI MACCHINE:");
    Console.WriteLine("─────────────────────────────────────────────────────────");
    
    var checkDuplicates = @"
        SELECT Codice, Nome, COUNT(*) as Conteggio 
        FROM Macchine 
        GROUP BY Codice, Nome 
        HAVING COUNT(*) > 1";
    
    using (var cmd = new SqlCommand(checkDuplicates, conn))
    using (var reader = cmd.ExecuteReader())
    {
        var hasDuplicates = false;
        while (reader.Read())
        {
            hasDuplicates = true;
            Console.WriteLine($"⚠️  Duplicato: {reader["Codice"]} - {reader["Nome"]} ({reader["Conteggio"]} volte)");
        }
        
        if (!hasDuplicates)
        {
            Console.WriteLine("✅ Nessun duplicato trovato");
        }
    }
    
    Console.WriteLine();
    Console.WriteLine("🔍 SAMPLE ANIME (primi 5):");
    Console.WriteLine("─────────────────────────────────────────────────────────");
    
    var sampleAnime = "SELECT TOP 5 Id, CodiceArticolo, DescrizioneArticolo FROM Anime ORDER BY Id";
    using (var cmd = new SqlCommand(sampleAnime, conn))
    using (var reader = cmd.ExecuteReader())
    {
        while (reader.Read())
        {
            Console.WriteLine($"  {reader["Id"],5} | {reader["CodiceArticolo"],-15} | {reader["DescrizioneArticolo"]}");
        }
    }
    
    Console.WriteLine();
    Console.WriteLine("🔍 SAMPLE OPERATORI:");
    Console.WriteLine("─────────────────────────────────────────────────────────");
    
    var sampleOp = "SELECT Id, NumeroOperatore, Nome, Cognome, Attivo FROM Operatori ORDER BY NumeroOperatore";
    using (var cmd = new SqlCommand(sampleOp, conn))
    using (var reader = cmd.ExecuteReader())
    {
        if (reader.HasRows)
        {
            while (reader.Read())
            {
                var attivo = (bool)reader["Attivo"] ? "✅" : "❌";
                Console.WriteLine($"  {attivo} Num: {reader["NumeroOperatore"],3} | {reader["Nome"]} {reader["Cognome"]}");
            }
        }
        else
        {
            Console.WriteLine("  ⚠️  Nessun operatore presente!");
        }
    }
    
    Console.WriteLine();
    Console.WriteLine("🔍 MACCHINE:");
    Console.WriteLine("─────────────────────────────────────────────────────────");
    
    var sampleMac = "SELECT Id, Codice, Nome, AttivaInGantt FROM Macchine ORDER BY Codice";
    using (var cmd = new SqlCommand(sampleMac, conn))
    using (var reader = cmd.ExecuteReader())
    {
        if (reader.HasRows)
        {
            while (reader.Read())
            {
                var attiva = (bool)reader["AttivaInGantt"] ? "✅" : "❌";
                Console.WriteLine($"  {attiva} {reader["Codice"],-10} | {reader["Nome"]}");
            }
        }
        else
        {
            Console.WriteLine("  ⚠️  Nessuna macchina presente!");
        }
    }
    
    Console.WriteLine();
    Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
    Console.WriteLine("║  DIAGNOSTICA COMPLETATA                                  ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine($"❌ ERRORE: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine(ex.StackTrace);
}
