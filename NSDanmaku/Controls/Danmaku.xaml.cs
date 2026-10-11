using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using NSDanmaku.Model;
using NSDanmaku.Helper;
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
            InitializePositionDanmaku();
            DanmakuArea = 1.0;
            DanmakuBold = true;
            DanmakuFontFamily = "黑体";
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
            ClearPositionDanmaku();
            positionSuspended = false;
            UpdatePositionDanmaku();
            UpdatePositionRendering();
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
            DependencyProperty.Register("DanmakuDuration", typeof(int), typeof(Danmaku), new PropertyMetadata(5, OnDanmakuDurationChanged));
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
            scrollReferenceDuration = Math.Max(MinimumScrollDuration, value);
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
            DependencyProperty.Register("DanmakuBold", typeof(bool), typeof(Danmaku), new PropertyMetadata(true));


        /// <summary>
        /// 弹幕字体名称
        /// </summary>
        public string DanmakuFontFamily
        {
            get { return (string)GetValue(DanmakuFontFamilyProperty); }
            set { SetValue(DanmakuFontFamilyProperty, value); }
        }

     
        public static readonly DependencyProperty DanmakuFontFamilyProperty =
            DependencyProperty.Register("DanmakuFontFamily", typeof(string), typeof(Danmaku), new PropertyMetadata("黑体"));


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
        List<FixedDanmakuLifetime> topBottomStoryList = new List<FixedDanmakuLifetime>();
        List<Storyboard> rollStoryList = new List<Storyboard>();
        // 滚动弹幕的故事板与动画目标的对应关系。UWP 没有 Storyboard.GetTarget，
        // 暂停/恢复时只能靠这里找到一条弹幕此刻该停在什么位置。
        readonly Dictionary<Storyboard, TranslateTransform> scrollTransforms = new Dictionary<Storyboard, TranslateTransform>();
        private const double FixedDanmakuDuration = 3.5;
        private const int FixedDanmakuTickMilliseconds = 350;
        private const int FixedDanmakuTickCount = 10;
        private const double OfficialDanmakuWidth = 543.0;
        private const double MinimumScrollDuration = 0.1;
        private const double PoolGap = 1.0;
        private double scrollReferenceDuration = 5;
        private long placementVersion;
        private int placementBatchDepth;
        private sealed class DanmakuSpaceItem
        {
            public Grid Element { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public double LogicalY { get; set; }
            public double StartTime { get; set; }
            public List<DanmakuSpaceItem> Pool { get; set; }
            public TranslateTransform Transform { get; set; }
            public long PositionVersion { get; set; }
            public double PositionX { get; set; }
        }

        private sealed class FixedDanmakuLifetime
        {
            private readonly DispatcherTimer timer;
            private readonly Action completed;
            private int tickCount;
            private bool stopped;

            public FixedDanmakuLifetime(Action completed)
            {
                this.completed = completed;
                timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(FixedDanmakuTickMilliseconds)
                };
                timer.Tick += OnTick;
            }

            public void Start()
            {
                if (!stopped)
                {
                    timer.Start();
                }
            }

            public void Pause()
            {
                if (!stopped)
                {
                    timer.Stop();
                }
            }

            public void Resume()
            {
                Start();
            }

            public void Stop()
            {
                if (stopped)
                {
                    return;
                }

                stopped = true;
                timer.Stop();
                timer.Tick -= OnTick;
            }

            private void OnTick(object sender, object args)
            {
                tickCount++;
                if (tickCount < FixedDanmakuTickCount)
                {
                    return;
                }

                Stop();
                completed?.Invoke();
            }
        }

        private readonly List<List<DanmakuSpaceItem>> topPools = new List<List<DanmakuSpaceItem>>();
        private readonly List<List<DanmakuSpaceItem>> bottomPools = new List<List<DanmakuSpaceItem>>();
        private readonly List<List<DanmakuSpaceItem>> scrollPools = new List<List<DanmakuSpaceItem>>();
        private readonly List<List<DanmakuSpaceItem>> reverseScrollPools = new List<List<DanmakuSpaceItem>>();
        private readonly Dictionary<Grid, DanmakuSpaceItem> spaceItems = new Dictionary<Grid, DanmakuSpaceItem>();
        private readonly Dictionary<Grid, FixedDanmakuLifetime> fixedLifetimes = new Dictionary<Grid, FixedDanmakuLifetime>();
        Dictionary<Grid, double> measuredRowHeights = new Dictionary<Grid, double>();
        private readonly Dictionary<Grid, DanmakuRowHeightIndex> rowHeightIndexes =
            new Dictionary<Grid, DanmakuRowHeightIndex>();
        double defaultRowHeight;
        double layoutHeight;

        private double GetScrollViewportWidth()
        {
            var width = gv.ActualWidth;
            if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
            {
                width = grid_Scroll.ActualWidth;
            }

            if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
            {
                width = ActualWidth;
            }

            return double.IsNaN(width) || double.IsInfinity(width) || width <= 0
                ? OfficialDanmakuWidth
                : width;
        }

        private double GetDanmakuWidth(Grid item)
        {
            if (item == null)
            {
                return 0;
            }

            var width = item.ActualWidth;
            if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
            {
                item.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                width = item.DesiredSize.Width;
            }

            return double.IsNaN(width) || double.IsInfinity(width) || width <= 0
                ? 0
                : width;
        }

        private double GetScrollReferenceDuration()
        {
            return scrollReferenceDuration;
        }

        public void BeginDanmakuBatch()
        {
            if (placementBatchDepth++ == 0) placementVersion++;
        }

        public void EndDanmakuBatch()
        {
            if (placementBatchDepth > 0) placementBatchDepth--;
        }

        //Official player speed: speede * 0.5 * (543 + itemWidth) / 3.
        private double GetScrollSpeed(double itemWidth)
        {
            itemWidth = Math.Max(0, itemWidth);
            return (OfficialDanmakuWidth + itemWidth) / GetScrollReferenceDuration();
        }

        private double GetScrollDuration(double viewportWidth, double itemWidth)
        {
            if (double.IsNaN(viewportWidth) || double.IsInfinity(viewportWidth) || viewportWidth <= 0)
            {
                viewportWidth = OfficialDanmakuWidth;
            }

            itemWidth = Math.Max(0, itemWidth);
            return (viewportWidth + itemWidth) / GetScrollSpeed(itemWidth);
        }

        private double GetDanmakuClock()
        {
            return (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
        }

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
            var index = new DanmakuRowHeightIndex();
            rowHeightIndexes[container] = index;

            foreach (var item in container.Children)
            {
                var grid = item as Grid;
                if (grid == null)
                {
                    continue;
                }

                var height = MeasureDanmakuHeight(grid);
                measuredRowHeights[grid] = height;
                if (Grid.GetRow(grid) == 0) index.Add(height);
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
            DanmakuRowHeightIndex heightIndex;
            if (row == 0 && rowHeightIndexes.TryGetValue(container, out heightIndex))
            {
                rowHeight = heightIndex.Maximum;
            }
            else
            {
                foreach (var item in container.Children)
                {
                    var grid = item as Grid;
                    double measuredHeight;
                    if (grid != null && Grid.GetRow(grid) == row
                        && measuredRowHeights.TryGetValue(grid, out measuredHeight))
                    {
                        rowHeight = Math.Max(rowHeight, measuredHeight);
                    }
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
            UpdateMeasuredRowHeight(container, item, MeasureDanmakuHeight(item));
            // 新弹幕都放在第 0 行，后续行只需在布局尺寸变化时由 MeasureOverride 调整。
            if (container.RowDefinitions.Count == 0) SetRows(container, GetLayoutHeight());
        }

        private void UpdateMeasuredRowHeight(Grid container, Grid item, double height)
        {
            DanmakuRowHeightIndex index;
            if (!rowHeightIndexes.TryGetValue(container, out index))
            {
                index = new DanmakuRowHeightIndex();
                rowHeightIndexes[container] = index;
            }
            double previous;
            if (measuredRowHeights.TryGetValue(item, out previous)) index.Remove(previous);
            measuredRowHeights[item] = height;
            if (Grid.GetRow(item) == 0) index.Add(height);
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
            double height;
            DanmakuRowHeightIndex index;
            if (measuredRowHeights.TryGetValue(item, out height)
                && rowHeightIndexes.TryGetValue(container, out index)
                && Grid.GetRow(item) == 0)
            {
                index.Remove(height);
            }
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
        private double GetFixedAvailableHeight(Grid container)
        {
            var height = container.ActualHeight;
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

            return Math.Max(0, height * Math.Max(0.1, Math.Min(1, area)));
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
            return Math.Max(0, height * area);
        }

        private double GetSpaceStartTime(DanmakuModel model)
        {
            if (model != null
                && !double.IsNaN(model.time)
                && !double.IsInfinity(model.time)
                && model.time > 0)
            {
                return model.time;
            }

            return GetDanmakuClock();
        }

        private static void InsertSpaceItem(List<DanmakuSpaceItem> pool, DanmakuSpaceItem item)
        {
            var itemBottom = item.LogicalY + item.Height;
            var index = pool.FindIndex(existing => existing.LogicalY + existing.Height > itemBottom);
            if (index < 0)
            {
                pool.Add(item);
            }
            else
            {
                pool.Insert(index, item);
            }
        }

        private void ReleaseSpace(Grid element)
        {
            DanmakuSpaceItem item;
            if (element == null || !spaceItems.TryGetValue(element, out item))
            {
                return;
            }

            if (item.Pool != null)
            {
                item.Pool.Remove(item);
            }

            item.Pool = null;
            spaceItems.Remove(element);
        }

        private void RemoveFixedDanmaku(Grid container, Grid element)
        {
            if (element == null)
            {
                return;
            }

            FixedDanmakuLifetime lifetime;
            if (fixedLifetimes.TryGetValue(element, out lifetime))
            {
                lifetime.Stop();
                fixedLifetimes.Remove(element);
                topBottomStoryList.Remove(lifetime);
            }

            ReleaseSpace(element);
            container.Children.Remove(element);
            RemoveMeasuredRowHeight(container, element);
            element.Children.Clear();
            SetRowHeight(container, 0);
        }

        private List<List<DanmakuSpaceItem>> GetFixedPools(bool fromBottom)
        {
            return fromBottom ? bottomPools : topPools;
        }

        private bool IsFixedPoolAvailable(
            List<DanmakuSpaceItem> pool,
            double logicalY,
            double itemHeight,
            double availableHeight,
            bool fromBottom)
        {
            var itemBottom = logicalY + itemHeight;
            foreach (var occupied in pool)
            {
                if (!fromBottom)
                {
                    if (!(occupied.LogicalY > itemBottom
                        || occupied.LogicalY + occupied.Height < logicalY))
                    {
                        return false;
                    }
                }
                else
                {
                    var occupiedTop = availableHeight - occupied.LogicalY - occupied.Height;
                    var occupiedLogicalY = availableHeight - occupiedTop - occupied.Height;
                    if (!(occupiedLogicalY > itemBottom
                        || occupiedLogicalY + occupied.Height < logicalY))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool TryGetFixedPoolPosition(
            List<DanmakuSpaceItem> pool,
            double itemHeight,
            double availableHeight,
            bool fromBottom,
            out double logicalY)
        {
            logicalY = 0;
            if (pool.Count == 0
                || IsFixedPoolAvailable(pool, 0, itemHeight, availableHeight, fromBottom))
            {
                return true;
            }

            foreach (var occupied in pool)
            {
                logicalY = occupied.LogicalY + occupied.Height + PoolGap;
                if (logicalY + itemHeight > availableHeight)
                {
                    break;
                }

                if (IsFixedPoolAvailable(pool, logicalY, itemHeight, availableHeight, fromBottom))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryReserveFixedSpace(
            Grid element,
            DanmakuModel model,
            bool fromBottom,
            out DanmakuSpaceItem space,
            out double visualTop)
        {
            space = null;
            visualTop = 0;
            var availableHeight = GetFixedAvailableHeight(fromBottom ? grid_Bottom : grid_Top);
            double itemHeight;
            if (!measuredRowHeights.TryGetValue(element, out itemHeight))
            {
                itemHeight = MeasureDanmakuHeight(element);
            }
            if (availableHeight <= 0 || itemHeight <= 0)
            {
                return false;
            }

            space = new DanmakuSpaceItem
            {
                Element = element,
                Width = GetDanmakuWidth(element),
                Height = itemHeight,
                StartTime = GetSpaceStartTime(model)
            };

            var pools = GetFixedPools(fromBottom);
            if (itemHeight < availableHeight)
            {
                for (var poolIndex = 0; ; poolIndex++)
                {
                    while (pools.Count <= poolIndex)
                    {
                        pools.Add(new List<DanmakuSpaceItem>());
                    }

                    var pool = pools[poolIndex];
                    double logicalY;
                    if (!TryGetFixedPoolPosition(pool, itemHeight, availableHeight, fromBottom, out logicalY))
                    {
                        continue;
                    }

                    space.LogicalY = logicalY;
                    space.Pool = pool;
                    InsertSpaceItem(pool, space);
                    visualTop = fromBottom
                        ? availableHeight - logicalY - itemHeight
                        : logicalY;
                    spaceItems[element] = space;
                    return true;
                }
            }

            //The original player renders an over-height comment at offset zero
            //without reserving a pool entry.
            spaceItems[element] = space;
            visualTop = fromBottom ? availableHeight - itemHeight : 0;
            return true;
        }

        private double GetScrollInitialX(bool reverse, double viewportWidth, double itemWidth)
        {
            return reverse ? -itemWidth : viewportWidth;
        }

        private double GetCurrentScrollX(
            DanmakuSpaceItem item,
            bool reverse,
            double viewportWidth,
            double comparisonTime)
        {
            if (item.PositionVersion == placementVersion) return item.PositionX;
            var transform = item.Transform;
            if (transform != null)
            {
                var x = transform.X;
                if (!double.IsNaN(x) && !double.IsInfinity(x))
                {
                    item.PositionVersion = placementVersion;
                    item.PositionX = x;
                    return x;
                }
            }

            var elapsed = Math.Max(0, comparisonTime - item.StartTime);
            var distance = GetScrollSpeed(item.Width) * elapsed;
            return GetScrollInitialX(reverse, viewportWidth, item.Width)
                + (reverse ? distance : -distance);
        }

        private bool TryGetScrollPoolPosition(
            List<DanmakuSpaceItem> pool,
            DanmakuSpaceItem item,
            bool reverse,
            double viewportWidth,
            double availableHeight,
            out double logicalY)
        {
            // mode6 仍按原规则用 viewportWidth 判定进入轨道时的碰撞。
            return ScrollDanmakuPlacement.TryFindPosition(
                pool,
                occupied => new ScrollDanmakuSpace
                {
                    Y = occupied.LogicalY,
                    Height = occupied.Height,
                    X = GetCurrentScrollX(occupied, reverse, viewportWidth, item.StartTime),
                    Width = occupied.Width,
                    EndTime = GetScrollEnd(occupied, viewportWidth)
                },
                item.Width,
                item.Height,
                viewportWidth,
                GetScrollMiddle(item, viewportWidth),
                availableHeight,
                out logicalY);
        }

        private bool TryReserveScrollSpace(
            Grid element,
            DanmakuModel model,
            bool reverse,
            out DanmakuSpaceItem space,
            out double logicalY)
        {
            if (placementBatchDepth == 0) placementVersion++;
            space = null;
            logicalY = 0;
            var viewportWidth = GetScrollViewportWidth();
            var availableHeight = GetScrollAvailableHeight();
            var itemWidth = GetDanmakuWidth(element);
            double itemHeight;
            if (!measuredRowHeights.TryGetValue(element, out itemHeight))
            {
                itemHeight = MeasureDanmakuHeight(element);
            }
            if (availableHeight <= 0 || itemWidth <= 0 || itemHeight <= 0)
            {
                return false;
            }

            space = new DanmakuSpaceItem
            {
                Element = element,
                Width = itemWidth,
                Height = itemHeight,
                StartTime = GetSpaceStartTime(model)
            };

            if (itemHeight < availableHeight)
            {
                var pools = reverse ? reverseScrollPools : scrollPools;
                for (var poolIndex = 0; ; poolIndex++)
                {
                    while (pools.Count <= poolIndex)
                    {
                        pools.Add(new List<DanmakuSpaceItem>());
                    }

                    var pool = pools[poolIndex];
                    if (!TryGetScrollPoolPosition(
                        pool,
                        space,
                        reverse,
                        viewportWidth,
                        availableHeight,
                        out logicalY))
                    {
                        continue;
                    }

                    space.LogicalY = logicalY;
                    space.Pool = pool;
                    InsertSpaceItem(pool, space);
                    spaceItems[element] = space;
                    return true;
                }
            }

            //The original player renders an over-height comment at offset zero
            //without reserving a pool entry.
            spaceItems[element] = space;
            return true;
        }

        private double GetScrollEnd(DanmakuSpaceItem item, double viewportWidth)
        {
            return item.StartTime + GetScrollDuration(viewportWidth, item.Width);
        }

        private double GetScrollMiddle(DanmakuSpaceItem item, double viewportWidth)
        {
            return item.StartTime + viewportWidth / GetScrollSpeed(item.Width);
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
            DanmakuSpaceItem space;
            double logicalY;
            if (!TryReserveScrollSpace(grid, m, false, out space, out logicalY))
            {
                RemoveMeasuredRowHeight(grid_Scroll, grid);
                return;
            }
            Grid.SetRow(grid, 0);
            grid.HorizontalAlignment = HorizontalAlignment.Left;
            grid.VerticalAlignment = VerticalAlignment.Top;
            grid_Scroll.Children.Add(grid);
            SetRowHeight(grid_Scroll, 0);

            TranslateTransform moveTransform = new TranslateTransform();
            var viewportWidth = GetScrollViewportWidth();
            var itemWidth = space.Width;
            moveTransform.X = GetScrollInitialX(false, viewportWidth, itemWidth);
            moveTransform.Y = logicalY;
            grid.RenderTransform = moveTransform;
            space.Transform = moveTransform;

            //创建动画
            Duration duration = new Duration(TimeSpan.FromSeconds(GetScrollDuration(viewportWidth, itemWidth)));
            DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
            myDoubleAnimationX.Duration = duration;
            //创建故事版
            Storyboard moveStoryboard = new Storyboard();
            moveStoryboard.Duration = duration;
            myDoubleAnimationX.From = moveTransform.X;
            myDoubleAnimationX.To = -itemWidth;//到达
            moveStoryboard.Children.Add(myDoubleAnimationX);
            Storyboard.SetTarget(myDoubleAnimationX, moveTransform);
            //故事版加入动画
            Storyboard.SetTargetProperty(myDoubleAnimationX, "X");
            rollStoryList.Add(moveStoryboard);
            scrollTransforms[moveStoryboard] = moveTransform;

            moveStoryboard.Completed += new EventHandler<object>((senders, obj) =>
            {
                ReleaseSpace(grid);
                grid_Scroll.Children.Remove(grid);
                RemoveMeasuredRowHeight(grid_Scroll, grid);
                SetRowHeight(grid_Scroll, 0);
                grid.Children.Clear();
                grid = null;
                rollStoryList.Remove(moveStoryboard);
                scrollTransforms.Remove(moveStoryboard);
                moveStoryboard.Stop();
                moveStoryboard = null;

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
            DanmakuSpaceItem space;
            double logicalY;
            if (!TryReserveScrollSpace(grid, m, reverse, out space, out logicalY))
            {
                RemoveMeasuredRowHeight(grid_Scroll, grid);
                grid = null;
                return;
            }

            Grid.SetRow(grid, 0);
            grid.HorizontalAlignment = HorizontalAlignment.Left;
            grid.VerticalAlignment = VerticalAlignment.Top;
            grid_Scroll.Children.Add(grid);
            SetRowHeight(grid_Scroll, 0);

            TranslateTransform moveTransform = new TranslateTransform();
            var viewportWidth = GetScrollViewportWidth();
            var itemWidth = space.Width;
            var fromX = GetScrollInitialX(reverse, viewportWidth, itemWidth);
            var toX = reverse ? viewportWidth : -itemWidth;
            moveTransform.X = fromX;
            moveTransform.Y = logicalY;
            grid.RenderTransform = moveTransform;
            space.Transform = moveTransform;

            //创建动画
            Duration duration = new Duration(TimeSpan.FromSeconds(GetScrollDuration(viewportWidth, itemWidth)));
            DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
            myDoubleAnimationX.Duration = duration;
            //创建故事版
            Storyboard moveStoryboard = new Storyboard();
            moveStoryboard.Duration = duration;
            myDoubleAnimationX.From = moveTransform.X;
            myDoubleAnimationX.To = toX;//到达
            moveStoryboard.Children.Add(myDoubleAnimationX);
            Storyboard.SetTarget(myDoubleAnimationX, moveTransform);
            //故事版加入动画
            Storyboard.SetTargetProperty(myDoubleAnimationX, "X");
            rollStoryList.Add(moveStoryboard);
            scrollTransforms[moveStoryboard] = moveTransform;

            moveStoryboard.Completed += new EventHandler<object>((senders, obj) =>
            {
                ReleaseSpace(grid);
                grid_Scroll.Children.Remove(grid);
                RemoveMeasuredRowHeight(grid_Scroll, grid);
                SetRowHeight(grid_Scroll, 0);
                grid.Children.Clear();
                grid = null;
                rollStoryList.Remove(moveStoryboard);
                scrollTransforms.Remove(moveStoryboard);
                moveStoryboard.Stop();
                moveStoryboard = null;
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
            DanmakuSpaceItem space;
            double logicalY;
            if (!TryReserveScrollSpace(grid, null, false, out space, out logicalY))
            {
                RemoveMeasuredRowHeight(grid_Scroll, grid);
                return;
            }
            Grid.SetRow(grid, 0);
            grid.HorizontalAlignment = HorizontalAlignment.Left;
            grid.VerticalAlignment = VerticalAlignment.Top;
            grid_Scroll.Children.Add(grid);
            SetRowHeight(grid_Scroll, 0);

            TranslateTransform moveTransform = new TranslateTransform();
            var viewportWidth = GetScrollViewportWidth();
            var itemWidth = space.Width;
            moveTransform.X = GetScrollInitialX(false, viewportWidth, itemWidth);
            moveTransform.Y = logicalY;
            grid.RenderTransform = moveTransform;
            space.Transform = moveTransform;

            //创建动画
            Duration duration = new Duration(TimeSpan.FromSeconds(GetScrollDuration(viewportWidth, itemWidth)));
            DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
            myDoubleAnimationX.Duration = duration;
            //创建故事版
            Storyboard moveStoryboard = new Storyboard();
            moveStoryboard.Duration = duration;
            myDoubleAnimationX.From = moveTransform.X;
            myDoubleAnimationX.To = -itemWidth;//到达
            moveStoryboard.Children.Add(myDoubleAnimationX);
            Storyboard.SetTarget(myDoubleAnimationX, moveTransform);
            //故事版加入动画
            Storyboard.SetTargetProperty(myDoubleAnimationX, "X");
            rollStoryList.Add(moveStoryboard);
            scrollTransforms[moveStoryboard] = moveTransform;

            moveStoryboard.Completed += new EventHandler<object>((senders, obj) =>
            {
                ReleaseSpace(grid);
                grid_Scroll.Children.Remove(grid);
                RemoveMeasuredRowHeight(grid_Scroll, grid);
                SetRowHeight(grid_Scroll, 0);
                grid = null;
                rollStoryList.Remove(moveStoryboard);
                scrollTransforms.Remove(moveStoryboard);

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
            DanmakuSpaceItem space;
            double top;
            if (!TryReserveFixedSpace(grid, m, false, out space, out top))
            {
                RemoveMeasuredRowHeight(grid_Top, grid);
                return;
            }

            grid.HorizontalAlignment = HorizontalAlignment.Center;
            grid.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetRow(grid, 0);
            grid.RenderTransform = new TranslateTransform { Y = top };
            grid_Top.Children.Add(grid);
            SetRowHeight(grid_Top, 0);


            var lifetime = new FixedDanmakuLifetime(() =>
            {
                RemoveFixedDanmaku(grid_Top, grid);
            });
            fixedLifetimes[grid] = lifetime;
            topBottomStoryList.Add(lifetime);
            lifetime.Start();
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
            DanmakuSpaceItem space;
            double top;
            if (!TryReserveFixedSpace(grid, m, true, out space, out top))
            {
                RemoveMeasuredRowHeight(grid_Bottom, grid);
                return;
            }
            Grid.SetRow(grid, 0);
            grid.RenderTransform = new TranslateTransform { Y = top };
            grid_Bottom.Children.Add(grid);
            SetRowHeight(grid_Bottom, 0);


            var lifetime = new FixedDanmakuLifetime(() =>
            {
                RemoveFixedDanmaku(grid_Bottom, grid);
            });
            fixedLifetimes[grid] = lifetime;
            topBottomStoryList.Add(lifetime);
            lifetime.Start();
        }
        /// <summary>
        /// 添加定位弹幕
        /// </summary>
        /// <param name="m"></param>
        public Task AddPositionDanmu(DanmakuModel m)
        {
            return AddPositionDanmuCore(m);
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
                PauseScrollDanmaku(item);
            }
            PausePositionDanmaku();
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
                ResumeScrollDanmaku(item);
            }
            ResumePositionDanmaku();
        }

        /// <summary>
        /// 暂停一条滚动弹幕。不能只调用 Storyboard.Pause：窗口最小化后再还原时，XAML 会把此前
        /// 挂起的动画时钟一并恢复，只被 Pause 的弹幕会重新滚动起来。这里直接停掉时钟，并把弹幕
        /// 冻结在它此刻应在的位置，系统就没有可以重新跑起来的时钟了。
        /// </summary>
        private void PauseScrollDanmaku(Storyboard storyboard)
        {
            var animation = GetScrollAnimation(storyboard);
            var transform = GetScrollTransform(storyboard);
            if (animation == null || transform == null)
            {
                return;
            }

            // 已经停止说明这条弹幕此前已被暂停，再次暂停会把它错误地拉回起点。
            if (storyboard.GetCurrentState() == ClockState.Stopped)
            {
                return;
            }

            var duration = animation.Duration;
            if (!duration.HasTimeSpan || duration.TimeSpan.TotalSeconds <= 0)
            {
                return;
            }

            var totalSeconds = duration.TimeSpan.TotalSeconds;
            var elapsedSeconds = storyboard.GetCurrentTime().TotalSeconds;
            if (elapsedSeconds < 0)
            {
                elapsedSeconds = 0;
            }
            if (elapsedSeconds > totalSeconds)
            {
                elapsedSeconds = totalSeconds;
            }

            var from = animation.From ?? 0;
            var to = animation.To ?? from;

            storyboard.Stop();
            transform.X = from + (to - from) * (elapsedSeconds / totalSeconds);
        }

        /// <summary>
        /// 恢复一条滚动弹幕：按剩余路程和原本的速度重建一段动画。
        /// </summary>
        private void ResumeScrollDanmaku(Storyboard storyboard)
        {
            var animation = GetScrollAnimation(storyboard);
            var transform = GetScrollTransform(storyboard);
            if (animation == null || transform == null)
            {
                return;
            }

            if (storyboard.GetCurrentState() == ClockState.Active)
            {
                return;
            }

            var duration = animation.Duration;
            if (!duration.HasTimeSpan || duration.TimeSpan.TotalSeconds <= 0)
            {
                return;
            }

            var totalSeconds = duration.TimeSpan.TotalSeconds;
            var from = animation.From ?? 0;
            var to = animation.To ?? from;
            var totalDistance = to - from;
            if (Math.Abs(totalDistance) < double.Epsilon)
            {
                return;
            }

            var currentX = transform.X;
            var remainingSeconds = totalSeconds * (to - currentX) / totalDistance;
            if (remainingSeconds < 0)
            {
                // 理论上不会出现；真出现时按 0 处理，让动画立刻走完并交给 Completed 回收这条弹幕。
                remainingSeconds = 0;
            }

            var remainingAnimation = new DoubleAnimation
            {
                From = currentX,
                To = to,
                Duration = new Duration(TimeSpan.FromSeconds(remainingSeconds))
            };
            Storyboard.SetTarget(remainingAnimation, transform);
            Storyboard.SetTargetProperty(remainingAnimation, "X");

            storyboard.Children.Clear();
            storyboard.Children.Add(remainingAnimation);
            storyboard.Duration = new Duration(TimeSpan.FromSeconds(remainingSeconds));
            storyboard.Begin();
        }

        private static DoubleAnimation GetScrollAnimation(Storyboard storyboard)
        {
            return storyboard != null && storyboard.Children.Count > 0
                ? storyboard.Children[0] as DoubleAnimation
                : null;
        }

        private TranslateTransform GetScrollTransform(Storyboard storyboard)
        {
            TranslateTransform transform;
            return storyboard != null && scrollTransforms.TryGetValue(storyboard, out transform)
                ? transform
                : null;
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

            RemoveFixedDanmaku(container, target);
        }
        /// <summary>
        /// 清空弹幕
        /// </summary>
        public void ClearAll()
        {
            foreach (var item in topBottomStoryList.ToList())
            {
                item.Stop();
            }
            foreach (var item in rollStoryList.ToList())
            {
                item.Stop();
            }
            ClearPositionDanmaku();
            topBottomStoryList.Clear();
            fixedLifetimes.Clear();
            rollStoryList.Clear();
            scrollTransforms.Clear();
            grid_Bottom.Children.Clear();
            grid_Top.Children.Clear();
            grid_Scroll.Children.Clear();
            topPools.Clear();
            bottomPools.Clear();
            scrollPools.Clear();
            reverseScrollPools.Clear();
            spaceItems.Clear();
            measuredRowHeights.Clear();
            rowHeightIndexes.Clear();
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
