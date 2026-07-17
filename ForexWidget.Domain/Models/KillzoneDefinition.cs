namespace ForexWidget.Domain.Models;

using System;

/// <summary>
/// Static definition of a Killzone loaded from killzones.json.
/// All times are in UTC.
/// AssociatedMarket es etiqueta informativa (organización en la UI) — el overlay
/// en el timeline se sigue calculando por solapamiento real de horarios, no por ella.
/// </summary>
public record KillzoneDefinition(
    string Name,
    TimeOnly StartUtc,
    TimeOnly EndUtc,
    string Color,
    string Methodology,
    bool Enabled,
    bool IsCustom = false,
    string? AssociatedMarket = null
);
