using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace CadTools.Drawing
{
    internal static class LotesDimHelper
    {
        // ── Acota todos los lados de un polígono ──────────────────────────────
        public static void CotarPoligono(
            Transaction tr, BlockTableRecord ms, Database db,
            Polyline pl,
            string dimStyleName,
            double offset,
            bool exterior)
        {
            ObjectId dsId = GetDimStyleId(db, tr, dimStyleName);
            int n = pl.NumberOfVertices;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Point3d p1 = pl.GetPoint3dAt(i);
                Point3d p2 = pl.GetPoint3dAt(j);

                double len = p1.DistanceTo(p2);
                if (len < 0.01) continue;

                double dx = p2.X - p1.X;
                double dy = p2.Y - p1.Y;
                double mag = Math.Sqrt(dx * dx + dy * dy);

                var nA = new Vector3d(-dy / mag, dx / mag, 0);
                var nB = new Vector3d(dy / mag, -dx / mag, 0);

                Point3d mid = new Point3d((p1.X + p2.X) / 2.0,
                                            (p1.Y + p2.Y) / 2.0, 0);
                Point3d testA = mid + nA * (offset * 0.1);
                Point3d testB = mid + nB * (offset * 0.1);

                bool aInside = PuntoDentroDePolilinea(pl, testA);

                Vector3d normal;
                if (exterior)
                    normal = aInside ? nB : nA;
                else
                    normal = aInside ? nA : nB;

                Point3d dimLine = mid + normal * offset;

                var dim = new RotatedDimension
                {
                    XLine1Point = p1,
                    XLine2Point = p2,
                    DimLinePoint = dimLine,
                    Rotation = Math.Atan2(dy, dx),
                    DimensionStyle = dsId,
                    Layer = dimStyleName
                };
                ms.AppendEntity(dim);
                tr.AddNewlyCreatedDBObject(dim, true);
            }
        }

        // ── Ray casting 2D ────────────────────────────────────────────────────
        public static bool PuntoDentroDePolilinea(Polyline pl, Point3d pt)
        {
            int n = pl.NumberOfVertices;
            bool inside = false;
            double px = pt.X, py = pt.Y;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                double xi = pl.GetPoint2dAt(i).X, yi = pl.GetPoint2dAt(i).Y;
                double xj = pl.GetPoint2dAt(j).X, yj = pl.GetPoint2dAt(j).Y;

                bool intersect = ((yi > py) != (yj > py)) &&
                    (px < (xj - xi) * (py - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        // ── DimStyle por nombre con fallback Standard ─────────────────────────
        private static ObjectId GetDimStyleId(Database db, Transaction tr,
                                              string name)
        {
            var dst = tr.GetObject(db.DimStyleTableId,
                                   OpenMode.ForRead) as DimStyleTable;
            return dst.Has(name) ? dst[name] : dst["Standard"];
        }
    }
}