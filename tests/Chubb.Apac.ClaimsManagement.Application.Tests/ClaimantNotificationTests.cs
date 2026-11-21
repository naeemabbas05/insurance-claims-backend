using Chubb.Apac.ClaimsManagement.Application.ClaimantNotifications;

namespace Chubb.Apac.ClaimsManagement.Application.Tests;

public sealed class ClaimantNotificationTests
{
    [Fact]
    public async Task QueryRequiresOwnerIdentityAndBoundedPageSize()
    {
        var validator = new ClaimantNotificationQueryValidator();

        var noIdentity = await validator.ValidateAsync(
            new ClaimantNotificationQuery(null, null, false, 1, 20, false));
        var invalidPage = await validator.ValidateAsync(
            new ClaimantNotificationQuery(Guid.NewGuid(), null, false, 1, 101, false));
        var overflowingPage = await validator.ValidateAsync(
            new ClaimantNotificationQuery(Guid.NewGuid(), null, false, int.MaxValue, 100, false));

        Assert.False(noIdentity.IsValid);
        Assert.Contains(invalidPage.Errors, error => error.PropertyName == nameof(ClaimantNotificationQuery.PageSize));
        Assert.False(overflowingPage.IsValid);
    }
}
