using StellarDotnetSdk;

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
    // Подключение к основной сети Stellar
    var server = new Server("https://horizon.stellar.org");

    // Получение информации о счетах
    var account = await server.Accounts.Account(walletAddress);

    // Поиск баланса XLM
    foreach (var balance in account.Balances)
    {
        if (balance.AssetType == "native")
        {
            Console.WriteLine($"Баланс XLM: {balance.BalanceString}");
            return;
        }
    }

    Console.WriteLine("Баланс XLM не найден.");
}
catch (Exception ex)
{
    Console.WriteLine($"Произошла ошибка: {ex.Message}");
}