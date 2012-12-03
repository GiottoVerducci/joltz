using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace JOLTZ
{
    /// <summary>
    /// The JOL Time Zone main page
    /// Created by Vincent Ripoll
    /// </summary>
    public partial class MainPage : UserControl
    {
        private readonly List<PlayerAvailability> _playerAvailabilities = new List<PlayerAvailability>();
        private readonly SolidColorBrush _fillBrush = new SolidColorBrush();
        private readonly SolidColorBrush _highlightFillBrush = new SolidColorBrush();
        private readonly SolidColorBrush _highlightStrokeBrush = new SolidColorBrush();
        private readonly SolidColorBrush _whiteBrush = new SolidColorBrush();
        private readonly SolidColorBrush _nowBrush = new SolidColorBrush();
        private readonly LinearGradientBrush _pathFillBrush;
        public MainPage()
        {
            InitializeComponent();

            _fillBrush.Color = Color.FromArgb(128, 128, 128, 128);
            _highlightFillBrush.Color = Color.FromArgb(192, 0, 200, 0);
            _highlightStrokeBrush.Color = Color.FromArgb(240, 0, 200, 0);
            _whiteBrush.Color = Color.FromArgb(255, 192, 192, 192);
            _nowBrush.Color = Color.FromArgb(255, 200, 0, 0);

            _pathFillBrush = new LinearGradientBrush
            {
                EndPoint = new Point(1, 0.5),
                StartPoint = new Point(0, 0.5),
                GradientStops = new GradientStopCollection
                { 
                    new GradientStop { Color = Color.FromArgb(0x4d, 0xfe, 0xff, 0xfe), Offset = 0.003 },
                    new GradientStop { Color = Color.FromArgb(0x34, 0xfe, 0xff, 0xfe), Offset = 1 }
                }
            };

            _playerAvailabilities.Add(new PlayerAvailability("Ankha, GMT+1, 10:00-12:00, 13:00-18:00"));
            _playerAvailabilities.Add(new PlayerAvailability("Klaital, GMT+2, 10:00-18:00"));
            _playerAvailabilities.Add(new PlayerAvailability("Omelet, GMT-5, 09:00-17:00, 19:00-23:00"));
            _playerAvailabilities.Add(new PlayerAvailability("Preston, GMT-6, 09:00-17:00"));
            _playerAvailabilities.Add(new PlayerAvailability("Dorrinal, GMT-8, 09:00-17:00, 20:00-21:00"));
            _playerAvailabilities.Add(new PlayerAvailability("Juggernaut1981, GMT+10, 16:00-19:00, 21:00-23:00"));
            _playerAvailabilities.Add(new PlayerAvailability("jamesdburns, GMT-5,19:00-23:00"));
            _playerAvailabilities.Add(new PlayerAvailability("Colin, GMT-5,09:00-16:00"));
            _playerAvailabilities.Add(new PlayerAvailability("Blooded, GMT+1,11:00-16:00"));
            _playerAvailabilities.Add(new PlayerAvailability("Jhattara, GMT+2,10:00-16:00, 17:00-20:00")); // weelends 14:00-20:00
             
            _playerAvailabilities = _playerAvailabilities.OrderBy(availability =>
                {
                    var diff = Math.Abs(availability.GmtOffset - TimeZoneInfo.Local.BaseUtcOffset.Hours);
                    if (diff > 12)
                        diff -= 12;
                    return diff;
                }).ToList();

            Canvas.SizeChanged += delegate { RefreshVisual(); };

            var timer = new DispatcherTimer { Interval = new TimeSpan(0, 1, 0) };
            timer.Tick += delegate { RefreshVisual(); };
            timer.Start();
        }

        const double XOffset = 20;
        const double YOffset = 20;

        private double _currentTime;

        public double TimeOffset { get { return 12 * 60 - _currentTime; } }

        private void RefreshVisual()
        {
            CleanVisual();
            CreateVisual();
        }

        private double TimeToX(DateTime time)
        {
            return TimeToX(time.TimeOfDay.TotalMinutes);
        }

        private double TimeToX(double minutes)
        {
            var min = (minutes + TimeOffset);
            if (min < 0)
                min += 24 * 60;
            else if (min > 24 * 60)
                min -= 24 * 60;
            return XOffset + ((Canvas.ActualWidth - XOffset) * min) / (24 * 60);
        }

        private void CreateVisual()
        {
            _currentTime = DateTime.Now.TimeOfDay.TotalMinutes; // will be centered on the screen

            var xCenter = XOffset + (Canvas.ActualWidth - XOffset) / 2;
            var centerLine = new Line
            {
                X1 = xCenter,
                X2 = xCenter,
                Y1 = 0,
                Y2 = Canvas.ActualHeight,
                StrokeThickness = 2,
                Stroke = _nowBrush
            };
            Canvas.Children.Add(centerLine);

            for (int i = 0; i < 24; ++i)
            {
                var x = TimeToX(i * 60);
                var line = new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = YOffset - 5,
                    Y2 = Canvas.ActualHeight,
                    StrokeThickness = 2,
                    Stroke = _whiteBrush
                };
                Canvas.Children.Add(line);

                var grid = new Grid { Width = 20 };
                var textBlock = new TextBlock { Text = i.ToString(), FontSize = 10, Foreground = _whiteBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                grid.Children.Add(textBlock);

                grid.SetValue(Canvas.LeftProperty, x - 10);
                grid.SetValue(Canvas.TopProperty, (double)0);
                Canvas.Children.Add(grid);
            }

            var height = Math.Min(50, (Canvas.ActualHeight - YOffset) / _playerAvailabilities.Count);

            double y = 0;
            foreach (var playerAvailability in _playerAvailabilities)
            {
                foreach (var availability in playerAvailability.Availabilities)
                {
                    var toolTipText = string.Format("GMT{0}: {1}-{2}",
                        playerAvailability.GmtOffset.ToString("+#;-#"),
                        availability.UtcStartTime.TimeOfDay + new TimeSpan(playerAvailability.GmtOffset, 0, 0),
                        availability.UtcEndTime.TimeOfDay + new TimeSpan(playerAvailability.GmtOffset, 0, 0));
                    var rectLeftX = TimeToX(availability.UtcStartTime.ToLocalTime());
                    var rectRightX = TimeToX(availability.UtcEndTime.ToLocalTime());
                    if (rectLeftX < rectRightX)
                    {
                        AddRectangle(rectRightX - rectLeftX, height, rectLeftX, y + YOffset, availability.Contains(_currentTime), playerAvailability.Name, toolTipText, HorizontalAlignment.Center);
                    }
                    else
                    {
                        AddRectangle(rectLeftX - rectRightX, height, rectLeftX, y + YOffset, availability.Contains(_currentTime), playerAvailability.Name, toolTipText, HorizontalAlignment.Left);
                        AddRectangle(rectLeftX - rectRightX, height, 2 * rectRightX - rectLeftX, y + YOffset, availability.Contains(_currentTime), playerAvailability.Name, toolTipText, HorizontalAlignment.Right);
                    }
                }
                y += height;
            }
        }

        private void AddRectangle(double width, double height, double left, double top, bool isHighlighted, string text, string toolTipText, HorizontalAlignment horizontalAlignment)
        {
            var grid = new Grid
            {
                Width = width,
                Height = height
            };

            var rectangle = new Rectangle
            {
                Fill = isHighlighted ? _highlightFillBrush : _fillBrush,
                StrokeThickness = isHighlighted ? 2 : 1,
                Stroke = isHighlighted ? _highlightStrokeBrush : _whiteBrush
            };

            grid.Children.Add(rectangle);

            var viewbox = new Viewbox { Width = width, Height = height, MaxHeight = 15, HorizontalAlignment = horizontalAlignment, StretchDirection = StretchDirection.DownOnly, Stretch = Stretch.Fill };
            var textBlock = new TextBlock { Text = text, FontSize = 10, Foreground = _whiteBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0) };

            viewbox.Child = textBlock;

            viewbox.SetValue(Canvas.LeftProperty, left);
            viewbox.SetValue(Canvas.TopProperty, top);
            grid.Children.Add(viewbox);

            var toolTip = new ToolTip { Content = new TextBlock { Text = toolTipText, FontSize = 10 } };
            grid.SetValue(ToolTipService.ToolTipProperty, toolTip);

            var path = (Path)XamlReader.Load(@"<Path xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                        Data=""M145.9993,278.88873 C145.9993,276.29611 143.90344,274.99982 141.80757,274.99982 C139.90289,274.99982 101.28758,274.99982 99.191719,274.99982 C97.095856,274.99982 95,276.29611 95,278.88873 C95,280.83319 95,291.20358 95,291.20358 C109.037,284.29858 126.688,281.44214 146,280.77213 C145.9995,280.18503 145.9993,279.53687 145.9993,278.88873 z""
                                        Stretch=""Fill"" UseLayoutRounding=""False""
                                        Opacity=""0.7"" Height=""25""
                                        VerticalAlignment=""Top"" />");

            path.Fill = _pathFillBrush;
            grid.Children.Add(path);

            grid.SetValue(Canvas.LeftProperty, left);
            grid.SetValue(Canvas.TopProperty, top);
            Canvas.Children.Add(grid);
        }

        private void CleanVisual()
        {
            var uielements = new UIElement[Canvas.Children.Count];
            Canvas.Children.CopyTo(uielements, 0);

            foreach (var uiElement in uielements)
            {
                Canvas.Children.Remove(uiElement);
            }
        }
    }
}
