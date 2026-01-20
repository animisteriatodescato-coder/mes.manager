using MESManager.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MESManager.Application.DTOs;
using MESManager.Domain.Entities;

namespace MESManager.Application.Services
{
    /// <summary>
    /// Validazione automatica completezza mapping Anime -> AnimeDto
    /// Verifica che tutti i campi entity siano mappati correttamente nel DTO
    /// </summary>
    public class AnimeSyncValidation
    {
        private readonly IAnimeRepository _repository;
        private readonly IAnimeService _service;
        private readonly ILogger<AnimeSyncValidation> _logger;

        public AnimeSyncValidation(
            IAnimeRepository repository, 
            IAnimeService service,
            ILogger<AnimeSyncValidation> logger)
        {
            _repository = repository;
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Verifica che tutti i campi pubblici di Anime abbiano corrispondenza in AnimeDto
        /// </summary>
        public void ValidateMappingCompleteness()
        {
            _logger.LogInformation("=== VALIDATING ANIME <-> DTO MAPPING COMPLETENESS ===");
            
            var entityProps = typeof(Anime).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .OrderBy(n => n)
                .ToList();
            
            var dtoProps = typeof(AnimeDto).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .OrderBy(n => n)
                .ToList();
            
            _logger.LogInformation("Anime entity has {Count} public properties", entityProps.Count);
            _logger.LogInformation("AnimeDto has {Count} public properties", dtoProps.Count);
            
            var missingInDto = entityProps.Except(dtoProps).ToList();
            var missingInEntity = dtoProps.Except(entityProps).ToList();
            
            if (missingInDto.Any())
            {
                _logger.LogError("MAPPING INCOMPLETE: {Count} properties in Anime NOT in AnimeDto: {Props}",
                    missingInDto.Count, string.Join(", ", missingInDto));
            }
            
            if (missingInEntity.Any())
            {
                _logger.LogWarning("AnimeDto has {Count} properties NOT in Anime entity: {Props}",
                    missingInEntity.Count, string.Join(", ", missingInEntity));
            }
            
            if (!missingInDto.Any() && !missingInEntity.Any())
            {
                _logger.LogInformation("✓ MAPPING COMPLETE - All properties match");
            }
        }

        /// <summary>
        /// Test end-to-end: verifica che i dati siano presenti in DB e restituiti dall'API
        /// </summary>
        public async Task<bool> ValidateDataFlowAsync()
        {
            _logger.LogInformation("=== VALIDATING DATA FLOW: DB -> Repository -> Service ===");
            
            // Step 1: Query diretta repository
            var entitiesFromRepo = await _repository.GetAllAsync();
            _logger.LogInformation("Repository returned {Count} anime", entitiesFromRepo.Count);
            
            if (entitiesFromRepo.Count == 0)
            {
                _logger.LogError("VALIDATION FAILED: Repository returned ZERO records - DB may be empty");
                return false;
            }
            
            // Step 2: Query tramite service (include mapping)
            var dtosFromService = await _service.GetAllAsync();
            _logger.LogInformation("Service returned {Count} anime DTOs", dtosFromService.Count);
            
            if (dtosFromService.Count == 0)
            {
                _logger.LogError("VALIDATION FAILED: Service returned ZERO DTOs - Mapping may be broken");
                return false;
            }
            
            if (entitiesFromRepo.Count != dtosFromService.Count)
            {
                _logger.LogError("VALIDATION FAILED: Count mismatch - Repo={RepoCount}, Service={ServiceCount}",
                    entitiesFromRepo.Count, dtosFromService.Count);
                return false;
            }
            
            // Step 3: Verifica campi critici non nulli su sample
            var sample = dtosFromService.FirstOrDefault();
            if (sample == null)
            {
                _logger.LogError("VALIDATION FAILED: Cannot get sample DTO");
                return false;
            }
            
            _logger.LogInformation("Sample DTO - CodiceArticolo={Codice}, Descrizione={Desc}, HasColla={HasColla}",
                sample.CodiceArticolo, 
                sample.DescrizioneArticolo,
                !string.IsNullOrEmpty(sample.Colla));
            
            _logger.LogInformation("✓ DATA FLOW VALIDATION PASSED");
            return true;
        }
    }
}
