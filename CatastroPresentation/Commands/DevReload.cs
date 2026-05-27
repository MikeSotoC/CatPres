#if DEBUG
using System;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace CadTools.Commands
{
    public class DevReload
    {
        [CommandMethod("LOTES_RELOAD")]
        public void Reload()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            try
            {
                string original = Assembly.GetExecutingAssembly().Location;
                string dir = Path.GetDirectoryName(original);
                string name = Path.GetFileNameWithoutExtension(original);
                string timestamp = DateTime.Now.ToString("HHmmss");
                string copy = Path.Combine(dir, $"{name}_{timestamp}.dll");
                string copyPdb = Path.ChangeExtension(copy, ".pdb");

                File.Copy(original, copy, overwrite: true);
                string pdb = Path.ChangeExtension(original, ".pdb");
                if (File.Exists(pdb)) File.Copy(pdb, copyPdb, overwrite: true);

                ExtensionLoader.Load(copy);
                ed.WriteMessage($"\n[RELOAD] Cargado: {Path.GetFileName(copy)}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[RELOAD ERROR] {ex.Message}");
            }
        }
    }
}
#endif