using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESManager.Domain.Constants;
using MESManager.Web.Services;

namespace MESManager.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Temporaneamente disabilitato per sviluppo - riabilitare in produzione
    public class TabelleController : ControllerBase
    {
        private readonly ITabelleService _tabelleService;

        public TabelleController(ITabelleService tabelleService)
        {
            _tabelleService = tabelleService;
        }

        // ─── GET ────────────────────────────────────────────────────────────

        [HttpGet("colla")]
        public ActionResult<List<LookupItem>> GetColla() =>
            Ok(_tabelleService.GetCollaList());

        [HttpGet("vernice")]
        public ActionResult<List<LookupItem>> GetVernice() =>
            Ok(_tabelleService.GetVerniceList());

        [HttpGet("sabbia")]
        public ActionResult<List<LookupItem>> GetSabbia() =>
            Ok(_tabelleService.GetSabbiaList());

        [HttpGet("imballo")]
        public ActionResult<List<LookupItem>> GetImballo() =>
            Ok(_tabelleService.GetImballoList());
        [HttpGet("tipologia-nc")]
        public ActionResult<List<LookupItem>> GetTipologiaNc() =>
            Ok(_tabelleService.GetTipologiaNcList());
        // ─── POST (salvataggio) ─────────────────────────────────────────────

        [HttpPost("colla")]
        public async Task<IActionResult> SalvaColla([FromBody] List<LookupItem> items)
        {
            if (items == null) return BadRequest("Payload vuoto");
            await _tabelleService.SalvaCollaAsync(items);
            return Ok();
        }

        [HttpPost("vernice")]
        public async Task<IActionResult> SalvaVernice([FromBody] List<LookupItem> items)
        {
            if (items == null) return BadRequest("Payload vuoto");
            await _tabelleService.SalvaVerniceAsync(items);
            return Ok();
        }

        [HttpPost("sabbia")]
        public async Task<IActionResult> SalvaSabbia([FromBody] List<LookupItem> items)
        {
            if (items == null) return BadRequest("Payload vuoto");
            await _tabelleService.SalvaSabbiaAsync(items);
            return Ok();
        }

        [HttpPost("imballo")]
        public async Task<IActionResult> SalvaImballo([FromBody] List<LookupItem> items)
        {
            if (items == null) return BadRequest("Payload vuoto");
            await _tabelleService.SalvaImballoAsync(items);
            return Ok();
        }

        [HttpPost("tipologia-nc")]
        public async Task<IActionResult> SalvaTipologiaNc([FromBody] List<LookupItem> items)
        {
            if (items == null) return BadRequest("Payload vuoto");
            await _tabelleService.SalvaTipologiaNcAsync(items);
            return Ok();
        }
    }
}
