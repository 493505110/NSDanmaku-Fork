using System;
using System.Collections.Generic;

namespace NSDanmaku.Helper
{
    public struct ScrollDanmakuSpace
    {
        public double Y;
        public double Height;
        public double X;
        public double Width;
        public double EndTime;
    }

    public static class ScrollDanmakuPlacement
    {
        // 池内条目按底边升序排列。只需跨过与新弹幕碰撞的条目，
        // 不必对每个候选位置重新扫描整池。边界相接仍按原规则视为碰撞。
        public static bool TryFindPosition<T>(
            IList<T> pool,
            Func<T, ScrollDanmakuSpace> getSpace,
            double width,
            double height,
            double viewportWidth,
            double middleTime,
            double availableHeight,
            out double y)
        {
            y = 0;
            foreach (var entry in pool)
            {
                var occupied = getSpace(entry);
                var right = occupied.X + occupied.Width;
                if ((right < viewportWidth || occupied.X > viewportWidth + width)
                    && occupied.EndTime <= middleTime)
                {
                    continue;
                }

                var bottom = occupied.Y + occupied.Height;
                if (occupied.Y > y + height || bottom < y)
                {
                    continue;
                }

                y = bottom + 1;
                if (y + height > availableHeight)
                {
                    y = 0;
                    return false;
                }
            }

            return true;
        }
    }
}
