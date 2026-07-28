using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WhoSabi.Application.Auth;
using WhoSabi.Application.Payments;
using WhoSabi.Application.Rooms;
using WhoSabi.Application.Users;
using WhoSabi.Application.Wallets;
using WhoSabi.Infrastructure.Auth;
using WhoSabi.Infrastructure.Payments;
using WhoSabi.Infrastructure.Persistence;
using WhoSabi.Infrastructure.Rooms;
using WhoSabi.Infrastructure.Users;
using WhoSabi.Infrastructure.Wallets;

namespace WhoSabi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing.");

        services.AddDbContext<WhoSabiDbContext>(options =>
            options.UseNpgsql(ToNpgsqlConnectionString(connectionString)));

        services.AddScoped<IRegistrationDataStore, EfRegistrationDataStore>();
        services.AddScoped<ILoginDataStore, EfLoginDataStore>();
        services.AddScoped<ICurrentUserDataStore, EfCurrentUserDataStore>();
        services.AddScoped<ILiveRoomDataStore, EfLiveRoomDataStore>();
        services.AddScoped<IRoomDetailDataStore, EfRoomDetailDataStore>();
        services.AddScoped<IJoinRoomDataStore, EfJoinRoomDataStore>();
        services.AddScoped<ISubmitRoomAnswerDataStore, EfSubmitRoomAnswerDataStore>();
        services.AddScoped<IWalletDataStore, EfWalletDataStore>();
        services.AddScoped<ISignupPaymentActivationDataStore, EfSignupPaymentActivationDataStore>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<IPasswordHashingService, Pbkdf2PasswordHashingService>();

        return services;
    }

    // Render supplies database credentials as a PostgreSQL URL, while Npgsql
    // configuration accepts its keyword connection-string format.
    private static string ToNpgsqlConnectionString(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2)
        {
            throw new InvalidOperationException("The PostgreSQL connection URL must include a username and password.");
        }

        return new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1])
        }.ConnectionString;
    }
}
