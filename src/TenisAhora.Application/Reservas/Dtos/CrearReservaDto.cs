using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Application.Reservas.Dtos;

// Los importes y el estado no se toman del cuerpo: salen de la tarifa de la cancha.
public record CrearReservaDto(
    string? CourtId,
    string? Date,
    string? StartTime,
    decimal? DurationHours,
    string? PlayersType,
    List<ParticipanteReserva>? Participants,
    string? Notes);
