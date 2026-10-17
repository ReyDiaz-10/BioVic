namespace BioVic.Api.Auth;

public static class SessionAuthenticationDefaults
{
    public const string Scheme = "BioVicSession";
    public const string EditorOrMasterPolicy = "EditorOrMaster";
    public const string MasterPolicy = "MasterOnly";
    public const string WebCookieName = "BioVic.WebSession";
}
