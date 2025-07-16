# Google Calendar File Exporter

A modern .NET 9.0 console application that converts Google Calendar export files (ICS format) to multiple output
formats (CSV, JSON, Excel, XML) for easier data analysis and manipulation.

## Features

- **Multiple Input Formats**: Supports both individual `.ics` files and `.zip` archives containing multiple `.ics` files
- **Multiple Output Formats**: Export to CSV, JSON, Excel (.xlsx), and XML formats
- **Interactive & Command-Line Modes**: Run with arguments or use interactive prompts
- **Format Auto-Detection**: Automatically detects output format from file extension
- **Format Override**: Use `--format` flag to specify output format
- **Comprehensive Data Export**: Extracts all major calendar event properties
- **Robust Parsing**: Handles various ICS date formats, multi-line properties, text encoding, and malformed content
- **Professional Output**: Each format properly formatted with appropriate escaping/encoding
- **Modern Architecture**: Built with dependency injection, comprehensive logging, and extensive testing
- **Cross-Platform Timezone Support**: Intelligent timezone handling for Windows and Unix systems
- **Malformed Content Detection**: Validates ICS structure and handles corrupted files gracefully
- **Extensive Testing**: 188 unit and integration tests ensuring reliability

## Requirements

- .NET 9.0 or later
- Dependencies: ClosedXML (Excel), Newtonsoft.Json (JSON), Microsoft.Extensions.* (DI/Logging)

## Installation

1. Clone or download the project
2. Navigate to the project directory
3. Restore packages and build the project:
   ```bash
   dotnet restore
   dotnet build
   ```

## Usage

### Command Line Mode

```bash
# Process a single ICS file (auto-detects format from extension)
dotnet run calendar.ics events.csv
dotnet run calendar.ics events.json
dotnet run calendar.ics events.xlsx
dotnet run calendar.ics events.xml

# Process a ZIP file containing ICS files
dotnet run calendar-export.zip events.json

# Override format detection
dotnet run calendar.ics --format json
dotnet run calendar.ics output.txt --format csv

# Use default output filename (calendar_export_<input>.<ext>)
dotnet run calendar.ics
```

### Interactive Mode

```bash
# Run without arguments to enter interactive mode
dotnet run
```

The application will prompt you to enter the file path.

### Example Usage with Test Data

```bash
# Use the provided sample test data
dotnet run GoogleCalendarFileExporter.Tests/Fixtures/TestIcsFiles/sample-archive.zip events.json
dotnet run GoogleCalendarFileExporter.Tests/Fixtures/TestIcsFiles/simple-event.ics events.xlsx
```

## Input Formats

### ICS Files

- Individual calendar files exported from Google Calendar
- Standard iCalendar format (.ics extension)

### ZIP Archives

- ZIP files containing one or more ICS files
- Useful for bulk calendar exports
- The application will process all `.ics` files found within the archive

## Output Formats

The application supports multiple output formats, each with the same comprehensive data set:

### CSV Format

- Comma-separated values with proper escaping
- Headers included for easy import into spreadsheet applications
- Special characters and newlines properly escaped

### JSON Format

- Structured JSON with clean formatting
- Each event as a separate object in an array
- Proper JSON encoding for all text fields

### Excel Format (.xlsx)

- Professional spreadsheet format
- Formatted headers and data columns
- Ready for analysis in Microsoft Excel or similar applications

### XML Format

- Well-structured XML with proper encoding
- Each event as an `<Event>` element
- UTF-8 encoding for international characters

### Data Fields (All Formats)

