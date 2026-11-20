using Isopoh.Cryptography.Argon2;

if (args.Length == 2)
{
    // Modo verificación: dotnet run password hash
    var password = args[0];
    var hash = args[1];
    var isValid = Argon2.Verify(hash, password);
    Console.WriteLine($"Password: {password}");
    Console.WriteLine($"Hash: {hash}");
    Console.WriteLine($"Valid: {isValid}");
}
else if (args.Length == 1)
{
    // Modo generación puntual: dotnet run password
    Console.WriteLine(Argon2.Hash(args[0]));
}
else
{
    // Modo generación: hashes Argon2id para las credenciales seed del proyecto (README.md, Data/Seeds/*.cs)
    var passwords = new[]
    {
        "AdminPass123!",
        "UserPass123!",
        "ManagerPass123!",
        "InactivePass123!",
        "TestPass123!",
        "SystemNonInteractive_DoNotUseForLogin!", // cuenta 'system' es no interactiva, no se usa para login real
    };

    foreach (var password in passwords)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var hash = Argon2.Hash(password);
        sw.Stop();
        Console.WriteLine($"{password,-20} -> {hash} ({sw.ElapsedMilliseconds} ms)");
    }
}
