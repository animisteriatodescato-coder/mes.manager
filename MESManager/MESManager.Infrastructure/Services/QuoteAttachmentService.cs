using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio per gestione allegati preventivo.
/// Storage su filesystem NON pubblico con metadati in DB.
/// </summary>
public class QuoteAttachmentService : IQuoteAttachmentService
{
    private readonly MesManagerDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuoteAttachmentService> _logger;
    
    // Configurazione (da appsettings)
    private readonly string _basePath;
    private readonly long _maxFileSize;
    private readonly HashSet<string> _allowedExtensions;

    public QuoteAttachmentService(
        MesManagerDbContext context,
        IConfiguration configuration,
        ILogger<QuoteAttachmentService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        
        // Leggi configurazione
        _basePath = configuration["QuoteAttachments:BasePath"] 
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "QuoteAttachments");
        
        _maxFileSize = configuration.GetValue<long>("QuoteAttachments:MaxFileSizeMB", 25) * 1024 * 1024;
        
        var extensions = configuration.GetSection("QuoteAttachments:AllowedExtensions").Get<string[]>()
            ?? new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".dwg" };
        
        _allowedExtensions = new HashSet<string>(extensions.Select(e => e.ToLowerInvariant()));
        
        // Assicura che la directory base esista
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Creata directory allegati preventivi: {Path}", _basePath);
        }
    }

    public async Task<List<QuoteAttachmentDto>> GetByQuoteIdAsync(Guid quoteId)
    {
        return await _context.QuoteAttachments
            .Where(a => a.QuoteId == quoteId)
            .OrderBy(a => a.UploadedAt)
            .Select(a => new QuoteAttachmentDto
            {
                Id = a.Id,
                QuoteId = a.QuoteId,
                FileName = a.FileName,
                FileSize = a.FileSize,
                ContentType = a.ContentType,
                Description = a.Description,
                UploadedAt = a.UploadedAt,
                UploadedBy = a.UploadedBy
            })
            .ToListAsync();
    }

    public async Task<QuoteAttachmentUploadResult> UploadAsync(
        Guid quoteId,
        Stream fileStream,
        string fileName,
        string contentType,
        string? description = null,
        string? userId = null)
    {
        var result = new QuoteAttachmentUploadResult();
        
        try
        {
            // Verifica preventivo esiste
            var quoteExists = await _context.Quotes.AnyAsync(q => q.Id == quoteId);
            if (!quoteExists)
            {
                result.ErrorMessage = "Preventivo non trovato.";
                return result;
            }
            
            // Valida estensione
            if (!IsAllowedExtension(fileName))
            {
                result.ErrorMessage = $"Estensione file non consentita. Consentite: {string.Join(", ", _allowedExtensions)}";
                return result;
            }
            
            // Valida dimensione
            if (!IsAllowedSize(fileStream.Length))
            {
                result.ErrorMessage = $"File troppo grande. Massimo: {_maxFileSize / (1024 * 1024)} MB";
                return result;
            }
            
            // Sanitizza nome file
            var safeName = SanitizeFileName(fileName);
            
            // Genera percorso relativo: quotes/yyyy/MM/guid_filename.ext
            var now = DateTime.Now;
            var relativePath = Path.Combine(
                "quotes",
                now.Year.ToString(),
                now.Month.ToString("D2"),
                $"{Guid.NewGuid():N}_{safeName}");
            
            var fullPath = Path.Combine(_basePath, relativePath);
            
            // Crea directory se non esiste
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Calcola hash e salva file
            string contentHash;
            using (var sha256 = SHA256.Create())
            {
                // Reset stream
                fileStream.Position = 0;
                
                // Copia su file
                using var fileOutput = File.Create(fullPath);
                await fileStream.CopyToAsync(fileOutput);
                
                // Calcola hash
                fileStream.Position = 0;
                var hashBytes = await sha256.ComputeHashAsync(fileStream);
                contentHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
            
            // Salva metadati in DB
            var attachment = new QuoteAttachment
            {
                Id = Guid.NewGuid(),
                QuoteId = quoteId,
                FileName = safeName,
                RelativePath = relativePath,
                FileSize = fileStream.Length,
                ContentType = contentType,
                ContentHash = contentHash,
                Description = description,
                UploadedAt = DateTime.Now,
                UploadedBy = userId
            };
            
            _context.QuoteAttachments.Add(attachment);
            await _context.SaveChangesAsync();
            
            result.Success = true;
            result.Attachment = new QuoteAttachmentDto
            {
                Id = attachment.Id,
                QuoteId = attachment.QuoteId,
                FileName = attachment.FileName,
                FileSize = attachment.FileSize,
                ContentType = attachment.ContentType,
                Description = attachment.Description,
                UploadedAt = attachment.UploadedAt,
                UploadedBy = attachment.UploadedBy
            };
            
            _logger.LogInformation("Caricato allegato {FileName} per preventivo {QuoteId}", safeName, quoteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore upload allegato per preventivo {QuoteId}", quoteId);
            result.ErrorMessage = $"Errore durante l'upload: {ex.Message}";
        }
        
        return result;
    }

    public async Task<(Stream? stream, string? contentType, string? fileName)> DownloadAsync(Guid attachmentId)
    {
        var attachment = await _context.QuoteAttachments.FindAsync(attachmentId);
        
        if (attachment == null)
        {
            _logger.LogWarning("Allegato {Id} non trovato", attachmentId);
            return (null, null, null);
        }
        
        var fullPath = Path.Combine(_basePath, attachment.RelativePath);
        
        if (!File.Exists(fullPath))
        {
            _logger.LogError("File fisico non trovato: {Path}", fullPath);
            return (null, null, null);
        }
        
        // Prevenzione path traversal
        var resolvedPath = Path.GetFullPath(fullPath);
        var resolvedBase = Path.GetFullPath(_basePath);
        
        if (!resolvedPath.StartsWith(resolvedBase))
        {
            _logger.LogError("Tentativo di path traversal: {Path}", fullPath);
            return (null, null, null);
        }
        
        var stream = File.OpenRead(fullPath);
        
        _logger.LogDebug("Download allegato {Id}: {FileName}", attachmentId, attachment.FileName);
        
        return (stream, attachment.ContentType, attachment.FileName);
    }

    public async Task<bool> DeleteAsync(Guid attachmentId)
    {
        var attachment = await _context.QuoteAttachments.FindAsync(attachmentId);
        
        if (attachment == null) return false;
        
        // Elimina file fisico
        var fullPath = Path.Combine(_basePath, attachment.RelativePath);
        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
                _logger.LogInformation("Eliminato file fisico: {Path}", fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossibile eliminare file fisico: {Path}", fullPath);
            }
        }
        
        // Elimina record DB
        _context.QuoteAttachments.Remove(attachment);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Eliminato allegato {Id} ({FileName})", attachmentId, attachment.FileName);
        
        return true;
    }

    public bool IsAllowedExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return !string.IsNullOrEmpty(ext) && _allowedExtensions.Contains(ext);
    }

    public bool IsAllowedSize(long fileSize)
    {
        return fileSize > 0 && fileSize <= _maxFileSize;
    }

    /// <summary>
    /// Sanitizza nome file rimuovendo caratteri pericolosi
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "file";
        }
        
        // Prendi solo il nome file (ignora path)
        fileName = Path.GetFileName(fileName);
        
        // Rimuovi caratteri non validi
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
        
        // Rimuovi doppi underscore
        fileName = Regex.Replace(fileName, @"_{2,}", "_");
        
        // Limita lunghezza
        if (fileName.Length > 200)
        {
            var ext = Path.GetExtension(fileName);
            var name = Path.GetFileNameWithoutExtension(fileName);
            fileName = name.Substring(0, 200 - ext.Length) + ext;
        }
        
        return fileName;
    }
}
