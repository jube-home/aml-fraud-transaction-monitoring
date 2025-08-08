using System.Security.Cryptography;
using System.Text;
using Jube.Cryptography.Exceptions;

namespace Jube.Cryptography;

public class AesEncryption
{
    private readonly byte[] _iv;
    private readonly byte[] _key;
    private readonly byte[] _salt;

    public AesEncryption(string password, string salt)
    {
        using var keyDerivationFunction =
            new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 100_000, HashAlgorithmName.SHA256);
        _key = keyDerivationFunction.GetBytes(32); // 256-bit key
        _iv = keyDerivationFunction.GetBytes(16); // 128-bit IV
        _salt = Encoding.UTF8.GetBytes(salt);
    }

    public byte[] Encrypt(byte[] data)
    {
        try
        {
            var hmac = ComputeHmac(data);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using MemoryStream ms = new();
            ms.Write(hmac);

            using CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();

            return ms.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidEncryptionException(ex.Message);
        }
    }

    public byte[] Decrypt(byte[] encryptedData)
    {
        try
        {
            var hmac = ComputeHmac(encryptedData[..32]);
            var clippedEncryptedData = encryptedData[32..];

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using MemoryStream ms = new();
            using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(clippedEncryptedData, 0, clippedEncryptedData.Length);
            cs.FlushFinalBlock();

            var decryptedData = ms.ToArray();

            if (!VerifyHmac(ComputeHmac(decryptedData), hmac)) throw new InvalidHmacException();

            return ms.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidDecryptionException(ex.Message);
        }
    }

    private byte[] ComputeHmac(byte[] data)
    {
        using var hmac = new HMACSHA256(_salt);
        return hmac.ComputeHash(data);
    }

    private bool VerifyHmac(byte[] data, byte[] expectedHmac)
    {
        var actualHmac = ComputeHmac(data);
        return CryptographicOperations.FixedTimeEquals(actualHmac, expectedHmac);
    }
}