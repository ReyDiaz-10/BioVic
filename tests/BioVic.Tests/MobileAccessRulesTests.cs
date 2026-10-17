using BioVic.Application.Services;
using BioVic.Domain.Enums;

namespace BioVic.Tests;

public sealed class MobileAccessRulesTests
{
    [Theory]
    [InlineData(UserRole.Searcher)]
    [InlineData(UserRole.Editor)]
    [InlineData(UserRole.MasterAdministrator)]
    public void EveryConfiguredRoleCanConsult(UserRole role) =>
        Assert.True(MobileAccessRules.CanConsult(role));

    [Fact]
    public void OnlyEditorAndMasterCanAddOperationalRecords()
    {
        Assert.False(MobileAccessRules.CanAddOperationalRecords(UserRole.Searcher));
        Assert.True(MobileAccessRules.CanAddOperationalRecords(UserRole.Editor));
        Assert.True(MobileAccessRules.CanAddOperationalRecords(UserRole.MasterAdministrator));
    }

    [Fact]
    public void OnlyMasterCanAdministerFromMobile()
    {
        Assert.False(MobileAccessRules.CanAdminister(UserRole.Searcher));
        Assert.False(MobileAccessRules.CanAdminister(UserRole.Editor));
        Assert.True(MobileAccessRules.CanAdminister(UserRole.MasterAdministrator));
    }

    [Theory]
    [InlineData(UserRole.Searcher)]
    [InlineData(UserRole.Editor)]
    [InlineData(UserRole.MasterAdministrator)]
    public void NoRoleCanDeleteFromMobile(UserRole role) =>
        Assert.False(MobileAccessRules.CanDeleteFromMobile(role));
}
