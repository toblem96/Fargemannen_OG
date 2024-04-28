using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using System.Runtime.CompilerServices;
using DocumentFormat.OpenXml.Spreadsheet;
using ClosedXML.Excel;
using Autodesk.AutoCAD.Internal.Calculator;
using Autodesk.AutoCAD.EditorInput;
namespace Fargemannen.Model
{
    internal class EXCEL
    {
        public  void kjørTestRapport(List<PunktInfo> PunktSymboler, string LagringsPunkt, int diff, string rapportNavn, string layerTerreng, string layerBergmodell)
        {
            //get points

            List<double> TotalLengde = new List<double>();
            List<double> bergmodellOnPoint = new List<double>();
            List<double> terrrengmodellOnPoint = new List<double>();
            List<PunktInfo> GodkjentPunkt = new List<PunktInfo>();
            CalculateElevationDifferences(TotalLengde, bergmodellOnPoint, terrrengmodellOnPoint, PunktSymboler, layerTerreng, layerBergmodell, GodkjentPunkt);
            GenererExcel(terrrengmodellOnPoint, bergmodellOnPoint, TotalLengde, GodkjentPunkt, LagringsPunkt, diff, rapportNavn);


        }

        public  void GenererExcel(List<double> terrengmodellOnPoint, List<double> bergmodellOnPoint, List<double> totalLengde, List<PunktInfo> PunktSymboler, string LagringsPunkt, int diff, string rapportNavn)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = doc.Database;
            Editor ed = doc.Editor;

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Rapport");

                // Sette celleverdier og gjøre teksten fet for overskriftene
                worksheet.Cell("A1").Value = "PunktID";
                worksheet.Cell("B1").Value = "Terrengkvote.SOSI";
                worksheet.Cell("C1").Value = "Terrengkvote.C3D";
                worksheet.Cell("D1").Value = "Bergkvote.SOSI";
                worksheet.Cell("E1").Value = "Bergkvote.C3D";
             

                // Appliserer fet skrift til alle overskriftene
                worksheet.Range("A1:G1").Style.Font.SetBold(true);

                int row = 2; // Starter fra rad 2 pga. overskrifter

                for (int i = 0; i < PunktSymboler.Count; i++)
                {
                    var punktInfo = PunktSymboler[i];

                    worksheet.Cell(row, 1).Value = punktInfo.MinPunktID;
                    worksheet.Cell(row, 2).Value = punktInfo.Terrengkvote;
                    worksheet.Cell(row, 3).Value = terrengmodellOnPoint[i];
                    worksheet.Cell(row, 4).Value = punktInfo.Punkt.Z;
                    worksheet.Cell(row, 5).Value = bergmodellOnPoint[i];
                 

                    // Alternativ fargelegging av rader
                    if (i % 2 == 0) // Lysegrå for partallsrader
                        worksheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.LightGray;
                    else // Mørkegrå for oddetallsrader
                        worksheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.Gray;

                    // Sjekker differansen og markerer raden rødt om nødvendig
                    if (Math.Abs(punktInfo.Terrengkvote - terrengmodellOnPoint[i]) > diff ||
                        Math.Abs(punktInfo.Punkt.Z - bergmodellOnPoint[i]) > diff)
                        
                    {
                        worksheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.Red;
                    }

                    row++;
                }

                // Legger til rutenett på alle fylte celler
                var dataRange = worksheet.Range(1, 1, row - 1, 7);
                dataRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                dataRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

                // Justere kolonnebredder
                worksheet.Columns().AdjustToContents();

                // Lagre filen på skrivebordet
              


                string filExtension = ".xlsx";
                string filePath = System.IO.Path.Combine(LagringsPunkt, rapportNavn + filExtension);

