using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;

namespace MESManager.Application.Services
{
    public class AnimeService : IAnimeService
    {
        private readonly IAnimeRepository _repo;
        public AnimeService(IAnimeRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<AnimeDto>> GetAllAsync()
        {
            var entities = await _repo.GetAllAsync();
            return entities.Select(x => MapToDto(x)).ToList();
        }

        public async Task<AnimeDto?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<AnimeDto> AddAsync(AnimeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CodiceArticolo))
                throw new ArgumentException("Codice Articolo obbligatorio");
            
            var entity = MapToEntity(dto);
            await _repo.AddAsync(entity);
            return MapToDto(entity);
        }

        public async Task<AnimeDto?> UpdateAsync(int id, AnimeDto dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return null;
            
            if (string.IsNullOrWhiteSpace(dto.CodiceArticolo))
                throw new ArgumentException("Codice Articolo obbligatorio");
            
            entity.CodiceArticolo = dto.CodiceArticolo;
            entity.DescrizioneArticolo = dto.DescrizioneArticolo;
            entity.DataModificaRecord = dto.DataModificaRecord;
            entity.UtenteModificaRecord = dto.UtenteModificaRecord;
            entity.UnitaMisura = dto.UnitaMisura;
            entity.Larghezza = dto.Larghezza;
            entity.Altezza = dto.Altezza;
            entity.Profondita = dto.Profondita;
            entity.Imballo = dto.Imballo;
            entity.Note = dto.Note;
            entity.Allegato = dto.Allegato;
            entity.Peso = dto.Peso;
            entity.Ubicazione = dto.Ubicazione;
            entity.Ciclo = dto.Ciclo;
            entity.CodiceCassa = dto.CodiceCassa;
            entity.CodiceAnime = dto.CodiceAnime;
            entity.IdArticolo = dto.IdArticolo;
            entity.MacchineSuDisponibili = dto.MacchineSuDisponibili;
            entity.TrasmettiTutto = dto.TrasmettiTutto;
            entity.Colla = dto.Colla;
            entity.Sabbia = dto.Sabbia;
            entity.Vernice = dto.Vernice;
            entity.Cliente = dto.Cliente;
            entity.TogliereSparo = dto.TogliereSparo;
            entity.QuantitaPiano = dto.QuantitaPiano;
            entity.NumeroPiani = dto.NumeroPiani;
            entity.Figure = dto.Figure;
            entity.Piastra = dto.Piastra;
            entity.Maschere = dto.Maschere;
            entity.Incollata = dto.Incollata;
            entity.Assemblata = dto.Assemblata;
            entity.ArmataL = dto.ArmataL;
            
            // Tracking modifiche locali
            entity.ModificatoLocalmente = dto.ModificatoLocalmente;
            entity.DataUltimaModificaLocale = dto.DataUltimaModificaLocale;
            entity.UtenteUltimaModificaLocale = dto.UtenteUltimaModificaLocale;
            
            await _repo.UpdateAsync(entity);
            return MapToDto(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repo.DeleteAsync(id);
        }

        private AnimeDto MapToDto(Anime entity)
        {
            // Mapping manuale - ATTENZIONE: aggiungere ogni nuovo campo a entity
            return new AnimeDto
            {
                Id = entity.Id,
                CodiceArticolo = entity.CodiceArticolo,
                DescrizioneArticolo = entity.DescrizioneArticolo,
                DataModificaRecord = entity.DataModificaRecord,
                UtenteModificaRecord = entity.UtenteModificaRecord,
                UnitaMisura = entity.UnitaMisura,
                Larghezza = entity.Larghezza,
                Altezza = entity.Altezza,
                Profondita = entity.Profondita,
                Imballo = entity.Imballo,
                Note = entity.Note,
                Allegato = entity.Allegato,
                Peso = entity.Peso,
                Ubicazione = entity.Ubicazione,
                Ciclo = entity.Ciclo,
                CodiceCassa = entity.CodiceCassa,
                CodiceAnime = entity.CodiceAnime,
                IdArticolo = entity.IdArticolo,
                MacchineSuDisponibili = entity.MacchineSuDisponibili,
                TrasmettiTutto = entity.TrasmettiTutto,
                Colla = entity.Colla,
                Sabbia = entity.Sabbia,
                Vernice = entity.Vernice,
                Cliente = entity.Cliente,
                TogliereSparo = entity.TogliereSparo,
                QuantitaPiano = entity.QuantitaPiano,
                NumeroPiani = entity.NumeroPiani,
                Figure = entity.Figure,
                Piastra = entity.Piastra,
                Maschere = entity.Maschere,
                Incollata = entity.Incollata,
                Assemblata = entity.Assemblata,
                ArmataL = entity.ArmataL,
                DataImportazione = entity.DataImportazione,
                ModificatoLocalmente = entity.ModificatoLocalmente,
                DataUltimaModificaLocale = entity.DataUltimaModificaLocale,
                UtenteUltimaModificaLocale = entity.UtenteUltimaModificaLocale
            };
        }

        private Anime MapToEntity(AnimeDto dto)
        {
            return new Anime
            {
                Id = dto.Id,
                CodiceArticolo = dto.CodiceArticolo,
                DescrizioneArticolo = dto.DescrizioneArticolo,
                DataModificaRecord = dto.DataModificaRecord,
                UtenteModificaRecord = dto.UtenteModificaRecord,
                UnitaMisura = dto.UnitaMisura,
                Larghezza = dto.Larghezza,
                Altezza = dto.Altezza,
                Profondita = dto.Profondita,
                Imballo = dto.Imballo,
                Note = dto.Note,
                Allegato = dto.Allegato,
                Peso = dto.Peso,
                Ubicazione = dto.Ubicazione,
                Ciclo = dto.Ciclo,
                CodiceCassa = dto.CodiceCassa,
                CodiceAnime = dto.CodiceAnime,
                IdArticolo = dto.IdArticolo,
                MacchineSuDisponibili = dto.MacchineSuDisponibili,
                TrasmettiTutto = dto.TrasmettiTutto,
                Colla = dto.Colla,
                Sabbia = dto.Sabbia,
                Vernice = dto.Vernice,
                Cliente = dto.Cliente,
                TogliereSparo = dto.TogliereSparo,
                QuantitaPiano = dto.QuantitaPiano,
                NumeroPiani = dto.NumeroPiani,
                Figure = dto.Figure,
                Piastra = dto.Piastra,
                Maschere = dto.Maschere,
                Incollata = dto.Incollata,
                Assemblata = dto.Assemblata,
                ArmataL = dto.ArmataL,
                DataImportazione = dto.DataImportazione,
                ModificatoLocalmente = dto.ModificatoLocalmente,
                DataUltimaModificaLocale = dto.DataUltimaModificaLocale,
                UtenteUltimaModificaLocale = dto.UtenteUltimaModificaLocale
            };
        }
    }
}
