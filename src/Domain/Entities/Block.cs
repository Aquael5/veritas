﻿using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using Domain.Common;
using Serilog;
using SJsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using NJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Domain.Entities;

public class Block
{
    private const int Difficulty = 6;

    private readonly List<Vote> _votes = [];

    public long Index { get; init; }

    public long Nonce { get; private set; }

    public string Hash
    {
        get => SHA256.HashData(this.GetHashPayload()).ToHexString();
        // ReSharper disable once UnusedMember.Local for EF Core
        private init => _ = value;
    }

    public string MerkleRoot
    {
        get => Common.MerkleRoot.BuildMerkleRoot(Votes.Select(v => v.Hash.ToBytesFromHex())).ToHexString();
        // ReSharper disable once UnusedMember.Local for EF Core
        private init => _ = value;
    }

    public string PreviousHash { get; init; } = default!;

    public IReadOnlyCollection<Vote> Votes => _votes.AsReadOnly();

    [NJsonIgnore, SJsonIgnore]
    public bool IsHashValid => Hash.StartsWith(new string('0', Difficulty));

    public void AddVote(Vote vote)
    {
        if (!vote.IsHashValid || !vote.IsSignatureValid)
            throw new InvalidOperationException("Invalid vote, either the hash or the signature is not valid");

        vote.BlockIndex = Index;

        _votes.Add(vote);
    }

    public void AddVotes(IEnumerable<Vote> votes)
    {
        var voteList = votes.ToList();

        voteList.ForEach(vote =>
        {
            if (!vote.IsHashValid || !vote.IsSignatureValid)
                throw new InvalidOperationException("Invalid vote, either the hash or the signature is not valid");

            vote.BlockIndex = Index;
        });

        _votes.AddRange(voteList);
    }

    public void Mine()
    {
        if (IsHashValid) return;

        var stopwatch = Stopwatch.StartNew();
        Nonce = Miner.Mine(this.GetHashPayload(), Difficulty);
        stopwatch.Stop();

        Log.Information("Block mined in {Elapsed:s}", stopwatch.Elapsed);
    }

    [Pure]
    public Block NextBlock() => new()
    {
        Index = Index + 1,
        PreviousHash = Hash,
    };

    public static Block GenesisBlock() => new()
    {
        Index = 0,
        Nonce = 14261917,
        PreviousHash = new string('0', 64),
    };
}

public static class BlockExt
{
    public static byte[] GetHashPayload(this Block block)
    {
        var buffer = new List<byte>();
        buffer.AddRange(BitConverter.GetBytes(block.Index));
        buffer.AddRange(block.PreviousHash.ToBytesFromHex());
        buffer.AddRange(block.MerkleRoot.ToBytesFromHex());
        buffer.AddRange(BitConverter.GetBytes(block.Nonce));
        return buffer.ToArray();
    }
}