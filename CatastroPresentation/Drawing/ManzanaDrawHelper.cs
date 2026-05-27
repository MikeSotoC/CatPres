using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadTools.Models;

namespace CadTools.Drawing
{
    internal static class ManzanaDrawHelper
    {
        // Tamaño hoja manzana — ajustar aquí: A2=420×594 | A1=594×841
        public const double HojaW = 420.0;
        public const double HojaH = 594.0;
        public const double Margen = 5.0;
        public const double AnchoCuadrosDerecha = 85.0;

        // ── Marco de hoja ─────────────────────────────────────────────────────
        public static void DrawFrame(Transaction tr, BlockTableRecord ms,
                                     double hX, double hY)
        {
            var b = new Polyline();
            b.AddVertexAt(0, new Point2d(hX, hY), 0, 0, 0);
            b.AddVertexAt(1, new Point2d(hX + HojaW, hY), 0, 0, 0);
            b.AddVertexAt(2, new Point2d(hX + HojaW, hY + HojaH), 0, 0, 0);
            b.AddVertexAt(3, new Point2d(hX, hY + HojaH), 0, 0, 0);
            b.Closed = true;
            b.Layer = "PRESENTACION_MARCO";
            b.ConstantWidth = 0.5;
            ms.AppendEntity(b);
            tr.AddNewlyCreatedDBObject(b, true);
        }

