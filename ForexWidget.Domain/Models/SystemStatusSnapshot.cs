namespace ForexWidget.Domain.Models;

using System.Collections.Generic;

public record SystemStatusSnapshot(
    IReadOnlyList<ProviderHealth> Providers,
    bool InternetAvailable
);
