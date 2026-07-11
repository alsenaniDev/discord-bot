using DiscordBot.Shared;
using FluentAssertions;
using Xunit;

namespace DiscordBot.Activities.UnitTests;

public class OperationResultTests
{
    [Fact]
    public void Fail_can_carry_feature_error_metadata()
    {
        var result = OperationResult<object>.Fail("غير متاح حاليًا.", 501, "feature_not_available", "roulette_power_ups");

        result.Succeeded.Should().BeFalse();
        result.Code.Should().Be("feature_not_available");
        result.Feature.Should().Be("roulette_power_ups");
        result.StatusCode.Should().Be(501);
    }
}