| Field         | Description                                            |
|---------------|--------------------------------------------------------|
| Subject       | Event title/summary                                    |
| Start Date    | Event start date (YYYY-MM-DD)                          |
| Start Time    | Event start time (HH:MM:SS)                            |
| End Date      | Event end date (YYYY-MM-DD)                            |
| End Time      | Event end time (HH:MM:SS)                              |
| All Day Event | Boolean indicating if it's an all-day event            |
| Description   | Event description/notes                                |
| Location      | Event location                                         |
| Attendees     | List of attendee email addresses (semicolon-separated) |
| Organizer     | Event organizer email address                          |
| Status        | Event status (e.g., CONFIRMED, TENTATIVE)              |
| Recurrence    | Recurrence rule (RRULE)                                |
| Event ID      | Unique event identifier (UID)                          |
| Calendar Name | Source calendar name                                   |
| Created       | Event creation timestamp                               |
| Modified      | Last modification timestamp                            |

## Technical Details

- **Framework**: .NET 9.0
- **Dependencies**: ClosedXML (Excel), Newtonsoft.Json (JSON), Microsoft.Extensions.* (DI/Logging)
- **Architecture**: Modern layered architecture with dependency injection
- **Text Encoding**: UTF-8 for both input and output
- **Date Handling**: Comprehensive timezone support with cross-platform compatibility
- **Memory Management**: Efficient resource handling with proper disposal
- **Logging**: Structured logging with configurable levels
- **Testing**: 188 comprehensive unit and integration tests
- **Error Handling**: Robust error handling with graceful degradation

## Architecture

The application follows modern .NET practices:

### Core Components

- **Program.cs**: Entry point and argument parsing
- **ServiceContainer**: Dependency injection configuration
- **IFileProcessor**: File processing abstraction
- **IExporter**: Export format abstraction
- **ExporterFactory**: Factory pattern for format selection

### Services

- **IcsParser**: ICS file parsing with malformed content detection
- **TimezoneService**: Cross-platform timezone handling
- **LoggingService**: Structured logging wrapper
- **ConfigurationService**: Configuration management

### Exporters

- **CsvExporter**: CSV export with proper escaping
- **JsonExporter**: JSON export with configurable formatting
- **ExcelExporter**: Excel export with formatting
- **XmlExporter**: XML export with proper encoding

## Error Handling

The application includes comprehensive error handling for:

- File not found errors
- Unsupported file formats
- Corrupted or invalid ICS data
- Malformed content detection and graceful handling
- Invalid output format specifications
- Directory path validation
- General I/O exceptions
- Cross-platform timezone issues

## Testing

The solution includes extensive testing:

- **188 Total Tests**: Comprehensive coverage
- **Unit Tests**: Individual component testing
- **Integration Tests**: End-to-end workflow testing
- **Test Coverage**: Core functionality, edge cases, and error scenarios
- **Test Frameworks**: xUnit, FluentAssertions, Moq
- **Continuous Testing**: All tests pass consistently

### Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test categories
dotnet test --filter "Category=Unit"
```

## Configuration

The application supports JSON-based configuration through `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": "Information",
    "EnableConsoleLogging": true
  },
  "Export": {
    "DefaultFormat": "csv",
    "SortEventsByDate": true
  },
  "Processing": {
    "ValidateIcsContent": true,
    "EnableAsyncProcessing": true
  }
}
```

## Sample Data

The `GoogleCalendarFileExporter.Tests/Fixtures/TestIcsFiles/` folder contains sample test data:

- `sample-archive.zip` - Example ZIP archive with multiple ICS files
- `simple-event.ics` - Example individual ICS file
- `all-day-event.ics` - Example all-day event ICS file

## Project Structure

```
GoogleCalendarFileExporter/
├── Program.cs                      # Main entry point
├── appsettings.json               # Configuration file
├── GoogleCalendarFileExporter.csproj # Project file
├── Classes/
│   ├── CalendarEvent.cs           # Data model
│   ├── IcsParser.cs               # ICS parsing with malformed content detection
│   ├── FileProcessor.cs           # File handling (ICS/ZIP)
│   └── ExporterFactory.cs         # Export format factory
├── Interfaces/
│   ├── IExporter.cs               # Export interface
│   ├── IExporterAsync.cs          # Async export interface
│   ├── IFileProcessor.cs          # File processing interface
│   └── ILoggingService.cs         # Logging service interface
├── Exporters/
│   ├── AsyncExporterBase.cs       # Base class for async exporters
│   ├── CsvExporter.cs             # CSV export implementation
│   ├── JsonExporter.cs            # JSON export implementation
│   ├── ExcelExporter.cs           # Excel export implementation
│   └── XmlExporter.cs             # XML export implementation
├── Services/
│   ├── ServiceContainer.cs        # Dependency injection container
│   ├── LoggingService.cs          # Structured logging service
│   ├── TimezoneService.cs         # Cross-platform timezone handling
│   └── ConfigurationService.cs    # Configuration management
└── Models/
    └── ExporterConfiguration.cs   # Configuration models

