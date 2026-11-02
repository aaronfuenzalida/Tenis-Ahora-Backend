using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Application.Competencias.Dtos;

public record ResultadoLigaDto(bool Success, Enfrentamiento Match, List<Posicion> Standings);
