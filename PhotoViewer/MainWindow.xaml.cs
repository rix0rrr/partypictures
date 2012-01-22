using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace PhotoViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string ScanDirectory = ".\\photos";
        const int PhotoIntervalSeconds = 20;

        private readonly Random random = new Random();
        private readonly Timer timer;
        private readonly Playlist playlist;
        private readonly DirectoryScanner scanner;

        public MainWindow()
        {
            InitializeComponent();

            Directory.CreateDirectory(ScanDirectory);

            playlist = new Playlist(random);
            scanner  = new DirectoryScanner(ScanDirectory, playlist);

            timer = new Timer(_ => DoNextPhoto(), null, Timeout.Infinite, Timeout.Infinite);
            ScheduleTimer(1);

            playlist.FirstFreshPhoto += () => ScheduleTimer(); // Also immediately invokes
        }

        private void DoNextPhoto() 
        {
            Dispatcher.BeginInvoke((Action)NextPhoto);
        }

        private void ScheduleTimer(int wait = 0)
        {
            timer.Change(TimeSpan.FromSeconds(wait), TimeSpan.FromSeconds(PhotoIntervalSeconds));
        }

        private void NextPhoto()
        {
            PlaylistEntry entry;
            if (playlist.TryPick(out entry))
            {
                ShowPhoto(entry.Filename, entry.Caption);
            }
        }

        private Size DisplaySize(BitmapSource bmp)
        {
            var minWidth  = ActualWidth * 0.50;
            var minHeight = ActualHeight * 0.50;

            var hFactor = minWidth / bmp.Width;
            var vFactor = minHeight / bmp.Height;

            var factor = Math.Max(hFactor, vFactor);

            return new Size(bmp.Width * factor, bmp.Height * factor);
        }

        private double Wiggle(double input, double range)
        {
            return input - range + random.NextDouble() * range * 2;
        }

        private void EnumerateMetaData(string keyprefix, BitmapMetadata meta)
        {
            foreach (var key in meta)
            {
                if (key == null) continue;

                object o = meta.GetQuery(key);

                if (o is BitmapMetadata) EnumerateMetaData(keyprefix + key, (BitmapMetadata)o);
                else Debug.WriteLine("{0}: {1}", keyprefix + key, o);
            }
        }

        /// <summary>
        /// Load the bitmap and return the bitmap and orientation
        /// </summary>
        private Tuple<BitmapSource, int> LoadBitmap(string filename)
        {
            if (System.IO.Path.GetExtension(filename).ToLower() == ".jpg")
            {
                var dec = new JpegBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                BitmapMetadata metadata = (BitmapMetadata)dec.Frames[0].Metadata;

                object metaori = metadata.GetQuery("/app1/ifd/{ushort=274}");

                int orientation = 1;
                if (metaori is UInt16) orientation = (ushort)metaori;

                int degrees = 0;
                switch (orientation)
                {
                    case 3: degrees = 180; break;
                    case 6: degrees = 90;  break;
                    case 8: degrees = 270; break;
                }

                return new Tuple<BitmapSource,int>(dec.Frames[0], degrees);
            }
            else
            {
                return new Tuple<BitmapSource,int>(new BitmapImage(new Uri(filename)), 0);
            }
        }

        private void ShowPhoto(string filename, string caption)
        {
            var bitmap = LoadBitmap(filename);
            var bmp    = bitmap.Item1;
            var orientation = bitmap.Item2;

            var size = DisplaySize(bmp);

            var img = new Image()
            {
                Source  = bmp,
                Stretch = Stretch.UniformToFill,
                Width   = size.Width,
                Height  = size.Height
            };

            var border = new Border() 
            {
                Background = new SolidColorBrush(Colors.White),
                Padding    = new Thickness(15)
            };
            Canvas.SetLeft(border, Wiggle((ActualWidth - size.Width - 2 * border.Padding.Left) / 2, ActualWidth * 0.3));
            Canvas.SetTop(border, Wiggle((ActualHeight - size.Height - 2 * border.Padding.Left) / 2 + 30, ActualHeight * 0.2));

            var g = new Grid();
            g.Children.Add(img);
            g.Children.Add(new TextBlock()
            {
                Text = caption,
                FontSize = 36,
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment   = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 10, 0)
            });

            border.Child = g;
            LayoutRoot.Children.Add(border);

            // Add a transform that compensates for the camera orientation
            if (orientation != 0)
            {
                img.LayoutTransform = new RotateTransform(orientation, size.Width / 2, size.Height / 2);
            }

            DoThrow(border);
        }

        private void DoThrow(UIElement el) 
        {
            var center = new Point(150, 150);

            var scale = new ScaleTransform(0, 0, center.X, center.Y);
            var rot   = new RotateTransform(0.0, center.X, center.Y);
            var trans = new TranslateTransform(50.0, 50.0);

            var tg = new TransformGroup();
            tg.Children.Add(scale);
            tg.Children.Add(rot);
            tg.Children.Add(trans);
            el.RenderTransform = tg;

            var cw   = random.Next(2) == 0 ? 1 : -1; // Clockwise
            var deg  = Wiggle(8, 6);
            var dist = Wiggle(50, 20);

            rot.BeginAnimation(RotateTransform.AngleProperty,  Animation(cw * 2, cw * deg, 2));
            trans.BeginAnimation(TranslateTransform.YProperty, Animation(0, -dist, 2));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, Animation(1.1, 1, 0.5));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, Animation(1.1, 1, 0.5));

            var fadeOut = new DoubleAnimationUsingKeyFrames()
            {
                DecelerationRatio = 0.9,
                KeyFrames = {
                    new LinearDoubleKeyFrame() { KeyTime = TimeSpan.FromSeconds(0),  Value = 0  },
                    new LinearDoubleKeyFrame() { KeyTime = TimeSpan.FromSeconds(1),  Value = 1  },
                    new LinearDoubleKeyFrame() { KeyTime = TimeSpan.FromSeconds(2 * PhotoIntervalSeconds), Value = 1  },
                    new LinearDoubleKeyFrame() { KeyTime = TimeSpan.FromSeconds(4 * PhotoIntervalSeconds), Value = 0  },
                }
            };

            fadeOut.Completed += (a, e) => ((Panel)LogicalTreeHelper.GetParent(el)).Children.Remove(el);

            el.BeginAnimation(UIElement.OpacityProperty, fadeOut);

            var gray = new GrayscaleEffect.GrayscaleEffect();
            el.Effect = gray;

            var grayscaleFade = new DoubleAnimationUsingKeyFrames()
            {
                DecelerationRatio = 0.9,
                KeyFrames = {
                    new LinearDoubleKeyFrame() { KeyTime = TimeSpan.FromSeconds(0),   Value = 1  },
                    new LinearDoubleKeyFrame() { KeyTime = TimeSpan.FromSeconds(PhotoIntervalSeconds),      Value = 1  },
                    new LinearDoubleKeyFrame() { KeyTime = TimeSpan.FromSeconds(2 * PhotoIntervalSeconds),  Value = 0  },
                }
            };  
            gray.BeginAnimation(GrayscaleEffect.GrayscaleEffect.DesaturationFactorProperty, grayscaleFade);
        }

        private DoubleAnimation Animation(double from, double too, double duration)
        {
            return new DoubleAnimation()
            {
                From = from,
                To = too,
                DecelerationRatio = 0.9,
                Duration = new Duration(TimeSpan.FromSeconds(duration))
            };
        }
    }
}
