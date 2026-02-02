namespace MESManager.Application.DTOs;

/// <summary>
/// DTO per listino prezzi (lista)
/// </summary>
public class PriceListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string DisplayName => $"{Name} (v{Version})";
    public string? Description { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? Source { get; set; }
    public bool IsDefault { get; set; }
    public bool IsArchived { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>
/// DTO per creazione listino
/// </summary>
public class PriceListCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? Description { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>
/// Dropdown item per selezione listino
/// </summary>
public class PriceListSelectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string DisplayName => $"{Name} v{Version}";
    public bool IsDefault { get; set; }
    public int ItemCount { get; set; }
}
