using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace CadTools.Sync
{
    public class LotesSyncApp : IExtensionApplication
    {
        internal static readonly LotesSyncReactor _reactor = new LotesSyncReactor();
        private static bool _active = false;

        public void Initialize()
        {
            Attach();
            Application.DocumentManager.DocumentCreated += (s, e) =>
            {
                if (_active)
                    e.Document.Database.ObjectModified += _reactor.OnObjectModified;
            };
        }

        public static void Attach()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            doc.Database.ObjectModified -= _reactor.OnObjectModified;
            doc.Database.ObjectModified += _reactor.OnObjectModified;
            _active = true;
        }

        public static void Detach()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            doc.Database.ObjectModified -= _reactor.OnObjectModified;
            _active = false;
        }

        public static bool IsActive => _active;

        public void Terminate() { Detach(); }
    }

    public class LotesSyncReactor
    {
        private bool _syncing = false;

        public void OnObjectModified(object sender, ObjectEventArgs e)
        {
            if (_syncing) return;
            if (!(e.DBObject is DBText texto)) return;
            if (!texto.Layer.Equals("PRESENTACION_TEXTO",
                    StringComparison.OrdinalIgnoreCase)) return;

            ResultBuffer xdata = texto.GetXDataForApplication(SyncXData.APP_NAME);
            if (xdata == null) return;

            SyncXData.Parse(xdata.AsArray(), out string syncId, out string tipo);
            if (syncId == null || tipo == null) return;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;

            _syncing = true;
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    var ms = tr.GetObject(
                        SymbolUtilityServices.GetBlockModelSpaceId(db),
                        OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId oid in ms)
                    {
                        if (oid == texto.ObjectId) continue;
                        var ent = tr.GetObject(oid, OpenMode.ForRead) as DBText;
                        if (ent == null) continue;
                        if (!ent.Layer.Equals("PRESENTACION_TEXTO",
                                StringComparison.OrdinalIgnoreCase)) continue;

                        ResultBuffer xd =
                            ent.GetXDataForApplication(SyncXData.APP_NAME);
                        if (xd == null) continue;

                        SyncXData.Parse(xd.AsArray(), out string pairId,
                                        out string pairTipo);
                        if (pairId != syncId || pairTipo == tipo) continue;

                        ent.UpgradeOpen();
                        ent.TextString = texto.TextString;
                        break;
                    }
                    tr.Commit();
                }
            }
            finally { _syncing = false; }
        }
    }
}