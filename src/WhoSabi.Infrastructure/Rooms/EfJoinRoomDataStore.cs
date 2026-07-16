using System.Data;
using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Rooms;
using WhoSabi.Domain.Rooms;
using WhoSabi.Domain.Wallets;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Infrastructure.Rooms;

public sealed class EfJoinRoomDataStore(WhoSabiDbContext dbContext) : IJoinRoomDataStore
{
    public async Task<JoinRoomResultRecord> JoinRoomAsync(
        Guid userId,
        string roomId,
        RoomParticipantRole role,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var room = await dbContext.Rooms.SingleOrDefaultAsync(
            candidate => candidate.Id == roomId,
            cancellationToken);

        if (room is null)
        {
            throw new JoinRoomException(
                "ROOM_NOT_FOUND",
                "Room was not found.");
        }

        if (room.Status != RoomStatus.Live)
        {
            throw new JoinRoomException(
                "ROOM_NOT_LIVE",
                "Room is not accepting joins right now.");
        }

        var userExists = await dbContext.Users.AnyAsync(
            user => user.Id == userId,
            cancellationToken);

        if (!userExists)
        {
            throw new JoinRoomException(
                "USER_NOT_FOUND",
                "User was not found.");
        }

        var wallet = await dbContext.WalletAccounts.SingleOrDefaultAsync(
            account => account.UserId == userId,
            cancellationToken);

        if (wallet is null)
        {
            throw new JoinRoomException(
                "WALLET_NOT_FOUND",
                "Wallet was not found.");
        }

        var participant = await dbContext.RoomParticipants.SingleOrDefaultAsync(
            candidate => candidate.RoomId == room.Id && candidate.UserId == userId,
            cancellationToken);

        if (participant is not null &&
            participant.Status == RoomParticipantStatus.Active &&
            participant.Role == RoomParticipantRole.Player)
        {
            await transaction.CommitAsync(cancellationToken);

            return new JoinRoomResultRecord(
                room.Id,
                userId,
                participant.Role,
                participant.EntryFeeKobo,
                wallet.BalanceKobo,
                null,
                "already_joined");
        }

        if (role == RoomParticipantRole.Spectator)
        {
            participant ??= AddParticipant(
                room.Id,
                userId,
                RoomParticipantRole.Spectator,
                0,
                now);

            participant.Role = RoomParticipantRole.Spectator;
            participant.Status = RoomParticipantStatus.Active;
            participant.EntryFeeKobo = 0;
            participant.JoinedAt = participant.JoinedAt == default
                ? now
                : participant.JoinedAt;
            participant.LeftAt = null;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new JoinRoomResultRecord(
                room.Id,
                userId,
                participant.Role,
                participant.EntryFeeKobo,
                wallet.BalanceKobo,
                null,
                "joined");
        }

        if (wallet.BalanceKobo < room.EntryFeeKobo)
        {
            throw new JoinRoomException(
                "INSUFFICIENT_BALANCE",
                "Wallet balance is not enough to join this room as a player.");
        }

        var wasActiveSpectator = participant is
        {
            Role: RoomParticipantRole.Spectator,
            Status: RoomParticipantStatus.Active
        };

        participant ??= AddParticipant(
            room.Id,
            userId,
            RoomParticipantRole.Player,
            room.EntryFeeKobo,
            now);

        participant.Role = RoomParticipantRole.Player;
        participant.Status = RoomParticipantStatus.Active;
        participant.EntryFeeKobo = room.EntryFeeKobo;
        participant.LeftAt = null;

        wallet.BalanceKobo -= room.EntryFeeKobo;
        wallet.UpdatedAt = now;

        room.PlayerCount += 1;
        room.UpdatedAt = now;

        var ledgerEntry = new WalletLedgerEntry
        {
            WalletAccountId = wallet.Id,
            UserId = userId,
            AmountKobo = -room.EntryFeeKobo,
            BalanceAfterKobo = wallet.BalanceKobo,
            Type = WalletLedgerEntryType.Debit,
            Title = "Room entry fee paid",
            Description = $"Joined {room.Title} as a player.",
            Reference = $"ROOM-{room.Id}-{participant.Id:N}",
            CreatedAt = now
        };

        dbContext.WalletLedgerEntries.Add(ledgerEntry);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new JoinRoomResultRecord(
            room.Id,
            userId,
            participant.Role,
            participant.EntryFeeKobo,
            wallet.BalanceKobo,
            ledgerEntry.Id,
            wasActiveSpectator ? "upgraded_to_player" : "joined");
    }

    private RoomParticipant AddParticipant(
        string roomId,
        Guid userId,
        RoomParticipantRole role,
        long entryFeeKobo,
        DateTimeOffset now)
    {
        var participant = new RoomParticipant
        {
            RoomId = roomId,
            UserId = userId,
            Role = role,
            Status = RoomParticipantStatus.Active,
            EntryFeeKobo = entryFeeKobo,
            JoinedAt = now
        };

        dbContext.RoomParticipants.Add(participant);
        return participant;
    }
}
