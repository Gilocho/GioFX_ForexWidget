namespace ForexWidget.Domain.Interfaces;

using ForexWidget.Domain.Models;

public interface ISystemStatusService
{
    SystemStatusSnapshot GetSnapshot();
}
