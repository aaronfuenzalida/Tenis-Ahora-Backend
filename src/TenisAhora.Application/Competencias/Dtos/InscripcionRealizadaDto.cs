using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Application.Competencias.Dtos;

public record InscripcionRealizadaDto(bool Success, ParticipanteCompetencia Participant);
