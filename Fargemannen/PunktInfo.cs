using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil;
using Autodesk.Civil.DataShortcuts;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.AutoCAD.Runtime;

using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using System.Windows.Interop;
using Autodesk.AutoCAD.Geometry;


namespace Fargemannen
{
    public class PunktInfo
    {
        public string MinPunktID { get; set; }
        public Point3d Punkt { get; set; }
        public string RapportID { get; set; }
        public string PunktID { get; set; }
        public string GBUMetode { get; set; }
        public double Terrengkvote { get; set; }
        public double BoreFjell { get; set; }
        public double BorLøs { get; set; }
    }
}
