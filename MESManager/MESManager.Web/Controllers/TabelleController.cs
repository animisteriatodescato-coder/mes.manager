using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MESManager.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Richiede autenticazione per tutti gli endpoint
    public class TabelleController : ControllerBase
    {
        /// <summary>
        /// Restituisce la lista delle opzioni Colla
        /// </summary>
        [HttpGet("colla")]
        public ActionResult<List<TabellaItemDto>> GetColla()
        {
            var items = new List<TabellaItemDto>
            {
                new() { Codice = "-1", Descrizione = "BIANCA" },
                new() { Codice = "-2", Descrizione = "A CALDO" },
                new() { Codice = "-3", Descrizione = "ROSSA S.G" }
            };
            return Ok(items);
        }

        /// <summary>
        /// Restituisce la lista delle opzioni Vernice
        /// </summary>
        [HttpGet("vernice")]
        public ActionResult<List<TabellaItemDto>> GetVernice()
        {
            var items = new List<TabellaItemDto>
            {
                new() { Codice = "-1", Descrizione = "" },
                new() { Codice = "-2", Descrizione = "YELLOW COVER" },
                new() { Codice = "-3", Descrizione = "CASTING COVER ZR" },
                new() { Codice = "-4", Descrizione = "CASTING COVER RK" },
                new() { Codice = "-5", Descrizione = "CASTINGCOVER 2001" },
                new() { Codice = "-6", Descrizione = "ARCOPAL 9030" },
                new() { Codice = "-7", Descrizione = "HYDRO COVER 22 Z" },
                new() { Codice = "-8", Descrizione = "FGR 55" }
            };
            return Ok(items);
        }

        /// <summary>
        /// Restituisce la lista delle opzioni Sabbia
        /// </summary>
        [HttpGet("sabbia")]
        public ActionResult<List<TabellaItemDto>> GetSabbia()
        {
            var items = new List<TabellaItemDto>
            {
                new() { Codice = "", Descrizione = "(nessuna)" },
                new() { Codice = "310/60", Descrizione = "310/60" },
                new() { Codice = "33BD600X", Descrizione = "33BD600X" },
                new() { Codice = "360/10", Descrizione = "360/10" },
                new() { Codice = "44/10", Descrizione = "SCARANELLO" },
                new() { Codice = "C1/30D", Descrizione = "C1/30D" },
                new() { Codice = "CP35B", Descrizione = "CP35B" },
                new() { Codice = "NB30B", Descrizione = "NB30B" },
                new() { Codice = "OLIVINA", Descrizione = "OLIVINA" },
                new() { Codice = "UP30B", Descrizione = "UP30B" }
            };
            return Ok(items);
        }

        /// <summary>
        /// Restituisce la lista delle opzioni Imballo
        /// </summary>
        [HttpGet("imballo")]
        public ActionResult<List<TabellaItemDto>> GetImballo()
        {
            var items = new List<TabellaItemDto>
            {
                new() { Codice = "-1", Descrizione = "CASSA GRANDE" },
                new() { Codice = "-2", Descrizione = "CASSA PICCOLA" },
                new() { Codice = "-3", Descrizione = "CASSA LUNGA" },
                new() { Codice = "-4", Descrizione = "PIANALE EURO" },
                new() { Codice = "-5", Descrizione = "PIANALE QUADRATO" },
                new() { Codice = "-6", Descrizione = "CARRELLI A PIANI" },
                new() { Codice = "-7", Descrizione = "CARRELLI GRANDI" },
                new() { Codice = "-8", Descrizione = "SCATOLE" }
            };
            return Ok(items);
        }
    }

    public class TabellaItemDto
    {
        public string Codice { get; set; } = string.Empty;
        public string Descrizione { get; set; } = string.Empty;
    }
}
