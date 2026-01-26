using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace MESManager.Web.Security;

/// <summary>
/// Helper per gestire file di configurazione criptati con DPAPI.
/// I file possono essere decriptati solo sulla stessa macchina e dallo stesso utente Windows.
/// </summary>
[SupportedOSPlatform("windows")]
public static class SecretsDecryptor
{
    /// <summary>
    /// Decripta un file .encrypted e restituisce il contenuto come stringa JSON.
    /// </summary>
    /// <param name="encryptedFilePath">Percorso del file .encrypted</param>
    /// <returns>Contenuto JSON decriptato, o null se il file non esiste</returns>
    public static string? DecryptSecretsFile(string encryptedFilePath)
    {
        if (!File.Exists(encryptedFilePath))
            return null;

        try
        {
            var encryptedBytes = File.ReadAllBytes(encryptedFilePath);
            var decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser
            );
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException(
                $"Impossibile decriptare {encryptedFilePath}. " +
                "Il file può essere decriptato solo dall'utente Windows che lo ha criptato. " +
                "Esegui 'protect-secrets.ps1 -Decrypt' per rigenerare il file in chiaro.");
        }
    }

    /// <summary>
    /// Decripta il file secrets e lo salva temporaneamente per la configurazione.
    /// Il file temporaneo viene eliminato dopo il caricamento.
    /// </summary>
    public static string? DecryptToTempFile(string encryptedFilePath)
    {
        var json = DecryptSecretsFile(encryptedFilePath);
        if (json == null) return null;

        var tempPath = Path.Combine(Path.GetTempPath(), $"mesmanager_secrets_{Guid.NewGuid()}.json");
        File.WriteAllText(tempPath, json);
        return tempPath;
    }

    /// <summary>
    /// Aggiunge la configurazione dai secrets criptati al builder.
    /// </summary>
    public static void AddEncryptedSecrets(this IConfigurationBuilder config, string encryptedFilePath)
    {
        var json = DecryptSecretsFile(encryptedFilePath);
        if (json == null) return;

        // Usa un MemoryStream per evitare di scrivere su disco
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        config.AddJsonStream(stream);
    }
}
