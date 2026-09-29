namespace EDUFIXiarz.Models;

public sealed class SetupProfile
{
    public bool ChangeHostname { get; set; } = true;
    public string Hostname { get; set; } = "";
    public bool JoinDomain { get; set; }
    public string Domain { get; set; } = "";
    public string DomainUser { get; set; } = "";
    public bool RemoveBloatware { get; set; } = true;
    public bool RemoveOffice { get; set; }
    public bool InstallApplications { get; set; } = true;
    public List<string> ApplicationIds { get; set; } = [];
}
