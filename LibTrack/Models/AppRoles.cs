namespace LibTrack.Models;

public static class AppRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
    public static string GetRoleName(int roleId)
    {
        return roleId == 2 ? Admin : User;
    }
}