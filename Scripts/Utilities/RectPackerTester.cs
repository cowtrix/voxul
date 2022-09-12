
using System.Collections.Generic;
using UnityEngine;
using Voxul.Utilities.RectanglePacker;

namespace Voxul.Utilities
{
    public class RectPackerTester : ExtendedMonoBehaviour
    {
        /*public List<Vector2> Rects = new List<Vector2>();
        public float Buffer = .01f;
        public RectanglePacker.PackingHints Hint;
        public float AcceptableDensity = 1;
        public uint StepSize = 1;

        [ContextMenu("Generate Random Rects")]
        public void GenRandomRects()
        {
            Rects.Clear();
            for (int i = 0; i < 32; i++)
            {
                Rects.Add(new Vector2(Random.Range(1, 5), Random.Range(1, 5)));
            }
        }

        private void OnDrawGizmos()
        {
            var packedRects = RectanglePacker.RectanglePacker.Pack(Rects, out var packingRectangle, Hint, AcceptableDensity, StepSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector2(packingRectangle.X + packingRectangle.Width / 2f, packingRectangle.Y + packingRectangle.Height / 2f), new Vector2(packingRectangle.Width, packingRectangle.Height));

            for (int i = 0; i < packedRects.Length; i++)
            {
                Random.InitState(i);
                Gizmos.color = Random.ColorHSV(0, 1, 1, 1, 1, 1);
                RectanglePacker.PackingRectangle r = packedRects[i];
                Gizmos.DrawWireCube(new Vector3(r.X + r.Width / 2f, r.Y + r.Height / 2f, i * .1f), new Vector2(r.Width, r.Height));
            }
        }*/

        public Rect rect;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(rect.center, rect.size);
            var prect = new PackingRectangle(rect);
            var uprect = (Rect)prect;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube((Vector3)uprect.center + Vector3.forward, uprect.size);

            var newPRect = (Rect)new PackingRectangle(10, 10, 3, 5);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(newPRect.center, newPRect.size);
        }
    }

}