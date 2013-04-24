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
        private List<PlayerAvailability> _playerAvailabilities = new List<PlayerAvailability>();
        private readonly List<PlayerAvailability> _realPlayerAvailabilities = new List<PlayerAvailability>();
        private readonly SolidColorBrush _fillBrush = new SolidColorBrush();
        private readonly SolidColorBrush _realFillBrush = new SolidColorBrush();
        private readonly SolidColorBrush _uncertainFillBrush = new SolidColorBrush();
        private readonly SolidColorBrush _highlightFillBrush = new SolidColorBrush();
        private readonly SolidColorBrush _highlightUncertainFillBrush = new SolidColorBrush();
        private readonly SolidColorBrush _highlightStrokeBrush = new SolidColorBrush();
        private readonly SolidColorBrush _whiteBrush = new SolidColorBrush();
        private readonly SolidColorBrush _nowBrush = new SolidColorBrush();
        private readonly LinearGradientBrush _pathFillBrush;
        private Sort _sort = Sort.Name;

        public MainPage()
        {
            InitializeComponent();
            _fillBrush.Color = Color.FromArgb(128, 128, 128, 128);
            _uncertainFillBrush.Color = Color.FromArgb(64, 128, 128, 128);
            _highlightUncertainFillBrush.Color = Color.FromArgb(64, 0, 200, 0);
            _highlightFillBrush.Color = Color.FromArgb(192, 0, 200, 0);
            _realFillBrush.Color = Color.FromArgb(255, 0, 255, 0);
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

            Data.InitializeData(_playerAvailabilities, _realPlayerAvailabilities);

            Canvas.SizeChanged += delegate { RefreshVisual(); };

            var timer = new DispatcherTimer { Interval = new TimeSpan(0, 1, 0) };
            timer.Tick += delegate { RefreshVisual(); };
            timer.Start();
        }

        const double XOffset = 100;
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
            return ((Canvas.ActualWidth - XOffset) * min) / (24 * 60);
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
                    X1 = x + XOffset,
                    X2 = x + XOffset,
                    Y1 = YOffset - 5,
                    Y2 = Canvas.ActualHeight,
                    StrokeThickness = 2,
                    Stroke = _whiteBrush
                };
                Canvas.Children.Add(line);

                var grid = new Grid { Width = 20 };
                var textBlock = new TextBlock { Text = i.ToString(), FontSize = 10, Foreground = _whiteBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                grid.Children.Add(textBlock);

                grid.SetValue(Canvas.LeftProperty, XOffset + x - 10);
                grid.SetValue(Canvas.TopProperty, (double)0);
                Canvas.Children.Add(grid);
            }

            var sortButton = new Button { Width = XOffset - 10, Height = YOffset - 2, Content = _sort.ToString() };
            sortButton.Click += (sender, args) => { _sort = (Sort)(((byte)_sort + 1) % 2); RefreshVisual(); };
            sortButton.SetValue(Canvas.LeftProperty, (XOffset - sortButton.Width) / 2);
            sortButton.SetValue(Canvas.TopProperty, (YOffset - sortButton.Height) / 2);
            Canvas.Children.Add(sortButton);

            _playerAvailabilities = _sort == Sort.GMT
                ? _playerAvailabilities.OrderBy(availability =>
                {
                    var diff = Math.Abs(availability.GmtOffset - TimeZoneInfo.Local.BaseUtcOffset.Hours);
                    if (diff > 12)
                        diff -= 12;
                    return diff;
                }).ToList()
                : _playerAvailabilities.OrderBy(availability => availability.Name).ToList();

            var height = Math.Min(50, (Canvas.ActualHeight - YOffset) / _playerAvailabilities.Count);

            double y = 0;
            foreach (var playerAvailability in _playerAvailabilities)
            {
                bool hasWeekEndAvailabilities = playerAvailability.Availabilities.Any(a => a.IsWeekEnd);
                bool isWeekEnd = hasWeekEndAvailabilities && IsWeekEnd(DateTime.UtcNow + new TimeSpan(playerAvailability.GmtOffset, 0, 0));
                AddName(y + YOffset, height, playerAvailability.Name);
                var realPlayerAvailabilities = _realPlayerAvailabilities.FirstOrDefault(pa => pa.Name == playerAvailability.Name);
                if (realPlayerAvailabilities != null)
                {
                    var detailDelay = 30;
                    var availabilities = isWeekEnd ? realPlayerAvailabilities.WePresence : realPlayerAvailabilities.Presence; //realPlayerAvailabilities.Availabilities.Where(a => !hasWeekEndAvailabilities || a.IsWeekEnd == isWeekEnd).ToList();
                    var counts = new List<int>();
                    for (int i = 0; i < 24 * 60 / detailDelay; ++i)
                    {
                        var count = 0;
                        for (int j = 0; j < detailDelay; ++j)
                            count += availabilities[i * detailDelay + j];
                        //var timeSpan = new Availability(new DateTime(2012, 1, 1, i, 0, 0, DateTimeKind.Utc), new DateTime(2012, 1, 1, i, 59, 59, DateTimeKind.Utc), false, false);
                        //var count = availabilities.Count(timeSpan.Overlaps);
                        counts.Add(count);
                    }
                    var max = counts.Max();

                    for (int i = 0; i < 24 * 60 / detailDelay; ++i)
                    {
                        if (counts[i] == 0)
                            continue;
                        var hours = i * detailDelay / 60;
                        var minutes = i * detailDelay % 60;
                        var nextHours = (-1 + (i + 1) * detailDelay) / 60;
                        var nextMinutes = (-1 + (i + 1) * detailDelay) % 60;

                        var rectLeftX = TimeToX(new DateTime(2012, 1, 1, hours, minutes, 0, DateTimeKind.Utc).ToLocalTime());
                        var rectRightX = TimeToX(new DateTime(2012, 1, 1, nextHours, nextMinutes, 59, DateTimeKind.Utc).ToLocalTime());
                        var intensity = counts[i] * counts[i] * counts[i] / (max * max * max * 2);
                        if (rectLeftX < rectRightX)
                        {
                            AddLightRectangle(rectRightX - rectLeftX, height, XOffset + rectLeftX, y + YOffset, intensity);
                        }
                        else
                        {
                            AddLightRectangle(Canvas.ActualWidth - XOffset - rectLeftX, height, XOffset + rectLeftX, y + YOffset, intensity);
                            AddLightRectangle(rectRightX, height, XOffset, y + YOffset, intensity);
                        }
                    }

                    //foreach (var availability in availabilities)
                    //{
                    //    var rectLeftX = TimeToX(availability.UtcStartTime.ToLocalTime());
                    //    var rectRightX = TimeToX(availability.UtcEndTime.ToLocalTime());
                    //    if (rectLeftX < rectRightX)
                    //    {
                    //        AddLightRectangle(rectRightX - rectLeftX, height, XOffset + rectLeftX, y + YOffset);
                    //    }
                    //    else
                    //    {
                    //        AddLightRectangle(Canvas.ActualWidth - XOffset - rectLeftX, height, XOffset + rectLeftX, y + YOffset);
                    //        AddLightRectangle(rectRightX, height, XOffset, y + YOffset);
                    //    }
                    //}
                }
                foreach (var availability in playerAvailability.Availabilities.Where(a => !hasWeekEndAvailabilities || a.IsWeekEnd == isWeekEnd))
                {
                    var toolTipText = string.Format("{4} GMT{0}: {1}-{2}{3}",
                        playerAvailability.GmtOffset == 0 ? null : playerAvailability.GmtOffset.ToString("+#;-#"),
                        availability.UtcStartTime.TimeOfDay + new TimeSpan(playerAvailability.GmtOffset, 0, 0),
                        availability.UtcEndTime.TimeOfDay + new TimeSpan(playerAvailability.GmtOffset, 0, 0),
                        availability.IsUncertain ? " (uncertain)" : null,
                        playerAvailability.Name);
                    var rectLeftX = TimeToX(availability.UtcStartTime.ToLocalTime());
                    var rectRightX = TimeToX(availability.UtcEndTime.ToLocalTime());
                    var isUncertain = availability.IsUncertain;
                    if (rectLeftX < rectRightX)
                    {
                        AddRectangle(rectRightX - rectLeftX, height, XOffset + rectLeftX, y + YOffset, availability.Contains(_currentTime), playerAvailability.Name, toolTipText, HorizontalAlignment.Center, isUncertain);
                    }
                    else
                    {
                        AddRectangle(Canvas.ActualWidth - XOffset - rectLeftX, height, XOffset + rectLeftX, y + YOffset, availability.Contains(_currentTime), playerAvailability.Name, toolTipText, HorizontalAlignment.Left, isUncertain);
                        AddRectangle(rectRightX, height, XOffset, y + YOffset, availability.Contains(_currentTime), playerAvailability.Name, toolTipText, HorizontalAlignment.Right, isUncertain);
                    }
                }

                y += height;
            }
        }

        public bool IsWeekEnd(DateTime date)
        {
            var dayOfWeek = date.DayOfWeek;
            return dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
        }

        private void AddName(double top, double height, string text)
        {
            var grid = new Grid
            {
                Width = XOffset,
                Height = height
            };
            var viewbox = new Viewbox { Width = XOffset, Height = height, MaxHeight = 15, HorizontalAlignment = HorizontalAlignment.Left, StretchDirection = StretchDirection.DownOnly, Stretch = Stretch.Fill };
            var textBlock = new TextBlock { Text = text, FontSize = 10, Foreground = _whiteBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0) };

            viewbox.Child = textBlock;

            viewbox.SetValue(Canvas.LeftProperty, 0d);
            viewbox.SetValue(Canvas.TopProperty, 0d);
            grid.Children.Add(viewbox);

            grid.SetValue(Canvas.LeftProperty, 0d);
            grid.SetValue(Canvas.TopProperty, top);
            Canvas.Children.Add(grid);
        }

        private void AddRectangle(double width, double height, double left, double top, bool isHighlighted, string text, string toolTipText, HorizontalAlignment horizontalAlignment, bool isUncertain)
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

            if (isUncertain)
            {
                rectangle.Fill = isHighlighted ? _highlightUncertainFillBrush : _uncertainFillBrush;
                rectangle.Stroke = null;
            }

            grid.Children.Add(rectangle);

            var viewbox = new Viewbox { Width = width, Height = height, MaxHeight = 15, HorizontalAlignment = horizontalAlignment, StretchDirection = StretchDirection.DownOnly, Stretch = Stretch.Fill };
            var textBlock = new TextBlock { Text = text, FontSize = 10, Foreground = _whiteBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0) };
            if (isUncertain)
                textBlock.Opacity = 0.7f;

            viewbox.Child = textBlock;

            viewbox.SetValue(Canvas.LeftProperty, left);
            viewbox.SetValue(Canvas.TopProperty, top);
            grid.Children.Add(viewbox);

            if (toolTipText != null)
            {
                var toolTip = new ToolTip { Content = new TextBlock { Text = toolTipText, FontSize = 10 } };
                grid.SetValue(ToolTipService.ToolTipProperty, toolTip);
            }

            var path = (Path)XamlReader.Load(@"<Path xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                        Data=""M145.9993,278.88873 C145.9993,276.29611 143.90344,274.99982 141.80757,274.99982 C139.90289,274.99982 101.28758,274.99982 99.191719,274.99982 C97.095856,274.99982 95,276.29611 95,278.88873 C95,280.83319 95,291.20358 95,291.20358 C109.037,284.29858 126.688,281.44214 146,280.77213 C145.9995,280.18503 145.9993,279.53687 145.9993,278.88873 z""
                                        Stretch=""Fill"" UseLayoutRounding=""False""
                                        Opacity=""0.7"" Height=""25""
                                        VerticalAlignment=""Top"" />");

            path.Fill = _pathFillBrush;
            if (isUncertain)
                path.Opacity = 0.3f;
            grid.Children.Add(path);

            grid.SetValue(Canvas.LeftProperty, left);
            grid.SetValue(Canvas.TopProperty, top);
            Canvas.Children.Add(grid);
        }

        private void AddLightRectangle(double width, double height, double left, double top, double intensity)
        {
            //var grid = new Grid
            //{
            //    Width = 2,//width,
            //    Height = 2, //height
            //};

            var rectangle = new Rectangle
            {
                Fill = _realFillBrush,
                StrokeThickness = 0,
                Width = width,
                Height = height / 4,
                Opacity = intensity
            };

            //grid.Children.Add(rectangle);

            //grid.SetValue(Canvas.LeftProperty, left + (width - 2) / 2);
            //grid.SetValue(Canvas.TopProperty, top + (height - 2) / 2);
            //Canvas.Children.Add(grid);
            rectangle.SetValue(Canvas.LeftProperty, left);
            rectangle.SetValue(Canvas.TopProperty, top + (height - rectangle.Height) / 2);
            Canvas.Children.Add(rectangle);
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

    public enum Sort : byte
    {
        GMT,
        Name,
    }
}
