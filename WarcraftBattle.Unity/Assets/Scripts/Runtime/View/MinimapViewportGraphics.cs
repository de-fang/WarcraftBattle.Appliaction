using UnityEngine;
using UnityEngine.UI;

namespace WarcraftBattle3D
{
    public class MinimapViewportQuad : MaskableGraphic
    {
        private Vector2 _a;
        private Vector2 _b;
        private Vector2 _c;
        private Vector2 _d;

        public void SetPoints(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            AddQuad(vh, _a, _b, _c, _d, color);
        }

        private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 color32)
        {
            int start = vh.currentVertCount;
            vh.AddVert(a, color32, Vector2.zero);
            vh.AddVert(b, color32, Vector2.zero);
            vh.AddVert(c, color32, Vector2.zero);
            vh.AddVert(d, color32, Vector2.zero);
            vh.AddTriangle(start + 0, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start + 0);
        }
    }

    public class MinimapViewportOutline : MaskableGraphic
    {
        [SerializeField]
        private float thickness = 1f;

        private Vector2 _a;
        private Vector2 _b;
        private Vector2 _c;
        private Vector2 _d;

        public float Thickness
        {
            get => thickness;
            set
            {
                thickness = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        public void SetPoints(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (thickness <= 0f)
            {
                return;
            }

            AddLine(vh, _a, _b, thickness, color);
            AddLine(vh, _b, _c, thickness, color);
            AddLine(vh, _c, _d, thickness, color);
            AddLine(vh, _d, _a, thickness, color);
        }

        private static void AddLine(VertexHelper vh, Vector2 start, Vector2 end, float width, Color32 color32)
        {
            var dir = end - start;
            float len = dir.magnitude;
            if (len <= 0.001f)
            {
                return;
            }

            dir /= len;
            var normal = new Vector2(-dir.y, dir.x) * (width * 0.5f);
            var v0 = start - normal;
            var v1 = start + normal;
            var v2 = end + normal;
            var v3 = end - normal;

            int startIndex = vh.currentVertCount;
            vh.AddVert(v0, color32, Vector2.zero);
            vh.AddVert(v1, color32, Vector2.zero);
            vh.AddVert(v2, color32, Vector2.zero);
            vh.AddVert(v3, color32, Vector2.zero);
            vh.AddTriangle(startIndex + 0, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex + 0);
        }
    }
}
