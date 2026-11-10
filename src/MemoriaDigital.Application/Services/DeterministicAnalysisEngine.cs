using System.Net;
using System.Text.RegularExpressions;
using MemoriaDigital.Application.Interfaces;
using MemoriaDigital.Domain.Entities;
using MemoriaDigital.Domain.Enums;

namespace MemoriaDigital.Application.Services;

public sealed class DeterministicAnalysisEngine : IAnalysisEngine
{
    private static readonly Regex UrlRegex = new(
        @"https?://[^\s]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly (string Brand, string[] OfficialHosts)[] KnownBrands =
    [
        ("paypal", ["paypal.com"]),
        ("microsoft", ["microsoft.com"]),
        ("amazon", ["amazon.com", "amazon.com.br"]),
        ("google", ["google.com"]),
        ("apple", ["apple.com"]),
        ("nubank", ["nubank.com.br"]),
        ("itau", ["itau.com.br"]),
        ("bradesco", ["bradesco.com.br"]),
        ("caixa", ["caixa.gov.br"]),
        ("bancodobrasil", ["bb.com.br"])
    ];

    private static readonly string[] UrlShortenerHosts =
    [
        "bit.ly",
        "tinyurl.com",
        "t.co",
        "is.gd",
        "ow.ly",
        "buff.ly",
        "cutt.ly"
    ];

    public void Analyze(Analysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        switch (analysis.Input.Type)
        {
            case InputType.Message:
                AnalyzeMessage(analysis, analysis.Input.Content!);
                break;
            case InputType.Link:
                AnalyzeLink(analysis, analysis.Input.Content!);
                break;
            case InputType.Image:
                analysis.MarkUnsupported();
                break;
            default:
                analysis.MarkUnsupported();
                break;
        }
    }

    private static void AnalyzeMessage(Analysis analysis, string content)
    {
        if (!content.Any(char.IsLetterOrDigit))
        {
            analysis.MarkInconclusive();
            return;
        }

        var normalized = content.ToLowerInvariant();

        var credentialTerm = ContainsAny(normalized, "senha", "password", "código de autenticação", "codigo de autenticacao", "código de segurança", "codigo de seguranca", "credencial");
        var credentialNegation = ContainsAny(normalized, "nunca compartilhe", "não compartilhe", "nao compartilhe", "nunca forneça", "nunca forneca", "não forneça", "nao forneca");
        var credentialRequest = !credentialNegation && ContainsAny(normalized, "informe", "forneça", "forneca", "digite", "envie", "confirme", "compartilhe", "atualize", "preencha");
        if (credentialTerm && credentialRequest)
        {
            AddIndicator(
                analysis,
                "MSG_CREDENTIAL_REQUEST",
                "A mensagem solicita senha, código de autenticação ou credencial.",
                IndicatorSeverity.High,
                FindEvidence(content, "senha", "password", "código", "codigo", "credencial"));
            AddRecommendation(analysis, "REC_PROTECT_CREDENTIALS", "Não forneça senhas, códigos ou credenciais.", RecommendationPriority.Urgent);
        }

        var paymentTerm = ContainsAny(normalized, "pagamento", "pagar", "pague", "transferência", "transferencia", "pix", "depósito", "deposito");
        var paymentAction = ContainsAny(normalized, "pagar", "pague", "faça o pagamento", "faca o pagamento", "transfira", "envie", "realize", "pix");
        var paymentRequested = paymentTerm && paymentAction;
        var urgentLanguage = ContainsAny(normalized, "urgente", "imediato", "imediatamente", "agora", "última chance", "ultima chance", "prazo final");
        if (paymentRequested && urgentLanguage)
        {
            AddIndicator(
                analysis,
                "MSG_URGENT_PAYMENT",
                "A mensagem combina solicitação de pagamento ou transferência com linguagem de urgência.",
                IndicatorSeverity.High,
                FindEvidence(content, "pagamento", "pagar", "transferência", "transferencia", "pix", "urgente", "imediato", "agora"));
            AddRecommendation(analysis, "REC_DONT_PAY_URGENT", "Não realize pagamentos ou transferências sob pressão; confirme por canal oficial.", RecommendationPriority.Urgent);
        }

        if (ContainsAny(normalized, "bloqueio", "bloqueado", "bloqueada", "cancelamento", "cancelar", "perda de acesso", "perder o acesso") && urgentLanguage)
        {
            AddIndicator(
                analysis,
                "MSG_ACCESS_THREAT",
                "A mensagem ameaça bloqueio, cancelamento ou perda de acesso e pressiona por ação imediata.",
                IndicatorSeverity.Medium,
                FindEvidence(content, "bloqueio", "bloqueado", "cancelamento", "cancelar", "perda de acesso", "urgente", "imediato"));
            AddRecommendation(analysis, "REC_VERIFY_OFFICIAL_CHANNEL", "Confirme a situação diretamente no canal oficial da instituição.", RecommendationPriority.Important);
        }

        var benefitPromise = ContainsAny(normalized, "prêmio", "premio", "benefício", "beneficio", "ganhou", "ganhador", "recompensa", "sorteio");
        if (benefitPromise && (paymentRequested || ContainsAny(normalized, "forneça seus dados", "forneca seus dados", "informe seus dados", "dados pessoais")))
        {
            AddIndicator(
                analysis,
                "MSG_CONDITIONAL_PRIZE",
                "A mensagem condiciona benefício ou prêmio ao fornecimento de dados ou pagamento.",
                IndicatorSeverity.High,
                FindEvidence(content, "prêmio", "premio", "benefício", "beneficio", "ganhou", "dados", "pagamento"));
            AddRecommendation(analysis, "REC_DONT_TRUST_PRIZE", "Não forneça dados nem pague para receber um prêmio sem confirmar a origem.", RecommendationPriority.Urgent);
        }

        var urlMatch = UrlRegex.Match(content);
        var urlEvidence = urlMatch.Success ? NormalizeUrlEvidence(urlMatch.Value) : null;
        if (urlEvidence is not null)
        {
            AddIndicator(
                analysis,
                "MSG_URL_PRESENT",
                "A mensagem contém uma URL que deve ser verificada antes do acesso.",
                IndicatorSeverity.Low,
                urlEvidence);
            AddRecommendation(analysis, "REC_REVIEW_LINK", "Verifique o destino do link e prefira acessar o site oficial diretamente.", RecommendationPriority.Important);
        }

        analysis.Complete();
    }

    private static void AnalyzeLink(Analysis analysis, string content)
    {
        if (!Uri.TryCreate(content, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            analysis.MarkInconclusive();
            return;
        }

        var host = uri.IdnHost.ToLowerInvariant();
        var ipHost = uri.Host.Trim('[', ']');
        if (IPAddress.TryParse(ipHost, out _))
        {
            AddIndicator(analysis, "LINK_IP_ADDRESS", "O link utiliza um endereço IP diretamente no host.", IndicatorSeverity.Medium, uri.Host);
            AddRecommendation(analysis, "REC_VERIFY_OFFICIAL_CHANNEL", "Confirme o endereço por um canal oficial antes de acessar.", RecommendationPriority.Important);
        }

        if (host.Contains("xn--", StringComparison.OrdinalIgnoreCase) || host.Any(ch => ch > 127))
        {
            AddIndicator(analysis, "LINK_IDN_DOMAIN", "O domínio utiliza caracteres internacionalizados que podem dificultar a identificação visual.", IndicatorSeverity.Medium, uri.Host);
            AddRecommendation(analysis, "REC_REVIEW_LINK", "Confira cuidadosamente o domínio antes de acessar o link.", RecommendationPriority.Important);
        }

        if (UrlShortenerHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
        {
            AddIndicator(analysis, "LINK_SHORTENER", "O link utiliza um serviço de encurtamento de URL.", IndicatorSeverity.Medium, uri.Host);
            AddRecommendation(analysis, "REC_AVOID_SHORTENED_LINK", "Evite acessar links encurtados sem confirmar o destino por outro canal.", RecommendationPriority.Important);
        }

        foreach (var (brand, officialHosts) in KnownBrands)
        {
            if (HasBrandToken(host, brand) &&
                !officialHosts.Any(officialHost => host.Equals(officialHost, StringComparison.OrdinalIgnoreCase) || host.EndsWith("." + officialHost, StringComparison.OrdinalIgnoreCase)))
            {
                AddIndicator(analysis, "LINK_BRAND_IMPERSONATION", "O domínio contém referência a uma marca conhecida, mas não corresponde a um domínio oficial listado.", IndicatorSeverity.Medium, uri.Host);
                AddRecommendation(analysis, "REC_VERIFY_OFFICIAL_CHANNEL", "Acesse a instituição pelo endereço oficial digitado ou salvo anteriormente.", RecommendationPriority.Important);
                break;
            }
        }

        analysis.Complete();
    }

    private static void AddIndicator(Analysis analysis, string code, string description, IndicatorSeverity severity, string evidence)
    {
        if (analysis.Indicators.Any(indicator => indicator.Code == code))
        {
            return;
        }

        analysis.AddIndicator(new Indicator(code, description, severity, evidence));
    }

    private static void AddRecommendation(Analysis analysis, string code, string description, RecommendationPriority priority)
    {
        if (analysis.Recommendations.Any(recommendation => recommendation.Code == code))
        {
            return;
        }

        analysis.AddRecommendation(new Recommendation(code, description, priority));
    }

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(value.Contains);

    private static string? NormalizeUrlEvidence(string value)
    {
        var candidate = value.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '}');
        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
               (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            ? candidate
            : null;
    }

    private static bool HasBrandToken(string host, string brand) =>
        host.Equals(brand, StringComparison.OrdinalIgnoreCase) ||
        host.StartsWith(brand + ".", StringComparison.OrdinalIgnoreCase) ||
        host.StartsWith(brand + "-", StringComparison.OrdinalIgnoreCase) ||
        host.Contains("." + brand + ".", StringComparison.OrdinalIgnoreCase) ||
        host.Contains("." + brand + "-", StringComparison.OrdinalIgnoreCase);

    private static string FindEvidence(string content, params string[] terms)
    {
        var term = terms.FirstOrDefault(term => content.Contains(term, StringComparison.OrdinalIgnoreCase));
        return term ?? content;
    }
}
