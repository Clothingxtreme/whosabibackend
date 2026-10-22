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
            ?? configuration["DATABASE_URL"]
            ?? throw new InvalidOperationException("Set DefaultConnection or DATABASE_URL.");

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

        var result = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1])
        };

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = parameter.Split('=', 2);
            var key = Uri.UnescapeDataString(pair[0]);
            var value = pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : "";
            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                result["SSL Mode"] = value;
            else if (key.Equals("channel_binding", StringComparison.OrdinalIgnoreCase))
                result["Channel Binding"] = value;
        }

        return result.ConnectionString;
    }
}
