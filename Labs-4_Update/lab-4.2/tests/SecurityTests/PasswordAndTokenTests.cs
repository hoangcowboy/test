using System.Threading.Tasks;
using TrainingLab.Security.Fixed;
using Xunit;

namespace SecurityTests;

public class PasswordAndTokenTests
{
    [Fact]
    public void HashPassword_ProducesDifferentHashes()
    {
        var ctrl = new AdminController(null!, null!, new System.Net.Http.HttpClient());
        var h1 = ctrl.HashPassword("password123");
        var h2 = ctrl.HashPassword("password123");
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForSamePassword()
    {
        var ctrl = new AdminController(null!, null!, new System.Net.Http.HttpClient());
        var hash = ctrl.HashPassword("mysecret");
        Assert.True(ctrl.VerifyPassword("mysecret", hash));
    }

    [Fact]
    public void GenerateResetToken_IsNonEmptyAndDifferent()
    {
        var ctrl = new AdminController(null!, null!, new System.Net.Http.HttpClient());
        var t1 = ctrl.GenerateResetToken();
        var t2 = ctrl.GenerateResetToken();
        Assert.False(string.IsNullOrEmpty(t1));
        Assert.False(string.IsNullOrEmpty(t2));
        Assert.NotEqual(t1, t2);
    }
}
