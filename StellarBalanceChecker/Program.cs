using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using StellarDotnetSdk.Requests;

// namespace StellarBalanceChecker;

Console.WriteLine("Введите адрес криптокошелька Stellar:");
string walletAddress = Console.ReadLine() ?? "";

if (string.IsNullOrEmpty(walletAddress))
{
    Console.WriteLine("Адрес криптокошелька не может быть пустым.");
    return;
}

try
{
    // Проверка корректности адреса
    try
    {
        var keyPair = KeyPair.FromAccountId(walletAddress);
    }
    catch (Exception)
    {
        Console.WriteLine("Введен некорректный адрес криптокошелька.");
        return;
    }

    // Подключение к основной сети Stellar
    var server = new Server("https://horizon.stellar.org");

    // Получение информации о счетах
    var account = await server.Accounts.Account(walletAddress);

    // Поиск баланса XLM
    foreach (var balance in account.Balances)
    {
        if (balance.AssetType == "native")
        {
            Console.WriteLine($"Баланс\tXLM:\t{balance.BalanceString}");
            continue;
        }

        Console.WriteLine($"Баланс\t{balance.AssetCode}\tby\t{balance.AssetIssuer}:\t{balance.BalanceString}");
    }
}
catch (HttpResponseException ex)
{
    Console.WriteLine($"Произошла ошибка при обращении к серверу: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Произошла ошибка: {ex.Message}");
}