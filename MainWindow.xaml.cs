using MyToolBar.Common;
using MyToolBar.Common.WinAPI;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace DesktopImgFrame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal readonly FrameConfig Config;
        public MainWindow(FrameConfig config)
        {
            InitializeComponent();
            Config = config;
            Init();
            SourceInitialized += MainWindow_SourceInitialized;
            GlobalService.OnThemeColorChanged += GlobalService_OnThemeColorChanged;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            WindowLongAPI.SetToolWindow(this);
        }

        private void GlobalService_OnThemeColorChanged()
        {
            if (App.Current.Resources["SystemThemeColor"] is SolidColorBrush { Color: var c })
                Material.CompositonColor = Color.FromArgb(153, c.R, c.G, c.B);
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            Config.WindowRect = new Rect(Left, Top, Width, Height);
        }


        private int ImgIndex = 0;
        private void Init()
        {
            GlobalService_OnThemeColorChanged();
            if (Config.WindowRect is { Width: > 0, Height: > 0 } rect)
            {
                Left = rect.Left;
                Top = rect.Top;
                Width = rect.Width;
                Height = rect.Height;
            }
            NextImage();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string? path = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0)?.ToString();
            if (path != null)
            {
                Config.ImgPaths.Add(path);
                NextImage();
            }
        }

        private void NextImage()
        {
            if (Config.ImgPaths.Count == 0) return;
            if (Config.RandomOrder)
            {
                var r = new Random();
                ImgIndex = r.Next(0, Config.ImgPaths.Count);
            }
            else
            {
                ImgIndex = (ImgIndex + 1) % Config.ImgPaths.Count;
            }
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(Config.ImgPaths[ImgIndex]);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                Img.Source = bitmap;
            }
            catch { }
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            NextImage();

            var easing = new CubicEase();
            Img.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(400)));
            var blur = new BlurEffect() { Radius = 80 };
            Img.Effect = blur;
            var da = new DoubleAnimation(0, TimeSpan.FromMilliseconds(500));
            da.EasingFunction = easing;
            da.Completed += delegate
            {
                Img.Effect = null;
            };
            blur.BeginAnimation(BlurEffect.RadiusProperty, da);
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            Img.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(300)));
            var blur = new BlurEffect() { Radius = 0 };
            Img.Effect = blur;
            var da = new DoubleAnimation(80, TimeSpan.FromMilliseconds(300));
            da.Completed += delegate
            {
                Img.Effect = null;
            };
            blur.BeginAnimation(BlurEffect.RadiusProperty, da);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            NextImage();
        }

        private void NewFrameBtn_Click(object sender, RoutedEventArgs e)
        {
            FrameService.ServiceInstance?.CreateNewFrame();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            BtnGrid.Opacity = 1;
        }

        private void BtnGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            BtnGrid.Opacity = 0;
        }
    }
}