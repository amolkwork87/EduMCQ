using EduMcq.Models;
using Xunit;

namespace EduMcq.Tests.Models;

public class UserTests
{
    [Fact]
    public void NewUserDefaultsToStudentAndActive()
    {
        var user = new User();

        Assert.Equal("student", user.Role);
        Assert.True(user.IsActive);
    }
}