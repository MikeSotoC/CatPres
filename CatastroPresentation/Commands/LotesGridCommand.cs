    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.AutoCAD.ApplicationServices;

    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using CadTools.Drawing;
    using CadTools.Models;
    using CadTools.Sync;

    namespace CadTools
    {
        public class LotesGridCommand
        {
            private static readonly HashSet<string> CapasInterior =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "PISO_01", "PISO_01_SINTECHO",
                    "PISO_02", "PISO_02_SINTECHO",
                    "PISO_03", "PISO_03_SINTECHO",
                    "PISO_04", "PISO_04_SINTECHO",
                    "TG_LOTE", "TG_EJE_VIA",
                    "_NUMERO_DE_LOTES", "DUCTO_01", "DUCTO_02"
                };

            private static readonly HashSet<string> CapasPisoPoligono =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "PISO_01", "PISO_02", "PISO_03", "PISO_04"
                };

            private static readonly HashSet<string> CapasABorrar =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "PISO_01", "PISO_01_SINTECHO",
                    "PISO_02", "PISO_02_SINTECHO",
                    "PISO_03", "PISO_03_SINTECHO",
                    "PISO_04", "PISO_04_SINTECHO",
                    "DUCTO_01", "DUCTO_02",
                    "AREA_LIBRE", "COTA_FABRICA", "INGRESO"
                };

            private static readonly string[] CapasPiso =
                { "PISO_01", "PISO_02", "PISO_03", "PISO_04" };

            private const double HojaW = LotesDrawHelper.HojaW;
            private const double HojaH = LotesDrawHelper.HojaH;
            private const double Margen = LotesDrawHelper.Margen;
            private const double PesoAngulo = 3.0;
            private const double PesoDist = 1.0;

            [CommandMethod("LOTESGRID")]
            public void Run()
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // ── 1. Selección ──────────────────────────────────────────────
                    ed.WriteMessage("\n[1] Seleccione polilíneas de LOTES: ");
                    var filter = new SelectionFilter(new[]
                    {
                        new TypedValue((int)DxfCode.Start,     "LWPOLYLINE"),
                        new TypedValue((int)DxfCode.LayerName, "TG_LOTE")
                    });
                    PromptSelectionResult psr = ed.GetSelection(filter);

                    if (psr.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\nSin filtro capa, seleccione manualmente: ");
                        psr = ed.GetSelection(new SelectionFilter(new[]
                            { new TypedValue((int)DxfCode.Start, "LWPOLYLINE") }));
                    }
                    if (psr.Status != PromptStatus.OK)
                    { ed.WriteMessage("\nCancelado."); return; }

                    // ── 2. Centroides / bbox / área ───────────────────────────────
                    var lotes = new List<LoteInfo>();
                    foreach (ObjectId oid in psr.Value.GetObjectIds())
                    {
                        var pl = tr.GetObject(oid, OpenMode.ForRead) as Polyline;
                        if (pl == null) continue;
                        Extents3d ext = pl.GeometricExtents;
                        lotes.Add(new LoteInfo
                        {
                            EntId = oid,
                            CenX = (ext.MinPoint.X + ext.MaxPoint.X) / 2.0,
                            CenY = (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0,
                            MinPt = ext.MinPoint,
                            MaxPt = ext.MaxPoint,
                            Width = ext.MaxPoint.X - ext.MinPoint.X,
                            Height = ext.MaxPoint.Y - ext.MinPoint.Y,
                            Area = Math.Abs(pl.Area),
                            Layer = pl.Layer
                        });
                    }
                    if (lotes.Count == 0)
                    { ed.WriteMessage("\nNo se encontraron lotes."); return; }

                    // ── 2b. Detección de manzanas múltiples ───────────────────────
                    var ms0 = tr.GetObject(
                        SymbolUtilityServices.GetBlockModelSpaceId(db),
                        OpenMode.ForRead) as BlockTableRecord;

                    var manzanas = new List<(ObjectId Id, Polyline Pl)>();
                    foreach (ObjectId oid in ms0)
                    {
                        var ent = tr.GetObject(oid, OpenMode.ForRead) as Polyline;
                        if (ent == null) continue;
                        if (!ent.Layer.Equals("TG_MANZANA",
                                StringComparison.OrdinalIgnoreCase)) continue;
                        manzanas.Add((oid, ent));
                    }

                    var manzanasAfectadas = new HashSet<ObjectId>();
                    Polyline manzanaPl = null;

                    foreach (LoteInfo info in lotes)
                    {
                        Point3d cen = new Point3d(info.CenX, info.CenY, 0);
                        foreach (var (mzId, mzPl) in manzanas)
                        {
                            if (LotesDimHelper.PuntoDentroDePolilinea(mzPl, cen))
                            {
                                manzanasAfectadas.Add(mzId);
                                manzanaPl = mzPl;
                                break;
                            }
                        }
                    }

                    if (manzanasAfectadas.Count > 1)
                    {
                        ed.WriteMessage(
                            $"\n⚠ ADVERTENCIA: Los lotes seleccionados pertenecen a " +
                            $"{manzanasAfectadas.Count} manzanas distintas.");
                        ed.WriteMessage(
                            "\n  El borrado de interiores afectará todas las " +
                            "manzanas seleccionadas.");

                        var pko = new PromptKeywordOptions(
                            "\n  ¿Desea continuar? [Si/No] <No>: ");
                        pko.Keywords.Add("Si");
                        pko.Keywords.Add("No");
                        pko.Keywords.Default = "No";
                        pko.AllowNone = true;

                        PromptResult pkr = ed.GetKeywords(pko);
                        string resp = pkr.Status == PromptStatus.OK
                            ? pkr.StringResult : "No";

                        if (!resp.Equals("Si", StringComparison.OrdinalIgnoreCase))
                        { ed.WriteMessage("\nOperación cancelada."); return; }

                        // Si hay múltiples manzanas no dibujamos presentación de manzana
                        manzanaPl = null;
                    }

                    // ── 3. Dos clics: inicio y dirección ──────────────────────────
                    ed.WriteMessage("\n[2] Clic en el LOTE 1 (inicio del recorrido): ");
                    PromptPointResult r1 = ed.GetPoint("\n");
                    if (r1.Status != PromptStatus.OK)
                    { ed.WriteMessage("\nCancelado."); return; }
                    Point3d ptInicio = r1.Value;

                    var ppo = new PromptPointOptions(
                        "\n[3] Clic en LOTE 2 (dirección del recorrido): ")
                    { UseBasePoint = true, BasePoint = ptInicio };
                    PromptPointResult r2 = ed.GetPoint(ppo);
                    if (r2.Status != PromptStatus.OK)
                    { ed.WriteMessage("\nCancelado."); return; }

                    double dxD = r2.Value.X - ptInicio.X;
                    double dyD = r2.Value.Y - ptInicio.Y;
                    double mag = Math.Sqrt(dxD * dxD + dyD * dyD);
                    if (mag > 0) { dxD /= mag; dyD /= mag; }
                    else { dxD = 1; dyD = 0; }

                    int idxInicio = 0; double dMin = double.MaxValue;
                    for (int k = 0; k < lotes.Count; k++)
                    {
                        double dx = lotes[k].CenX - ptInicio.X;
                        double dy = lotes[k].CenY - ptInicio.Y;
                        double d2 = dx * dx + dy * dy;
                        if (d2 < dMin) { dMin = d2; idxInicio = k; }
                    }

                    // ── 4. Recorrido dirigido ─────────────────────────────────────
                    var ordenados = new List<int> { idxInicio };
                    var noUsados = Enumerable.Range(0, lotes.Count)
                                              .Where(k => k != idxInicio).ToList();
                    int idxActual = idxInicio;

                    while (noUsados.Count > 0)
                    {
                        double cx = lotes[idxActual].CenX, cy = lotes[idxActual].CenY;
                        double best = double.MaxValue; int bestIdx = -1;

                        foreach (int idx in noUsados)
                        {
                            double dx = lotes[idx].CenX - cx;
                            double dy = lotes[idx].CenY - cy;
                            double dist = Math.Sqrt(dx * dx + dy * dy);
                            double angP = dist > 0.001
                                ? 1.0 - (dx / dist * dxD + dy / dist * dyD) : 1.0;
                            double score = PesoDist * dist + PesoAngulo * angP * dist;
                            if (score < best) { best = score; bestIdx = idx; }
                        }
                        if (bestIdx < 0) break;

                        dxD = lotes[bestIdx].CenX - lotes[idxActual].CenX;
                        dyD = lotes[bestIdx].CenY - lotes[idxActual].CenY;
                        mag = Math.Sqrt(dxD * dxD + dyD * dyD);
                        if (mag > 0) { dxD /= mag; dyD /= mag; }

                        ordenados.Add(bestIdx);
                        noUsados.Remove(bestIdx);
                        idxActual = bestIdx;
                    }

                    for (int n = 0; n < ordenados.Count; n++)
                        lotes[ordenados[n]].NumLote = n + 1;

                    // ── 5. Entidades interiores ───────────────────────────────────
                    var ms2 = tr.GetObject(
                        SymbolUtilityServices.GetBlockModelSpaceId(db),
                        OpenMode.ForWrite) as BlockTableRecord;

                    var interior = new List<(ObjectId Id, Extents3d Ext, string Layer)>();
                    foreach (ObjectId oid in ms2)
                    {
                        var ent = tr.GetObject(oid, OpenMode.ForRead) as Entity;
                        if (ent == null || !CapasInterior.Contains(ent.Layer)) continue;
                        try { interior.Add((oid, ent.GeometricExtents, ent.Layer)); }
                        catch { }
                    }

                    // ── 6. Punto de inserción ─────────────────────────────────────
                    int total = ordenados.Count;
                    int cols = (total + 1) / 2;

                    PromptPointResult rIns = ed.GetPoint(
                        "\n[4] Esquina inf-izq de la primera hoja A4: ");
                    if (rIns.Status != PromptStatus.OK)
                    { ed.WriteMessage("\nCancelado."); return; }
                    Point3d ptBase = rIns.Value;

                    var pdoX = new PromptDoubleOptions(
                        "\n    Separación X entre hojas <5.0>: ")
                    { DefaultValue = 5.0, AllowNone = true };
                    double sepX = ed.GetDouble(pdoX).Value;
                    if (double.IsNaN(sepX)) sepX = 5.0;

                    var pdoY = new PromptDoubleOptions(
                        "\n    Separación Y entre filas <10.0>: ")
                    { DefaultValue = 10.0, AllowNone = true };
                    double sepY = ed.GetDouble(pdoY).Value;
                    if (double.IsNaN(sepY)) sepY = 10.0;

                    // ── 7. Capas y XData ──────────────────────────────────────────
                    LotesDrawHelper.EnsureLayer(db, tr, "PRESENTACION_MARCO", 7);
                    LotesDrawHelper.EnsureLayer(db, tr, "PRESENTACION_TEXTO", 2);
                    SyncXData.EnsureRegApp(db, tr);

                    // ── 8. Bucle principal — hojas A4 por lote ────────────────────
                    for (int i = 0; i < ordenados.Count; i++)
                    {
                        LoteInfo info = lotes[ordenados[i]];
                        int col = i % cols;
                        int fila = i / cols;

                        double hX = ptBase.X + col * (HojaW + sepX);
                        double hY = ptBase.Y + fila * (HojaH + sepY);

                        LotesDrawHelper.DrawFrame(tr, ms2, hX, hY);

                        double utilCX = hX + HojaW / 2.0;
                        double utilCY = hY + Margen + (HojaH - 2 * Margen) / 2.0;

                        Vector3d delta = new Vector3d(
                            utilCX - info.CenX,
                            utilCY - info.CenY,
                            0.0);

                        var toBeCopied = new ObjectIdCollection { info.EntId };
                        double lx0 = info.MinPt.X + 0.01, lx1 = info.MaxPt.X - 0.01;
                        double ly0 = info.MinPt.Y + 0.01, ly1 = info.MaxPt.Y - 0.01;

                        foreach (var (oid2, ext2, _) in interior)
                        {
                            if (oid2 == info.EntId) continue;
                            double cjX = (ext2.MinPoint.X + ext2.MaxPoint.X) / 2.0;
                            double cjY = (ext2.MinPoint.Y + ext2.MaxPoint.Y) / 2.0;
                            if (cjX > lx0 && cjX < lx1 && cjY > ly0 && cjY < ly1)
                                toBeCopied.Add(oid2);
                        }

                        // Deep clone + trasladar
                        var idMap = new IdMapping();
                        db.DeepCloneObjects(toBeCopied, ms2.ObjectId, idMap, false);

                        ObjectId clonedLoteId = ObjectId.Null;
                        var clonedPisos = new List<(ObjectId Id, string Layer)>();

                        foreach (IdPair pair in idMap)
                        {
                            if (!pair.IsCloned) continue;
                            var clone = tr.GetObject(pair.Value, OpenMode.ForWrite)
                                        as Entity;
                            if (clone == null) continue;

                            clone.TransformBy(Matrix3d.Displacement(delta));

                            if (clone is Polyline clPl)
                            {
                                if (pair.Key == info.EntId)
                                    clonedLoteId = pair.Value;
                                else if (CapasPisoPoligono.Contains(clPl.Layer))
                                    clonedPisos.Add((pair.Value, clPl.Layer));
                            }
                        }

                        // Cotas COTA_LOTE — exterior al lote clonado
                        if (clonedLoteId != ObjectId.Null)
                        {
                            var plLote = tr.GetObject(clonedLoteId, OpenMode.ForRead)
                                         as Polyline;
                            if (plLote != null)
                                LotesDimHelper.CotarPoligono(
                                    tr, ms2, db, plLote,
                                    "COTA_LOTE", 1.0, exterior: true);
                        }

                        // Cotas COTA_FABRICA — interior a cada piso clonado
                        foreach (var (pidOid, _) in clonedPisos)
                        {
                            var plPiso = tr.GetObject(pidOid, OpenMode.ForRead)
                                         as Polyline;
                            if (plPiso != null)
                                LotesDimHelper.CotarPoligono(
                                    tr, ms2, db, plPiso,
                                    "COTA_FABRICA", 1.0, exterior: false);
                        }

                        // Cuadros de datos
                        var pisos = RecolectarPisos(tr, ms2, db, info, interior);
                        LotesDrawHelper.DrawCuadroSector(tr, ms2, hX, hY);
                        LotesDrawHelper.DrawCuadroEdificacion(tr, ms2, hX, hY, pisos);
                        LotesDrawHelper.DrawCuadroArea(tr, ms2, hX, hY, info.Area);
                    }

                    // ── 9. Borrar interiores del modelo (solo lotes seleccionados) ─
                    var aEliminar = new List<ObjectId>();
                    foreach (var (oid, ext, layer) in interior)
                    {
                        if (!CapasABorrar.Contains(layer)) continue;

                        double cjX = (ext.MinPoint.X + ext.MaxPoint.X) / 2.0;
                        double cjY = (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0;

                        foreach (int idx in ordenados)
                        {
                            using (var plLote = tr.GetObject(lotes[idx].EntId, OpenMode.ForRead)
                                         as Polyline)
                            {
                                if (plLote == null) continue;
                                if (LotesDimHelper.PuntoDentroDePolilinea(
                                        plLote, new Point3d(cjX, cjY, 0)))
                                {
                                    aEliminar.Add(oid);
                                    break;
                                }
                            }
                        }
                    }

                    foreach (ObjectId oid in aEliminar)
                    {
                        try
                        {
                            var ent = tr.GetObject(oid, OpenMode.ForWrite) as Entity;
                            ent?.Erase();
                        }
                        catch { }
                    }

                    ed.WriteMessage(
                        $"\n✓ {aEliminar.Count} entidades interiores borradas del modelo.");

                    // ── 10. Presentación de manzana ───────────────────────────────
                    if (manzanaPl != null)
                    {
                        Extents3d mzExt = manzanaPl.GeometricExtents;
                        var (mhX, mhY) = ManzanaDrawHelper.CalcInsercion(mzExt);

                        // Marco de hoja
                        ManzanaDrawHelper.DrawFrame(tr, ms2, mhX, mhY);

                        // Cotas COTA_MANZANA — exterior a la manzana (in-situ)
                        LotesDimHelper.CotarPoligono(
                            tr, ms2, db, manzanaPl,
                            "COTA_MANZANA", 1.0, exterior: true);

                        // Cotas COTA_LOTE — exterior a cada lote (in-situ)
                        foreach (int idx in ordenados)
                        {
                            using (var plLote = tr.GetObject(lotes[idx].EntId, OpenMode.ForRead)
                                         as Polyline)
                            {
                                if (plLote != null)
                                    LotesDimHelper.CotarPoligono(
                                        tr, ms2, db, plLote,
                                        "COTA_LOTE", 1.0, exterior: true);
                            }
                        }

                        // Recolectar filas para resumen
                        var filasLote = new List<FilaLote>();
                        foreach (int idx in ordenados)
                        {
                            using (var plLote = tr.GetObject(lotes[idx].EntId, OpenMode.ForRead)
                                         as Polyline)
                            {
                                filasLote.Add(new FilaLote
                                {
                                    NumLote = lotes[idx].NumLote,
                                    Area = lotes[idx].Area,
                                    Frente = plLote != null
                                              ? ManzanaDrawHelper.CalcFrente(plLote)
                                              : 0.0
                                });
                            }
                        }

                        // Cuadros de datos de manzana
                        ManzanaDrawHelper.DrawCuadroUbicacion(tr, ms2, mhX, mhY);
                        ManzanaDrawHelper.DrawCuadroResumenLotes(
                            tr, ms2, mhX, mhY, filasLote);
                        ManzanaDrawHelper.DrawCuadroAreaManzana(
                            tr, ms2, mhX, mhY,
                            Math.Abs(manzanaPl.Area),
                            ordenados.Count);
                        ManzanaDrawHelper.DrawNorte(tr, ms2, mhX, mhY);

                        ed.WriteMessage("\n✓ Presentación de manzana generada.");
                    }

                    tr.Commit();

                    int filas2 = (total <= cols) ? 1 : 2;
                    ed.WriteMessage(
                        $"\n✓ {total} lotes — {filas2} fila(s) × {cols} columnas.");
                }
            }

            // ── Recolectar pisos ──────────────────────────────────────────────────
            private static List<FilaPiso> RecolectarPisos(
                Transaction tr, BlockTableRecord ms, Database db,
                LoteInfo info,
                List<(ObjectId Id, Extents3d Ext, string Layer)> interior)
            {
                var pisos = new List<FilaPiso>();

                double lx0 = info.MinPt.X + 0.01, lx1 = info.MaxPt.X - 0.01;
                double ly0 = info.MinPt.Y + 0.01, ly1 = info.MaxPt.Y - 0.01;

                var syncMap = new Dictionary<string, (string SyncId, string Etiqueta)>();
                foreach (ObjectId oid in ms)
                {
                    var t = tr.GetObject(oid, OpenMode.ForRead) as DBText;
                    if (t == null) continue;
                    ResultBuffer xd = t.GetXDataForApplication(SyncXData.APP_NAME);
                    if (xd == null) continue;
                    SyncXData.Parse(xd.AsArray(), out string sid, out string tipo);
                    if (sid == null || tipo != "POLIGONO") continue;
                    string key =
                        $"{Math.Round(t.Position.X, 2)}|{Math.Round(t.Position.Y, 2)}";
                    syncMap[key] = (sid, t.TextString);
                }

                foreach (string capa in CapasPiso)
                {
                    int numPiso = Array.IndexOf(CapasPiso, capa) + 1;

                    foreach (var (oid, ext, layer) in interior)
                    {
                        if (!layer.Equals(capa, StringComparison.OrdinalIgnoreCase))
                            continue;

                        double cx = (ext.MinPoint.X + ext.MaxPoint.X) / 2.0;
                        double cy = (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0;
                        if (cx <= lx0 || cx >= lx1 || cy <= ly0 || cy >= ly1) continue;

                        var pl = tr.GetObject(oid, OpenMode.ForRead) as Polyline;
                        if (pl == null) continue;

                        string key =
                            $"{Math.Round(cx, 2)}|{Math.Round(cy, 2)}";
                        string syncId, etiqueta;

                        if (syncMap.ContainsKey(key))
                        {
                            syncId = syncMap[key].SyncId;
                            etiqueta = syncMap[key].Etiqueta;
                        }
                        else
                        {
                            syncId = Guid.NewGuid().ToString();
                            etiqueta = $"{numPiso}P --";
                            InsertTextInPolygon(tr, ms, cx, cy, etiqueta, syncId);
                        }

                        pisos.Add(new FilaPiso
                        {
                            Etiqueta = etiqueta,
                            Area = Math.Abs(pl.Area),
                            SyncId = syncId
                        });
                    }
                }
                return pisos;
            }

            private static void InsertTextInPolygon(
                Transaction tr, BlockTableRecord ms,
                double cx, double cy, string etiqueta, string syncId)
            {
                var t = new DBText
                {
                    TextString = etiqueta,
                    Height = 0.50,
                    Layer = "PRESENTACION_TEXTO",
                    HorizontalMode = TextHorizontalMode.TextCenter,
                    VerticalMode = TextVerticalMode.TextVerticalMid,
                    AlignmentPoint = new Point3d(cx, cy, 0),
                    Position = new Point3d(cx, cy, 0)
                };
                ms.AppendEntity(t);
                tr.AddNewlyCreatedDBObject(t, true);
                SyncXData.Stamp(t, syncId, "POLIGONO");
            }
        }
    }