        // ── Calcular esquina inf-izq de hoja centrada sobre la manzana ────────
        public static (double hX, double hY) CalcInsercion(Extents3d ext)
        {
            double cenX = (ext.MinPoint.X + ext.MaxPoint.X) / 2.0;
            double cenY = (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0;

            // Área útil horizontal = hoja - margen izq - margen der - cuadros
            double areaUtilW = HojaW - 2 * Margen - AnchoCuadrosDerecha;
            double areaUtilH = HojaH - 2 * Margen;

            // Centro del área útil coincide con centro de manzana
            double hX = cenX - Margen - areaUtilW / 2.0;
            double hY = cenY - HojaH / 2.0;

            return (hX, hY);
        }

        // ── Cuadro UBICACIÓN ──────────────────────────────────────────────────
        public static void DrawCuadroUbicacion(Transaction tr, BlockTableRecord ms,
                                               double hX, double hY)
        {
            const double W = 80.0;
            const double RH = 6.0;
            const int NF = 5;
            double H = RH * (NF + 1);

            double x = hX + HojaW - Margen - W;
            double y = hY + HojaH - Margen - H;

            LotesDrawHelper.DrawRect(tr, ms, x, y, W, H);

            double yTit = y + H - RH;
            LotesDrawHelper.AddLine(tr, ms, x, yTit, x + W, yTit,
                "PRESENTACION_MARCO");
            LotesDrawHelper.AddTextCenter(tr, ms, "UBICACIÓN", RH * 0.6,
                x + W / 2.0, yTit + RH / 2.0, "PRESENTACION_TEXTO");

            double cL = W * 0.45;
            LotesDrawHelper.AddLine(tr, ms, x + cL, y, x + cL, yTit,
                "PRESENTACION_MARCO");

            string[] etiquetas =
                { "DEPARTAMENTO", "PROVINCIA", "DISTRITO", "SECTOR", "MANZANA" };

            for (int i = 0; i < NF; i++)
            {
                double yFila = yTit - (i + 1) * RH;
                LotesDrawHelper.AddLine(tr, ms, x, yFila, x + W, yFila,
                    "PRESENTACION_MARCO");

                double cy = yFila + RH / 2.0;
                LotesDrawHelper.AddTextCenter(tr, ms, etiquetas[i], RH * 0.45,
                    x + cL / 2.0, cy, "PRESENTACION_TEXTO");
                LotesDrawHelper.AddTextCenter(tr, ms, "---", RH * 0.45,
                    x + cL + (W - cL) / 2.0, cy, "PRESENTACION_TEXTO");
            }
        }

        // ── Cuadro RESUMEN DE LOTES ───────────────────────────────────────────
        public static void DrawCuadroResumenLotes(
            Transaction tr, BlockTableRecord ms,
            double hX, double hY,
            List<FilaLote> filas)
        {
            const double W = 80.0;
            const double RH = 6.0;

            int nFilas = Math.Max(filas.Count, 1);
            double H = RH * 2 + nFilas * RH;

            double x = hX + HojaW - Margen - W;
            double y = hY + Margen;

            LotesDrawHelper.DrawRect(tr, ms, x, y, W, H);

            double yTit = y + H - RH;
            double yEnc = y + H - RH * 2;

            LotesDrawHelper.AddLine(tr, ms, x, yTit, x + W, yTit,
                "PRESENTACION_MARCO");
            LotesDrawHelper.AddLine(tr, ms, x, yEnc, x + W, yEnc,
                "PRESENTACION_MARCO");

            LotesDrawHelper.AddTextCenter(tr, ms, "RESUMEN DE LOTES", RH * 0.55,
                x + W / 2.0, yTit + RH / 2.0, "PRESENTACION_TEXTO");

            double c0 = W * 0.20;
            double c1 = W * 0.45;
            double c2 = W * 0.35;

            LotesDrawHelper.AddLine(tr, ms, x + c0, y, x + c0, yTit,
                "PRESENTACION_MARCO");
            LotesDrawHelper.AddLine(tr, ms, x + c0 + c1, y, x + c0 + c1, yTit,
                "PRESENTACION_MARCO");

            double encCY = yEnc + RH / 2.0;
            LotesDrawHelper.AddTextCenter(tr, ms, "Nº LOTE", RH * 0.40,
                x + c0 / 2.0, encCY, "PRESENTACION_TEXTO");
            LotesDrawHelper.AddTextCenter(tr, ms, "ÁREA m²", RH * 0.40,
                x + c0 + c1 / 2.0, encCY, "PRESENTACION_TEXTO");
            LotesDrawHelper.AddTextCenter(tr, ms, "FRENTE ml", RH * 0.40,
                x + c0 + c1 + c2 / 2.0, encCY, "PRESENTACION_TEXTO");

            for (int i = 0; i < filas.Count; i++)
            {
                double yFila = yEnc - (i + 1) * RH;
                LotesDrawHelper.AddLine(tr, ms, x, yFila, x + W, yFila,
                    "PRESENTACION_MARCO");

                double cy = yFila + RH / 2.0;
                LotesDrawHelper.AddTextCenter(tr, ms,
                    filas[i].NumLote.ToString(), RH * 0.40,
                    x + c0 / 2.0, cy, "PRESENTACION_TEXTO");
                LotesDrawHelper.AddTextCenter(tr, ms,
                    filas[i].Area.ToString("F2"), RH * 0.40,
                    x + c0 + c1 / 2.0, cy, "PRESENTACION_TEXTO");
                LotesDrawHelper.AddTextCenter(tr, ms,
                    filas[i].Frente.ToString("F2"), RH * 0.40,
                    x + c0 + c1 + c2 / 2.0, cy, "PRESENTACION_TEXTO");
            }
        }

        // ── Cuadro ÁREA TOTAL MANZANA ─────────────────────────────────────────
        public static void DrawCuadroAreaManzana(Transaction tr, BlockTableRecord ms,
                                                 double hX, double hY,
                                                 double areaManzana, int totalLotes)
        {
            const double W = 80.0;
            const double H = 14.0;
            const double RH = H / 2.0;

            double x = hX + HojaW - Margen - W;
            double y = hY + Margen + 20.0;   // sobre el norte

            LotesDrawHelper.DrawRect(tr, ms, x, y, W, H);
            LotesDrawHelper.AddLine(tr, ms, x, y + RH, x + W, y + RH,
                "PRESENTACION_MARCO");

            double cL = W / 2.0;
            LotesDrawHelper.AddLine(tr, ms, x + cL, y, x + cL, y + H,
                "PRESENTACION_MARCO");

            LotesDrawHelper.AddTextCenter(tr, ms, "ÁREA MANZANA", RH * 0.45,
                x + cL / 2.0, y + RH * 1.5, "PRESENTACION_TEXTO");
            LotesDrawHelper.AddTextCenter(tr, ms, "TOTAL LOTES", RH * 0.45,
                x + cL + cL / 2.0, y + RH * 1.5, "PRESENTACION_TEXTO");

            LotesDrawHelper.AddTextCenter(tr, ms,
                areaManzana.ToString("F2") + " m²", RH * 0.55,
                x + cL / 2.0, y + RH * 0.5, "PRESENTACION_TEXTO");
            LotesDrawHelper.AddTextCenter(tr, ms,
                totalLotes.ToString(), RH * 0.55,
                x + cL + cL / 2.0, y + RH * 0.5, "PRESENTACION_TEXTO");
        }

        // ── Norte gráfico ─────────────────────────────────────────────────────
        public static void DrawNorte(Transaction tr, BlockTableRecord ms,
                                     double hX, double hY)
        {
            double x = hX + HojaW - Margen - 80.0 + 20.0;
            double y = hY + Margen + 2.0;

            const double r = 5.0;
            var circ = new Polyline();
            const int seg = 16;
            for (int k = 0; k < seg; k++)
            {
                double ang = 2.0 * Math.PI * k / seg;
                circ.AddVertexAt(k, new Point2d(
                    x + r * Math.Cos(ang),
                    y + r * Math.Sin(ang)), 0, 0, 0);
            }
            circ.Closed = true;
            circ.Layer = "PRESENTACION_MARCO";
            ms.AppendEntity(circ);
            tr.AddNewlyCreatedDBObject(circ, true);

            LotesDrawHelper.AddLine(tr, ms, x, y - r, x, y + r + 3.0,
                "PRESENTACION_MARCO");
            LotesDrawHelper.AddTextCenter(tr, ms, "N", r * 0.8,
                x, y + r + 3.0 + r * 0.5, "PRESENTACION_TEXTO");
        }

        // ── Calcular frente de un lote (lado más largo que toca TG_EJE_VIA) ──
        // Si no hay eje de vía disponible, retorna el lado más largo
        public static double CalcFrente(Polyline pl)
        {
            double maxLen = 0;
            int n = pl.NumberOfVertices;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double len = pl.GetPoint3dAt(i).DistanceTo(pl.GetPoint3dAt(j));
                if (len > maxLen) maxLen = len;
            }
            return maxLen;
        }
    }
}