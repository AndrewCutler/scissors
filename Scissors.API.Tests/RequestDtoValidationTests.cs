/*
 * CODEX-MODIFIED: the contents of this file were written by a human and modified after the fact by a Codex agent.
 */

using System.ComponentModel.DataAnnotations;
using Scissors.API.Tests.Infrastructure;
using Xunit;

namespace Scissors.API.Tests;

public class RequestDtoValidationTests
{
    [Fact]
    public void SaveClippingRequestAcceptsValidPayload()
    {
        var model = new SaveClippingRequestDTO
        {
            Text = "clipboard text",
            CapturedAt = DateTimeOffset.UtcNow,
        };

        AssertValid(model);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void SaveClippingRequestRejectsInvalidText(string? text)
    {
        var model = new SaveClippingRequestDTO
        {
            Text = text ?? string.Empty,
            CapturedAt = DateTimeOffset.UtcNow,
        };

        AssertInvalid(model, nameof(SaveClippingRequestDTO.Text));
    }

    [Fact]
    public void SaveClippingRequestRejectsOverlongText()
    {
        var model = new SaveClippingRequestDTO
        {
            Text = new string('x', 2001),
            CapturedAt = DateTimeOffset.UtcNow,
        };

        AssertInvalid(model, nameof(SaveClippingRequestDTO.Text));
    }

    [Theory]
    [InlineData("token")]
    public void MobileRefreshTokenRequestValidatesRefreshToken(string refreshToken)
    {
        AssertValid(new GetMobileRefreshTokenRequestDTO { RefreshToken = refreshToken, DeviceId = "device-id" });
    }

    [Theory]
    [InlineData("")]
    public void MobileRefreshTokenRequestRejectsEmptyRefreshToken(string refreshToken)
    {
        AssertInvalid(new GetMobileRefreshTokenRequestDTO { RefreshToken = refreshToken, DeviceId = "device-id" }, nameof(GetMobileRefreshTokenRequestDTO.RefreshToken));
    }

    [Fact]
    public void GoogleMobileRequestValidatesIdToken()
    {
        AssertValid(new CompleteGoogleOAuthMobileRequestDTO
        {
            IdToken = "id-token",
            DeviceId = "device-id",
            Platform = Platform.Android,
        });
        AssertInvalid(new CompleteGoogleOAuthMobileRequestDTO
        {
            IdToken = string.Empty,
            DeviceId = "device-id",
            Platform = Platform.Android,
        }, nameof(CompleteGoogleOAuthMobileRequestDTO.IdToken));
    }

    [Fact]
    public void GoogleDesktopRequestValidatesCode()
    {
        AssertValid(new CompleteGoogleOAuthDesktopRequestDTO { Code = "code", CodeVerifier = "verifier", DeviceId = "device-id" });
        AssertInvalid(new CompleteGoogleOAuthDesktopRequestDTO { Code = string.Empty, CodeVerifier = "verifier", DeviceId = "device-id" }, nameof(CompleteGoogleOAuthDesktopRequestDTO.Code));
    }

    private static void AssertValid(object model)
    {
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    private static void AssertInvalid(object model, string memberName)
    {
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(memberName));
    }
}
