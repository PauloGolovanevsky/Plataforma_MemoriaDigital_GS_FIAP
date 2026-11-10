using MemoriaDigital.Domain.Entities;

namespace MemoriaDigital.Application.Interfaces;

public interface IAnalysisEngine
{
    void Analyze(Analysis analysis);
}
