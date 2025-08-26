using MyToolBar.Common;
using MyToolBar.Common.WinAPI;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
            SizeChanged += MainWindow_SizeChanged;
            Loaded += MainWindow_Loaded;

            // Mouse events
            this.MouseWheel += MainWindow_MouseWheel;
            Img.MouseLeftButtonDown += Img_MouseLeftButtonDown;
            Img.MouseLeftButtonUp += Img_MouseLeftButtonUp;
            Img.MouseMove += Img_MouseMove;

            // Touch and manipulation events
            this.IsManipulationEnabled = true;
            this.ManipulationStarted += MainWindow_ManipulationStarted;
            this.ManipulationDelta += MainWindow_ManipulationDelta;
            this.ManipulationCompleted += MainWindow_ManipulationCompleted;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ResetImageSize();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResetImageSize();
        }

        private void ResetImageSize()
        {
            if (Img.Source is BitmapSource { PixelWidth: var w, PixelHeight: var h })
            {
                ImageScaleTransform.ScaleX = 1;
                ImageScaleTransform.ScaleY = 1;
                Canvas.SetLeft(Img, 0);
                Canvas.SetTop(Img, 0);

                double ratio = (double)w / h;
                //先尝试固定高度，计算宽度
                Img.Height = ActualHeight;
                var width = ActualHeight * ratio;
                if (width >= ActualWidth)
                {
                    Img.Width = width;
                }
                else 
                {
                    //宽度不够，固定宽度，计算高度
                    Img.Width = ActualWidth;
                    var height = ActualWidth / ratio;
                    Img.Height = height;
                }
            }
        }

        private Point? _lastDragPoint, _mouseDownPoint;
        private bool _isManipulating = false;
        private Point _manipulationStartCenter;

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
                ResetImageSize();
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
            ScaleImage(e.GetPosition(this), e.Delta > 0 ? 1.1 : 1 / 1.1);
        }

        private void ScaleImage(Point centerPoint, double scaleFactor)
        {
            // Get current image position and scale
            var currentLeft = Canvas.GetLeft(Img);
            var currentTop = Canvas.GetTop(Img);
            var currentScaleX = ImageScaleTransform.ScaleX;
            var currentScaleY = ImageScaleTransform.ScaleY;

            // Calculate new scale values
            var newScaleX = currentScaleX * scaleFactor;
            var newScaleY = currentScaleY * scaleFactor;

            // Prevent scaling too small or too large
            if (newScaleX < 0.1 || newScaleX > 10 || newScaleY < 0.1 || newScaleY > 10)
                return;

            // Calculate the point relative to the image's current position
            var imagePoint = new Point(centerPoint.X - currentLeft, centerPoint.Y - currentTop);

            // Scale the relative point by the scale factor
            var scaledImagePoint = new Point(imagePoint.X * scaleFactor, imagePoint.Y * scaleFactor);

            // Calculate new position to keep the center point fixed
            var newLeft = centerPoint.X - scaledImagePoint.X;
            var newTop = centerPoint.Y - scaledImagePoint.Y;

            // Apply the new transformation
            Canvas.SetLeft(Img, newLeft);
            Canvas.SetTop(Img, newTop);
            ImageScaleTransform.ScaleX = newScaleX;
            ImageScaleTransform.ScaleY = newScaleY;
        }

        private void Img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Ignore mouse events when manipulating with touch
            if (_isManipulating) return;

            if (e.ClickCount == 2)
            {
                //reset
                ResetImageSize();
                _mouseDownPoint = null;
                return;
            }
            _mouseDownPoint = _lastDragPoint = e.GetPosition(this);
            Img.CaptureMouse();
        }

        private void Img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Ignore mouse events when manipulating with touch
            if (_isManipulating) return;

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
            // Ignore mouse events when manipulating with touch
            if (_isManipulating) return;

            if (_lastDragPoint.HasValue)
            {
                var currentPosition = e.GetPosition(this);
                var offset = currentPosition - _lastDragPoint.Value;
                _lastDragPoint = currentPosition;

                var x= Canvas.GetLeft(Img);
                x += offset.X;
                Canvas.SetLeft(Img, x);
                var y = Canvas.GetTop(Img);
                y+= offset.Y;
                Canvas.SetTop(Img, y);
            }
        }

        #region Touch and Manipulation Events
        private void MainWindow_ManipulationStarted(object? sender, ManipulationStartedEventArgs e)
        {
            _isManipulating = true;
            _manipulationStartCenter = e.ManipulationOrigin;
            e.Handled = true;
        }

        private void MainWindow_ManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
        {
            if (!_isManipulating) return;

            // Handle scaling (pinch-to-zoom)
            if (e.DeltaManipulation.Scale.X != 1.0 || e.DeltaManipulation.Scale.Y != 1.0)
            {
                var scaleFactor = (e.DeltaManipulation.Scale.X + e.DeltaManipulation.Scale.Y) / 2.0;
                var centerPoint = e.ManipulationOrigin;
                ScaleImage(centerPoint, scaleFactor);
            }

            // Handle translation (pan/drag)
            if (e.DeltaManipulation.Translation.X != 0 || e.DeltaManipulation.Translation.Y != 0)
            {
                var currentLeft = Canvas.GetLeft(Img);
                var currentTop = Canvas.GetTop(Img);
                
                Canvas.SetLeft(Img, currentLeft + e.DeltaManipulation.Translation.X);
                Canvas.SetTop(Img, currentTop + e.DeltaManipulation.Translation.Y);
            }

            e.Handled = true;
        }

        private void MainWindow_ManipulationCompleted(object? sender, ManipulationCompletedEventArgs e)
        {
            _isManipulating = false;
            e.Handled = true;
        }
        #endregion
        #endregion
    }
}