namespace TenisAhora.Application.Pagos.Dtos;

public record CrearPagoDto(string? PaymentMethod, decimal? Amount, string? Concept, TarjetaDto? Card);