GoogleCalendarFileExporter.Tests/
├── GoogleCalendarFileExporter.Tests.csproj # Test project file
├── UnitTests/                     # Unit tests for individual components
│   ├── CalendarEventTests.cs
│   ├── ConfigurationServiceTests.cs
│   ├── CsvExporterTests.cs
│   ├── ExcelExporterTests.cs
│   ├── ExporterFactoryTests.cs
│   ├── FileProcessorTests.cs
│   ├── IcsParserTests.cs
│   ├── JsonExporterTests.cs
│   ├── TimezoneServiceTests.cs
│   └── XmlExporterTests.cs
├── IntegrationTests/              # End-to-end integration tests
│   ├── EndToEndTests.cs
│   └── FileProcessingTests.cs
├── TestUtilities/                 # Test helpers and utilities
│   ├── MockLoggingService.cs
│   ├── TestEventBuilder.cs
│   └── TestFileHelper.cs
└── Fixtures/                      # Test data and fixtures
    ├── TestData.cs
    └── TestIcsFiles/
        ├── all-day-event.ics      # Sample all-day event
        ├── sample-archive.zip     # Sample ZIP archive
        └── simple-event.ics       # Sample simple event
```

## Development

To modify or extend the application:

### Core Components

1. **Main Logic**: `Program.cs` contains entry point and orchestration
2. **Data Model**: `Classes/CalendarEvent.cs` defines the event structure
3. **Parsing**: `Classes/IcsParser.cs` handles ICS file parsing with malformed content detection
4. **Export Interface**: `Interfaces/IExporter.cs` defines the export contract
5. **Export Factory**: `Classes/ExporterFactory.cs` manages format selection
6. **Format Implementations**: `Exporters/` folder contains format-specific exporters
7. **Services**: `Services/` folder contains cross-cutting concerns (logging, timezone, config)

### Adding New Export Formats

1. Create a new exporter class in `Exporters/` implementing `IExporter`
2. Extend `AsyncExporterBase` for async support
3. Add the format to `ExporterFactory.cs`
4. Add comprehensive tests in `GoogleCalendarFileExporter.Tests/UnitTests/`
5. Update supported formats list and documentation

### Development Workflow

1. **Setup**: `dotnet restore && dotnet build`
2. **Run Tests**: `dotnet test`
3. **Run Application**: `dotnet run`
4. **Debug**: Use Visual Studio, VS Code, or JetBrains Rider

### Key Features for Extension

- **Dependency Injection**: Easy to add new services
- **Async Support**: All exporters support async operations
- **Comprehensive Logging**: Structured logging throughout
- **Configuration**: JSON-based configuration system
- **Testing**: Extensive test coverage for reliability
- **Error Handling**: Robust error handling patterns

## Contributing

Contributions are welcome! Please ensure:

1. **Tests**: Add comprehensive tests for new features
2. **Documentation**: Update README and code comments
3. **Code Style**: Follow existing patterns and conventions
4. **Error Handling**: Include proper error handling
5. **Logging**: Add appropriate logging statements

## License

This project is provided as-is for educational and utility purposes.
