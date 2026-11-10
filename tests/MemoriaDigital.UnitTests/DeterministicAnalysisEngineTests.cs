using MemoriaDigital.Application.Services;
using MemoriaDigital.Domain.Entities;
using MemoriaDigital.Domain.Enums;

namespace MemoriaDigital.UnitTests;

public sealed class DeterministicAnalysisEngineTests
{
    private readonly DeterministicAnalysisEngine _engine = new();

    [Fact]
    public void Message_RequestingCredentials_CreatesHighIndicatorAndRecommendation()
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Message, "Informe sua senha e o código de autenticação.");

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        Assert.Equal(RiskLevel.High, analysis.RiskLevel);
        Assert.Contains(analysis.Indicators, indicator => indicator.Code == "MSG_CREDENTIAL_REQUEST");
        Assert.Contains(analysis.Recommendations, recommendation => recommendation.Code == "REC_PROTECT_CREDENTIALS");
    }

    [Fact]
    public void Message_WarningAgainstSharingCredentials_DoesNotCreateCredentialIndicator()
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Message, "Nunca compartilhe sua senha com terceiros.");

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Equal(RiskLevel.Low, analysis.RiskLevel);
        Assert.DoesNotContain(analysis.Indicators, indicator => indicator.Code == "MSG_CREDENTIAL_REQUEST");
    }

    [Fact]
    public void Message_UrgentPayment_CreatesHighIndicator()
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Message, "Faça o pagamento Pix agora, é urgente.");

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Contains(analysis.Indicators, indicator => indicator.Code == "MSG_URGENT_PAYMENT");
        Assert.Equal(RiskLevel.High, analysis.RiskLevel);
    }

    [Fact]
    public void Message_AccessThreat_CreatesMediumIndicator()
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Message, "Sua conta será bloqueada agora se não confirmar.");

        // Act
        _engine.Analyze(analysis);

        // Assert
        var indicator = Assert.Single(analysis.Indicators);
        Assert.Equal("MSG_ACCESS_THREAT", indicator.Code);
        Assert.Equal(IndicatorSeverity.Medium, indicator.Severity);
        Assert.Equal(RiskLevel.Medium, analysis.RiskLevel);
    }

    [Fact]
    public void Message_ConditionalPrize_CreatesHighIndicator()
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Message, "Você ganhou um prêmio, pague a taxa agora para receber.");

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Contains(analysis.Indicators, indicator => indicator.Code == "MSG_CONDITIONAL_PRIZE");
        Assert.Equal(RiskLevel.High, analysis.RiskLevel);
    }

    [Fact]
    public void Message_WithUrl_CreatesLowUrlIndicator()
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Message, "Consulte https://example.com para mais informações.");

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Contains(analysis.Indicators, indicator => indicator.Code == "MSG_URL_PRESENT");
        Assert.Equal(IndicatorSeverity.Low, analysis.Indicators.Single().Severity);
        Assert.Equal(RiskLevel.Low, analysis.RiskLevel);
    }

    [Fact]
    public void Message_WithUrlFollowedByPunctuation_StoresCleanUrlEvidence()
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Message, "Acesse https://example.com.");

        // Act
        _engine.Analyze(analysis);

        // Assert
        var indicator = Assert.Single(analysis.Indicators);
        Assert.Equal("https://example.com", indicator.Evidence);
    }

    [Fact]
    public void Message_WithoutSignals_CompletesWithLowRisk()
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Message, "Olá, podemos conversar amanhã.");

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        Assert.Equal(RiskLevel.Low, analysis.RiskLevel);
        Assert.Empty(analysis.Indicators);
        Assert.Empty(analysis.Recommendations);
    }

    [Fact]
    public void Message_WithInsufficientAnalyzableContent_IsInconclusive()
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Message, "...---!!!");

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Equal(AnalysisStatus.Inconclusive, analysis.Status);
        Assert.NotNull(analysis.CompletedAt);
        Assert.Null(analysis.RiskLevel);
    }

    [Fact]
    public void Message_WithMediumAndLowIndicators_UsesCombinedScore()
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Message, "Sua conta será bloqueada agora. Consulte https://example.com.");

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Equal(RiskLevel.Medium, analysis.RiskLevel);
        Assert.Contains(analysis.Indicators, indicator => indicator.Code == "MSG_ACCESS_THREAT");
        Assert.Contains(analysis.Indicators, indicator => indicator.Code == "MSG_URL_PRESENT");
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://unknown-domain.example")]
    public void SafeOrUnknownDomain_DoesNotCreateSuspiciousIndicators(string url)
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Link, url);

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        Assert.Equal(RiskLevel.Low, analysis.RiskLevel);
        Assert.Empty(analysis.Indicators);
    }

    [Theory]
    [InlineData("https://192.168.0.1/login", "LINK_IP_ADDRESS")]
    [InlineData("https://bit.ly/abc", "LINK_SHORTENER")]
    [InlineData("https://xn--exmple-cua.com", "LINK_IDN_DOMAIN")]
    [InlineData("https://paypal-security.example", "LINK_BRAND_IMPERSONATION")]
    public void SuspiciousLinkPattern_CreatesExpectedIndicator(string url, string indicatorCode)
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Link, url);

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        Assert.Contains(analysis.Indicators, indicator => indicator.Code == indicatorCode);
        Assert.NotEmpty(analysis.Recommendations);
    }

    [Fact]
    public void Link_WithInvalidIpAddressPattern_DoesNotCreateIpIndicator()
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Link, "https://999.999.999.999/login");

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Equal(RiskLevel.Low, analysis.RiskLevel);
        Assert.DoesNotContain(analysis.Indicators, indicator => indicator.Code == "LINK_IP_ADDRESS");
    }

    [Fact]
    public void Link_WithBrandAsPartOfUnrelatedLabel_DoesNotCreateBrandIndicator()
    {
        // Arrange
        var analysis = CreateAnalysis(InputType.Link, "https://notpaypal.example");

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Equal(RiskLevel.Low, analysis.RiskLevel);
        Assert.DoesNotContain(analysis.Indicators, indicator => indicator.Code == "LINK_BRAND_IMPERSONATION");
    }

    [Fact]
    public void Image_IsUnsupportedWithoutAnalysis()
    {
        // Arrange
        var input = new AnalysisInput(InputType.Image, reference: "image.png");
        var analysis = new Analysis(input);

        // Act
        _engine.Analyze(analysis);

        // Assert
        Assert.Equal(AnalysisStatus.Unsupported, analysis.Status);
        Assert.Null(analysis.RiskLevel);
        Assert.Empty(analysis.Indicators);
        Assert.Empty(analysis.Recommendations);
    }

    private static Analysis CreateAnalysis(InputType type, string content)
    {
        var input = new AnalysisInput(type, content: content);
        return new Analysis(input);
    }
}
