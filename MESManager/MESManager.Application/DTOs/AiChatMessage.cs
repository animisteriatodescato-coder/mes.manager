namespace MESManager.Application.DTOs;

/// <summary>Messaggio della chat AI — stato locale UI, non persistito su DB.</summary>
public class AiChatMessage
{
    public string Role      { get; set; } = "user"; // "user" | "assistant"
    public string Content   { get; set; } = string.Empty;
    public bool   IsError   { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
