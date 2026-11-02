using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Reservas.Dtos;

public record CancelacionReservaDto(Reserva Data, bool IsWithinFreeWindow, Reembolso? Refund, Recibo? PenaltyReceipt);
