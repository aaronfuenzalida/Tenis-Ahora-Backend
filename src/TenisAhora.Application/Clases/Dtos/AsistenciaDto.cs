namespace TenisAhora.Application.Clases.Dtos;

// Status: P (presente) o A (ausente).
public record AsistenciaDto(string? StudentId, int? SessionIndex, string? Status);
