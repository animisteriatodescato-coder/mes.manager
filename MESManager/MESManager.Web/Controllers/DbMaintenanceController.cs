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
            var sql = @"
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
END";
            
            await _context.Database.ExecuteSqlRawAsync(sql);
            
            // Verifica
            var checkSql = "SELECT CASE WHEN EXISTS(SELECT * FROM sys.tables WHERE name = 'Festivi') THEN 1 ELSE 0 END AS TableExists";
            var exists = await _context.Database.SqlQueryRaw<int>(checkSql).FirstOrDefaultAsync();
            
            if (exists == 1)
            {
                _logger.LogInformation("Tabella Festivi creata/verificata con successo");
                return Ok(new { success = true, message = "Tabella Festivi presente" });
            }
            else
            {
                _logger.LogError("Tabella Festivi non è stata creata");
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
