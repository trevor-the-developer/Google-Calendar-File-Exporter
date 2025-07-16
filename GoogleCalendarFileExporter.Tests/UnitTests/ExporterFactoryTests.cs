using FluentAssertions;
using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Exporters;
using GoogleCalendarFileExporter.Interfaces;
using Xunit;

namespace GoogleCalendarFileExporter.Tests.UnitTests;

public class ExporterFactoryTests
{
    [Theory]
    [InlineData("csv", typeof(CsvExporter))]
    [InlineData("CSV", typeof(CsvExporter))]
    [InlineData("json", typeof(JsonExporter))]
    [InlineData("JSON", typeof(JsonExporter))]
    [InlineData("xlsx", typeof(ExcelExporter))]
    [InlineData("XLSX", typeof(ExcelExporter))]
    [InlineData("xml", typeof(XmlExporter))]
    [InlineData("XML", typeof(XmlExporter))]
    public void CreateExporter_WithValidFormat_ShouldReturnCorrectExporter(string format, Type expectedType)
    {
        // Act
        var exporter = ExporterFactory.CreateExporter(format);

        // Assert
        exporter.Should().NotBeNull();
        exporter.Should().BeOfType(expectedType);
    }

    [Theory]
    [InlineData("txt")]
    [InlineData("pdf")]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateExporter_WithInvalidFormat_ShouldThrowArgumentException(string format)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ExporterFactory.CreateExporter(format));
        exception.Message.Should().Contain("Unsupported export format");
    }

    [Fact]
    public void CreateExporter_WithNullFormat_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ExporterFactory.CreateExporter(null!));
        exception.Message.Should().Contain("Unsupported export format");
    }

    [Fact]
    public void GetSupportedFormats_ShouldReturnAllSupportedFormats()
    {
        // Act
        var supportedFormats = ExporterFactory.GetSupportedFormats();

        // Assert
        supportedFormats.Should().NotBeNull();
        supportedFormats.Should().HaveCount(4);
        supportedFormats.Should().Contain("csv");
        supportedFormats.Should().Contain("json");
        supportedFormats.Should().Contain("xlsx");
        supportedFormats.Should().Contain("xml");
    }

    [Fact]
    public void GetSupportedFormats_ShouldReturnArray()
    {
        // Act
        var supportedFormats = ExporterFactory.GetSupportedFormats();

        // Assert
        supportedFormats.Should().BeAssignableTo<string[]>();
    }

    [Fact]
    public void CreateExporterFromFilePath_WithValidFilePaths_ShouldReturnCorrectExporter()
    {
        // Arrange
        var testCases = new[]
        {
            ("test.csv", typeof(CsvExporter)),
            ("test.json", typeof(JsonExporter)),
            ("test.xlsx", typeof(ExcelExporter)),
            ("test.xml", typeof(XmlExporter)),
            ("test.CSV", typeof(CsvExporter)),
            ("test.JSON", typeof(JsonExporter)),
            ("test.XLSX", typeof(ExcelExporter)),
            ("test.XML", typeof(XmlExporter))
        };

        // Act & Assert
        foreach (var (filePath, expectedType) in testCases)
        {
            var exporter = ExporterFactory.CreateExporterFromFilePath(filePath);
            exporter.Should().NotBeNull();
            exporter.Should().BeOfType(expectedType);
        }
    }

    [Fact]
    public void CreateExporterFromFilePath_WithUnsupportedExtension_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ExporterFactory.CreateExporterFromFilePath("test.txt"));
        exception.Message.Should().Contain("Unsupported export format");
    }

    [Fact]
    public void CreateExporter_MultipleCallsForSameFormat_ShouldReturnDifferentInstances()
    {
        // Act
        var exporter1 = ExporterFactory.CreateExporter("csv");
        var exporter2 = ExporterFactory.CreateExporter("csv");

        // Assert
        exporter1.Should().NotBeNull();
        exporter2.Should().NotBeNull();
        exporter1.Should().NotBeSameAs(exporter2);
        exporter1.Should().BeOfType<CsvExporter>();
        exporter2.Should().BeOfType<CsvExporter>();
    }

    [Fact]
    public void CreateExporter_WithDifferentCasing_ShouldReturnSameType()
    {
        // Act
        var csvExporter = ExporterFactory.CreateExporter("csv");
        var CSV_Exporter = ExporterFactory.CreateExporter("CSV");
        var CsV_Exporter = ExporterFactory.CreateExporter("CsV");

        // Assert
        csvExporter.Should().BeOfType<CsvExporter>();
        CSV_Exporter.Should().BeOfType<CsvExporter>();
        CsV_Exporter.Should().BeOfType<CsvExporter>();
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("json")]
    [InlineData("xlsx")]
    [InlineData("xml")]
    public void CreateExporter_WithAllSupportedFormats_ShouldReturnValidExporters(string format)
    {
        // Act
        var exporter = ExporterFactory.CreateExporter(format);

        // Assert
        exporter.Should().NotBeNull();
        exporter.Should().BeAssignableTo<IExporter>();
    }

    [Fact]
    public void GetSupportedFormatsString_ShouldReturnFormattedString()
    {
        // Act
        var formatsString = ExporterFactory.GetSupportedFormatsString();

        // Assert
        formatsString.Should().NotBeNull();
        formatsString.Should().Contain("csv");
        formatsString.Should().Contain("json");
        formatsString.Should().Contain("xlsx");
        formatsString.Should().Contain("xml");
        formatsString.Should().Contain(",");
    }
}