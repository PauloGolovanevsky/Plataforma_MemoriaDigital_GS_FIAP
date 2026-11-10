using MemoriaDigital.Domain.Enums;

namespace MemoriaDigital.Application.DTOs;

public sealed record CreateAnalysisRequest(
    InputType InputType,
    string? Content = null,
    string? Reference = null,
    string? FileName = null,
    string? MediaType = null);
