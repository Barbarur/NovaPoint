namespace NovaPointLibrary.Commands.Utilities.GraphModel;

internal class GraphSignInActivity
{
    public DateTime? LastSignInDateTime { get; set; }
    public DateTime? LastNonInteractiveSignInDateTime { get; set; }
    public DateTime? LastSuccessfulSignInDateTime { get; set; }
}

internal class GraphServicePrincipalSignInActivity
{
    public string AppId { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public GraphSignInActivity? LastSignInActivity { get; set; }
    public GraphSignInActivity? DelegatedClientSignInActivity { get; set; }
    public GraphSignInActivity? ApplicationAuthenticationClientSignInActivity { get; set; }
}
