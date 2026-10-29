using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Auth;
using WhoSabi.Domain.Users;
using WhoSabi.Domain.Wallets;
using WhoSabi.Domain.Rooms;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Api.Platform;

public static class TestAccountSeed
{
    public static async Task Apply(IServiceProvider services, IConfiguration config)
    {
        if (!config.GetValue<bool>("TestAccount:Enabled")) return;
        var db = services.GetRequiredService<WhoSabiDbContext>();
        var email = config["TestAccount:Email"]?.Trim().ToLowerInvariant(); var password = config["TestAccount:Password"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || password.Length < 16)
            throw new InvalidOperationException("Enabled test account requires an email and a password of at least 16 characters.");
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(85320129)");
        var user = await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == email);
        if (user is not null && !user.IsTestAccount) throw new InvalidOperationException("Refusing to modify an existing real account.");
        if (user is null)
        {
            user = new User {
                Username = "qa_" + Guid.NewGuid().ToString("N")[..12], Email = email, NormalizedEmail = email,
                PasswordHash = services.GetRequiredService<IPasswordHashingService>().HashPassword(password),
                AvatarLabel = "QA", ReferralCode = "QA" + Guid.NewGuid().ToString("N")[..10], IsTestAccount = true
            };
            db.Add(user); db.Add(new WalletAccount { UserId = user.Id, BalanceKobo = 0 });
        }
        // No fake payment, activation bonus, spendable balance or policy consent is created.
        if (!await db.Rooms.AnyAsync(x => x.Id == "qa-free-room"))
        {
            db.Add(new Room { Id = "qa-free-room", Title = "QA free practice room", HostUserId = user.Id, IsTestRoom = true, Status = RoomStatus.Live, EntryFeeKobo = 0 });
            db.Add(new RoomQuestion { Id = "qa-question-1", RoomId = "qa-free-room", Category = "QA", Prompt = "What is 2 + 2?", CorrectIndex = 1, SortOrder = 1 });
            db.AddRange(new[] { "3", "4", "5", "6" }.Select((value, index) => new RoomQuestionOption { QuestionId = "qa-question-1", Value = value, SortOrder = index }));
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
