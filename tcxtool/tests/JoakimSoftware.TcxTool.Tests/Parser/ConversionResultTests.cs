using JoakimSoftware.TcxTool.Parser.Conversion;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies the result and error types that cross the parser boundary (data-model.md "Errors",
/// "Results"; constitution Principle III).
/// </summary>
public class ConversionResultTests
{
    [Fact]
    public void SuccessCarriesTheSavedFiles()
    {
        SavedCsv[] files = [new("2026-11-01_run.csv", 10, "2026-11-01_run.states.csv", 2), new("2026-11-01_ride.csv", 0, "2026-11-01_ride.states.csv", 0)];

        var result = ConversionResult.Success(files);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Files.Should().Equal(files);
    }

    [Fact]
    public void FailureCarriesTheErrorAndNoFiles()
    {
        var error = new TcxError(TcxErrorKind.Malformed, "message", null, null);

        var result = ConversionResult.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
        result.Files.Should().BeEmpty();
    }

    [Fact]
    public void ErrorKeepsItsDetails()
    {
        var error = new TcxError(TcxErrorKind.InvalidValue, "message", "HeartRateBpm", 3);

        error.Kind.Should().Be(TcxErrorKind.InvalidValue);
        error.Message.Should().Be("message");
        error.Element.Should().Be("HeartRateBpm");
        error.TrackpointNumber.Should().Be(3);
    }

    [Fact]
    public void EveryKindHasItsOwnNonEmptyMessage()
    {
        var messages = Enum.GetValues<TcxErrorKind>()
            .Select(kind => TcxErrorMessages.For(kind, maxUploadBytes: 52_428_800, element: "HeartRateBpm", trackpointNumber: 3))
            .ToList();

        messages.Should().OnlyContain(message => !string.IsNullOrWhiteSpace(message));
        messages.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void InvalidValueMessageNamesTheElementAndTrackpoint()
    {
        var message = TcxErrorMessages.For(TcxErrorKind.InvalidValue, element: "HeartRateBpm", trackpointNumber: 3);

        message.Should().Contain("HeartRateBpm").And.Contain("trackpoint 3");
    }

    [Fact]
    public void TooLargeMessageStatesTheLimitInMegabytes()
    {
        TcxErrorMessages.For(TcxErrorKind.TooLarge, maxUploadBytes: 52_428_800).Should().Contain("50 MB");
    }

    [Fact]
    public void NoFileMessageAsksForAFile()
    {
        TcxErrorMessages.For(TcxErrorKind.NoFile).Should().Be("Choose a .tcx file to convert.");
    }
}
