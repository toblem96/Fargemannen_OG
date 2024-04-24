using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Autodesk.AutoCAD.Geometry;
using System.Windows.Controls;
namespace Fargemannen.Model
{
   public static class SymbolHandlers
    {

        public static void EndreLagFarge(string lagNavn, System.Drawing.Color farge)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                    if (lt.Has(lagNavn))
                    {
                        lt.UpgradeOpen();
                        LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[lagNavn], OpenMode.ForWrite);

                        // Konverterer System.Drawing.Color til Autodesk.AutoCAD.Colors.Color
                        Autodesk.AutoCAD.Colors.Color acColor = Autodesk.AutoCAD.Colors.Color.FromColor(farge);

                        ltr.Color = acColor;

                        tr.Commit();
                    }
                }
            }
            doc.Editor.Regen();
        }

        public static System.Windows.Media.Color ConvertToMediaColor(System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }


        public static void RoterBlokker(List<string> blockNames, double rotationAngleInDegrees)
        {
            Transaction acTrans = null;

            var acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var acCurDb = acDoc.Database;

            using (acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                using (acDoc.LockDocument())
                {
                    var bt = (BlockTable)acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                    var btr = (BlockTableRecord)acTrans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    foreach (var objId in btr)
                    {
                        var entity = acTrans.GetObject(objId, OpenMode.ForRead) as Entity;
                        if (entity is BlockReference blockRef)
                        {
                            blockRef.UpgradeOpen(); // Tillater endringer
                            blockRef.Rotation = rotationAngleInDegrees *(Math.PI / 180);
                            rotation = rotationAngleInDegrees * (Math.PI / 180);
                        }
                    }
                }
                acTrans.Commit(); // Utfører transaksjonen
                blockNames.Clear(); // Tømmer listen
            }

            acDoc.Editor.Regen(); // Oppdaterer visningen
        }


        public static double Scale;
        public static double rotation;



        public static void EndreSkala(List<string> blockNames, double scale)
        {
            Transaction acTrans = null;

            var acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var acCurDb = acDoc.Database;

            using (acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                using (acDoc.LockDocument())
                {
                    var bt = (BlockTable)acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                    var btr = (BlockTableRecord)acTrans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    foreach (var objId in btr)
                    {
                        var entity = acTrans.GetObject(objId, OpenMode.ForRead) as Entity;
                        if (entity is BlockReference blockRef)
                        {
                            blockRef.UpgradeOpen(); // Tillater endringer
                            blockRef.ScaleFactors = new Scale3d(scale); // Skaler blokken
                            Scale = scale;
                        }
                    }
                }
                acTrans.Commit(); // Utfører transaksjonen
                blockNames.Clear(); // Tømmer listen
            }

            acDoc.Editor.Regen(); // Oppdaterer visningen
        }
    }
}