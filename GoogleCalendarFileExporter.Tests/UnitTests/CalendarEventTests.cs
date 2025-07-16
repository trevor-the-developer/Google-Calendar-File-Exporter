using FluentAssertions;
using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Tests.TestUtilities;
using Xunit;

namespace GoogleCalendarFileExporter.Tests.UnitTests;

public class CalendarEventTests
{
    [Fact]
    public void CalendarEvent_DefaultConstruction_ShouldInitializeEmptyAttendeesList()
    {
        // Act
        var calendarEvent = new CalendarEvent();

        // Assert
        calendarEvent.Attendees.Should().NotBeNull();
        calendarEvent.Attendees.Should().BeEmpty();
    }

    [Fact]
    public void CalendarEvent_AllProperties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var expectedSummary = "Test Event";
        var expectedDescription = "Test Description";
        var expectedLocation = "Test Location";
        var expectedStartDateTime = DateTime.Now;
        var expectedEndDateTime = DateTime.Now.AddHours(1);
        var expectedCreated = DateTime.Now.AddDays(-1);
        var expectedLastModified = DateTime.Now.AddHours(-1);
        var expectedUid = "test-uid";
        var expectedStatus = "CONFIRMED";
        var expectedOrganizer = "organizer@example.com";
        var expectedAttendees = new List<string> { "attendee1@example.com", "attendee2@example.com" };
        var expectedRecurrenceRule = "FREQ=WEEKLY";
        var expectedCalendarName = "Test Calendar";

        // Act
        var calendarEvent = new CalendarEvent
        {
            Summary = expectedSummary,
            Description = expectedDescription,
            Location = expectedLocation,
            StartDateTime = expectedStartDateTime,
            EndDateTime = expectedEndDateTime,
            Created = expectedCreated,
            LastModified = expectedLastModified,
            Uid = expectedUid,
            Status = expectedStatus,
            Organizer = expectedOrganizer,
            Attendees = expectedAttendees,
            RecurrenceRule = expectedRecurrenceRule,
            CalendarName = expectedCalendarName
        };

        // Assert
        calendarEvent.Summary.Should().Be(expectedSummary);
        calendarEvent.Description.Should().Be(expectedDescription);
        calendarEvent.Location.Should().Be(expectedLocation);
        calendarEvent.StartDateTime.Should().Be(expectedStartDateTime);
        calendarEvent.EndDateTime.Should().Be(expectedEndDateTime);
        calendarEvent.Created.Should().Be(expectedCreated);
        calendarEvent.LastModified.Should().Be(expectedLastModified);
        calendarEvent.Uid.Should().Be(expectedUid);
        calendarEvent.Status.Should().Be(expectedStatus);
        calendarEvent.Organizer.Should().Be(expectedOrganizer);
        calendarEvent.Attendees.Should().BeEquivalentTo(expectedAttendees);
        calendarEvent.RecurrenceRule.Should().Be(expectedRecurrenceRule);
        calendarEvent.CalendarName.Should().Be(expectedCalendarName);
    }

    [Fact]
    public void CalendarEvent_NullableProperties_ShouldAcceptNullValues()
    {
        // Act
        var calendarEvent = new CalendarEvent
        {
            Summary = null,
            Description = null,
            Location = null,
            StartDateTime = null,
            EndDateTime = null,
            Created = null,
            LastModified = null,
            Uid = null,
            Status = null,
            Organizer = null,
            RecurrenceRule = null,
            CalendarName = null
        };

        // Assert
        calendarEvent.Summary.Should().BeNull();
        calendarEvent.Description.Should().BeNull();
        calendarEvent.Location.Should().BeNull();
        calendarEvent.StartDateTime.Should().BeNull();
        calendarEvent.EndDateTime.Should().BeNull();
        calendarEvent.Created.Should().BeNull();
        calendarEvent.LastModified.Should().BeNull();
        calendarEvent.Uid.Should().BeNull();
        calendarEvent.Status.Should().BeNull();
        calendarEvent.Organizer.Should().BeNull();
        calendarEvent.RecurrenceRule.Should().BeNull();
        calendarEvent.CalendarName.Should().BeNull();
    }

    [Fact]
    public void CalendarEvent_AttendeesCollection_ShouldBeModifiable()
    {
        // Arrange
        var calendarEvent = new CalendarEvent();
        var attendees = new[] { "attendee1@example.com", "attendee2@example.com" };

        // Act
        calendarEvent.Attendees.AddRange(attendees);

        // Assert
        calendarEvent.Attendees.Should().HaveCount(2);
        calendarEvent.Attendees.Should().Contain("attendee1@example.com");
        calendarEvent.Attendees.Should().Contain("attendee2@example.com");
    }

    [Fact]
    public void CalendarEvent_WithTestEventBuilder_ShouldCreateValidEvent()
    {
        // Act
        var calendarEvent = TestEventBuilder.Create()
            .WithSummary("Test Meeting")
            .WithDescription("Test Description")
            .WithLocation("Test Location")
            .WithTimeRange(DateTime.Now, DateTime.Now.AddHours(1))
            .WithAttendees("attendee1@example.com", "attendee2@example.com")
            .WithOrganizer("organizer@example.com")
            .WithStatus("CONFIRMED")
            .Build();

        // Assert
        calendarEvent.Summary.Should().Be("Test Meeting");
        calendarEvent.Description.Should().Be("Test Description");
        calendarEvent.Location.Should().Be("Test Location");
        calendarEvent.StartDateTime.Should().NotBeNull();
        calendarEvent.EndDateTime.Should().NotBeNull();
        calendarEvent.Attendees.Should().HaveCount(2);
        calendarEvent.Organizer.Should().Be("organizer@example.com");
        calendarEvent.Status.Should().Be("CONFIRMED");
    }
}