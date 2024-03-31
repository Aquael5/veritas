﻿using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Dtos;

public record VoteDto
{
    public string Hash { get; init; } = default!;

    public string VoterAddress { get; init; } = default!;

    public string VoterPubKey { get; init; } = default!;

    public string Signature { get; init; } = default!;

    public int PartyId { get; init; }

    public DateTime Timestamp { get; init; }

    public long Nonce { get; init; }

    public long BlockIndex { get; set; }

    public BlockDto Block { get; init; } = default!;

    public static explicit operator Vote(VoteDto source)
    {
        var vote = new Vote
        {
            Voter = Voter.FromPubKey(source.VoterPubKey.ToBytesFromHex()),
            PartyId = source.PartyId,
            Timestamp = new DateTimeOffset(source.Timestamp).ToUnixTimeMilliseconds(),
            Signature = source.Signature.ToBytesFromHex(),
            Nonce = source.Nonce,
            BlockIndex = source.BlockIndex,
        };

        if (!vote.IsHashValid)
            throw new InvalidOperationException(
                $"failed to convert Vote, the hash is not valid. Missing proof of work. was: {vote.Hash.ToHexString()}");

        if (!vote.IsSignatureValid)
            throw new InvalidOperationException($"failed to convert Vote, Invalid signature. was: {vote.Hash.ToHexString()}");

        if (source.Hash != vote.Hash.ToHexString())
            throw new InvalidOperationException($"failed to convert Vote, Invalid hash, expected: {vote.Hash.ToHexString()} but was: {source.Hash}");

        if (!vote.VerifySignature(source.Signature.ToBytesFromHex()))
            throw new InvalidOperationException($"failed to convert Vote, Invalid signature. was: {vote.Signature.ToHexString()}");

        return vote;
    }

    public static implicit operator VoteDto(Vote source)
    {
        return new VoteDto
        {
            Hash = source.Hash.ToHexString(),
            VoterAddress = source.Voter.Address,
            VoterPubKey = source.Voter.PublicKey.ToHexString(),
            PartyId = source.PartyId,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(source.Timestamp).UtcDateTime,
            Signature = source.Signature.ToHexString(),
            BlockIndex = source.BlockIndex,
            Nonce = source.Nonce,
        };
    }
}