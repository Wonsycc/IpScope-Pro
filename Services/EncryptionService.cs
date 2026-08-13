using System.Security.Cryptography;
using System.Text;

namespace IpScopePro.Services;

public class EncryptionService
{
    private const int KeySize = 256;
    private const int DeriveIterations = 10000;
    private const int SaltSize = 16;
    private const int IvSize = 16;

    public string Encrypt(string plainText, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var iv = RandomNumberGenerator.GetBytes(IvSize);
        var key = DeriveKey(password, salt);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[SaltSize + IvSize + cipherBytes.Length];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(iv, 0, result, SaltSize, IvSize);
        Buffer.BlockCopy(cipherBytes, 0, result, SaltSize + IvSize, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText, string password)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        var salt = new byte[SaltSize];
        var iv = new byte[IvSize];
        var cipherBytes = new byte[fullCipher.Length - SaltSize - IvSize];

        Buffer.BlockCopy(fullCipher, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(fullCipher, SaltSize, iv, 0, IvSize);
        Buffer.BlockCopy(fullCipher, SaltSize + IvSize, cipherBytes, 0, cipherBytes.Length);

        var key = DeriveKey(password, salt);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password, salt, DeriveIterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize / 8);
    }
}
