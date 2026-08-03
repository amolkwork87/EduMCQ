using System.Security.Claims;

namespace EduMcq.Models;

public class User
{
    public string UserId {get;set;}
    public string MobileNumber { get; set; }

    public string Role {get;set;}

    public string Status {get;set;}

    public string Name {get;set;}
}
