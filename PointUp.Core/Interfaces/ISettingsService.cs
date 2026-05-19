using PointUp.Core.Models;

namespace PointUp.Core.Interfaces;

public interface ISettingsService
{
    AppSettings Settings { get; }
    void Save();
    void Load();
}
