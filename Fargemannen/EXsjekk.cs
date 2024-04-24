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


namespace Fargemannen
{
    internal class EXsjekk
    {

        public static List<PunktInfo> symbolSjekk = new List<PunktInfo>();



        public static void kjørTestRapport()
        {
            List<double> TotalLengde = new List<double>();
            List<double> bergmodellOnPoint = new List<double>();
            List<double> terrrengmodellOnPoint = new List<double>();
            CalculateElevationDifferences(TotalLengde, bergmodellOnPoint, terrrengmodellOnPoint);
            GenererExcel(terrrengmodellOnPoint, bergmodellOnPoint, TotalLengde);


        }

        public static void GenererExcel(List<double> terrengmodellOnPoint, List<double> bergmodellOnPoint, List<double> totalLengde)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Rapport");

                // Sette celleverdier og gjøre teksten fet for overskriftene
                worksheet.Cell("A1").Value = "PunktID";
                worksheet.Cell("B1").Value = "Terrengkvote.SOSI";
                worksheet.Cell("C1").Value = "Terrengkvote.C3D";
                worksheet.Cell("D1").Value = "Bergkvote.SOSI";
                worksheet.Cell("E1").Value = "Bergkvote.C3D";
                worksheet.Cell("F1").Value = "Borelengde.SOSI";
                worksheet.Cell("G1").Value = "Borelengde.C3D";

                // Appliserer fet skrift til alle overskriftene
                worksheet.Range("A1:G1").Style.Font.SetBold(true);

                int row = 2; // Starter fra rad 2 pga. overskrifter

                for (int i = 0; i < symbolSjekk.Count; i++)
                {
                    var punktInfo = symbolSjekk[i];

                    worksheet.Cell(row, 1).Value = punktInfo.PunktID;
                    worksheet.Cell(row, 2).Value = punktInfo.Terrengkvote;
                    worksheet.Cell(row, 3).Value = terrengmodellOnPoint[i];
                    worksheet.Cell(row, 4).Value = punktInfo.Punkt.Z;
                    worksheet.Cell(row, 5).Value = bergmodellOnPoint[i];
                    worksheet.Cell(row, 6).Value = punktInfo.BorLøs + punktInfo.BoreFjell;
                    worksheet.Cell(row, 7).Value = totalLengde[i];

                    // Alternativ fargelegging av rader
                    if (i % 2 == 0) // Lysegrå for partallsrader
                        worksheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.LightGray;
                    else // Mørkegrå for oddetallsrader
                        worksheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.Gray;

                    // Sjekker differansen og markerer raden rødt om nødvendig
                    if (Math.Abs(punktInfo.Terrengkvote - terrengmodellOnPoint[i]) > 3 ||
                        Math.Abs(punktInfo.Punkt.Z - bergmodellOnPoint[i]) > 3 ||
                        Math.Abs((punktInfo.BorLøs + punktInfo.BoreFjell) - totalLengde[i]) > 3)
                    {
                        worksheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.Red;
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
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = System.IO.Path.Combine(desktopPath, "Rapport.xlsx");
                workbook.SaveAs(filePath);
            }
        }



        public static void CalculateElevationDifferences(List<double> TotalLengde, List<double> bergmodellOnPoint, List<double> terrrengmodellOnPoint)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = doc.Database;

    


            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            using (doc.LockDocument())
            {



                TinSurface bergmodell = GetTinSurfaceFromLayerName("Bergmodell", acTrans, acCurDb);
                List<GridSurface> surfaces = new List<GridSurface>();
                List<List<Point2d>> surfaceBorders = new List<List<Point2d>>(); // Liste for å holde grensene som punktlister

                BlockTable blockTable = (BlockTable)acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)acTrans.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId objectId in modelSpace)
                {
                    Autodesk.Civil.DatabaseServices.Entity entity = acTrans.GetObject(objectId, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Entity;
                    if (entity != null && entity.Layer == "C-TOPO-GRID" && entity is GridSurface gridSurface)
                    {
                        surfaces.Add(gridSurface);
                        ObjectIdCollection borderIds = gridSurface.ExtractBorder(SurfaceExtractionSettingsType.Plan);
                        List<Point2d> borderPoints = new List<Point2d>();

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
                        if (borderPoints.Count > 0)
                            surfaceBorders.Add(borderPoints); // Legger til punktlisten til listen over grenser
                    }
                }


                foreach (PunktInfo point in symbolSjekk)
                {
                    Point3d midPoint = new Point3d(point.Punkt.X, point.Punkt.Y, 0);

                    double minAvstandTilTerreng = double.MaxValue;
                    double avstandTilBerg = bergmodell.FindElevationAtXY(midPoint.X, midPoint.Y);
                    double differanse;

                    for (int i = 0; i < surfaces.Count; i++)
                    {
                        if (IsPointInsidePolygon(surfaceBorders[i], new Point2d(midPoint.X, midPoint.Y)))
                        {
                            double avstandTilTerreng = surfaces[i].FindElevationAtXY(midPoint.X, midPoint.Y);
                            if (avstandTilTerreng < minAvstandTilTerreng)
                                minAvstandTilTerreng = avstandTilTerreng;
                        }
                    }

                    if (minAvstandTilTerreng != double.MaxValue)  // Sjekker om vi fant noen gyldig terrengmodell for dette punktet
                    {
                        differanse = Math.Round(minAvstandTilTerreng - avstandTilBerg);
                        TotalLengde.Add(differanse);
                        bergmodellOnPoint.Add(avstandTilBerg);
                        terrrengmodellOnPoint.Add(Math.Round(minAvstandTilTerreng, 2));
                    }
                }
            }
      
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
