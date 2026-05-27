using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CadTools.Sync;

namespace CadTools.Commands
{
    public class LotesPluginControl
    {
        [CommandMethod("LOTES_OFF")]
        public void Desactivar()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            if (!LotesSyncApp.IsActive)
            { ed.WriteMessage("\n[LOTES] El plugin ya está desactivado."); return; }
            LotesSyncApp.Detach();
            ed.WriteMessage("\n[LOTES] Sincronización desactivada. Use LOTES_ON para reactivar.");
        }

        [CommandMethod("LOTES_ON")]
        public void Activar()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            if (LotesSyncApp.IsActive)
            { ed.WriteMessage("\n[LOTES] El plugin ya está activo."); return; }
            LotesSyncApp.Attach();
            ed.WriteMessage("\n[LOTES] Sincronización reactivada.");
        }

        [CommandMethod("LOTES_STATUS")]
        public void Estado()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            string estado = LotesSyncApp.IsActive ? "ACTIVO ✓" : "INACTIVO ✗";
            ed.WriteMessage($"\n[LOTES] Estado del plugin: {estado}");
        }
    }
}