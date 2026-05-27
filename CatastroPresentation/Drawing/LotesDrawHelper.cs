using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadTools.Models;
using CadTools.Sync;

namespace CadTools.Drawing
{
    internal static class LotesDrawHelper
    {
        public const double HojaW = 210.0;
        public const double HojaH = 297.0;
        public const double Margen = 5.0;

        // ── Marco A4 ──────────────────────────────────────────────────────────
        public static void DrawFrame(Transaction tr, BlockTableRecord ms,
                                     double hX, double hY)
        {
            var b = new Polyline();
            b.AddVertexAt(0, Pt2(hX, hY), 0, 0, 0);
            b.AddVertexAt(1, Pt2(hX + HojaW, hY), 0, 0, 0);
            b.AddVertexAt(2, Pt2(hX + HojaW, hY + HojaH), 0, 0, 0);
            b.AddVertexAt(3, Pt2(hX, hY + HojaH), 0, 0, 0);
            b.Closed = true;
            b.Layer = "PRESENTACION_MARCO";
            b.ConstantWidth = 0.5;
            ms.AppendEntity(b);
            tr.AddNewlyCreatedDBObject(b, true);
        }

        // ── A. Cuadro SECTOR ──────────────────────────────────────────────────
        public static void DrawCuadroSector(Transaction tr, BlockTableRecord ms,
                                            double hX, double hY)
        {
            const double W = 13.20;
            const double H = 3.66;
            const double RH = 1.22;
            const double c0 = 4.20;
            const double c1 = 4.61;

            double x = hX + HojaW - Margen - W;
            double y = hY + HojaH - Margen - H;

            DrawRect(tr, ms, x, y, W, H);

            AddLine(tr, ms, x, y + RH, x + W, y + RH, "PRESENTACION_MARCO");
            AddLine(tr, ms, x, y + RH * 2, x + W, y + RH * 2, "PRESENTACION_MARCO");

            double vx0 = x + c0;
            double vx1 = x + c0 + c1;
            double botMid = x + c0 + (W - c0) / 2.0;

            AddLine(tr, ms, vx0, y + RH, vx0, y + H, "PRESENTACION_MARCO");
            AddLine(tr, ms, vx1, y + RH, vx1, y + H, "PRESENTACION_MARCO");
            AddLine(tr, ms, vx0, y, vx0, y + RH, "PRESENTACION_MARCO");
            AddLine(tr, ms, botMid, y, botMid, y + RH, "PRESENTACION_MARCO");

            double cy2 = y + RH * 2.5;
            double cy1 = y + RH * 1.5;
            double cy0 = y + RH * 0.5;

            const double TH = 0.70;
            const double TV = 1.10;

            double cx_s = x + c0 / 2.0;
            double cx_m = x + c0 + c1 / 2.0;
            double cx_l = x + c0 + c1 + (W - c0 - c1) / 2.0;
            double halfW = (W - c0) / 2.0;
            double cx_mhu = x + c0 / 2.0;
            double cx_mhuV = x + c0 + halfW / 4.0;
            double cx_lhu = x + c0 + halfW + halfW / 4.0;
            double cx_lhuV = x + c0 + halfW + halfW * 3.0 / 4.0;

            AddTextCenter(tr, ms, "SECTOR", TH, cx_s, cy2, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, "MZ. CAT", TH, cx_m, cy2, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, "LOTE CAT", TH, cx_l, cy2, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, "---", TV, cx_s, cy1, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, "---", TV, cx_m, cy1, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, "---", TV, cx_l, cy1, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, "MZ. H.U", TH, cx_mhu, cy0, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, "---", TV, cx_mhuV, cy0, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, "LOTE H.U", TH, cx_lhu, cy0, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, "---", TV, cx_lhuV, cy0, "PRESENTACION_TEXTO");
        }

