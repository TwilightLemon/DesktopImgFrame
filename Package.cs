using MyToolBar.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopImgFrame;

public class Package : IPackage
{
    internal const string GlobalName = "DesktopImgFrame";
    public string PackageName => GlobalName;

    public string DisplayName => GlobalName;

    public string Description { get; } = "a simple image frame";

    public Version Version { get; } = new(1, 0, 0, 0);

    public List<IPlugin> Plugins { get; set; }= [new FramePlugin()];
}

public class FramePlugin : IPlugin
{
    public IPackage? AcPackage { get; set; }

    public string Name => Package.GlobalName;

    public string DisplayName => Package.GlobalName;

    public string Description => "a simple image frame";

    public List<string>? SettingsSignKeys { get; set; } = null;// [MainWindow.ConfigKey]; 

    public PluginType Type => PluginType.UserService;
    public ServiceBase GetServiceHost() => new FrameService();
}
