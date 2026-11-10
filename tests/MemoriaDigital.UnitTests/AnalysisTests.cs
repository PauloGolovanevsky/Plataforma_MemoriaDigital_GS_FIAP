using MemoriaDigital.Domain.Entities;
using MemoriaDigital.Domain.Enums;

namespace MemoriaDigital.UnitTests;

public sealed class AnalysisTests
{
    [Fact]
    public void CreateAnalysis_WithValidInput_StartsProcessing()
    {
        // Arrange
        var input = new AnalysisInput(InputType.Message, content: "Olá, gostaria de confirmar a reunião.");

        // Act
        var analysis = new Analysis(input);

        // Assert
        Assert.Equal(input, analysis.Input);
        Assert.Equal(AnalysisStatus.Processing, analysis.Status);
        Assert.Null(analysis.RiskLevel);
        Assert.Null(analysis.CompletedAt);
        Assert.Empty(analysis.Indicators);
        Assert.Empty(analysis.Recommendations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateMessageInput_WithEmptyContent_Throws(string? content)
    {
        // Arrange, Act and Assert
        Assert.Throws<ArgumentException>(() => new AnalysisInput(InputType.Message, content: content));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("/relative/path")]
    public void CreateLinkInput_WithInvalidUrl_Throws(string? content)
    {
        // Arrange, Act and Assert
        Assert.Throws<ArgumentException>(() => new AnalysisInput(InputType.Link, content: content));
    }

    [Fact]
    public void CreateImageInput_WithoutReferenceOrMinimumMetadata_Throws()
    {
        // Arrange, Act and Assert
        Assert.Throws<ArgumentException>(() => new AnalysisInput(InputType.Image));
    }

    [Fact]
    public void CreateLinkInput_WithAbsoluteUrl_Succeeds()
    {
        // Arrange, Act
        var input = new AnalysisInput(InputType.Link, content: "https://example.com/account");

        // Assert
        Assert.Equal(InputType.Link, input.Type);
        Assert.Equal("https://example.com/account", input.Content);
    }

    [Fact]
    public void CreateImageInput_WithReference_Succeeds()
    {
        // Arrange, Act
        var input = new AnalysisInput(InputType.Image, reference: "uploads/image-001.png");

        // Assert
        Assert.Equal(InputType.Image, input.Type);
        Assert.Equal("uploads/image-001.png", input.Reference);
    }

    [Fact]
    public void Processing_CanTransitionToCompleted()
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act
        analysis.Complete();

        // Assert
        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        Assert.NotNull(analysis.CompletedAt);
        Assert.NotNull(analysis.RiskLevel);
    }

    [Theory]
    [InlineData(AnalysisStatus.Inconclusive)]
    [InlineData(AnalysisStatus.Unsupported)]
    public void Processing_CanTransitionToNonRiskFinalStates(AnalysisStatus expectedStatus)
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act
        if (expectedStatus == AnalysisStatus.Inconclusive)
        {
            analysis.MarkInconclusive();
        }
        else
        {
            analysis.MarkUnsupported();
        }

        // Assert
        Assert.Equal(expectedStatus, analysis.Status);
        Assert.NotNull(analysis.CompletedAt);
        Assert.Null(analysis.RiskLevel);
        Assert.Null(analysis.FailureReason);
    }

    [Fact]
    public void Processing_CanTransitionToFailedWithInternalReason()
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act
        analysis.MarkFailed("Internal processing failure");

