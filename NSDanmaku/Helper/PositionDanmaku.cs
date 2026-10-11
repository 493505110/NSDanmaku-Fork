using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace NSDanmaku.Helper
{
    /// <summary>M7 的解析和按媒体时间求值；不依赖 XAML，也不修改原始弹幕。</summary>
    public sealed class PositionDanmaku
    {
        // 与 B 站 DanmakuFlameMaster 的 M7 参考舞台一致。
        public const double StageWidth = 682;
        public const double StageHeight = 438;
        public string Text { get; private set; }
        public string FontFamily { get; private set; }
        public bool HasOutline { get; private set; }
        public double Duration { get; private set; }
        public double MoveDuration { get; private set; }
        public double MoveDelay { get; private set; }
        public double RotateZ { get; private set; }
        public double RotateY { get; private set; }
        public bool QuadraticEaseOut { get; private set; }
        private double alphaFrom, alphaTo;
        private PositionDanmakuPoint[] points;
        private double[] distances;
        private double totalDistance;
        private bool hasPath;

        public static bool TryParse(string json, out PositionDanmaku result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                var data = JArray.Parse(json);
                if (data.Count < 5 || data[4].Type != JTokenType.String) return false;
                var item = new PositionDanmaku();
                item.Text = data[4].ToString().Replace("/n", "\n").Replace("\\n", "\n");
                item.Duration = Number(data[3]);
                if (item.Duration <= 0) return false;
                var alpha = (data[2].Type == JTokenType.String ? data[2].ToString()
                    : Number(data[2]).ToString(CultureInfo.InvariantCulture)).Split('-');
                if (alpha.Length < 1 || alpha.Length > 2) return false;
                item.alphaFrom = Clamp(Number(alpha[0]));
                item.alphaTo = alpha.Length == 2 ? Clamp(Number(alpha[1])) : item.alphaFrom;
                var start = new PositionDanmakuPoint(Coordinate(data[0], StageWidth), Coordinate(data[1], StageHeight));
                var end = start;
                item.RotateZ = OptionalNumber(data, 5, 0);
                item.RotateY = OptionalNumber(data, 6, 0);
                item.MoveDuration = Missing(data, 9) ? item.Duration : Number(data[9]) / 1000;
                item.MoveDelay = OptionalNumber(data, 10, 0) / 1000;
                if (item.MoveDuration < 0 || item.MoveDelay < 0) return false;
                if (data.Count > 7) end.X = OptionalCoordinate(data, 7, start.X, StageWidth);
                if (data.Count > 8) end.Y = OptionalCoordinate(data, 8, start.Y, StageHeight);
                // B 站的旧式标志 true 表示关闭描边；缺省保留描边。
                item.HasOutline = data.Count <= 11 || !Flag(data[11], false);
                item.FontFamily = data.Count > 12 && data[12].Type == JTokenType.String ? data[12].ToString() : null;
                // Linear.easeIn / Quadratic.easeOut，0 对应后者。
                item.QuadraticEaseOut = data.Count > 13 && data[13].ToString() == "0";
                item.hasPath = data.Count > 14 && !string.IsNullOrWhiteSpace(data[14].ToString());
                item.points = item.hasPath
                    ? ParsePath(data[14].ToString()) : new[] { start, end };
                if (item.points == null) return false;
                item.distances = new double[item.points.Length];
                for (var i = 1; i < item.points.Length; i++)
                {
                    var dx = item.points[i].X - item.points[i - 1].X;
                    var dy = item.points[i].Y - item.points[i - 1].Y;
                    item.totalDistance += Math.Sqrt(dx * dx + dy * dy);
                    if (!Finite(item.totalDistance)) return false;
                    item.distances[i] = item.totalDistance;
                }
                result = item;
                return true;
            }
            catch (Exception ex) when (ex is Newtonsoft.Json.JsonException || ex is FormatException || ex is OverflowException || ex is ArgumentException)
            {
                return false;
            }
        }

        public PositionDanmakuState Evaluate(double elapsed, double width, double height)
        {
            if (!Finite(elapsed) || !Finite(width) || !Finite(height) || width <= 0 || height <= 0)
                return new PositionDanmakuState();
            var progress = elapsed < MoveDelay ? 0 : MoveDuration == 0 ? 1 : Clamp((elapsed - MoveDelay) / MoveDuration);
            // 路径按总长度匀速分配时间，补间只用于两点直线。
            if (!hasPath && QuadraticEaseOut) progress = progress * (2 - progress);
            var point = points[0];
            if (progress >= 1) point = points[points.Length - 1];
            else if (totalDistance > 0)
            {
                var distance = progress * totalDistance;
                var lo = 1;
                var hi = distances.Length - 1;
                while (lo < hi)
                {
                    var mid = (lo + hi) / 2;
                    if (distances[mid] <= distance) lo = mid + 1;
                    else hi = mid;
                }
                var length = distances[lo] - distances[lo - 1];
                var fraction = length > 0 ? Clamp((distance - distances[lo - 1]) / length) : 0;
                point = new PositionDanmakuPoint(
                    points[lo - 1].X + (points[lo].X - points[lo - 1].X) * fraction,
                    points[lo - 1].Y + (points[lo].Y - points[lo - 1].Y) * fraction);
            }
            var x = point.X * (width / StageWidth);
            var y = point.Y * (height / StageHeight);
            if (!Finite(x) || !Finite(y)) return new PositionDanmakuState();
            return new PositionDanmakuState
            {
                Visible = elapsed >= 0 && elapsed < Duration,
                X = x,
                Y = y,
                Opacity = alphaFrom + (alphaTo - alphaFrom) * Clamp(elapsed / Duration),
                Scale = Math.Min(width / StageWidth, height / StageHeight)
            };
        }

        private static PositionDanmakuPoint[] ParsePath(string path)
        {
            // M/L 是 M7 格式支持的折线路径；同时接受负数、小数和小写相对命令。
            var tokens = Regex.Matches(path, @"\G[\s,]*(?:([MLml])|([-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?))");
            var list = new List<PositionDanmakuPoint>();
            var command = '\0';
            var offset = 0;
            for (var i = 0; i < tokens.Count;)
            {
                var token = tokens[i];
                offset = token.Index + token.Length;
                if (token.Groups[1].Success)
                {
                    command = token.Groups[1].Value[0];
                    i++;
                    if (list.Count == 0 && command != 'M' && command != 'm') return null;
                    if (list.Count > 0 && (command == 'M' || command == 'm')) return null;
                }
                if (command == '\0' || i + 1 >= tokens.Count || !tokens[i].Groups[2].Success || !tokens[i + 1].Groups[2].Success) return null;
                var x = Number(tokens[i].Groups[2].Value);
                var y = Number(tokens[i + 1].Groups[2].Value);
                offset = tokens[i + 1].Index + tokens[i + 1].Length;
                if (char.IsLower(command) && list.Count > 0)
                {
                    x += list[list.Count - 1].X;
                    y += list[list.Count - 1].Y;
                }
                list.Add(new PositionDanmakuPoint(x, y));
                if (list.Count > 4096) return null;
                command = char.IsLower(command) ? 'l' : 'L';
                i += 2;
            }
            return list.Count >= 2 && string.IsNullOrWhiteSpace(path.Substring(offset)) ? list.ToArray() : null;
        }

        private static double Coordinate(JToken token, double dimension)
        {
            var value = Number(token);
            // 整数为参考舞台坐标，浮点数/带小数点的字符串为比例坐标。
            var text = token.ToString();
            var coordinate = token.Type == JTokenType.Float || text.Contains(".") || text.IndexOf('e') >= 0 || text.IndexOf('E') >= 0 ? value * dimension : value;
            if (!Finite(coordinate)) throw new FormatException();
            return coordinate;
        }

        private static double OptionalCoordinate(JArray data, int index, double fallback, double dimension)
        {
            return Missing(data, index) ? fallback : Coordinate(data[index], dimension);
        }
        private static double OptionalNumber(JArray data, int index, double fallback)
        {
            return Missing(data, index) ? fallback : Number(data[index]);
        }
        private static bool Missing(JArray data, int index)
        {
            return data.Count <= index || data[index].Type == JTokenType.Null || data[index].Type == JTokenType.String && string.IsNullOrWhiteSpace(data[index].ToString());
        }
        private static bool Flag(JToken token, bool fallback)
        {
            var text = token.ToString();
            if (text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (text == "0" || text.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            return fallback;
        }
        private static double Number(JToken token)
        {
            if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float && token.Type != JTokenType.String) throw new FormatException();
            return Number(Convert.ToString(((JValue)token).Value, CultureInfo.InvariantCulture));
        }
        private static double Number(string text)
        {
            double value;
            if (!double.TryParse(text.Replace('０', '0'), NumberStyles.Float, CultureInfo.InvariantCulture, out value) || !Finite(value)) throw new FormatException();
            return value;
        }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static double Clamp(double value) { return Math.Max(0, Math.Min(1, value)); }
    }

    internal struct PositionDanmakuPoint
    {
        public double X, Y;
        public PositionDanmakuPoint(double x, double y) { X = x; Y = y; }
    }

    public struct PositionDanmakuState
    {
        public bool Visible;
        public double X, Y, Opacity, Scale;
    }

    /// <summary>媒体时间为准，帧间使用单调时钟；暂停不推进，校准可向前或向后跳转。</summary>
    public sealed class PositionDanmakuClock
    {
        private double position, wallAnchor, rate = 1;
        public bool IsPlaying { get; private set; } = true;
        public void Synchronize(double seconds, bool playing, double playbackRate, double wallSeconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds)
                || double.IsNaN(wallSeconds) || double.IsInfinity(wallSeconds)) return;
            position = Math.Max(0, seconds);
            wallAnchor = wallSeconds;
            rate = double.IsNaN(playbackRate) || double.IsInfinity(playbackRate) || playbackRate <= 0 ? 1 : playbackRate;
            IsPlaying = playing;
        }
        public double Read(double wallSeconds)
        {
            return position + (IsPlaying ? Math.Max(0, wallSeconds - wallAnchor) * rate : 0);
        }
        public void Pause(double wallSeconds) { Synchronize(Read(wallSeconds), false, rate, wallSeconds); }
        public void Resume(double wallSeconds)
        {
            if (!IsPlaying) Synchronize(position, true, rate, wallSeconds);
        }
    }

    /// <summary>按存活区间查询 M7，跳转时恢复已经开始但尚未结束的条目。</summary>
    public sealed class PositionDanmakuIndex<T> where T : class
    {
        private readonly T[] items;
        private readonly double[] starts, ends, maximumEnds;
        public PositionDanmakuIndex(IEnumerable<T> source, Func<T, double> start, Func<T, double> duration)
        {
            items = source.OrderBy(start).ToArray();
            starts = new double[items.Length];
            ends = new double[items.Length];
            maximumEnds = new double[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                starts[i] = start(items[i]);
                ends[i] = starts[i] + duration(items[i]);
                maximumEnds[i] = i == 0 ? ends[i] : Math.Max(maximumEnds[i - 1], ends[i]);
            }
        }
        public IReadOnlyList<T> ActiveAt(double position)
        {
            var result = new List<T>();
            if (double.IsNaN(position) || double.IsInfinity(position)) return result;
            var lo = 0;
            var hi = items.Length;
            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (starts[mid] <= position) lo = mid + 1;
                else hi = mid;
            }
            for (var i = lo - 1; i >= 0 && maximumEnds[i] > position; i--)
                if (ends[i] > position) result.Add(items[i]);
            result.Reverse();
            return result;
        }
    }
}
