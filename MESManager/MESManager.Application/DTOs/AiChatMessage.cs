namespace MESManager.Application.DTOs;

/// <summary>Messaggio della chat AI — stato locale UI, non persistito su DB.</summary>
public class AiChatMessage
{
    public string Role      { get; set; } = "user"; // "user" | "assistant"
    public string Content   { get; set; } = string.Empty;
    public List<AiChatAttachment> Attachments { get; set; } = [];
    public bool   IsError   { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>Allegato immagine per l'assistente AI. Non persistito su DB.</summary>
public class AiChatAttachment
{
    public string FileName    { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Base64Data  { get; set; } = string.Empty;

    public string DataUrl => $"data:{ContentType};base64,{Base64Data}";
}
