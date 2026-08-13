using System.Text.Json;
using IpScopePro.Models;

namespace IpScopePro.Services;

public class DataPersistenceService
{
    private readonly EncryptionService _encryption;

    public DataPersistenceService(EncryptionService encryption)
    {
        _encryption = encryption;
    }

    public string ExportProbes(IEnumerable<Probe> probes) =>
        JsonSerializer.Serialize(probes, new JsonSerializerOptions { WriteIndented = true });

    public List<Probe>? ImportProbes(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<Probe>>(json);
        }
        catch { return null; }
    }

    public string ExportSettings(ApplicationOptions options) =>
        JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });

    public string ExportSettingsEncrypted(ApplicationOptions options, string password)
    {
        var json = ExportSettings(options);
        return _encryption.Encrypt(json, password);
    }

    public ApplicationOptions? ImportSettings(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ApplicationOptions>(json);
        }
        catch { return null; }
    }

    public ApplicationOptions? ImportSettingsEncrypted(string encryptedData, string password)
    {
        try
        {
            var json = _encryption.Decrypt(encryptedData, password);
            return ImportSettings(json);
        }
        catch { return null; }
    }
}
