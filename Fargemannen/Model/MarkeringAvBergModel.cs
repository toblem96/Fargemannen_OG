using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;

using Microsoft.Win32;
using Autodesk.AutoCAD.Geometry;
using System.IO;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.AutoCAD.Runtime;


using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Windows.Media.Animation;
using System.Security.Cryptography;


using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Colors;

namespace Fargemannen.Model
{
    public class MarkeringAvBergModel
    {
        public static void KjørMakeringAvBerg(List<PunktInfo> punterInfo, double minBor, double RuteStørresle)
        {

            var punterMedInfo = LageDic(punterInfo, minBor);
            FinnOgLagSirkler(punterMedInfo, RuteStørresle);
            //Lage funk for DIC
            //Lage Funk for nørmeste (Lage funk får å genere geometri)




        }

        private static List<Dictionary<Point3d, bool>> LageDic(List<PunktInfo> punktInfos, double minBor)
        {
            var dict = new List<Dictionary<Point3d, bool>>();

            foreach (var punktInfo in punktInfos)
            {
                var result = new Dictionary<Point3d, bool>();
                result.Add(punktInfo.Punkt, punktInfo.BoreFjell >= minBor);
                dict.Add(result);
            }

            return dict;
        }
        public static void FinnOgLagSirkler(List<Dictionary<Point3d, bool>> punktData, double diameter)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            Editor acEd = acDoc.Editor;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())


            using (acDoc.LockDocument())
            {
                BlockTable acBlkTbl;
                BlockTableRecord acBlkTblRec;

                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (var midPoint in Analyse.MidPoints)
                {
                    Point3d nearestPoint = FindNearestPoint(midPoint.Position, punktData);
                    if (!punktData.First(d => d.ContainsKey(nearestPoint))[nearestPoint])  // Sjekker om verdien er false
                    {
                        Circle acCircle = new Circle
                        {
                            Center = new Point3d(midPoint.Position.X, midPoint.Position.Y, 0),
                            Radius = diameter / 2,
                            Normal = new Vector3d(0, 0, 1)
                        };

                        acBlkTblRec.AppendEntity(acCircle);
                        acTrans.AddNewlyCreatedDBObject(acCircle, true);
                    }
                }

                acTrans.Commit();
            }
        }

        private static Point3d FindNearestPoint(Point3d fromPoint, List<Dictionary<Point3d, bool>> punktData)
        {
            Point3d nearestPoint = new Point3d();
            double minDistance = double.MaxValue;

            Point3d fromPointXY = new Point3d(fromPoint.X, fromPoint.Y, 0); // Setter Z-koordinaten til 0 for fra-punktet

            foreach (var dict in punktData)
            {
                foreach (var entry in dict)
                {
                    Point3d entryPointXY = new Point3d(entry.Key.X, entry.Key.Y, 0); // Setter Z-koordinaten til 0 for entry-punktet
                    double dist = fromPointXY.DistanceTo(entryPointXY);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestPoint = entry.Key;  // Lagrer originalpunktet med Z-koordinat
                    }
                }
            }

            return nearestPoint;
        }

    }
}

    }
}
