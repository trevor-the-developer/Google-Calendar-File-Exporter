using FluentAssertions;
using GoogleCalendarFileExporter.Services;
using GoogleCalendarFileExporter.Tests.TestUtilities;
using Xunit;

namespace GoogleCalendarFileExporter.Tests.UnitTests;

public class TimezoneServiceTests
{
    private readonly MockLoggingService _mockLogger;
    private readonly TimezoneService _timezoneService;

    public TimezoneServiceTests()
    {
        _mockLogger = new MockLoggingService();
        _timezoneService = new TimezoneService(_mockLogger);
    }

    [Theory]
    [InlineData("20241215T100000Z", "2024-12-15 10:00:00")]
    [InlineData("20241215T100000", "2024-12-15 10:00:00")]
    [InlineData("20241215", "2024-12-15 00:00:00")]
    public void ParseIcsDateTime_WithValidFormats_ShouldParseCorrectly(string dateValue, string expectedDateTime)
    {
        // Arrange
        var expected = DateTime.Parse(expectedDateTime);
        if (dateValue.EndsWith("Z")) expected = TimeZoneInfo.ConvertTimeFromUtc(expected, TimeZoneInfo.Local);

        // Act
        var result = _timezoneService.ParseIcsDateTime(dateValue);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("INVALID_DATE")]
    [InlineData("20241215T")]
    [InlineData("")]
    [InlineData("20241215T250000")]
    public void ParseIcsDateTime_WithInvalidFormats_ShouldReturnNull(string dateValue)
    {
        // Act
        var result = _timezoneService.ParseIcsDateTime(dateValue);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseIcsDateTime_WithTimezoneParameter_ShouldHandleTimezone()
    {
        // Arrange
        var dateValue = "20241215T100000";
        var parameters = "TZID=America/New_York";

        // Act
        var result = _timezoneService.ParseIcsDateTime(dateValue, parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(new DateTime(2024, 12, 15, 10, 0, 0));
    }

    [Fact]
    public void ParseIcsDateTime_WithInvalidTimezone_ShouldFallbackToLocal()
    {
        // Arrange
        var dateValue = "20241215T100000";
        var parameters = "TZID=Invalid/Timezone";

        // Act
        var result = _timezoneService.ParseIcsDateTime(dateValue, parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(new DateTime(2024, 12, 15, 10, 0, 0));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("2024-12-15 10:00:00", "2024-12-15 10:00:00")]
    [InlineData("2024-12-15 00:00:00", "2024-12-15")]
    public void FormatDateTime_WithVariousInputs_ShouldFormatCorrectly(string? dateTimeString, string expected)
    {
        // Arrange
        DateTime? dateTime = dateTimeString != null ? DateTime.Parse(dateTimeString) : null;

        // Act
        var result = TimezoneService.FormatDateTime(dateTime);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("2024-12-15 10:00:00", true, "2024-12-15 10:00:00")]
    [InlineData("2024-12-15 10:00:00", false, "2024-12-15")]
    public void FormatDateTime_WithIncludeTimeParameter_ShouldFormatCorrectly(string dateTimeString, bool includeTime,
        string expected)
    {
        // Arrange
        var dateTime = DateTime.Parse(dateTimeString);

        // Act
        var result = TimezoneService.FormatDateTime(dateTime, includeTime);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("2024-12-15 00:00:00", "2024-12-16 00:00:00", true)]
    [InlineData("2024-12-15 10:00:00", "2024-12-15 11:00:00", false)]
    [InlineData("2024-12-15 00:00:00", "2024-12-15 01:00:00", false)]
    public void IsAllDayEvent_WithVariousDateTimes_ShouldDetectCorrectly(string startString, string endString,
        bool expected)
    {
        // Arrange
        var startDateTime = DateTime.Parse(startString);
        var endDateTime = DateTime.Parse(endString);

        // Act
        var result = TimezoneService.IsAllDayEvent(startDateTime, endDateTime);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsAllDayEvent_WithNullDateTimes_ShouldReturnFalse()
    {
        // Act
        var result = TimezoneService.IsAllDayEvent(null, null);

        // Assert
        result.Should().BeFalse();
    }
}