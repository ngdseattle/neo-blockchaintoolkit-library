
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Linq;
using Neo.BlockchainToolkit.Models;
using Neo.Cryptography.ECC;
using Neo.Wallets;
using Newtonsoft.Json;

namespace Neo.BlockchainToolkit
{
    public static class Extensions
    {
        public static ProtocolSettings GetProtocolSettings(this ExpressChain? chain, uint secondsPerBlock = 0)
        {
            return chain == null 
                ? ProtocolSettings.Default 
                : ProtocolSettings.Default with {
                    Magic = chain.Magic,
                    AddressVersion = chain.AddressVersion,
                    MillisecondsPerBlock = secondsPerBlock == 0 ? 15000 : secondsPerBlock * 1000,
                    ValidatorsCount = chain.ConsensusNodes.Count,
                    StandbyCommittee = chain.ConsensusNodes.Select(GetPublicKey).ToArray(),
                    SeedList = chain.ConsensusNodes
                        .Select(n => $"{System.Net.IPAddress.Loopback}:{n.TcpPort}")
                        .ToArray(),
                };

            static ECPoint GetPublicKey(ExpressConsensusNode node)
                => new KeyPair(node.Wallet.Accounts.Select(a => a.PrivateKey).Distinct().Single().HexToBytes()).PublicKey;
        }

        public static ExpressChain LoadChain(this IFileSystem fileSystem, string path)
        {
            var serializer = new JsonSerializer();
            using var stream = fileSystem.File.OpenRead(path);
            using var streamReader = new System.IO.StreamReader(stream);
            using var reader = new JsonTextReader(streamReader);
            return serializer.Deserialize<ExpressChain>(reader)
                ?? throw new Exception($"Cannot load Neo-Express instance information from {path}");
        }

        public static void SaveChain(this IFileSystem fileSystem, ExpressChain chain, string path)
        {
            var serializer = new JsonSerializer();
            using var stream = fileSystem.File.Open(path, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            using var streamWriter = new System.IO.StreamWriter(stream);
            using var writer = new JsonTextWriter(streamWriter) { Formatting = Formatting.Indented };
            serializer.Serialize(writer, chain);
        }

        public static ExpressChain FindChain(this IFileSystem fileSystem, string fileName = Constants.DEFAULT_EXPRESS_FILENAME, string? searchFolder = null)
        {
            if (fileSystem.TryFindChain(out var chain, fileName, searchFolder)) return chain;
            throw new Exception($"{fileName} Neo-Express file not found");
        }

        public static bool TryFindChain(this IFileSystem fileSystem, [NotNullWhen(true)] out ExpressChain? chain, string fileName = Constants.DEFAULT_EXPRESS_FILENAME, string? searchFolder = null)
        {
            searchFolder ??= fileSystem.Directory.GetCurrentDirectory();
            while (searchFolder != null)
            {
                var filePath = fileSystem.Path.Combine(searchFolder, fileName);
                if (fileSystem.File.Exists(filePath))
                {
                    chain = fileSystem.LoadChain(filePath);
                    return true;
                }

                searchFolder = fileSystem.Path.GetDirectoryName(searchFolder);
            }

            chain = null;
            return false;
        }

        public static ExpressWallet GetWallet(this ExpressChain chain, string name)
        {
            if (TryGetWallet(chain, name, out var wallet)) return wallet;
            throw new Exception($"wallet {name} not found");
        }

        public static bool TryGetWallet(this ExpressChain chain, string name, [NotNullWhen(true)] out ExpressWallet? wallet)
        {
            for (int i = 0; i < chain.Wallets.Count; i++)
            {
                if (string.Equals(name, chain.Wallets[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    wallet = chain.Wallets[i];
                    return true;
                }
            }

            wallet = null;
            return false;
        }

        public static ExpressWalletAccount GetDefaultAccount(this ExpressChain chain, string name)
        {
            if (TryGetDefaultAccount(chain, name, out var account)) return account;
            throw new Exception($"default account for {name} wallet not found");
        }

        public static bool TryGetDefaultAccount(this ExpressChain chain, string name, [NotNullWhen(true)] out ExpressWalletAccount? account)
        {
            if (chain.TryGetWallet(name, out var wallet) && wallet.DefaultAccount != null)
            {
                account = wallet.DefaultAccount;
                return true;
            }

            account = null;
            return false;
        }
        public static UInt160 ToScriptHash(this ExpressWalletAccount account, byte addressVersion)
            => account.ScriptHash.ToScriptHash(addressVersion);
    }
}
