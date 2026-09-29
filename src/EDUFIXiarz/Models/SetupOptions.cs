namespace EDUFIXiarz.Models;

public sealed class SetupOptions
{
    public bool ChangeHostname { get; set; }
    public bool JoinDomain { get; set; }
    public bool RemoveBloatware { get; set; }
    public bool RemoveOffice { get; set; }
    public bool InstallApplications { get; set; }

    public string Hostname { get; set; } = "";
    public string Domain { get; set; } = "";
    public string DomainUser { get; set; } = "";
}
