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

            this.MouseWheel += MainWindow_MouseWheel;
            Img.MouseLeftButtonDown += Img_MouseLeftButtonDown;
            Img.MouseLeftButtonUp += Img_MouseLeftButtonUp;
            Img.MouseMove += Img_MouseMove;
        }

        private Point? _lastDragPoint,_mouseDownPoint;

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            WindowLongAPI.SetToolWindow(this);
           // DesktopWindowHelper.EmbedWindowToDesktop(this);
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
            if (Config.ImgPaths.Count >Config.Index)
            {
                Img.Source = new BitmapImage(new Uri(Config.ImgPaths[Config.Index]));
            }
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
                Config.Index = r.Next(0, Config.ImgPaths.Count);
            }
            else
            {
                Config.Index = (Config.Index + 1) % Config.ImgPaths.Count;
            }
            try
            {
                Img.Source = new BitmapImage(new Uri(Config.ImgPaths[Config.Index]));
            }
            catch { }
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Config.Locked)
            {
                return;
            }
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
            if (Config.Locked)
            {
                return;
            }
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

        #region Buttons
        private void NewFrameBtn_Click(object sender, RoutedEventArgs e)
        {
            FrameService.ServiceInstance?.CreateNewFrame();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            FrameService.ServiceInstance?.RemoveFrame(this);
        }

        private void BtnGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            BtnGrid.Opacity = 1;
        }

        private void BtnGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            BtnGrid.Opacity = 0;
        }
        private void LockBtn_Click(object sender, RoutedEventArgs e)
        {
            Config.Locked = !Config.Locked;
        }
        #endregion

        #region Scale and Move
        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var point = e.GetPosition(Img);
            var scale = e.Delta > 0 ? 1.1 : 1 / 1.1;

            // To scale around the mouse pointer, we need to adjust the translation.
            // The formula is T_new = T_old * scale_factor + mouse_pos * (1 - scale_factor).
            // This assumes the ScaleTransform's center is (0,0).

            var oldTranslateX = ImageTranslateTransform.X;
            var oldTranslateY = ImageTranslateTransform.Y;

            ImageTranslateTransform.X = oldTranslateX * scale + point.X * (1 - scale);
            ImageTranslateTransform.Y = oldTranslateY * scale + point.Y * (1 - scale);

            ImageScaleTransform.ScaleX *= scale;
            ImageScaleTransform.ScaleY *= scale;
        }

        private void Img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                //reset
                ImageScaleTransform.ScaleX = 1;
                ImageScaleTransform.ScaleY = 1;
                ImageTranslateTransform.X = 0;
                ImageTranslateTransform.Y = 0;
                _mouseDownPoint = null;
                return;
            }
            _mouseDownPoint = _lastDragPoint = e.GetPosition(this);
            Img.CaptureMouse();
        }

        private void Img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Img.ReleaseMouseCapture();

            if(_mouseDownPoint.HasValue && (e.GetPosition(this) == _mouseDownPoint.Value))
            {
                // click without move
                if (!Config.Locked)
                {
                    NextImage();
                }
            }
            _mouseDownPoint = null;
            _lastDragPoint = null;
        }

        private void Img_MouseMove(object sender, MouseEventArgs e)
        {
            if (_lastDragPoint.HasValue)
            {
                var currentPosition = e.GetPosition(this);
                var offset = currentPosition - _lastDragPoint.Value;
                _lastDragPoint = currentPosition;

                ImageTranslateTransform.X += offset.X;
                ImageTranslateTransform.Y += offset.Y;
            }
        }
        #endregion
    }
}