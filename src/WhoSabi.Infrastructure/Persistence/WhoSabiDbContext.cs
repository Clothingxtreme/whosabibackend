using Microsoft.EntityFrameworkCore;
using WhoSabi.Domain.Auth;
using WhoSabi.Domain.Payments;
using WhoSabi.Domain.Rooms;
using WhoSabi.Domain.Users;
using WhoSabi.Domain.Wallets;

namespace WhoSabi.Infrastructure.Persistence;

public sealed class WhoSabiDbContext(DbContextOptions<WhoSabiDbContext> options)
    : DbContext(options)
{
    public DbSet<PendingSignup> PendingSignups => Set<PendingSignup>();

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<User> Users => Set<User>();

    public DbSet<WalletAccount> WalletAccounts => Set<WalletAccount>();

    public DbSet<WalletLedgerEntry> WalletLedgerEntries => Set<WalletLedgerEntry>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<RoomTag> RoomTags => Set<RoomTag>();

    public DbSet<RoomGradientColor> RoomGradientColors => Set<RoomGradientColor>();

    public DbSet<RoomParticipant> RoomParticipants => Set<RoomParticipant>();

    public DbSet<RoomDisplayParticipant> RoomDisplayParticipants => Set<RoomDisplayParticipant>();

    public DbSet<RoomQuestion> RoomQuestions => Set<RoomQuestion>();

    public DbSet<RoomQuestionOption> RoomQuestionOptions => Set<RoomQuestionOption>();

    public DbSet<RoomAnswerSubmission> RoomAnswerSubmissions => Set<RoomAnswerSubmission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PendingSignup>(entity =>
        {
            entity.ToTable("pending_signups");

            entity.HasKey(signup => signup.Id);

            entity.Property(signup => signup.Username)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(signup => signup.Email)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(signup => signup.NormalizedEmail)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(signup => signup.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(signup => signup.ReferralCode)
                .HasMaxLength(50);

            entity.Property(signup => signup.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(signup => signup.ExpiresAt)
                .IsRequired();

            entity.Property(signup => signup.CreatedAt)
                .IsRequired();

            entity.HasIndex(signup => signup.NormalizedEmail)
                .HasDatabaseName("ix_pending_signups_normalized_email");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("payment_transactions");

            entity.HasKey(payment => payment.Id);

            entity.Property(payment => payment.InternalReference)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(payment => payment.ProviderReference)
                .HasMaxLength(150);

            entity.Property(payment => payment.Purpose)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(payment => payment.Provider)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(payment => payment.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(payment => payment.AmountKobo)
                .IsRequired();

            entity.Property(payment => payment.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(payment => payment.PaymentMethod)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(payment => payment.PaymentUrl)
                .HasMaxLength(2048);

            entity.Property(payment => payment.ExpiresAt)
                .IsRequired();

            entity.Property(payment => payment.CreatedAt)
                .IsRequired();

            entity.HasIndex(payment => payment.InternalReference)
                .IsUnique()
                .HasDatabaseName("ux_payment_transactions_internal_reference");

            entity.HasIndex(payment => payment.ProviderReference)
                .IsUnique()
                .HasDatabaseName("ux_payment_transactions_provider_reference");

            entity.HasIndex(payment => payment.PendingSignupId)
                .HasDatabaseName("ix_payment_transactions_pending_signup_id");

            entity.HasOne<PendingSignup>()
                .WithMany()
                .HasForeignKey(payment => payment.PendingSignupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Username)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(user => user.Email)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(user => user.NormalizedEmail)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(user => user.AvatarLabel)
                .HasMaxLength(5)
                .IsRequired();

            entity.Property(user => user.ReferralCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(user => user.CreatedAt)
                .IsRequired();

            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique()
                .HasDatabaseName("ux_users_normalized_email");

            entity.HasIndex(user => user.ReferralCode)
                .IsUnique()
                .HasDatabaseName("ux_users_referral_code");
        });

        modelBuilder.Entity<WalletAccount>(entity =>
        {
            entity.ToTable("wallet_accounts");

            entity.HasKey(wallet => wallet.Id);

            entity.Property(wallet => wallet.BalanceKobo)
                .IsRequired();

            entity.Property(wallet => wallet.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(wallet => wallet.CreatedAt)
                .IsRequired();

            entity.HasIndex(wallet => wallet.UserId)
                .IsUnique()
                .HasDatabaseName("ux_wallet_accounts_user_id");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(wallet => wallet.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WalletLedgerEntry>(entity =>
        {
            entity.ToTable("wallet_ledger_entries");

            entity.HasKey(entry => entry.Id);

            entity.Property(entry => entry.AmountKobo)
                .IsRequired();

            entity.Property(entry => entry.BalanceAfterKobo)
                .IsRequired();

            entity.Property(entry => entry.Type)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(entry => entry.Title)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(entry => entry.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(entry => entry.Reference)
                .HasMaxLength(150);

            entity.Property(entry => entry.CreatedAt)
                .IsRequired();

            entity.HasIndex(entry => entry.WalletAccountId)
                .HasDatabaseName("ix_wallet_ledger_entries_wallet_account_id");

            entity.HasIndex(entry => entry.UserId)
                .HasDatabaseName("ix_wallet_ledger_entries_user_id");

            entity.HasIndex(entry => entry.Reference)
                .HasDatabaseName("ix_wallet_ledger_entries_reference");

            entity.HasOne<WalletAccount>()
                .WithMany()
                .HasForeignKey(entry => entry.WalletAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(entry => entry.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");

            entity.HasKey(room => room.Id);

            entity.Property(room => room.Id)
                .HasMaxLength(80);

            entity.Property(room => room.Title)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(room => room.Subtitle)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(room => room.PrizePoolKobo)
                .IsRequired();

            entity.Property(room => room.PlayerCount)
                .IsRequired();

            entity.Property(room => room.EntryFeeKobo)
                .IsRequired();

            entity.Property(room => room.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(room => room.CreatedAt)
                .IsRequired();

            entity.HasIndex(room => room.Status)
                .HasDatabaseName("ix_rooms_status");

            entity.HasData(RoomSeedData.Rooms);
        });

        modelBuilder.Entity<RoomTag>(entity =>
        {
            entity.ToTable("room_tags");

            entity.HasKey(tag => tag.Id);

            entity.Property(tag => tag.RoomId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(tag => tag.Value)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(tag => tag.SortOrder)
                .IsRequired();

            entity.HasIndex(tag => new { tag.RoomId, tag.SortOrder })
                .IsUnique()
                .HasDatabaseName("ux_room_tags_room_id_sort_order");

            entity.HasOne<Room>()
                .WithMany()
                .HasForeignKey(tag => tag.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(RoomSeedData.Tags);
        });

        modelBuilder.Entity<RoomGradientColor>(entity =>
        {
            entity.ToTable("room_gradient_colors");

            entity.HasKey(color => color.Id);

            entity.Property(color => color.RoomId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(color => color.Hex)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(color => color.SortOrder)
                .IsRequired();

            entity.HasIndex(color => new { color.RoomId, color.SortOrder })
                .IsUnique()
                .HasDatabaseName("ux_room_gradient_colors_room_id_sort_order");

            entity.HasOne<Room>()
                .WithMany()
                .HasForeignKey(color => color.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(RoomSeedData.GradientColors);
        });

        modelBuilder.Entity<RoomParticipant>(entity =>
        {
            entity.ToTable("room_participants");

            entity.HasKey(participant => participant.Id);

            entity.Property(participant => participant.RoomId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(participant => participant.Role)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(participant => participant.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(participant => participant.EntryFeeKobo)
                .IsRequired();

            entity.Property(participant => participant.JoinedAt)
                .IsRequired();

            entity.HasIndex(participant => new { participant.RoomId, participant.UserId })
                .IsUnique()
                .HasDatabaseName("ux_room_participants_room_id_user_id");

            entity.HasIndex(participant => participant.UserId)
                .HasDatabaseName("ix_room_participants_user_id");

            entity.HasOne<Room>()
                .WithMany()
                .HasForeignKey(participant => participant.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(participant => participant.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RoomDisplayParticipant>(entity =>
        {
            entity.ToTable("room_display_participants");

            entity.HasKey(participant => participant.Id);

            entity.Property(participant => participant.Id)
                .HasMaxLength(80);

            entity.Property(participant => participant.RoomId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(participant => participant.Name)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(participant => participant.Initials)
                .HasMaxLength(5)
                .IsRequired();

            entity.Property(participant => participant.GlowColorHex)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(participant => participant.SortOrder)
                .IsRequired();

            entity.HasIndex(participant => new { participant.RoomId, participant.SortOrder })
                .IsUnique()
                .HasDatabaseName("ux_room_display_participants_room_id_sort_order");

            entity.HasOne<Room>()
                .WithMany()
                .HasForeignKey(participant => participant.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(RoomContentSeedData.Participants);
        });

        modelBuilder.Entity<RoomQuestion>(entity =>
        {
            entity.ToTable("room_questions");

            entity.HasKey(question => question.Id);

            entity.Property(question => question.Id)
                .HasMaxLength(80);

            entity.Property(question => question.RoomId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(question => question.Category)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(question => question.Prompt)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(question => question.CorrectIndex)
                .IsRequired();

            entity.Property(question => question.SortOrder)
                .IsRequired();

            entity.HasIndex(question => new { question.RoomId, question.SortOrder })
                .IsUnique()
                .HasDatabaseName("ux_room_questions_room_id_sort_order");

            entity.HasOne<Room>()
                .WithMany()
                .HasForeignKey(question => question.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(RoomContentSeedData.Questions);
        });

        modelBuilder.Entity<RoomQuestionOption>(entity =>
        {
            entity.ToTable("room_question_options");

            entity.HasKey(option => option.Id);

            entity.Property(option => option.QuestionId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(option => option.Value)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(option => option.SortOrder)
                .IsRequired();

            entity.HasIndex(option => new { option.QuestionId, option.SortOrder })
                .IsUnique()
                .HasDatabaseName("ux_room_question_options_question_id_sort_order");

            entity.HasOne<RoomQuestion>()
                .WithMany()
                .HasForeignKey(option => option.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(RoomContentSeedData.QuestionOptions);
        });

        modelBuilder.Entity<RoomAnswerSubmission>(entity =>
        {
            entity.ToTable("room_answer_submissions");

            entity.HasKey(submission => submission.Id);

            entity.Property(submission => submission.RoomId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(submission => submission.QuestionId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(submission => submission.SelectedIndex)
                .IsRequired();

            entity.Property(submission => submission.IsCorrect)
                .IsRequired();

            entity.Property(submission => submission.RewardKobo)
                .IsRequired();

            entity.Property(submission => submission.SubmittedAt)
                .IsRequired();

            entity.HasIndex(submission => new
                {
                    submission.RoomId,
                    submission.QuestionId,
                    submission.UserId
                })
                .IsUnique()
                .HasDatabaseName("ux_room_answer_submissions_room_question_user");

            entity.HasIndex(submission => submission.UserId)
                .HasDatabaseName("ix_room_answer_submissions_user_id");

            entity.HasIndex(submission => submission.WalletLedgerEntryId)
                .HasDatabaseName("ix_room_answer_submissions_wallet_ledger_entry_id");

            entity.HasOne<Room>()
                .WithMany()
                .HasForeignKey(submission => submission.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<RoomQuestion>()
                .WithMany()
                .HasForeignKey(submission => submission.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(submission => submission.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<WalletLedgerEntry>()
                .WithMany()
                .HasForeignKey(submission => submission.WalletLedgerEntryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

internal static class RoomSeedData
{
    private static readonly DateTimeOffset SeededAt =
        new(2026, 7, 14, 0, 0, 0, TimeSpan.Zero);

    public static readonly Room[] Rooms =
    [
        new()
        {
            Id = "naija-night",
            Title = "Naija Night Showdown",
            Subtitle = "Fast money. Faster answers. 30 seconds to flex your knowledge.",
            PrizePoolKobo = 15_000_000,
            PlayerCount = 486,
            EntryFeeKobo = 80_000,
            Status = RoomStatus.Live,
            CreatedAt = SeededAt
        },
        new()
        {
            Id = "campus-clash",
            Title = "Campus Clash Arena",
            Subtitle = "Students battle for bragging rights and a midnight payout.",
            PrizePoolKobo = 22_500_000,
            PlayerCount = 712,
            EntryFeeKobo = 120_000,
            Status = RoomStatus.Live,
            CreatedAt = SeededAt
        },
        new()
        {
            Id = "family-fuse",
            Title = "Family Fuse Live",
            Subtitle = "Survey style answers with social pressure and crowd reactions.",
            PrizePoolKobo = 9_800_000,
            PlayerCount = 368,
            EntryFeeKobo = 65_000,
            Status = RoomStatus.Live,
            CreatedAt = SeededAt
        },
        new()
        {
            Id = "millionaire-rush",
            Title = "Millionaire Rush",
            Subtitle = "Premium stakes with dramatic countdown rounds and clutch prizes.",
            PrizePoolKobo = 50_000_000,
            PlayerCount = 932,
            EntryFeeKobo = 250_000,
            Status = RoomStatus.Live,
            CreatedAt = SeededAt
        }
    ];

    public static readonly RoomTag[] Tags =
    [
        BuildTag("65d84339-11e4-42cb-a58c-e7a1ea330001", "naija-night", "Live", 1),
        BuildTag("65d84339-11e4-42cb-a58c-e7a1ea330002", "naija-night", "Trivia", 2),
        BuildTag("65d84339-11e4-42cb-a58c-e7a1ea330003", "naija-night", "Hot room", 3),
        BuildTag("65d84339-11e4-42cb-a58c-e7a1ea330004", "campus-clash", "Campus", 1),
        BuildTag("65d84339-11e4-42cb-a58c-e7a1ea330005", "campus-clash", "Versus", 2),
        BuildTag("65d84339-11e4-42cb-a58c-e7a1ea330006", "campus-clash", "Top trend", 3),
        BuildTag("65d84339-11e4-42cb-a58c-e7a1ea330007", "family-fuse", "Survey", 1),
        BuildTag("65d84339-11e4-42cb-a58c-e7a1ea330008", "family-fuse", "Party", 2),
        BuildTag("65d84339-11e4-42cb-a58c-e7a1ea330009", "family-fuse", "Casual", 3),
        BuildTag("65d84339-11e4-42cb-a58c-e7a1ea330010", "millionaire-rush", "Premium", 1),
        BuildTag("65d84339-11e4-42cb-a58c-e7a1ea330011", "millionaire-rush", "Elite", 2),
        BuildTag("65d84339-11e4-42cb-a58c-e7a1ea330012", "millionaire-rush", "Reward drop", 3)
    ];

    public static readonly RoomGradientColor[] GradientColors =
    [
        BuildGradient("2aa2ca4f-9a13-44ce-83b5-efe5b9960001", "naija-night", "#071323", 1),
        BuildGradient("2aa2ca4f-9a13-44ce-83b5-efe5b9960002", "naija-night", "#143D93", 2),
        BuildGradient("2aa2ca4f-9a13-44ce-83b5-efe5b9960003", "naija-night", "#21C9F3", 3),
        BuildGradient("2aa2ca4f-9a13-44ce-83b5-efe5b9960004", "campus-clash", "#170A2B", 1),
        BuildGradient("2aa2ca4f-9a13-44ce-83b5-efe5b9960005", "campus-clash", "#5A1AB2", 2),
        BuildGradient("2aa2ca4f-9a13-44ce-83b5-efe5b9960006", "campus-clash", "#FF5DB1", 3),
        BuildGradient("2aa2ca4f-9a13-44ce-83b5-efe5b9960007", "family-fuse", "#1A1205", 1),
        BuildGradient("2aa2ca4f-9a13-44ce-83b5-efe5b9960008", "family-fuse", "#7E4A00", 2),
        BuildGradient("2aa2ca4f-9a13-44ce-83b5-efe5b9960009", "family-fuse", "#FFBE4D", 3),
        BuildGradient("2aa2ca4f-9a13-44ce-83b5-efe5b9960010", "millionaire-rush", "#060D18", 1),
        BuildGradient("2aa2ca4f-9a13-44ce-83b5-efe5b9960011", "millionaire-rush", "#18305F", 2),
        BuildGradient("2aa2ca4f-9a13-44ce-83b5-efe5b9960012", "millionaire-rush", "#4A7DFF", 3)
    ];

    private static RoomTag BuildTag(
        string id,
        string roomId,
        string value,
        int sortOrder)
    {
        return new RoomTag
        {
            Id = Guid.Parse(id),
            RoomId = roomId,
            Value = value,
            SortOrder = sortOrder
        };
    }

    private static RoomGradientColor BuildGradient(
        string id,
        string roomId,
        string hex,
        int sortOrder)
    {
        return new RoomGradientColor
        {
            Id = Guid.Parse(id),
            RoomId = roomId,
            Hex = hex,
            SortOrder = sortOrder
        };
    }
}