        // ── B. Cuadro EDIFICACIÓN ─────────────────────────────────────────────
        public static void DrawCuadroEdificacion(Transaction tr, BlockTableRecord ms,
                                                 double hX, double hY,
                                                 List<FilaPiso> pisos)
        {
            const double W = 11.10;
            const double SEP = 0.30;
            const double RH = 0.85;
            const double cL = 3.56;
            const double cAL = 7.52;

            int nPisos = Math.Max(pisos.Count, 1);
            double H = RH * 3 + SEP + nPisos * RH;

            double x = hX + Margen;
            double y = hY + Margen;

            DrawRect(tr, ms, x, y, W, H);

            double yTitulo = y + H - RH;
            double yArea = y + H - RH * 2;
            double yUnidad = y + H - RH * 3;
            double ySep = yUnidad - SEP;

            AddLine(tr, ms, x, yTitulo, x + W, yTitulo, "PRESENTACION_MARCO");
            AddLine(tr, ms, x, yArea, x + W, yArea, "PRESENTACION_MARCO");
            AddLine(tr, ms, x, yUnidad, x + W, yUnidad, "PRESENTACION_MARCO");
            AddLine(tr, ms, x, ySep, x + W, ySep, "PRESENTACION_MARCO");

            for (int p = 0; p < nPisos - 1; p++)
                AddLine(tr, ms,
                    x, ySep - (p + 1) * RH,
                    x + W, ySep - (p + 1) * RH,
                    "PRESENTACION_MARCO");

            AddLine(tr, ms, x + cAL, yArea, x + cAL, yTitulo, "PRESENTACION_MARCO");
            AddLine(tr, ms, x + cL, y, x + cL, yArea, "PRESENTACION_MARCO");

            const double TTI = 1.00;
            const double TH = 0.70;
            const double TV = 0.70;

            AddTextCenter(tr, ms, "EDIFICACIÓN 01", TTI,
                x + W / 2.0, y + H - RH / 2.0, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, "ÁREA DE EDIFICA 01", TH,
                x + cAL / 2.0, yArea + RH / 2.0, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, "UNIDAD 1", TH,
                x + cL / 2.0, yUnidad + RH / 2.0, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, "CASA HABITACIÓN", TH,
                x + cL + (W - cL) / 2.0, yUnidad + RH / 2.0, "PRESENTACION_TEXTO");

            for (int p = 0; p < pisos.Count; p++)
            {
                double filaCY = ySep - p * RH - RH / 2.0;

                var tEtiq = new DBText
                {
                    TextString = pisos[p].Etiqueta,
                    Height = TV,
                    Layer = "PRESENTACION_TEXTO",
                    HorizontalMode = TextHorizontalMode.TextCenter,
                    VerticalMode = TextVerticalMode.TextBase,
                    AlignmentPoint = new Point3d(x + cL / 2.0, filaCY - TV / 2.0, 0),
                    Position = new Point3d(x + cL / 2.0, filaCY - TV / 2.0, 0)
                };
                ms.AppendEntity(tEtiq);
                tr.AddNewlyCreatedDBObject(tEtiq, true);
                SyncXData.Stamp(tEtiq, pisos[p].SyncId, "TABLA");

                AddTextCenter(tr, ms,
                    pisos[p].Area.ToString("F2") + " m²", TV,
                    x + cL + (W - cL) / 2.0, filaCY, "PRESENTACION_TEXTO");
            }
        }

        // ── C. Cuadro ÁREA DE TERRENO ─────────────────────────────────────────
        public static void DrawCuadroArea(Transaction tr, BlockTableRecord ms,
                                          double hX, double hY, double area)
        {
            const double W = 8.05;
            const double H = 1.93;

            double x = hX + HojaW - Margen - W;
            double y = hY + Margen;

            DrawRect(tr, ms, x, y, W, H);
            AddLine(tr, ms, x, y + H / 2.0, x + W, y + H / 2.0, "PRESENTACION_MARCO");

            AddTextCenter(tr, ms, "ÁREA DE TERRENO", 0.60,
                x + W / 2.0, y + H * 3.0 / 4.0, "PRESENTACION_TEXTO");
            AddTextCenter(tr, ms, area.ToString("F2") + " m²", 0.85,
                x + W / 2.0, y + H / 4.0, "PRESENTACION_TEXTO");
        }

        // ── Capas ─────────────────────────────────────────────────────────────
        public static void EnsureLayer(Database db, Transaction tr,
                                       string name, short colorIdx)
        {
            var lt = tr.GetObject(db.LayerTableId, OpenMode.ForWrite) as LayerTable;
            if (lt.Has(name)) return;
            var lr = new LayerTableRecord
            {
                Name = name,
                Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(
                            Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIdx)
            };
            lt.Add(lr);
            tr.AddNewlyCreatedDBObject(lr, true);
        }

        // ── Primitivas públicas ───────────────────────────────────────────────
        public static void DrawRect(Transaction tr, BlockTableRecord ms,
                                    double x, double y, double w, double h)
        {
            var b = new Polyline();
            b.AddVertexAt(0, Pt2(x, y), 0, 0, 0);
            b.AddVertexAt(1, Pt2(x + w, y), 0, 0, 0);
            b.AddVertexAt(2, Pt2(x + w, y + h), 0, 0, 0);
            b.AddVertexAt(3, Pt2(x, y + h), 0, 0, 0);
            b.Closed = true;
            b.Layer = "PRESENTACION_MARCO";
            ms.AppendEntity(b);
            tr.AddNewlyCreatedDBObject(b, true);
        }

        public static void AddLine(Transaction tr, BlockTableRecord ms,
                                   double x1, double y1, double x2, double y2,
                                   string layer)
        {
            var l = new Line(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0))
            { Layer = layer };
            ms.AppendEntity(l);
            tr.AddNewlyCreatedDBObject(l, true);
        }

        public static void AddTextCenter(Transaction tr, BlockTableRecord ms,
                                         string text, double height,
                                         double cx, double cy, string layer)
        {
            double baseY = cy - height / 2.0;
            var t = new DBText
            {
                TextString = text,
                Height = height,
                Layer = layer,
                HorizontalMode = TextHorizontalMode.TextCenter,
                VerticalMode = TextVerticalMode.TextBase,
                AlignmentPoint = new Point3d(cx, baseY, 0),
                Position = new Point3d(cx, baseY, 0)
            };
            ms.AppendEntity(t);
            tr.AddNewlyCreatedDBObject(t, true);
        }

        public static Point2d Pt2(double x, double y) => new Point2d(x, y);
    }
}