        // Assert
        Assert.Equal(AnalysisStatus.Failed, analysis.Status);
        Assert.NotNull(analysis.CompletedAt);
        Assert.Null(analysis.RiskLevel);
        Assert.Equal("Internal processing failure", analysis.FailureReason);
    }

    [Fact]
    public void MarkFailed_WithEmptyReason_Throws()
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act and Assert
        Assert.Throws<ArgumentException>(() => analysis.MarkFailed(" "));
        Assert.Equal(AnalysisStatus.Processing, analysis.Status);
        Assert.Null(analysis.CompletedAt);
    }

    [Theory]
    [InlineData(0, RiskLevel.Low)]
    [InlineData(2, RiskLevel.Low)]
    [InlineData(3, RiskLevel.Medium)]
    [InlineData(5, RiskLevel.Medium)]
    [InlineData(6, RiskLevel.High)]
    public void Complete_ClassifiesRiskAtScoreBoundaries(int score, RiskLevel expectedRisk)
    {
        // Arrange
        var analysis = CreateAnalysis();
        AddIndicatorsForScore(analysis, score);

        // Act
        analysis.Complete();

        // Assert
        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        Assert.Equal(expectedRisk, analysis.RiskLevel);
    }

    [Fact]
    public void Complete_WithHighIndicator_AlwaysReturnsHighRisk()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.AddIndicator(CreateIndicator("high", IndicatorSeverity.High));

        // Act
        analysis.Complete();

        // Assert
        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        Assert.Equal(RiskLevel.High, analysis.RiskLevel);
    }

    [Fact]
    public void FinalizedAnalysis_CannotAddIndicatorsOrRecommendations()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.Complete();

        // Act and Assert
        Assert.Throws<InvalidOperationException>(() =>
            analysis.AddIndicator(CreateIndicator("late", IndicatorSeverity.Low)));
        Assert.Throws<InvalidOperationException>(() =>
            analysis.AddRecommendation(new Recommendation("late", "Não deveria ser adicionada.")));
    }

    [Fact]
    public void FinalizedAnalysis_CannotChangeClassification()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.AddIndicator(CreateIndicator("medium", IndicatorSeverity.Medium));
        analysis.Complete();
        var completedAt = analysis.CompletedAt;

        // Act and Assert
        Assert.Throws<InvalidOperationException>(() => analysis.Complete());
        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        Assert.Equal(RiskLevel.Medium, analysis.RiskLevel);
        Assert.Equal(completedAt, analysis.CompletedAt);
    }

    [Fact]
    public void FinalStates_DoNotExposeFailureReasonExceptFailed()
    {
        // Arrange
        var completed = CreateAnalysis();
        var inconclusive = CreateAnalysis();
        var unsupported = CreateAnalysis();
        var failed = CreateAnalysis();

        // Act
        completed.Complete();
        inconclusive.MarkInconclusive();
        unsupported.MarkUnsupported();
        failed.MarkFailed("Internal failure");

        // Assert
        Assert.Null(completed.FailureReason);
        Assert.Null(inconclusive.FailureReason);
        Assert.Null(unsupported.FailureReason);
        Assert.Equal("Internal failure", failed.FailureReason);
    }

    private static Analysis CreateAnalysis() =>
        new(new AnalysisInput(InputType.Message, content: "Mensagem válida para análise."));

    private static Indicator CreateIndicator(string code, IndicatorSeverity severity) =>
        new(code, $"Indicador {code}", severity);

    private static void AddIndicatorsForScore(Analysis analysis, int score)
    {
        switch (score)
        {
            case 0:
                return;
            case 1:
                analysis.AddIndicator(CreateIndicator("low-1", IndicatorSeverity.Low));
                return;
            case 2:
                analysis.AddIndicator(CreateIndicator("low-1", IndicatorSeverity.Low));
                analysis.AddIndicator(CreateIndicator("low-2", IndicatorSeverity.Low));
                return;
            case 3:
                analysis.AddIndicator(CreateIndicator("medium-1", IndicatorSeverity.Medium));
                return;
            case 5:
                analysis.AddIndicator(CreateIndicator("medium-1", IndicatorSeverity.Medium));
                analysis.AddIndicator(CreateIndicator("low-1", IndicatorSeverity.Low));
                analysis.AddIndicator(CreateIndicator("low-2", IndicatorSeverity.Low));
                return;
            case 6:
                analysis.AddIndicator(CreateIndicator("medium-1", IndicatorSeverity.Medium));
                analysis.AddIndicator(CreateIndicator("medium-2", IndicatorSeverity.Medium));
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(score));
        }
    }
}
