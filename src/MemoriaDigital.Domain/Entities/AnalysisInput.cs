using MemoriaDigital.Domain.Enums;

namespace MemoriaDigital.Domain.Entities;

public sealed class AnalysisInput
{
    private AnalysisInput()
    {
    }

    public AnalysisInput(
        InputType type,
        string? content = null,
        string? reference = null,
        string? fileName = null,
        string? mediaType = null)
    {
        Id = Guid.NewGuid();
        Type = type;
        Content = Normalize(content);
        Reference = Normalize(reference);
        FileName = Normalize(fileName);
        MediaType = Normalize(mediaType);

        Validate();
    }

    public Guid Id { get; private set; }

    public InputType Type { get; private set; }

    public string? Content { get; private set; }

    public string? Reference { get; private set; }

    public string? FileName { get; private set; }

    public string? MediaType { get; private set; }

    private void Validate()
    {
        // Cada tipo aceita uma forma diferente de evidência; validar aqui evita criar análises impossíveis de interpretar.
        switch (Type)
        {
            case InputType.Message when string.IsNullOrWhiteSpace(Content):
                throw new ArgumentException("Message input requires content.", nameof(Content));

            case InputType.Link:
                ValidateLink();
                break;

            case InputType.Image when !HasImageReference() && !HasImageMetadata():
                throw new ArgumentException(
                    "Image input requires a reference or the minimum image metadata.",
                    nameof(Reference));
        }
    }

    private void ValidateLink()
    {
        if (string.IsNullOrWhiteSpace(Content) ||
            !Uri.TryCreate(Content, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Scheme) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new ArgumentException("Link input requires a valid absolute URL.", nameof(Content));
        }
    }

    private bool HasImageReference() => !string.IsNullOrWhiteSpace(Reference);

    private bool HasImageMetadata() =>
        !string.IsNullOrWhiteSpace(FileName) &&
        !string.IsNullOrWhiteSpace(MediaType);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
