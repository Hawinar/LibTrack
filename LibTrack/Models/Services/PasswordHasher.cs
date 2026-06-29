using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace LibTrack.Services;

public sealed class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    // Параметры Argon2id.
    private const int MemorySizeKiB = 19 * 1024;
    private const int Iterations = 2;
    private const int DegreeOfParallelism = 1;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        try
        {
            byte[] hash = GetHash(
                passwordBytes,
                salt,
                MemorySizeKiB,
                Iterations,
                DegreeOfParallelism,
                HashSize);

            // argon2id$версия$память$итерации$параллелизм$соль$хэш
            return string.Join(
                '$',
                "argon2id",
                "19",
                MemorySizeKiB,
                Iterations,
                DegreeOfParallelism,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    public bool VerifyPassword(string password, string storedPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(storedPasswordHash))
        {
            return false;
        }

        string[] parts = storedPasswordHash.Split('$');

        if (parts.Length != 7 ||
            parts[0] != "argon2id" ||
            parts[1] != "19")
        {
            return false;
        }

        if (!int.TryParse(parts[2], out int memorySizeKiB) ||
            !int.TryParse(parts[3], out int iterations) ||
            !int.TryParse(parts[4], out int degreeOfParallelism))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(parts[5]);
            expectedHash = Convert.FromBase64String(parts[6]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        try
        {
            byte[] actualHash = GetHash(
                passwordBytes,
                salt,
                memorySizeKiB,
                iterations,
                degreeOfParallelism,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private static byte[] GetHash(
        byte[] passwordBytes,
        byte[] salt,
        int memorySizeKiB,
        int iterations,
        int degreeOfParallelism,
        int hashSize)
    {
        using var argon2 = new Argon2id(passwordBytes)
        {
            Salt = salt,
            MemorySize = memorySizeKiB,
            Iterations = iterations,
            DegreeOfParallelism = degreeOfParallelism
        };

        return argon2.GetBytes(hashSize);
    }
}