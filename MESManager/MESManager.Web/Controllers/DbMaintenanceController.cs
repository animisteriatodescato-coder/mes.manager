using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MESManager.Infrastructure.Data;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DbMaintenanceController : ControllerBase
{
    private readonly MesManagerDbContext _context;
    private readonly ILogger<DbMaintenanceController> _logger;

    public DbMaintenanceController(MesManagerDbContext context, ILogger<DbMaintenanceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("ensure-festivi-table")]
    public async Task<IActionResult> EnsureFestiviTable()
    {
        try
        {
            // Prima creiamo la tabella (se non esiste) e gli indici
            var createTableSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Festivi')
BEGIN
    CREATE TABLE [dbo].[Festivi](
        [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
        [Data] [date] NOT NULL,
        [Descrizione] [nvarchar](200) NOT NULL,
        [Ricorrente] [bit] NOT NULL,
        [Anno] [int] NULL,
        [DataCreazione] [datetime2](7) NOT NULL DEFAULT GETUTCDATE()
    );
    
    CREATE INDEX IX_Festivi_Data ON [dbo].[Festivi]([Data]);
    CREATE INDEX IX_Festivi_Ricorrente ON [dbo].[Festivi]([Ricorrente]);
END";
            
            await _context.Database.ExecuteSqlRawAsync(createTableSql);
            
            // Verifica usando ExecuteSqlRawAsync con output parameter o contando le tabelle
            var checkSql = "SELECT COUNT(*) FROM sys.tables WHERE name = 'Festivi'";
            using var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = checkSql;
            var result = await command.ExecuteScalarAsync();
            var exists = Convert.ToInt32(result) > 0;
            
            if (exists)
            {
                _logger.LogInformation("✅ Tabella Festivi creata/verificata con successo");
                return Ok(new { success = true, message = "Tabella Festivi presente" });
            }
            else
            {
                _logger.LogError("❌ Tabella Festivi non è stata creata");
                return BadRequest(new { success = false, message = "Errore creazione tabella Festivi" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante creazione tabella Festivi");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
