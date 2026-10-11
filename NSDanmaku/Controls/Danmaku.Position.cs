using NSDanmaku.Helper;
using NSDanmaku.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace NSDanmaku.Controls
{
    public sealed partial class Danmaku
    {
        private sealed class PositionSource
        {
            public DanmakuModel Model;
            public PositionDanmaku Definition;
            public double Start;
            public int Order;
        }
        private sealed class PositionVisual
        {
            public Grid Element;
            public ScaleTransform Scale = new ScaleTransform();
        }
        private List<PositionSource> positionSources = new List<PositionSource>();
        private PositionDanmakuIndex<PositionSource> positionIndex;
        private readonly Dictionary<PositionSource, PositionVisual> positionVisuals = new Dictionary<PositionSource, PositionVisual>();
        private readonly Stopwatch positionClock = Stopwatch.StartNew();
        private readonly PositionDanmakuClock positionPlaybackClock = new PositionDanmakuClock();
        private bool positionVisible = true, positionLoaded, positionRendering, externalPositionClock, positionSuspended;
        private int positionGeneration;

        private void InitializePositionDanmaku()
        {
            Loaded += (sender, args) =>
            {
                positionLoaded = true;
                positionSuspended = !positionVisible;
                UpdatePositionDanmaku();
                UpdatePositionRendering();
            };
            Unloaded += (sender, args) =>
            {
                positionLoaded = false;
                ClearPositionDanmaku();
                // 全屏切换可能暂时卸载控件，保留池以便 Loaded 时恢复。
                // 退出播放器由宿主 SetPositionDanmakuList(null) 释放数据。
            };
            SizeChanged += (sender, args) => UpdatePositionDanmaku();
        }

        public void SetPositionDanmakuList(IEnumerable<DanmakuModel> items)
        {
            ClearPositionDanmaku();
            var sources = new List<PositionSource>();
            foreach (var model in items ?? Enumerable.Empty<DanmakuModel>())
            {
                PositionDanmaku definition;
                if (model != null && model.location == DanmakuLocation.Position
                    && !double.IsNaN(model.time) && !double.IsInfinity(model.time)
                    && PositionDanmaku.TryParse(model.text, out definition))
                    sources.Add(new PositionSource { Model = model, Definition = definition, Start = model.time, Order = sources.Count });
            }
            positionSources = sources;
            RebuildPositionIndex();
        }

        /// <summary>以媒体时钟校准，帧间按播放速率推进；跳转可直接恢复任意存活区间。</summary>
        public void SetPositionDanmakuPlayback(double seconds, bool playing, double rate, bool visible)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
            externalPositionClock = true;
            positionPlaybackClock.Synchronize(seconds, playing, rate, positionClock.Elapsed.TotalSeconds);
            if (positionVisible && !visible) ClearPositionDanmaku();
            positionVisible = visible;
            positionSuspended = !visible;
            if (visible) UpdatePositionDanmaku();
            UpdatePositionRendering();
        }

        private double PositionDanmakuTime
        {
            get { return positionPlaybackClock.Read(positionClock.Elapsed.TotalSeconds); }
        }

        private async Task AddPositionDanmuCore(DanmakuModel model)
        {
            PositionDanmaku definition;
            if (model == null || !PositionDanmaku.TryParse(model.text, out definition)) return;
            if (positionSources.Any(source => ReferenceEquals(source.Model, model))) return;
            var now = PositionDanmakuTime;
            if (!externalPositionClock)
                positionSources.RemoveAll(source => source.Start + source.Definition.Duration <= now);
            positionSources.Add(new PositionSource { Model = model, Definition = definition, Start = externalPositionClock ? model.time : now,
                Order = positionSources.Count == 0 ? 0 : positionSources.Max(source => source.Order) + 1 });
            positionSuspended = false;
            RebuildPositionIndex();
            await UpdatePositionDanmakuAsync();
            UpdatePositionRendering();
        }

        private void RebuildPositionIndex()
        {
            positionIndex = new PositionDanmakuIndex<PositionSource>(positionSources, source => source.Start, source => source.Definition.Duration);
        }

        private void UpdatePositionRendering()
        {
            var render = positionLoaded && positionVisible && !positionSuspended && positionPlaybackClock.IsPlaying && positionSources.Count > 0;
            if (render == positionRendering) return;
            positionRendering = render;
            if (render) CompositionTarget.Rendering += PositionDanmakuRendering;
            else CompositionTarget.Rendering -= PositionDanmakuRendering;
        }

        private void PositionDanmakuRendering(object sender, object args) { UpdatePositionDanmaku(); }
        private void UpdatePositionDanmaku() { _ = UpdatePositionDanmakuAsync(); }

        private async Task UpdatePositionDanmakuAsync()
        {
            if (!positionLoaded || !positionVisible || positionSuspended || positionIndex == null || ActualWidth <= 0 || ActualHeight <= 0) return;
            var active = positionIndex.ActiveAt(PositionDanmakuTime);
            var activeSet = new HashSet<PositionSource>(active);
            foreach (var source in positionVisuals.Keys.Where(source => !activeSet.Contains(source)).ToList())
            {
                var element = positionVisuals[source].Element;
                if (element != null) canvas.Children.Remove(element);
                positionVisuals.Remove(source);
            }
            // 在 await 前登记所有创建任务，避免帧间重复创建。各任务都有代次和存活检查。
            var tasks = new List<Task>();
            foreach (var source in active)
            {
                PositionVisual visual;
                if (!positionVisuals.TryGetValue(source, out visual))
                {
                    visual = new PositionVisual();
                    positionVisuals.Add(source, visual);
                    tasks.Add(CreatePositionVisualAsync(source, visual, positionGeneration));
                }
                else ApplyPositionState(source, visual);
            }
            await Task.WhenAll(tasks);
        }

        private async Task CreatePositionVisualAsync(PositionSource source, PositionVisual visual, int generation)
        {
            try
            {
                // 只创建显示模型，原始 JSON 留在池中供重播及脚本读取。
                var display = new DanmakuModel { text = source.Definition.Text, size = source.Model.size, color = source.Model.color };
                var font = string.IsNullOrWhiteSpace(source.Definition.FontFamily) ? DanmakuFontFamily : source.Definition.FontFamily;
                var element = source.Definition.HasOutline
                    ? await DanmakuItemControl.CreateControlBorder((float)DanmakuSizeZoom, DanmakuBold, font, display)
                    : DanmakuItemControl.CreateControlNoBorder((float)DanmakuSizeZoom, DanmakuBold, font, display);
                PositionVisual current;
                if (generation != positionGeneration || !positionLoaded || !positionVisible
                    || !positionVisuals.TryGetValue(source, out current) || !ReferenceEquals(current, visual)
                    || !source.Definition.Evaluate(PositionDanmakuTime - source.Start, ActualWidth, ActualHeight).Visible) return;
                element.Tag = source.Model;
                element.RenderTransform = visual.Scale;
                element.Projection = new PlaneProjection { RotationZ = -source.Definition.RotateZ, RotationY = source.Definition.RotateY };
                visual.Element = element;
                ApplyPositionState(source, visual);
                // 按池中顺序插入，异步绘制完成的先后不改变 M7 叠放关系。
                var before = positionVisuals.Where(pair => pair.Value.Element != null && pair.Key != source
                    && pair.Key.Order < source.Order).Count();
                canvas.Children.Insert(Math.Min(before, canvas.Children.Count), element);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("创建 M7 弹幕失败：" + ex.Message);
                // 保留占位，避免异常字体/渲染资源导致每帧重试。
            }
        }

        private void ApplyPositionState(PositionSource source, PositionVisual visual)
        {
            if (visual.Element == null) return;
            var state = source.Definition.Evaluate(PositionDanmakuTime - source.Start, ActualWidth, ActualHeight);
            Canvas.SetLeft(visual.Element, state.X);
            Canvas.SetTop(visual.Element, state.Y);
            visual.Element.Opacity = state.Visible ? state.Opacity : 0;
            visual.Scale.ScaleX = visual.Scale.ScaleY = state.Scale;
        }

        private void PausePositionDanmaku()
        {
            positionPlaybackClock.Pause(positionClock.Elapsed.TotalSeconds);
            UpdatePositionRendering();
        }
        private void ResumePositionDanmaku()
        {
            positionPlaybackClock.Resume(positionClock.Elapsed.TotalSeconds);
            UpdatePositionRendering();
        }
        private void ClearPositionDanmaku()
        {
            positionGeneration++;
            positionSuspended = true;
            positionVisuals.Clear();
            canvas.Children.Clear();
            if (positionRendering) CompositionTarget.Rendering -= PositionDanmakuRendering;
            positionRendering = false;
        }
    }
}
