using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class BlockCountersTests
{
    [TestMethod]
    [TestCategory("T-P1-17")]
    public void Total_SumWithinInt64_ReturnsSum()
    {
        // Arrange
        var counters = new BlockCounters(54, 2);

        // Act
        var total = counters.Total;

        // Assert
        total.Should().Be(56);
    }

    [TestMethod]
    [TestCategory("T-P1-17")]
    public void Total_SumExceedsInt64_ThrowsOverflowException()
    {
        // Arrange
        var counters = new BlockCounters(long.MaxValue, long.MaxValue);

        // Act
        Action act = () => _ = counters.Total;

        // Assert
        act.Should().Throw<OverflowException>();
    }
}
