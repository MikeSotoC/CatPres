using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace CadTools.Models
{
    internal class LoteInfo
    {
        public ObjectId EntId { get; set; }
        public double CenX { get; set; }
        public double CenY { get; set; }
        public Point3d MinPt { get; set; }
        public Point3d MaxPt { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Area { get; set; }
        public string Layer { get; set; }
        public int NumLote { get; set; }
    }
}
