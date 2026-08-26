using System.IO;

namespace Vexel.App.Infrastructure;

public sealed class VexelPaths
{
    public VexelPaths()
    {
        Root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vexel");
        Settings = Path.Combine(Root, "settings.json");
        Logs = Path.Combine(Root, "logs");
    }

    public string Root { get; }

    public string Settings { get; }

    public string Logs { get; }
}
