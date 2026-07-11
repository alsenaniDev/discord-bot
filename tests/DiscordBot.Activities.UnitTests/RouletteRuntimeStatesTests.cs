using DiscordBot.Activities.Domain.Roulette;
using FluentAssertions;
using Xunit;

namespace DiscordBot.Activities.UnitTests;

public class RouletteRuntimeStatesTests
{
    [Theory]
    [InlineData(RouletteRuntimeStates.WaitingForPlayers, RouletteRuntimeStates.BettingOpen)]
    [InlineData(RouletteRuntimeStates.WaitingForPlayers, RouletteRuntimeStates.Cancelled)]
    [InlineData(RouletteRuntimeStates.BettingOpen, RouletteRuntimeStates.Spinning)]
    [InlineData(RouletteRuntimeStates.Spinning, RouletteRuntimeStates.Settling)]
    [InlineData(RouletteRuntimeStates.Settling, RouletteRuntimeStates.Completed)]
    public void CanTransition_allows_valid_transitions(string current, string next)
    {
        RouletteRuntimeStates.CanTransition(current, next).Should().BeTrue();
    }

    [Theory]
    [InlineData(RouletteRuntimeStates.Completed, RouletteRuntimeStates.BettingOpen)]
    [InlineData(RouletteRuntimeStates.Cancelled, RouletteRuntimeStates.BettingOpen)]
    [InlineData(RouletteRuntimeStates.Expired, RouletteRuntimeStates.BettingOpen)]
    [InlineData(RouletteRuntimeStates.WaitingForPlayers, RouletteRuntimeStates.Completed)]
    public void CanTransition_rejects_invalid_transitions(string current, string next)
    {
        RouletteRuntimeStates.CanTransition(current, next).Should().BeFalse();
    }

    [Fact]
    public void IsTerminal_identifies_closed_states()
    {
        RouletteRuntimeStates.IsTerminal(RouletteRuntimeStates.Completed).Should().BeTrue();
        RouletteRuntimeStates.IsTerminal(RouletteRuntimeStates.Cancelled).Should().BeTrue();
        RouletteRuntimeStates.IsTerminal(RouletteRuntimeStates.Expired).Should().BeTrue();
        RouletteRuntimeStates.IsTerminal(RouletteRuntimeStates.BettingOpen).Should().BeFalse();
    }
}
