using System.Data.Common;
using System.Text.RegularExpressions;

namespace WebShop.Infrastructure;

/// <summary>
/// The database connection: ConnectionStrings:DefaultConnection, with every
/// ${NAME} in it replaced by the setting of that name.
///
/// appsettings.json holds only the shape of the connection, never a password:
///
///   Server=${MYSQLHOST};Port=${MYSQLPORT};Database=jumbus;User Id=${MYSQLUSER};Password=${MYSQLPASSWORD};CharSet=utf8mb4;
///
/// The values come from wherever the app runs: on Railway from the app
/// service's Variables, which point at the MySQL service; on your own
/// machine from User Secrets. A full connection string in User Secrets also
/// works, since it replaces this one.
///
/// The database stays "jumbus" rather than ${MYSQLDATABASE}: on Railway that
/// variable names the service's default "railway" database, and the shop's
/// tables are in "jumbus", which sql/01-schema.sql creates.
/// </summary>
public static partial class DatabaseConnection
{
    [GeneratedRegex(@"\$\{([A-Za-z0-9_:]+)\}")]
    private static partial Regex Placeholder();

    public static string Resolve(IConfiguration configuration)
    {
        var template = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing from appsettings.json.");

        // Parsed first and rebuilt after: the builder quotes a value that
        // needs it, so a password with a semicolon cannot break the string.
        var parts = new DbConnectionStringBuilder { ConnectionString = template };

        foreach (var key in parts.Keys.Cast<string>().ToList())
        {
            if (parts[key] is string value && value.Contains("${", StringComparison.Ordinal))
            {
                parts[key] = Placeholder().Replace(value, match =>
                    configuration[match.Groups[1].Value]
                    ?? throw new InvalidOperationException(
                        $"{match.Groups[1].Value} is not set. Add it to the Railway Variables, or to User Secrets on your own machine."));
            }
        }

        return parts.ConnectionString;
    }
}
