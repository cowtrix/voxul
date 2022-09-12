using UnityEngine;

namespace Voxul.Utilities
{
    public static class RectExtensions
    {
        public static Rect Encapsulate(this Rect rect, Rect other)
        {
            rect = rect.Encapsulate(other.min);
            rect = rect.Encapsulate(other.max);
            return rect;
        }

        public static Rect Encapsulate(this Rect rect, Vector2 point)
        {
            if (rect.Contains(point))
            {
                // We don't need to do anything
                return rect;
            }
            if (point.x < rect.xMin)
            {
                if (point.y < rect.yMin)
                {
                    var size = rect.max - point;
                    rect.Set(point.x, point.y, size.x, size.y);
                    return rect;
                }
                if (point.y > rect.yMax)
                {
                    var newPos = new Vector2(point.x, rect.y);
                    var newMax = new Vector2(rect.xMax, point.y);
                    var size = newMax - newPos;
                    rect.Set(newPos.x, newPos.y, size.x, size.y);
                    return rect;
                }
                rect.Set(point.x, rect.y, rect.xMax - point.x, rect.height);
                return rect;
            }
            if (point.x > rect.xMax)
            {
                if (point.y < rect.yMin)
                {
                    var newPos = new Vector2(rect.x, point.y);
                    var newMax = new Vector2(point.x, rect.yMax);
                    var size = newMax - newPos;
                    rect.Set(newPos.x, newPos.y, size.x, size.y);
                    return rect;
                }
                if (point.y > rect.yMax)
                {
                    var newPos = rect.position;
                    var newMax = point;
                    var size = newMax - newPos;
                    rect.Set(newPos.x, newPos.y, size.x, size.y);
                    return rect;
                }
                rect.Set(rect.x, rect.y, point.x - rect.xMin, rect.height);
                return rect;
            }
            if (point.y > rect.yMax)
            {
                rect.Set(rect.x, rect.y, rect.width, point.y - rect.yMin);
                return rect;
            }
            if (point.y < rect.yMin)
            {
                rect.Set(rect.x, point.y, rect.width, rect.yMax - point.y);
                return rect;
            }
            return rect;
        }
    }
}
