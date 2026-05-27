using Autodesk.AutoCAD.DatabaseServices;

namespace CadTools.Sync
{
    internal static class SyncXData
    {
        public const string APP_NAME = "LOTES_SYNC";

        public static void EnsureRegApp(Database db, Transaction tr)
        {
            var rat = tr.GetObject(db.RegAppTableId,
                                   OpenMode.ForWrite) as RegAppTable;
            if (rat.Has(APP_NAME)) return;
            var ra = new RegAppTableRecord { Name = APP_NAME };
            rat.Add(ra);
            tr.AddNewlyCreatedDBObject(ra, true);
        }

        public static void Stamp(DBText texto, string syncId, string tipo)
        {
            var rb = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, APP_NAME),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, "SYNC_ID"),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, syncId),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, "TIPO"),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, tipo)
            );
            texto.XData = rb;
        }

        public static void Parse(TypedValue[] vals,
                                 out string syncId, out string tipo)
        {
            syncId = null; tipo = null;
            for (int i = 0; i < vals.Length; i++)
            {
                if (vals[i].TypeCode != (short)DxfCode.ExtendedDataAsciiString)
                    continue;
                string v = vals[i].Value.ToString();
                if (v == "SYNC_ID" && i + 1 < vals.Length)
                    syncId = vals[i + 1].Value.ToString();
                else if (v == "TIPO" && i + 1 < vals.Length)
                    tipo = vals[i + 1].Value.ToString();
            }
        }
    }
}