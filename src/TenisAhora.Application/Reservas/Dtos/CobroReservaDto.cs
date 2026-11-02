using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Reservas.Dtos;

public record CobroReservaDto(Reserva Data, Recibo Receipt);
