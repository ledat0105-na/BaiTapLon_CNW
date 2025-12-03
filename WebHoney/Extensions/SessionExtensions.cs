using System.Text;
using System.Text.Json;

namespace WebHoney.Extensions;

public static class SessionExtensions
{
    public static void Set<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? Get<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }

    // Helper methods để lấy thông tin user từ session
    public static int? GetUserId(this ISession session)
    {
        var userIdStr = session.GetString("UserId");
        return int.TryParse(userIdStr, out var userId) ? userId : null;
    }

    public static string? GetUsername(this ISession session)
    {
        return session.GetString("Username");
    }

    public static string? GetUserRole(this ISession session)
    {
        return session.GetString("Role");
    }

    public static bool IsLoggedIn(this ISession session)
    {
        return !string.IsNullOrEmpty(session.GetString("UserId"));
    }

    public static bool IsAdmin(this ISession session)
    {
        return session.GetString("Role") == "Admin";
    }

    public static bool IsStaff(this ISession session)
    {
        var role = session.GetString("Role");
        return role == "Admin" || role == "Staff";
    }
}

