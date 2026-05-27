using Autodesk.AutoCAD.Runtime;
using CadTools;
using CadTools.Commands;

[assembly: CommandClass(typeof(LotesGridCommand))]
[assembly: CommandClass(typeof(LotesPluginControl))]
[assembly: ExtensionApplication(typeof(CadTools.Sync.LotesSyncApp))]