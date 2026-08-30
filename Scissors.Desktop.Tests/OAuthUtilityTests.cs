using Xunit;

namespace Scissors.Desktop.Tests;

public class OAuthUtilityTests
{
    [Fact]
    public void GenerateStateProducesAUrlSafeValue()
    {
        var value = OAuthUtility.GenerateState();

        Assert.True(value.Length >= 43);
        Assert.DoesNotContain('+', value);
        Assert.DoesNotContain('/', value);
        Assert.DoesNotContain('=', value);
    }

    [Fact]
    public void GenerateCodeVerifierProducesDifferentValues()
    {
        var first = OAuthUtility.GenerateCodeVerifier();
        var second = OAuthUtility.GenerateCodeVerifier();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GenerateCodeChallengeIsDeterministicAndUrlSafe()
    {
        var verifier = "sample-verifier-value";

        var first = OAuthUtility.GenerateCodeChallenge(verifier);
        var second = OAuthUtility.GenerateCodeChallenge(verifier);

        Assert.Equal(first, second);
        Assert.DoesNotContain('+', first);
        Assert.DoesNotContain('/', first);
        Assert.DoesNotContain('=', first);
    }
}
