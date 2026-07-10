namespace ForexWidget.Domain.Interfaces;

using ForexWidget.Domain.Models;
using System.Collections.Generic;

public interface IConfigurationLoader
{
    AppSettings LoadSettings();
    IReadOnlyList<KillzoneDefinition> LoadKillzones();
    IReadOnlyList<AlertDefinition> LoadAlerts();
    void SaveSettings(AppSettings settings);
    void SaveKillzones(IReadOnlyList<KillzoneDefinition> killzones);
}
