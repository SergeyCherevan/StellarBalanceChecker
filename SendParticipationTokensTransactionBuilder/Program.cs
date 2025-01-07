using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using StellarDotnetSdk;
using StellarDotnetSdk.Responses;
using StellarDotnetSdk.Xdr;

class Program
{
    const string MAIN_ACCOUNT = "GCNVDZIHGX473FEI7IXCUAEXUJ4BGCKEMHF36VYP5EMS7PX2QBLAMTLA";
    const string SECRETARY_ACCOUNT = "GDGC46H4MQKRW3TZTNCWUU6R2C7IPXGN7HQLZBJTNQO6TW7ZOS6MSECR";
    const string MTLAP = "MTLAP";
    const string MTLAC = "MTLAC";

    static async Task Main(string[] args)
{
    var stellar = new Server("https://horizon.stellar.org");

    var list = new Dictionary<string, (string, int)>
        {
            { "GA4XOEQF4VQXWGJZQBIYWGBKZXGCELLRT6NBELGEB3KHP7J362BSMSIV", (MTLAP, 1) },
            { "GCODQDDIFNOXEUXJ6UPKQSLMZX5JJW32MUMMK3L6IFL5YNKVJ53Z5CO5", (MTLAC, 1) },
            { "GD7FKVKVCGFEDPC36SU272BX6Z3JPWYYHMTVOM56ANMKZ3S7UOMEDDEF", (MTLAC, 1) }
        };

    var stellarAccount = await stellar.Accounts.Account(SECRETARY_ACCOUNT);
    var transactionBuilder = new StellarDotnetSdk.Transactions.TransactionBuilder(stellarAccount)
        .AddMemo(StellarDotnetSdk.Memos.Memo.Text("Membership distribution"))
        .SetFee(10000);

    var assets = new Dictionary<string, StellarDotnetSdk.Assets.Asset>
        {
            { MTLAP, StellarDotnetSdk.Assets.Asset.CreateNonNativeAsset(MTLAP, MAIN_ACCOUNT) },
            { MTLAC, StellarDotnetSdk.Assets.Asset.CreateNonNativeAsset(MTLAC, MAIN_ACCOUNT) }
        };

    foreach (var entry in list)
    {
        var address = entry.Key;
        var (token, amount) = entry.Value;

        var account = await stellar.Accounts.Account(address);

        if (!HasAsset(account, assets[token]))
        {
            Console.WriteLine($"Аккаунт {address} не открыл линии доверия к {token}!");
            return;
        }

        if (amount == 0)
        {
            Console.WriteLine($"Для аккаунта {address} указано 0 токенов, шо?");
            return;
        }

        if (amount > 0)
        {
            var setFlagsOp1 = new SetTrustLineFlagsOperation.Builder(address, assets[token])
                .SetSourceAccount(MAIN_ACCOUNT)
                .SetFlags((uint)TrustLineFlags.TrustlineClawbackEnabledFlag)
                .Build();
            transactionBuilder.AddOperation(setFlagsOp1);

            var paymentOp = new PaymentOperation.Builder(address, assets[token], amount.ToString()).Build();
            transactionBuilder.AddOperation(paymentOp);

            var setFlagsOp2 = new SetTrustLineFlagsOperation.Builder(address, assets[token])
                .SetSourceAccount(MAIN_ACCOUNT)
                .ClearFlags((uint)TrustLineFlags.TrustlineClawbackEnabledFlag)
                .Build();
            transactionBuilder.AddOperation(setFlagsOp2);
        }
        else
        {
            var clawbackOp = new ClawbackOperation.Builder(assets[token], address, Math.Abs(amount).ToString())
                .SetSourceAccount(MAIN_ACCOUNT)
                .Build();
            transactionBuilder.AddOperation(clawbackOp);
        }

        var xlmAmount = GetAmountOfXlm(account);
        if (xlmAmount < 10)
        {
            var nativePaymentOp = new PaymentOperation.Builder(address, new AssetTypeNative(), "3.33").Build();
            transactionBuilder.AddOperation(nativePaymentOp);
        }
    }

    var transaction = transactionBuilder.Build();
    Console.WriteLine(transaction.ToEnvelopeXdrBase64());
}

static bool HasAsset(AccountResponse account, Asset assetNeedle)
{
    foreach (var balance in account.Balances)
    {
        if (balance.AssetType == assetNeedle.GetType() && balance.AssetCode == assetNeedle.GetCode() && balance.AssetIssuer == assetNeedle.GetIssuer())
        {
            return true;
        }
    }
    return false;
}

static double GetAmountOfXlm(AccountResponse account)
{
    foreach (var balance in account.Balances)
    {
        if (balance.AssetType == "native")
        {
            return double.Parse(balance.BalanceString);
        }
    }
    return 0.0;
}
}
