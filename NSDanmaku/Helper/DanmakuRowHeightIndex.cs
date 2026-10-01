using System.Collections.Generic;

namespace NSDanmaku.Helper
{
    // 按高度计数，移除一条弹幕时无需遍历其余 XAML 控件寻找最大行高。
    public sealed class DanmakuRowHeightIndex
    {
        private readonly Dictionary<double, int> counts = new Dictionary<double, int>();
        private readonly SortedSet<double> heights = new SortedSet<double>();

        public double Maximum => heights.Count == 0 ? 0 : heights.Max;

        public void Add(double height)
        {
            int count;
            counts.TryGetValue(height, out count);
            counts[height] = count + 1;
            heights.Add(height);
        }

        public void Remove(double height)
        {
            int count;
            if (!counts.TryGetValue(height, out count)) return;
            if (count > 1) counts[height] = count - 1;
            else
            {
                counts.Remove(height);
                heights.Remove(height);
            }
        }
    }
}
