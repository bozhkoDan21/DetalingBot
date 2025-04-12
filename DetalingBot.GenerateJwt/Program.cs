using System.Security.Cryptography;

public class Program
{
    public static void Main()
    {
        // Генерация 256-битного ключа (32 байта)
        byte[] keyBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }

        string base64Key = Convert.ToBase64String(keyBytes);
        Console.WriteLine("Сгенерированный JWT ключ:");
        Console.WriteLine(base64Key);
    }
}