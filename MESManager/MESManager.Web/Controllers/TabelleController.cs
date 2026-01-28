using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Domain.Constants;

namespace MESManager.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Temporaneamente disabilitato per sviluppo - riabilitare in produzione
    public class TabelleController : ControllerBase
    {
        /// <summary>
        /// Restituisce la lista delle opzioni Colla
        /// </summary>
        [HttpGet("colla")]
        public ActionResult<List<LookupItem>> GetColla()
        {
            return Ok(LookupTables.ToList(LookupTables.Colla));
        }

        /// <summary>
        /// Restituisce la lista delle opzioni Vernice
        /// </summary>
        [HttpGet("vernice")]
        public ActionResult<List<LookupItem>> GetVernice()
        {
            return Ok(LookupTables.ToList(LookupTables.Vernice));
        }

        /// <summary>
        /// Restituisce la lista delle opzioni Sabbia
        /// </summary>
        [HttpGet("sabbia")]
        public ActionResult<List<LookupItem>> GetSabbia()
        {
            return Ok(LookupTables.ToList(LookupTables.Sabbia));
        }

        /// <summary>
        /// Restituisce la lista delle opzioni Imballo
        /// </summary>
        [HttpGet("imballo")]
        public ActionResult<List<LookupItem>> GetImballo()
        {
            return Ok(LookupTables.ToList(LookupTables.Imballo));
        }
    }
}
