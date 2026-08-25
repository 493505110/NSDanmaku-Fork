using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using NSDanmaku.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace NSDanmaku.Controls
{
    public sealed partial class Danmaku : UserControl
    {
        public static float LogicalDpi { get; set; } = 0;
        /// <summary>
        /// 初始化弹幕DPI
        /// </summary>
        public static void InitDanmakuDpi()
        {
            try
            {
                Windows.Graphics.Display.DisplayInformation displayInformation = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
                LogicalDpi = displayInformation.LogicalDpi;
            }
            catch (Exception)
            {
                LogicalDpi = 96f;
            }
           
        }


        public Danmaku()
        {
            this.InitializeComponent();
            DanmakuArea = 1.0;
            DanmakuBold = false;
            DanmakuFontFamily = "";
        }
        #region 弹幕属性
        /// <summary>
        /// 字体大小缩放，电脑推荐默认1.0，手机推荐0.5
        /// </summary>
        public double DanmakuSizeZoom
        {
            get { return (double)GetValue(DanmakuSizeZoomProperty); }
            set { SetValue(DanmakuSizeZoomProperty, value); }
        }
       
        public static readonly DependencyProperty DanmakuSizeZoomProperty =
            DependencyProperty.Register("DanmakuSizeZoom", typeof(double), typeof(Danmaku), new PropertyMetadata(1.0, OnDanmakuSizeZoomChanged));

        private static void OnDanmakuSizeZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = Convert.ToDouble(e.NewValue);
            if (value > 3)
            {
                value = 3;
            }
            if (value < 0.1)
            {
                value = 0.1;
            }
            //DanmakuSizeZoom = value;

            ((Danmaku)d).SetFontDanmakuSizeZoom(value);
        }
        private void SetFontDanmakuSizeZoom(double value)
        {
            defaultRowHeight = 0;
            UpdateFontSize(grid_Scroll, value);
            UpdateFontSize(grid_Top, value);
            UpdateFontSize(grid_Bottom, value);
            RefreshRowHeights();
            SetRows(GetLayoutHeight());
        }

        private void UpdateFontSize(Grid container, double value)
        {
            foreach (var item in container.Children)
            {
                var grid = item as Grid;
                var model = grid?.Tag as DanmakuModel;
                if (model == null)
                {
                    continue;
                }

                foreach (var child in grid.Children)
                {
                    var textBlock = child as TextBlock;
                    if (textBlock != null)
                    {
                        textBlock.FontSize = model.size * value;
                    }
                }
            }
        }

        /// <summary>
        /// 滚动弹幕动画持续时间,单位:秒,越小弹幕移动速度越快
        /// </summary>
        public int DanmakuDuration
        {
            get { return (int)GetValue(DanmakuDurationProperty); }
            set { SetValue(DanmakuDurationProperty, value); }
        }

        public static readonly DependencyProperty DanmakuDurationProperty =
            DependencyProperty.Register("DanmakuDuration", typeof(int), typeof(Danmaku), new PropertyMetadata(10, OnDanmakuDurationChanged));
        private static void OnDanmakuDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = Convert.ToInt32(e.NewValue);
            if (value <= 0)
            {
                value = 1;
            }
           ((Danmaku)d).SetDanmakuDuration(value);
        }
        public void SetDanmakuDuration(int value)
        {
            DanmakuDuration = value;
        }


        /// <summary>
        /// 弹幕是否加粗
        /// </summary>
        public bool DanmakuBold
        {
            get { return (bool)GetValue(DanmakuBoldProperty); }
            set { SetValue(DanmakuBoldProperty, value); }
        }

        public static readonly DependencyProperty DanmakuBoldProperty =
            DependencyProperty.Register("DanmakuBold", typeof(bool), typeof(Danmaku), new PropertyMetadata(0));


        /// <summary>
        /// 弹幕字体名称
        /// </summary>
        public string DanmakuFontFamily
        {
            get { return (string)GetValue(DanmakuFontFamilyProperty); }
            set { SetValue(DanmakuFontFamilyProperty, value); }
        }

     
        public static readonly DependencyProperty DanmakuFontFamilyProperty =
            DependencyProperty.Register("DanmakuFontFamily", typeof(string), typeof(Danmaku), new PropertyMetadata(0));


        /// <summary>
        /// 弹幕样式
        /// </summary>
        public DanmakuBorderStyle DanmakuStyle
        {
            get { return (DanmakuBorderStyle)GetValue(DanmakuStyleProperty); }
            set { SetValue(DanmakuStyleProperty, value); }
        }
        public static readonly DependencyProperty DanmakuStyleProperty =
          DependencyProperty.Register("DanmakuStyle", typeof(DanmakuBorderStyle), typeof(Danmaku), new PropertyMetadata(DanmakuBorderStyle.Stroke));


        /// <summary>
        /// 弹幕显示区域，取值0.1-1.0
        /// </summary>
        public double DanmakuArea
        {
            get { return (double)GetValue(DanmakuAreaProperty); }
            set { SetValue(DanmakuAreaProperty, value); }
        }
       
        public static readonly DependencyProperty DanmakuAreaProperty =
            DependencyProperty.Register("DanmakuArea", typeof(double), typeof(Danmaku), new PropertyMetadata(1, OnDanmakuAreaChanged));

        private static void OnDanmakuAreaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = Convert.ToDouble(e.NewValue);
            if (value <=0)
            {
                value = 0.1;
            }
            if (value > 1)
            {
                value = 1;
            }

           ((Danmaku)d).SetDanmakuArea(value);
        }
        public void SetDanmakuArea(double value)
        {
            DanmakuArea = value;
        }
        #endregion

        //动画管理
        List<Storyboard> topBottomStoryList = new List<Storyboard>();
        List<Storyboard> rollStoryList = new List<Storyboard>();
        List<Storyboard> positionStoryList = new List<Storyboard>();
        Dictionary<Grid, double> measuredRowHeights = new Dictionary<Grid, double>();
        double defaultRowHeight;
        double layoutHeight;

        protected override Size MeasureOverride(Size availableSize)
        {
            layoutHeight = availableSize.Height;
            SetRows(availableSize.Height);
            return base.MeasureOverride(availableSize);
        }

        private void SetRows(double height)
        {
            SetRows(grid_Top, height);
            SetRows(grid_Bottom, height);
            SetRows(grid_Scroll, height);
        }

        private void SetRows(Grid container, double height)
        {
            if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
            {
                return;
            }

            var rowCount = Math.Max(1, (int)Math.Floor(height / GetDefaultRowHeight()));
            rowCount = Math.Max(rowCount, GetRequiredRowCount(container));

            while (container.RowDefinitions.Count < rowCount)
            {
                container.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(GetDefaultRowHeight(), GridUnitType.Pixel)
                });
            }

            while (container.RowDefinitions.Count > rowCount
                && !HasChildrenInRow(container, container.RowDefinitions.Count - 1))
            {
                container.RowDefinitions.RemoveAt(container.RowDefinitions.Count - 1);
            }
        }

        private int GetRequiredRowCount(Grid container)
        {
            var rowCount = 0;
            foreach (var child in container.Children)
            {
                var grid = child as Grid;
                if (grid != null)
                {
                    rowCount = Math.Max(rowCount, Grid.GetRow(grid) + 1);
                }
            }

            return rowCount;
        }

        private bool HasChildrenInRow(Grid container, int row)
        {
            foreach (var item in container.Children)
            {
                var grid = item as Grid;
                if (grid != null && Grid.GetRow(grid) == row)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshRowHeights()
        {
            RefreshRowHeights(grid_Top);
            RefreshRowHeights(grid_Bottom);
            RefreshRowHeights(grid_Scroll);
        }

        private void RefreshRowHeights(Grid container)
        {
            SetRows(container, GetLayoutHeight());

            foreach (var item in container.Children)
            {
                var grid = item as Grid;
                if (grid == null)
                {
                    continue;
                }

                measuredRowHeights[grid] = MeasureDanmakuHeight(grid);
            }

            for (int row = 0; row < container.RowDefinitions.Count; row++)
            {
                SetRowHeight(container, row);
            }
        }

        private void SetRowHeight(Grid container, int row)
        {
            if (row < 0 || row >= container.RowDefinitions.Count)
            {
                return;
            }

            var rowHeight = 0.0;
            foreach (var item in container.Children)
            {
                var grid = item as Grid;
                double measuredHeight;
                if (grid != null
                    && Grid.GetRow(grid) == row
                    && measuredRowHeights.TryGetValue(grid, out measuredHeight))
                {
                    rowHeight = Math.Max(rowHeight, measuredHeight);
                }
            }

            if (rowHeight <= 0)
            {
                rowHeight = GetDefaultRowHeight();
            }

            rowHeight = NormalizeRowHeight(rowHeight);
            var definition = container.RowDefinitions[row];
            if (definition.Height.GridUnitType != GridUnitType.Pixel
                || Math.Abs(definition.Height.Value - rowHeight) >= 0.1)
            {
                definition.Height = new GridLength(rowHeight, GridUnitType.Pixel);
            }
        }

        private double GetDefaultRowHeight()
        {
            if (defaultRowHeight > 0)
            {
                return defaultRowHeight;
            }

            var textBlock = new TextBlock
            {
                Text = "测试test",
                FontSize = 25 * DanmakuSizeZoom
            };
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            defaultRowHeight = NormalizeRowHeight(textBlock.DesiredSize.Height);
            return defaultRowHeight;
        }

        private static double NormalizeRowHeight(double height)
        {
            if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
            {
                return 1;
            }

            return Math.Max(1, Math.Ceiling(height));
        }

        private double GetLayoutHeight()
        {
            if (!double.IsNaN(ActualHeight) && !double.IsInfinity(ActualHeight) && ActualHeight > 0)
            {
                return ActualHeight;
            }

            return layoutHeight;
        }

        private void EnsureRowsForItem(Grid container, Grid item)
        {
            measuredRowHeights[item] = MeasureDanmakuHeight(item);
            SetRows(container, GetLayoutHeight());
        }

        private double MeasureDanmakuHeight(Grid grid)
        {
            var rowHeight = 0.0;
            foreach (var child in grid.Children)
            {
                var textBlock = child as TextBlock;
                if (textBlock != null)
                {
                    textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    var marginHeight = textBlock.Margin.Top + textBlock.Margin.Bottom;
                    var minimumHeight = textBlock.FontSize + marginHeight + 2;
                    rowHeight = Math.Max(rowHeight, Math.Max(textBlock.DesiredSize.Height, minimumHeight));
                    continue;
                }

                var element = child as FrameworkElement;
                if (element != null)
                {
                    element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    rowHeight = Math.Max(rowHeight, element.DesiredSize.Height);
                }
            }

            if (rowHeight <= 0)
            {
                grid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                rowHeight = grid.DesiredSize.Height;
            }
            else
            {
                rowHeight += grid.Margin.Top
                    + grid.Margin.Bottom
                    + grid.BorderThickness.Top
                    + grid.BorderThickness.Bottom;
            }

            return NormalizeRowHeight(rowHeight);
        }

        private void RemoveMeasuredRowHeight(Grid container, Grid item)
        {
            measuredRowHeights.Remove(item);
        }

        private void ResetRowHeights(Grid container)
        {
            var rowHeight = GetDefaultRowHeight();
            for (int row = 0; row < container.RowDefinitions.Count; row++)
            {
                var definition = container.RowDefinitions[row];
                if (definition.Height.GridUnitType != GridUnitType.Pixel
                    || Math.Abs(definition.Height.Value - rowHeight) >= 0.1)
                {
                    definition.Height = new GridLength(rowHeight, GridUnitType.Pixel);
                }
            }
        }
        private int GetTopAvailableRow()
        {
          
            var max = grid_Top.RowDefinitions.Count/2;
           
            for (int i = 0; i < max; i++)
            {
                
                var row = grid_Top.Children.FirstOrDefault(x=>Grid.GetRow((x as Grid)) == i);
                if (row!=null)
                {
                    continue;
                }
                else
                {
                    return i;
                }
                
            }
            return -1;
        }
        private int GetBottomAvailableRow()
        {

            var max = grid_Bottom.RowDefinitions.Count/2;
            for (int i = 1; i <= max; i++)
            {
                var rowNum = grid_Bottom.RowDefinitions.Count - i;
                var row = grid_Bottom.Children.FirstOrDefault(x => Grid.GetRow((x as Grid)) == rowNum);
                if (row != null)
                {
                    continue;
                }
                else
                {
                    return rowNum;
                }
            }
            //for (int i = grid_Bottom.RowDefinitions.Count; i >= 0; i--)
            //{
            //    var row = grid_Bottom.Children.FirstOrDefault(x => Grid.GetRow((x as Grid)) == i);
            //    if (row != null)
            //    {
            //        continue;
            //    }
            //    else
            //    {
            //        if (i>=max)
            //        {
            //            return i;
            //        }
                    
            //    }

            //}
            return -1;
        }
        private double GetScrollAvailableHeight()
        {
            var height = grid_Scroll.ActualHeight;
            if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
            {
                height = GetLayoutHeight();
            }

            if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
            {
                return 0;
            }

            var area = DanmakuArea;
            if (double.IsNaN(area) || double.IsInfinity(area))
            {
                area = 1;
            }

            area = Math.Max(0.1, Math.Min(1, area));
            var safetyHeight = Math.Max(2, Math.Min(8, Math.Ceiling(GetDefaultRowHeight() * 0.1)));
            return Math.Max(0, height * area - safetyHeight);
        }

        private double GetScrollRowHeight(int row)
        {
            if (row < 0 || row >= grid_Scroll.RowDefinitions.Count)
            {
                return 0;
            }

            var definition = grid_Scroll.RowDefinitions[row];
            if (definition.Height.GridUnitType == GridUnitType.Pixel
                && definition.Height.Value > 0
                && !double.IsNaN(definition.Height.Value)
                && !double.IsInfinity(definition.Height.Value))
            {
                return definition.Height.Value;
            }

            return GetDefaultRowHeight();
        }

        private bool CanFitScrollRow(int row, double itemHeight)
        {
            var availableHeight = GetScrollAvailableHeight();
            if (availableHeight <= 0 || itemHeight <= 0)
            {
                return false;
            }

            var usedHeight = 0.0;
            for (int i = 0; i <= row; i++)
            {
                var rowHeight = GetScrollRowHeight(i);
                if (i == row)
                {
                    rowHeight = Math.Max(rowHeight, itemHeight);
                }

                usedHeight += rowHeight;
                if (usedHeight > availableHeight + 0.1)
                {
                    return false;
                }
            }

            return true;
        }

        private int GetScrollAvailableRow(Grid item, bool reverse = false)
        {
            var width = grid_Scroll.ActualWidth;
            //计算弹幕尺寸
            item.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var newWidth = item.DesiredSize.Width;
            if (newWidth <= 0) return -1;

            double newHeight;
            if (!measuredRowHeights.TryGetValue(item, out newHeight))
            {
                newHeight = MeasureDanmakuHeight(item);
            }

            var max = grid_Scroll.RowDefinitions.Count;

            for (int i = 0; i < max; i++)
            {
                //每行的实际高度可能不同，累计高度不能超过滚动弹幕区域。
                if (!CanFitScrollRow(i, newHeight))
                {
                    break;
                }

                //1、检查当前行是否存在弹幕
                var lastItem=grid_Scroll.Children.LastOrDefault(x => Grid.GetRow((x as Grid)) == i);
                if (lastItem == null)
                {
                    return i;
                }

                var lastModel = (lastItem as Grid).Tag as DanmakuModel;
                if (lastModel != null
                    && (lastModel.location == DanmakuLocation.ReverseScroll) != reverse)
                {
                    continue;
                }

                var lastWidth = (lastItem as Grid).ActualWidth;
                var lastX = (lastItem.RenderTransform as TranslateTransform).X;

                //2、前弹幕必须已经完全从右侧移动完毕
                if ((!reverse && lastX > width - lastWidth)
                    || (reverse && lastX < 0))
                {
                    continue;
                }
                var lastPosition = reverse ? width - lastX - lastWidth : lastX;
                //3、后弹幕速度小于等于前弹幕速度
                var lastSpeed = (lastWidth + width) / DanmakuDuration;
                var newSpeed = (newWidth + width) / DanmakuDuration;
                if (newSpeed<= lastSpeed)
                {
                    return i;
                }
                //4、弹幕移动期间不会重叠
                var runDistance = width - lastPosition;
                var t1 = (runDistance - newWidth) / (newSpeed - lastSpeed);
                var t2 = lastPosition / lastSpeed;
                if (t1 > t2)
                {
                    return i;
                }    
            }
            return -1;
        }


        private async Task<Grid> CreateNewDanmuControl(DanmakuModel m)
        {
            switch (DanmakuStyle)
            {
                case DanmakuBorderStyle.WithoutStroke:
                    return DanmakuItemControl.CreateControlNoBorder((float)DanmakuSizeZoom, DanmakuBold, DanmakuFontFamily, m);
                case DanmakuBorderStyle.Stroke:
                    return await DanmakuItemControl.CreateControlBorder((float)DanmakuSizeZoom, DanmakuBold, DanmakuFontFamily, m);
                default:
                    return DanmakuItemControl.CreateControlOverlap((float)DanmakuSizeZoom, DanmakuBold, DanmakuFontFamily, m);
            }

        }
        /// <summary>
        /// 添加直播滚动弹幕
        /// </summary>
        /// <param name="text">参数</param>
        /// <param name="own">是否自己发送的</param>
        /// <param name="color">颜色</param>
        public async void AddLiveDanmu(string text, bool own, Color? color)
        {
            if (color == null)
            {
                color = Colors.White;
            }
            var m = new DanmakuModel()
            {
                text = text,
                color = color.Value,
                location = DanmakuLocation.Scroll,
                size = 25
            };
            Grid grid = await CreateNewDanmuControl(m);
            if (own)
            {
                grid.BorderBrush = new SolidColorBrush(color.Value);
                grid.BorderThickness = new Thickness(1);
            }
            EnsureRowsForItem(grid_Scroll, grid);
            var r = GetScrollAvailableRow(grid);
            if (r == -1)
            {
                RemoveMeasuredRowHeight(grid_Scroll, grid);
                return;
            }
            Grid.SetRow(grid, r);
            grid.HorizontalAlignment = HorizontalAlignment.Left;
            grid.VerticalAlignment = VerticalAlignment.Center;
            grid_Scroll.Children.Add(grid);
            SetRowHeight(grid_Scroll, r);
            grid_Scroll.UpdateLayout();

            TranslateTransform moveTransform = new TranslateTransform();
            moveTransform.X = gv.ActualWidth;
            grid.RenderTransform = moveTransform;

            //创建动画
            Duration duration = new Duration(TimeSpan.FromSeconds(DanmakuDuration));
            DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
            myDoubleAnimationX.Duration = duration;
            //创建故事版
            Storyboard moveStoryboard = new Storyboard();
            moveStoryboard.Duration = duration;
            myDoubleAnimationX.To = -(grid.ActualWidth);//到达
            moveStoryboard.Children.Add(myDoubleAnimationX);
            Storyboard.SetTarget(myDoubleAnimationX, moveTransform);
            //故事版加入动画
            Storyboard.SetTargetProperty(myDoubleAnimationX, "X");
            rollStoryList.Add(moveStoryboard);

            moveStoryboard.Completed += new EventHandler<object>((senders, obj) =>
            {
                grid_Scroll.Children.Remove(grid);
                RemoveMeasuredRowHeight(grid_Scroll, grid);
                grid.Children.Clear();
                grid = null;
                rollStoryList.Remove(moveStoryboard);
                moveStoryboard.Stop();
                moveStoryboard = null;
                SetRows(GetLayoutHeight());

            });
            moveStoryboard.Begin();
        }
        /// <summary>
        /// 添加一条弹幕
        /// </summary>
        /// <param name="m"></param>
        /// <param name="own"></param>
        /// <returns></returns>
        public async Task AddDanmu(DanmakuModel m, bool own)
        {
            switch (m.location)
            {
                case DanmakuLocation.Scroll:
                    await AddScrollDanmu(m, own);
                    break;
                case DanmakuLocation.ReverseScroll:
                    await AddReverseScrollDanmu(m, own);
                    break;
                case DanmakuLocation.Top:
                    await AddTopDanmu(m, own);
                    break;
                case DanmakuLocation.Bottom:
                    await AddBottomDanmu(m, own);
                    break;
                case DanmakuLocation.Position:
                    await AddPositionDanmu(m);
                    break;
                default:
                    //await AddScrollDanmu(m, own);
                    break;
            }
        }

       
        /// <summary>
        /// 添加滚动弹幕
        /// </summary>
        /// <param name="m">参数</param>
        /// <param name="own">是否自己发送的</param>
        public Task AddScrollDanmu(DanmakuModel m, bool own)
        {
            return AddHorizontalScrollDanmu(m, own, false);
        }

        /// <summary>
        /// 添加逆向滚动弹幕
        /// </summary>
        /// <param name="m">参数</param>
        /// <param name="own">是否自己发送的</param>
        public Task AddReverseScrollDanmu(DanmakuModel m, bool own)
        {
            return AddHorizontalScrollDanmu(m, own, true);
        }

        private async Task AddHorizontalScrollDanmu(DanmakuModel m, bool own, bool reverse)
        {
            Grid grid = await CreateNewDanmuControl(m);

            if (own)
            {
                grid.BorderBrush = new SolidColorBrush(m.color);
                grid.BorderThickness = new Thickness(1);
            }
            EnsureRowsForItem(grid_Scroll, grid);
            var r = GetScrollAvailableRow(grid, reverse);
            if (r == -1)
            {
                RemoveMeasuredRowHeight(grid_Scroll, grid);
                grid = null;
                return;
            }

            Grid.SetRow(grid, r);
            grid.HorizontalAlignment = HorizontalAlignment.Left;
            grid.VerticalAlignment = VerticalAlignment.Center;
            grid_Scroll.Children.Add(grid);
            SetRowHeight(grid_Scroll, r);
            grid_Scroll.UpdateLayout();

            TranslateTransform moveTransform = new TranslateTransform();
            var fromX = reverse ? -grid.ActualWidth : gv.ActualWidth;
            var toX = reverse ? gv.ActualWidth : -grid.ActualWidth;
            moveTransform.X = fromX;
            grid.RenderTransform = moveTransform;

            //创建动画
            Duration duration = new Duration(TimeSpan.FromSeconds(DanmakuDuration));
            DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
            myDoubleAnimationX.Duration = duration;
            //创建故事版
            Storyboard moveStoryboard = new Storyboard();
            moveStoryboard.Duration = duration;
            myDoubleAnimationX.To = toX;//到达
            moveStoryboard.Children.Add(myDoubleAnimationX);
            Storyboard.SetTarget(myDoubleAnimationX, moveTransform);
            //故事版加入动画
            Storyboard.SetTargetProperty(myDoubleAnimationX, "X");
            rollStoryList.Add(moveStoryboard);

            moveStoryboard.Completed += new EventHandler<object>((senders, obj) =>
            {
                grid_Scroll.Children.Remove(grid);
                RemoveMeasuredRowHeight(grid_Scroll, grid);
                grid.Children.Clear();
                grid = null;
                rollStoryList.Remove(moveStoryboard);
                moveStoryboard.Stop();
                moveStoryboard = null;
                SetRows(GetLayoutHeight());
            });
            moveStoryboard.Begin();


        }

        /// <summary>
        /// 添加图片滚动弹幕
        /// </summary>
        /// <param name="m">参数</param>
        public void AddScrollImageDanmu(BitmapImage m)
        {
            Grid grid = null;
            grid = DanmakuItemControl.CreateImageControl(m);
            EnsureRowsForItem(grid_Scroll, grid);
            var r = GetScrollAvailableRow(grid);
            if (r == -1)
            {
                RemoveMeasuredRowHeight(grid_Scroll, grid);
                return;
            }
            Grid.SetRow(grid, r);
            grid.HorizontalAlignment = HorizontalAlignment.Left;
            grid.VerticalAlignment = VerticalAlignment.Center;
            grid_Scroll.Children.Add(grid);
            SetRowHeight(grid_Scroll, r);
            grid_Scroll.UpdateLayout();

            TranslateTransform moveTransform = new TranslateTransform();
            moveTransform.X = gv.ActualWidth;
            grid.RenderTransform = moveTransform;

            //创建动画
            Duration duration = new Duration(TimeSpan.FromSeconds(DanmakuDuration));
            DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
            myDoubleAnimationX.Duration = duration;
            //创建故事版
            Storyboard moveStoryboard = new Storyboard();
            moveStoryboard.Duration = duration;
            myDoubleAnimationX.To = -(grid.ActualWidth);//到达
            moveStoryboard.Children.Add(myDoubleAnimationX);
            Storyboard.SetTarget(myDoubleAnimationX, moveTransform);
            //故事版加入动画
            Storyboard.SetTargetProperty(myDoubleAnimationX, "X");
            rollStoryList.Add(moveStoryboard);

            moveStoryboard.Completed += new EventHandler<object>((senders, obj) =>
            {

                grid_Scroll.Children.Remove(grid);
                RemoveMeasuredRowHeight(grid_Scroll, grid);
                grid = null;
                rollStoryList.Remove(moveStoryboard);
                SetRows(GetLayoutHeight());

            });
            moveStoryboard.Begin();


        }
        /// <summary>
        ///  添加顶部弹幕
        /// </summary>
        /// <param name="m">参数</param>
        /// <param name="own">是否自己发送的</param>
        public async Task AddTopDanmu(DanmakuModel m, bool own)
        {

            Grid grid = await CreateNewDanmuControl(m);
            if (own)
            {
                grid.BorderBrush = new SolidColorBrush(m.color);
                grid.BorderThickness = new Thickness(1);
            }

            EnsureRowsForItem(grid_Top, grid);
            var r = GetTopAvailableRow();
            if (r == -1)
            {
                RemoveMeasuredRowHeight(grid_Top, grid);
                return;
            }

            grid.HorizontalAlignment = HorizontalAlignment.Center;
            grid.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetRow(grid, r);
            grid_Top.Children.Add(grid);
            SetRowHeight(grid_Top, r);


            //创建空转换动画
            TranslateTransform moveTransform = new TranslateTransform();
            //创建动画
            Duration duration = new Duration(TimeSpan.FromSeconds(5));
            DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
            myDoubleAnimationX.Duration = duration;
            //创建故事版
            Storyboard moveStoryboard = new Storyboard();
            moveStoryboard.Duration = duration;
            moveStoryboard.Children.Add(myDoubleAnimationX);
            Storyboard.SetTarget(myDoubleAnimationX, moveTransform);
            //故事版加入动画
            Storyboard.SetTargetProperty(myDoubleAnimationX, "X");
            topBottomStoryList.Add(moveStoryboard);

            moveStoryboard.Completed += new EventHandler<object>((senders, obj) =>
            {
                grid_Top.Children.Remove(grid);
                RemoveMeasuredRowHeight(grid_Top, grid);
                grid.Children.Clear();
                grid = null;
                topBottomStoryList.Remove(moveStoryboard);
                moveStoryboard.Stop();
                moveStoryboard = null;
                SetRows(GetLayoutHeight());

            });
            moveStoryboard.Begin();
        }
        /// <summary>
        ///  添加底部弹幕
        /// </summary>
        /// <param name="m">参数</param>
        /// <param name="own">是否自己发送的</param>
        public async Task AddBottomDanmu(DanmakuModel m, bool own)
        {
            Grid grid = await CreateNewDanmuControl(m);
            if (own)
            {
                grid.BorderBrush = new SolidColorBrush(m.color);
                grid.BorderThickness = new Thickness(1);
            }
            grid.HorizontalAlignment = HorizontalAlignment.Center;
            grid.VerticalAlignment = VerticalAlignment.Top;
            EnsureRowsForItem(grid_Bottom, grid);
            var row = GetBottomAvailableRow();
            if (row == -1)
            {
                RemoveMeasuredRowHeight(grid_Bottom, grid);
                return;
            }
            Grid.SetRow(grid, row);
            grid_Bottom.Children.Add(grid);
            SetRowHeight(grid_Bottom, row);


            //创建空转换动画
            TranslateTransform moveTransform = new TranslateTransform();
            //创建动画
            Duration duration = new Duration(TimeSpan.FromSeconds(5));
            DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
            myDoubleAnimationX.Duration = duration;
            //创建故事版
            Storyboard moveStoryboard = new Storyboard();
            moveStoryboard.Duration = duration;
            moveStoryboard.Children.Add(myDoubleAnimationX);
            Storyboard.SetTarget(myDoubleAnimationX, moveTransform);
            //故事版加入动画   
            Storyboard.SetTargetProperty(myDoubleAnimationX, "X");
            topBottomStoryList.Add(moveStoryboard);

            moveStoryboard.Completed += new EventHandler<object>((senders, obj) =>
            {
                grid_Bottom.Children.Remove(grid);
                RemoveMeasuredRowHeight(grid_Bottom, grid);
                grid.Children.Clear();
                grid = null;
                topBottomStoryList.Remove(moveStoryboard);
                moveStoryboard.Stop();
                moveStoryboard = null;
                SetRows(GetLayoutHeight());
            });
            moveStoryboard.Begin();
        }
        /// <summary>
        /// 添加定位弹幕
        /// </summary>
        /// <param name="m"></param>
        public async Task AddPositionDanmu(DanmakuModel m)
        {
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(m.text);
            m.text = data[4].ToString().Replace("/n", "\r\n");
             Grid grid = await CreateNewDanmuControl(m); ;
            var DanmakuFontFamilyFamily = data[data.Length - 2].ToString();
            
            grid.Tag = m;
            grid.HorizontalAlignment = HorizontalAlignment.Left;
            grid.VerticalAlignment = VerticalAlignment.Center;

            double toX = 0;
            double toY = 0;

            double X = 0, Y = 0;
            double dur = 0;

            if (data.Length > 7)
            {
                X =data[0].ToDouble();
                Y = data[1].ToDouble();

                toX =data[7].ToDouble();
                toY = data[8].ToDouble();

                dur = data[10].ToDouble();

            }
            else
            {
                toX = data[0].ToDouble();
                toY =data[1].ToDouble();
            }
            if (toX < 1 && toY < 1)
            {
                toX = this.ActualWidth * toX;
                toY = this.ActualHeight * toY;
            }
            if (X < 1 && Y < 1)
            {
                X = this.ActualWidth * X;
                Y = this.ActualHeight * Y;
            }

            if (data.Length >= 7)
            {
                var rotateZ = data[5].ToDouble();
                var rotateY = data[6].ToDouble();
                PlaneProjection projection = new PlaneProjection();
                projection.RotationZ = -rotateZ;
                projection.RotationY = rotateY;
                grid.Projection = projection;
            }

            //Canvas.SetLeft(grid, toX);
            //Canvas.SetTop(grid, toY);

            canvas.Children.Add(grid);


            double dmDuration = data[3].ToDouble();
            var opacitys = data[2].ToString().Split('-');
            double opacityFrom = opacitys[0].ToDouble();
            double opacityTo = opacitys[1].ToDouble();

            //创建故事版
            Storyboard moveStoryboard = new Storyboard();


            //if (X != toX || Y != toY)
            //{
            Duration duration = new Duration(TimeSpan.FromMilliseconds(dur));
            {
                DoubleAnimation myDoubleAnimationY = new DoubleAnimation();
                myDoubleAnimationY.Duration = duration;
                myDoubleAnimationY.From = Y;
                myDoubleAnimationY.To = toY;


                Storyboard.SetTarget(myDoubleAnimationY, grid);
                Storyboard.SetTargetProperty(myDoubleAnimationY, "(Canvas.Top)");
                moveStoryboard.Children.Add(myDoubleAnimationY);
            }
            {
                DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
                myDoubleAnimationX.Duration = duration;
                myDoubleAnimationX.From = X;
                myDoubleAnimationX.To = toX;
                Storyboard.SetTarget(myDoubleAnimationX, grid);
                Storyboard.SetTargetProperty(myDoubleAnimationX, "(Canvas.Left)");
                moveStoryboard.Children.Add(myDoubleAnimationX);
            }
            //}
            //else
            //{
            //    Canvas.SetTop(grid,toY);
            //    Canvas.SetLeft(grid,toX);
            //}

            //透明度动画 
            DoubleAnimation opacityAnimation = new DoubleAnimation()
            {
                Duration = new Duration(TimeSpan.FromSeconds(dmDuration)),
                From = opacityFrom,
                To = opacityTo
            };
            Storyboard.SetTarget(opacityAnimation, grid);
            Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
            moveStoryboard.Children.Add(opacityAnimation);



            positionStoryList.Add(moveStoryboard);

            moveStoryboard.Completed += new EventHandler<object>((senders, obj) =>
            {
                canvas.Children.Remove(grid);
                positionStoryList.Remove(moveStoryboard);
            });
            moveStoryboard.Begin();
        }
        #region 弹幕控制方法
        /// <summary>
        /// 暂停弹幕
        /// </summary>
        public void PauseDanmaku()
        {
            foreach (var item in topBottomStoryList)
            {
                item.Pause();
            }
            foreach (var item in rollStoryList)
            {
                item.Pause();
            }
            foreach (var item in positionStoryList)
            {
                item.Pause();
            }
        }
        /// <summary>
        /// 继续弹幕
        /// </summary>
        public void ResumeDanmaku()
        {
            foreach (var item in topBottomStoryList)
            {
                item.Resume();
            }
            foreach (var item in rollStoryList)
            {
                item.Resume();
            }
            foreach (var item in positionStoryList)
            {
                item.Resume();
            }
        }
        /// <summary>
        /// 移除指定弹幕
        /// </summary>
        /// <param name="danmaku"></param>
        public void Remove(DanmakuModel danmaku)
        {
            switch (danmaku.location)
            {
                case DanmakuLocation.Top:
                    RemoveFromRowContainer(grid_Top, danmaku);
                    break;
                case DanmakuLocation.Bottom:
                    RemoveFromRowContainer(grid_Bottom, danmaku);
                    break;
                case DanmakuLocation.Scroll:
                case DanmakuLocation.ReverseScroll:
                case DanmakuLocation.Other:
                    RemoveFromRowContainer(grid_Scroll, danmaku);
                    break;
                default:
                    break;
            }
        }

        private void RemoveFromRowContainer(Grid container, DanmakuModel danmaku)
        {
            Grid target = null;
            foreach (Grid item in container.Children)
            {
                if (item.Tag as DanmakuModel == danmaku)
                {
                    target = item;
                    break;
                }
            }

            if (target == null)
            {
                return;
            }

            container.Children.Remove(target);
            RemoveMeasuredRowHeight(container, target);
            SetRows(GetLayoutHeight());
        }
        /// <summary>
        /// 清空弹幕
        /// </summary>
        public void ClearAll()
        {
            topBottomStoryList.Clear();
            rollStoryList.Clear();
            grid_Bottom.Children.Clear();
            grid_Top.Children.Clear();
            grid_Scroll.Children.Clear();
            measuredRowHeights.Clear();
            defaultRowHeight = 0;
            SetRows(GetLayoutHeight());
            ResetRowHeights(grid_Top);
            ResetRowHeights(grid_Bottom);
            ResetRowHeights(grid_Scroll);

        }

        /// <summary>
        /// 读取屏幕上的全部弹幕
        /// </summary>
        /// <param name="danmakuLocation">类型</param>
        /// <returns></returns>
        public List<DanmakuModel> GetDanmakus(DanmakuLocation? danmakuLocation = null)
        {
            List<DanmakuModel> danmakus = new List<DanmakuModel>();
            if (danmakuLocation == null || danmakuLocation == DanmakuLocation.Top)
            {
                foreach (Grid item in grid_Top.Children)
                {
                    danmakus.Add(item.Tag as DanmakuModel);
                }
            }
            if (danmakuLocation == null || danmakuLocation == DanmakuLocation.Bottom)
            {
                foreach (Grid item in grid_Bottom.Children)
                {
                    danmakus.Add(item.Tag as DanmakuModel);
                }
            }
            if (danmakuLocation == null
                || danmakuLocation == DanmakuLocation.Scroll
                || danmakuLocation == DanmakuLocation.ReverseScroll)
            {
                foreach (Grid item in grid_Scroll.Children)
                {
                    var model = item.Tag as DanmakuModel;
                    if (danmakuLocation == null
                        || model == null
                        || model.location == danmakuLocation)
                    {
                        danmakus.Add(model);
                    }
                }
            }
            return danmakus;
        }

        /// <summary>
        /// 隐藏弹幕
        /// </summary>
        /// <param name="location">需要隐藏的位置</param>
        public void HideDanmaku(DanmakuLocation location)
        {
            switch (location)
            {
                case DanmakuLocation.Scroll:
                case DanmakuLocation.ReverseScroll:
                    grid_Scroll.Visibility = Visibility.Collapsed;
                    break;
                case DanmakuLocation.Top:
                    grid_Top.Visibility = Visibility.Collapsed;
                    break;
                case DanmakuLocation.Bottom:
                    grid_Bottom.Visibility = Visibility.Collapsed;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 显示弹幕
        /// </summary>
        /// <param name="location">需要显示的位置</param>
        public void ShowDanmaku(DanmakuLocation location)
        {
            switch (location)
            {
                case DanmakuLocation.Scroll:
                case DanmakuLocation.ReverseScroll:
                    grid_Scroll.Visibility = Visibility.Visible;
                    break;
                case DanmakuLocation.Top:
                    grid_Top.Visibility = Visibility.Visible;
                    break;
                case DanmakuLocation.Bottom:
                    grid_Bottom.Visibility = Visibility.Visible;
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}
