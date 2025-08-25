using System.Windows;

namespace DesktopImgFrame;

public class FrameConfig
{
    public List<string> ImgPaths { get; set; } = [];
    public int Index { get; set; } = 0;
    public bool Locked { get; set; } = false;
    public int Interval { get; set; } = 5; // minutes
    public bool RandomOrder { get; set; } = false;
    public Rect WindowRect { get; set; }
}
public class ConfigEntity
{
    public List<FrameConfig> FrameConfigs { get; set; } = [];
}