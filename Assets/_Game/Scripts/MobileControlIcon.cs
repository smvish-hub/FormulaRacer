using UnityEngine;

namespace FormulaRacer
{
    // Vector icons avoid imported images/fonts and remain crisp at different Canvas scales.
    public sealed class MobileControlIcon : UnityEngine.UI.MaskableGraphic
    {
        public MobileControl Control;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            switch (Control)
            {
                case MobileControl.Left: Arrow(mesh, -1f); break;
                case MobileControl.Right: Arrow(mesh, 1f); break;
                case MobileControl.Accelerator:
                    Line(mesh, new Vector2(-0.25f, -0.22f), new Vector2(0f, 0.12f));
                    Line(mesh, new Vector2(0f, 0.12f), new Vector2(0.25f, -0.22f));
                    Line(mesh, new Vector2(-0.25f, 0.02f), new Vector2(0f, 0.36f));
                    Line(mesh, new Vector2(0f, 0.36f), new Vector2(0.25f, 0.02f)); break;
                case MobileControl.Brake:
                    Line(mesh, new Vector2(-0.22f, -0.3f), new Vector2(-0.22f, 0.3f), 0.15f);
                    Line(mesh, new Vector2(0.22f, -0.3f), new Vector2(0.22f, 0.3f), 0.15f); break;
                case MobileControl.Camera:
                    Line(mesh, new Vector2(-0.32f, -0.23f), new Vector2(0.32f, -0.23f));
                    Line(mesh, new Vector2(-0.32f, 0.23f), new Vector2(0.32f, 0.23f));
                    Line(mesh, new Vector2(-0.32f, -0.23f), new Vector2(-0.32f, 0.23f));
                    Line(mesh, new Vector2(0.32f, -0.23f), new Vector2(0.32f, 0.23f));
                    Circle(mesh, 0.13f, 0f, 360f); break;
                case MobileControl.Reset:
                    Circle(mesh, 0.29f, -30f, 260f);
                    Line(mesh, new Vector2(-0.05f, 0.29f), new Vector2(-0.29f, 0.29f));
                    Line(mesh, new Vector2(-0.05f, 0.29f), new Vector2(-0.05f, 0.05f)); break;
            }
        }

        private void Arrow(UnityEngine.UI.VertexHelper mesh, float direction)
        {
            Line(mesh, new Vector2(-0.3f * direction, 0f), new Vector2(0.3f * direction, 0f));
            Line(mesh, new Vector2(0.3f * direction, 0f), new Vector2(0f, 0.3f));
            Line(mesh, new Vector2(0.3f * direction, 0f), new Vector2(0f, -0.3f));
        }
        private void Circle(UnityEngine.UI.VertexHelper mesh, float radius, float start, float end)
        {
            for (float angle = start; angle < end; angle += 15f)
                Line(mesh, new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius,
                    new Vector2(Mathf.Cos(Mathf.Min(angle + 15f, end) * Mathf.Deg2Rad), Mathf.Sin(Mathf.Min(angle + 15f, end) * Mathf.Deg2Rad)) * radius, 0.055f);
        }
        private void Line(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, float thickness = 0.08f)
        {
            Rect rect = GetPixelAdjustedRect();
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * thickness * 0.5f;
            int index = mesh.currentVertCount;
            foreach (Vector2 point in new[] { a - normal, a + normal, b + normal, b - normal })
                mesh.AddVert(new Vector3(rect.center.x + point.x * rect.width, rect.center.y + point.y * rect.height), color, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