                ed.WriteMessage(filePath);
                workbook.SaveAs(filePath);
            }
        }

        public static void CalculateElevationDifferences(List<double> TotalLengde, List<double> bergmodellOnPoint, List<double> terrrengmodellOnPoint, List<PunktInfo> PunktSymboler, string terrengModellLagNavn, string bergmodellLagNavn, List<PunktInfo> GodkjentPunk)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = doc.Database;
            Editor ed = doc.Editor;


            bergmodellOnPoint.Clear();
            terrrengmodellOnPoint.Clear();
            GodkjentPunk.Clear();

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            using (doc.LockDocument())
            {

                // Hent bergmodell og terrengmodeller
                TinSurface bergmodell = GetTinSurfaceFromLayerName(bergmodellLagNavn, acTrans, acCurDb);
                List<GridSurface> terrengmodeller = GetSurfacesFromLayer(acTrans, acCurDb, terrengModellLagNavn);

                // Hent grenser for bergmodellen

                List<Point2d> bergGrenser = ExtractBordersFromTinSurface(acTrans, bergmodell);

                // Hent grenser for hver terrengmodell
                List<List<Point2d>> terrengBorders = terrengmodeller.Select(surface => ExtractBorders(acTrans, surface)).ToList();

                foreach (var point in PunktSymboler)
                {
                    Point3d midPoint = point.Punkt;
                    var (isInside, terrengIndex) = IsPointInsideBothBorders(new Point2d(midPoint.X, midPoint.Y), terrengBorders, bergGrenser);

                    if (!isInside)
                    {
                        continue;
                    }
                    else
                    {
                        // Hent den spesifikke terrengmodellen for det gjeldende punktet
                        GridSurface specificSurface = terrengmodeller[terrengIndex.Value];
                        double elevationDifference = CalculateElevationDifference(acTrans, bergmodell, specificSurface, midPoint, bergmodellOnPoint, terrrengmodellOnPoint);
                        TotalLengde.Add(Math.Round(elevationDifference, 1));
                        GodkjentPunk.Add(point);
                    }
                }
            }
        }
        private static List<GridSurface> GetSurfacesFromLayer(Transaction acTrans, Database acCurDb, string terrengModellLagNavn)
        {
            List<GridSurface> surfaces = new List<GridSurface>();
            BlockTable blockTable = (BlockTable)acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
            BlockTableRecord modelSpace = (BlockTableRecord)acTrans.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            foreach (ObjectId objectId in modelSpace)
            {
                Autodesk.Civil.DatabaseServices.Entity entity = acTrans.GetObject(objectId, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Entity;
                if (entity != null && entity.Layer == terrengModellLagNavn && entity is GridSurface gridSurface)
                {
                    surfaces.Add(gridSurface);
                }

            }
            return surfaces;
        }
        private static (bool, int?) IsPointInsideBothBorders(Point2d point, List<List<Point2d>> terrengBorders, List<Point2d> bergBorder)
        {
            int? insideTerrengIndex = null;
            for (int i = 0; i < terrengBorders.Count; i++)
            {
                if (IsPointInsidePolygon(terrengBorders[i], point))
                {
                    insideTerrengIndex = i;
                    break;
                }
            }

            bool insideBerg = IsPointInsidePolygon(bergBorder, point);
            return (insideBerg && insideTerrengIndex != null, insideTerrengIndex);
        }
        private static double CalculateElevationDifference(Transaction acTrans, TinSurface bergmodell, GridSurface terrengmodell, Point3d midPoint, List<double> bergmodellOnPoint, List<double> terrrengmodellOnPoint)
        {

            double avstandTilBerg = bergmodell.FindElevationAtXY(midPoint.X, midPoint.Y);
            double avstandTilTerreng = terrengmodell.FindElevationAtXY(midPoint.X, midPoint.Y);
            bergmodellOnPoint.Add(Math.Round(avstandTilBerg, 1));
            terrrengmodellOnPoint.Add(Math.Round(avstandTilTerreng, 1));


            double differanse = Math.Round(avstandTilTerreng - avstandTilBerg);
            return Math.Round(Math.Max(0, differanse)); // Returner differansen, men minimum 0

        }

        private static List<Point2d> ExtractBorders(Transaction acTrans, GridSurface surface)
        {
            List<Point2d> borderPoints = new List<Point2d>();
            ObjectIdCollection borderIds = surface.ExtractBorder(SurfaceExtractionSettingsType.Plan);
            foreach (ObjectId borderId in borderIds)
            {
                Polyline3d polyline3d = acTrans.GetObject(borderId, OpenMode.ForRead) as Polyline3d;
                if (polyline3d != null)
                {
                    foreach (ObjectId vertexId in polyline3d)
                    {
                        PolylineVertex3d vertex = acTrans.GetObject(vertexId, OpenMode.ForRead) as PolylineVertex3d;
                        borderPoints.Add(new Point2d(vertex.Position.X, vertex.Position.Y));
                    }
                }
            }
            return borderPoints;
        }
        private static List<Point2d> ExtractBordersFromTinSurface(Transaction acTrans, TinSurface surface)
        {
            List<Point2d> borderPoints = new List<Point2d>();
            if (surface == null)
                return borderPoints; // Returner tom liste hvis overflaten ikke er gyldig

            // Ekstraher grensene fra TinSurface
            ObjectIdCollection borderIds = surface.ExtractBorder(SurfaceExtractionSettingsType.Plan);
            Console.WriteLine("# of surface borders: " + borderIds.Count);

            foreach (ObjectId borderId in borderIds)
            {
                Polyline3d border = acTrans.GetObject(borderId, OpenMode.ForRead) as Polyline3d;
                if (border != null)
                {
                    Console.WriteLine("Surface border vertices:");
                    foreach (ObjectId vertexId in border)
                    {
                        PolylineVertex3d vertex = acTrans.GetObject(vertexId, OpenMode.ForRead) as PolylineVertex3d;
                        if (vertex != null)
                        {
                            borderPoints.Add(new Point2d(vertex.Position.X, vertex.Position.Y));
                            Console.WriteLine(String.Format("  - Border vertex at: {0}", vertex.Position.ToString()));
                        }
                    }
                }
            }

            return borderPoints;
        }


        public static bool IsPointInsidePolygon(List<Point2d> polygon, Point2d testPoint)
        {
            bool result = false;
            int j = polygon.Count - 1;
            for (int i = 0; i < polygon.Count; i++)
            {
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y || polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }



        private static TinSurface GetTinSurfaceFromLayerName(string layerName, Transaction trans, Database db)
        {
            TinSurface tinSurface = null;

            // Hent LayerTableRecord for det spesifiserte laget
            LayerTableRecord layer = GetLayerByName(layerName, trans, db);
            if (layer == null) return null;

            // Søk gjennom database for å finne TinSurface objekter på det spesifiserte laget
            var bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
            var btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            foreach (ObjectId objId in btr)
            {
                var obj = trans.GetObject(objId, OpenMode.ForRead);
                var surface = obj as TinSurface;
                if (surface != null && surface.Layer == layerName)
                {
                    tinSurface = surface;
                    break; // Anta at det kun finnes én TinSurface per lag
                }
            }

            return tinSurface;
        }


        private static LayerTableRecord GetLayerByName(string layerName, Transaction trans, Database db)
        {
            LayerTable lt = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(layerName))
            {
                ObjectId layerId = lt[layerName];
                return (LayerTableRecord)trans.GetObject(layerId, OpenMode.ForRead);
            }
            return null;
        }
    }
}
