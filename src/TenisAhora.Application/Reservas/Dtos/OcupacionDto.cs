namespace TenisAhora.Application.Reservas.Dtos;

// Turno tomado, sin datos de quién reservó.
public record OcupacionDto(
    string Id,
    string CourtId,
    string CourtName,
    string Date,
    string StartTime,
    string EndTime,
    decimal DurationHours,
    string Status);
