using Microsoft.EntityFrameworkCore;
using WhoSabi.Domain.Platform;
using WhoSabi.Domain.Users;
using WhoSabi.Domain.Auth;
using WhoSabi.Domain.Rooms;

namespace WhoSabi.Infrastructure.Persistence;

internal static class PlatformModel
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<Room>().HasOne<User>().WithMany().HasForeignKey(x => x.HostUserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<WhoSabi.Domain.Payments.PaymentTransaction>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<User>().Property(x => x.PhoneNumber).HasMaxLength(16);
        model.Entity<User>().Property(x => x.AvatarUrl).HasMaxLength(2048);
        model.Entity<User>().Property(x => x.AcceptedPolicyVersion).HasMaxLength(80);
        model.Entity<User>().HasIndex(x => x.PhoneNumber).IsUnique();
        model.Entity<User>().Property(x => x.GoogleSubject).HasMaxLength(255);
        model.Entity<User>().HasIndex(x => x.GoogleSubject).IsUnique();
        model.Entity<WhoSabi.Domain.Wallets.WalletAccount>().ToTable("wallet_accounts", t => t.HasCheckConstraint("ck_wallet_balance_nonnegative", "\"BalanceKobo\" >= 0"));
        model.Entity<WhoSabi.Domain.Wallets.WalletLedgerEntry>().HasIndex(x => new { x.WalletAccountId, x.Reference }).IsUnique().HasFilter("\"Reference\" IS NOT NULL");
        // Expression index for case-insensitive username uniqueness is added in the migration.
        model.Entity<PendingSignup>().Property(x => x.PhoneNumber).HasMaxLength(16);
        model.Entity<PendingSignup>().Property(x => x.AcceptedPolicyVersion).HasMaxLength(80);
        model.Entity<RoomRound>(e => {
            e.ToTable("room_rounds"); e.HasKey(x => x.Id);
            e.ToTable("room_rounds", t => t.HasCheckConstraint("ck_round_deadline", "\"EndsAt\" >= \"StartedAt\""));
            e.HasIndex(x => new { x.RoomId, x.QuestionId }).IsUnique();
            e.HasIndex(x => x.RoomId).IsUnique().HasFilter("\"ClosedAt\" IS NULL");
            e.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<RoomQuestion>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<ChatMessage>(e => {
            e.ToTable("chat_messages"); e.HasKey(x => x.Id);
            e.Property(x => x.Text).HasMaxLength(1000); e.Property(x => x.ClientMessageId).HasMaxLength(80);
            e.HasIndex(x => new { x.RoomId, x.UserId, x.ClientMessageId }).IsUnique();
            e.HasIndex(x => new { x.RoomId, x.CreatedAt });
            e.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<AppNotification>(e => {
            e.ToTable("notifications"); e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(120); e.Property(x => x.Body).HasMaxLength(1000);
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<PushDevice>(e => {
            e.ToTable("push_devices"); e.HasKey(x => x.Id);
            e.Property(x => x.Token).HasMaxLength(4096); e.Property(x => x.Platform).HasMaxLength(20);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<WalletOperation>(e => {
            e.ToTable("wallet_operations"); e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(80); e.Property(x => x.Fingerprint).HasMaxLength(256);
            e.HasIndex(x => new { x.UserId, x.Key }).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<PushDelivery>(e => {
            e.ToTable("push_deliveries"); e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.NotificationId, x.DeviceId }).IsUnique();
            e.HasOne<AppNotification>().WithMany().HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<PushDevice>().WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<BankAccount>(e => {
            e.ToTable("bank_accounts"); e.HasKey(x => x.Id);
            e.Property(x => x.BankCode).HasMaxLength(10); e.Property(x => x.AccountNumber).HasMaxLength(10);
            e.Property(x => x.AccountName).HasMaxLength(200);
            e.HasIndex(x => new { x.UserId, x.BankCode, x.AccountNumber }).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Tournament>(e => {
            e.ToTable("tournaments"); e.HasKey(x => x.Id); e.Property(x => x.Title).HasMaxLength(120);
            e.HasIndex(x => x.RoomId).IsUnique();
            e.HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<TournamentInvite>(e => {
            e.ToTable("tournament_invites"); e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TournamentId, x.UserId }).IsUnique();
            e.HasOne<Tournament>().WithMany().HasForeignKey(x => x.TournamentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<TournamentEntry>(e => {
            e.ToTable("tournament_entries"); e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TournamentId, x.UserId }).IsUnique();
            e.HasOne<Tournament>().WithMany().HasForeignKey(x => x.TournamentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
