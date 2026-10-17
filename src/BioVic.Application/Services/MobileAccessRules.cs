using BioVic.Domain.Enums;

namespace BioVic.Application.Services;

public static class MobileAccessRules
{
    public static bool CanConsult(UserRole role) => role is UserRole.Searcher or UserRole.Editor or UserRole.MasterAdministrator;
    public static bool CanAddOperationalRecords(UserRole role) => role is UserRole.Editor or UserRole.MasterAdministrator;
    public static bool CanAdminister(UserRole role) => role == UserRole.MasterAdministrator;

    // La eliminación queda deliberadamente reservada para el cliente de escritorio.
    public static bool CanDeleteFromMobile(UserRole role) => false;
}
