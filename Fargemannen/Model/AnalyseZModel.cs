using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fargemannen.ViewModel;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.AutoCAD.Customization;
namespace Fargemannen.Model
{
    internal class AnalyseZModel
    {
        public static List<double> VerdierZ = new List<double>();
        public static List<Autodesk.AutoCAD.DatabaseServices.Polyline> MiniPlListZ = new List<Autodesk.AutoCAD.DatabaseServices.Polyline>();
        public static List<DBPoint> MidPointsZ = new List<DBPoint>(); // Liste for å lagre midtpunkter
        public static int gjennomsiktighetZ = 0;

        public static double minVerdiZ;
        public static double maxVerdiZ;
        public static double gjennomsnittVerdiZ;

        public static void Start(List<Point3d> borPunkter, double ruteStr, string terrengModellLagNavn, string bergmodellLagNavn)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Ingen aktivt dokument funnet. Åpne eller opprett et nytt dokument før du starter.");
                return;
            }

            Editor ed = doc.Editor;
            // Be brukeren om å velge en polyline
            PromptEntityOptions peo = new PromptEntityOptions("\nVelg en firkant (Polyline): ");
            peo.SetRejectMessage("\nObjektet må være en Polyline.");
            peo.AddAllowedClass(typeof(Autodesk.AutoCAD.DatabaseServices.Polyline), true);
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
                return;

            Transaction tr = doc.Database.TransactionManager.StartTransaction();
            using (tr)
            {
                Autodesk.AutoCAD.DatabaseServices.DBObject obj = tr.GetObject(per.ObjectId, OpenMode.ForRead);
                Autodesk.AutoCAD.DatabaseServices.Polyline pl = obj as Autodesk.AutoCAD.DatabaseServices.Polyline;
                if (pl == null)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Valgt objekt er ikke en Polyline.");
                    return;
                }

                if (!pl.Closed || pl.NumberOfVertices != 4)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Polylinjen er ikke en lukket firkant med 4 hjørner.");
                    return;
                }

                // Hvis kriteriene er oppfylt, fortsett med å dele firkanten
                DelFirkantI1mX1m(pl,borPunkter,ruteStr, terrengModellLagNavn, bergmodellLagNavn);
                BeregnVerdier();
            }
        }


        private static void DelFirkantI1mX1m(Autodesk.AutoCAD.DatabaseServices.Polyline pl, List<Point3d> borPunkter, double ruteStr, string terrengModellLagNavn, string bergmodellLagNavn)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = doc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                using (doc.LockDocument())
                {
                    string layerName = "UsynligLagZ";
                    OpprettEllerOppdaterLayer(acTrans, acCurDb, layerName);




                    BlockTable bt = (BlockTable)acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)acTrans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    (double minX, double minY, double maxX, double maxY) = FinnGrenser(pl);

                    GenererRuterOgMidtpunkter(acTrans, btr, layerName, ruteStr, minX, minY, maxX, maxY);

                    VerdierZ.Clear();


                    CalculateElevationDifferences(acTrans, acCurDb, terrengModellLagNavn, bergmodellLagNavn);
                    acTrans.Commit();

                    doc.Editor.Regen();
                }
            }
        }
        private static void OpprettEllerOppdaterLayer(Transaction acTrans, Database acCurDb, string layerName)
        {
            LayerTable lt = (LayerTable)acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead);

            if (!lt.Has(layerName))
            {
                lt.UpgradeOpen();
                LayerTableRecord ltr = new LayerTableRecord
                {
                    Name = layerName,
                    IsOff = true
                };

                lt.Add(ltr);
                acTrans.AddNewlyCreatedDBObject(ltr, true);
            }
        }

        private static (double, double, double, double) FinnGrenser(Autodesk.AutoCAD.DatabaseServices.Polyline pl)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Point2d pt = pl.GetPoint2dAt(i);
                minX = Math.Min(minX, pt.X);
                minY = Math.Min(minY, pt.Y);
                maxX = Math.Max(maxX, pt.X);
                maxY = Math.Max(maxY, pt.Y);
            }

            return (minX, minY, maxX, maxY);
        }

        public static void CalculateElevationDifferences(Transaction acTrans, Database acCurDb, string terrengModellLagNavn, string bergmodellLagNavn)
        {
            VerdierZ.Clear();

            // Hent bergmodell og terrengmodeller
            TinSurface bergmodell = GetTinSurfaceFromLayerName(bergmodellLagNavn, acTrans, acCurDb);
            List<GridSurface> terrengmodeller = GetSurfacesFromLayer(acTrans, acCurDb, terrengModellLagNavn);

            // Hent grenser for bergmodellen
      
            List<Point2d> bergGrenser = ExtractBordersFromTinSurface(acTrans, bergmodell);

            // Hent grenser for hver terrengmodell
            List<List<Point2d>> terrengBorders = terrengmodeller.Select(surface => ExtractBorders(acTrans, surface)).ToList();

            foreach (DBPoint midPointEntity in MidPointsZ)
            {
                Point3d midPoint = midPointEntity.Position;
                var (isInside, terrengIndex) = IsPointInsideBothBorders(new Point2d(midPoint.X, midPoint.Y), terrengBorders, bergGrenser);

                if (!isInside)
                {
                    VerdierZ.Add(-999);
                }
                else
                {
                    // Hent den spesifikke terrengmodellen for det gjeldende punktet
                    GridSurface specificSurface = terrengmodeller[terrengIndex.Value];
                    double elevationDifference = CalculateElevationDifference(acTrans, bergmodell, specificSurface, midPoint);
                    VerdierZ.Add(elevationDifference);
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
        private static double CalculateElevationDifference(Transaction acTrans, TinSurface bergmodell, GridSurface terrengmodell, Point3d midPoint)
        {
            
            double avstandTilBerg = bergmodell.FindElevationAtXY(midPoint.X, midPoint.Y);
             double avstandTilTerreng = terrengmodell.FindElevationAtXY(midPoint.X, midPoint.Y);
          

          
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

        private static GridSurface GetGridSurfaceFromLayerName(string layerName, Transaction trans, Database db)
        {
            GridSurface gridSurface = null;

            // Hent LayerTableRecord for det spesifiserte laget
            LayerTableRecord layer = GetLayerByName(layerName, trans, db);
            if (layer == null) return null;

            // Søk gjennom database for å finne GridSurface objekter på det spesifiserte laget
            var bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
            var btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            foreach (ObjectId objId in btr)
            {
                var obj = trans.GetObject(objId, OpenMode.ForRead);
                var surface = obj as GridSurface;
                if (surface != null && surface.Layer == layerName)
                {
                    gridSurface = surface;
                    break; // Anta at det kun finnes én GridSurface per lag
                }
            }

            return gridSurface;
        }

        private static void GenererRuterOgMidtpunkter(Transaction acTrans, BlockTableRecord btr, string layerName, double ruteStr, double minX, double minY, double maxX, double maxY)
        {

            MiniPlListZ.Clear();
            MidPointsZ.Clear();


            for (double x = minX; x < maxX; x += ruteStr)
            {
                for (double y = minY; y < maxY; y += ruteStr)
                {
                    Autodesk.AutoCAD.DatabaseServices.Polyline miniPl = new Autodesk.AutoCAD.DatabaseServices.Polyline();
                    miniPl.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
                    miniPl.AddVertexAt(1, new Point2d(x + ruteStr, y), 0, 0, 0);
                    miniPl.AddVertexAt(2, new Point2d(x + ruteStr, y + ruteStr), 0, 0, 0);
                    miniPl.AddVertexAt(3, new Point2d(x, y + ruteStr), 0, 0, 0);
                    miniPl.Closed = true;

                    miniPl.Layer = layerName;
                    MiniPlListZ.Add(miniPl);
                    btr.AppendEntity(miniPl);
                    acTrans.AddNewlyCreatedDBObject(miniPl, true);


                    Point3d midPoint = new Point3d((x + x + ruteStr) / 2, (y + y + ruteStr) / 2, 0);

                    DBPoint midPointEntity = new DBPoint(midPoint);
                    midPointEntity.Layer = layerName;
                    MidPointsZ.Add(midPointEntity);
                    btr.AppendEntity(midPointEntity);
                    acTrans.AddNewlyCreatedDBObject(midPointEntity, true);
                }
            }



            string melding = $"Later rutenett på {ruteStr}m x {ruteStr}m er generert. Dette kan ta litt tid";

            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog(melding);
        }


        public static void BeregnVerdier()
        {
            minVerdiZ = VerdierZ.Min();
            maxVerdiZ = VerdierZ.Max();
            gjennomsnittVerdiZ = VerdierZ.Average();
        }

        public static void PlasserFirkanterIIntervallLayersOgFyllMedFargeZ(double Gjennomsiktighet, List<Fargemannen.ViewModel.IntervallZ> intervaller)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (acDoc == null)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Ingen aktivt dokument funnet. Åpne eller opprett et nytt dokument før du starter.");
                return;
            }

            Database acCurDb = acDoc.Database;
            ProgressWindow progressWindow = new ProgressWindow(VerdierZ.Count);  // Set up the progress window with the number of steps equal to the number of lengths

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                using (acDoc.LockDocument())
                {
                    LayerTable lt = (LayerTable)acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)acTrans.GetObject(acCurDb.CurrentSpaceId, OpenMode.ForWrite);
                    BlockTable bt = (BlockTable)acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);

                    ClearLayers(acTrans, lt, bt, intervaller);

                    int processedItems = 0;
                    try
                    {
                        foreach (var lengde in VerdierZ)
                        {
                            if (lengde != -999)
                            {

                            
                            Autodesk.AutoCAD.DatabaseServices.Polyline pl = MiniPlListZ[processedItems];
                            foreach (var intervall in intervaller)
                            {
                                if (lengde >= intervall.StartVerdi && lengde <= intervall.SluttVerdi)
                                {
                                    ProcessLayerAndHatch(lt, btr, acTrans, intervall, pl, Gjennomsiktighet);
                                    break; // Exit the loop once the correct interval is processed
                                }
                                }
                            }
                            else
                            {

                            }
                            processedItems++;
                            progressWindow.UpdateProgress(processedItems); // Update the progress bar each time an item is processed
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Error during processing: " + ex.Message);
                    }

                    acTrans.Commit();
                }
            }

            progressWindow.Complete();  // Close the progress bar window after completion
        }

        private static void ProcessLayerAndHatch(LayerTable lt, BlockTableRecord btr, Transaction acTrans, Fargemannen.ViewModel.IntervallZ intervall, Autodesk.AutoCAD.DatabaseServices.Polyline pl, double Gjennomsiktighet)
        {
            if (!lt.Has(intervall.Navn))
            {
                lt.UpgradeOpen();  // Oppgraderer LayerTable til skrivemodus
                LayerTableRecord ltr = new LayerTableRecord
                {
                    Name = intervall.Navn  // Setter navnet på det nye laget
                };
                ObjectId layerId = lt.Add(ltr);
                acTrans.AddNewlyCreatedDBObject(ltr, true);  // Legger til det nye laget i transaksjonen
            }

            // Henter det eksisterende eller nyopprettede laget for modifikasjon
            LayerTableRecord ltrExisting = (LayerTableRecord)acTrans.GetObject(lt[intervall.Navn], OpenMode.ForWrite);
            ltrExisting.Color = Autodesk.AutoCAD.Colors.Color.FromColor(ConvertHexToDrawingColor(intervall.Farge));
            ltrExisting.IsPlottable = true;  // Sørger for at laget er plottbart


            byte transparencyValue = (byte)(255 * (1 - Gjennomsiktighet / 100.0)); // Korrekt beregning for transparens
            ltrExisting.Transparency = new Transparency(transparencyValue);

            // Oppretter et nytt Hatch-objekt
            Hatch hatch = new Hatch();
            btr.AppendEntity(hatch);
            acTrans.AddNewlyCreatedDBObject(hatch, true);
            hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            hatch.Associative = true;
            hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection(new ObjectId[] { pl.ObjectId }));
            hatch.EvaluateHatch(true);
            hatch.Layer = intervall.Navn;  // Setter hatchets lag til intervallens lag


            DrawOrderTable dot = (DrawOrderTable)acTrans.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite);
            dot.MoveToBottom(new ObjectIdCollection(new ObjectId[] { hatch.ObjectId }));
        }

        private static void ClearLayers(Transaction acTrans, LayerTable lt, BlockTable btr, List<Fargemannen.ViewModel.IntervallZ> intervaller)
        {
            foreach (var intervall in intervaller)
            {
                if (lt.Has(intervall.Navn))
                {
                    // Henter det eksisterende laget for modifikasjon
                    LayerTableRecord ltr = (LayerTableRecord)acTrans.GetObject(lt[intervall.Navn], OpenMode.ForWrite);

                    // Finner og sletter alle objekter på dette laget
                    var allObjects = new ObjectIdCollection();
                    foreach (ObjectId btrId in btr)
                    {
                        BlockTableRecord block = (BlockTableRecord)acTrans.GetObject(btrId, OpenMode.ForRead);
                        foreach (ObjectId objId in block)
                        {
                            Autodesk.AutoCAD.DatabaseServices.Entity entity = (Autodesk.AutoCAD.DatabaseServices.Entity)acTrans.GetObject(objId, OpenMode.ForRead);
                            if (entity.Layer == intervall.Navn)
                            {
                                allObjects.Add(objId);
                            }
                        }
                    }

                    foreach (ObjectId objId in allObjects)
                    {
                        Autodesk.AutoCAD.DatabaseServices.DBObject obj = acTrans.GetObject(objId, OpenMode.ForWrite, true);
                        obj.Erase();
                    }
                }
            }
        }
        private static System.Drawing.Color ConvertHexToDrawingColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor))
                return System.Drawing.Color.Transparent;  // Returnerer transparent hvis ingen farge er spesifisert

            try
            {
                return System.Drawing.ColorTranslator.FromHtml(hexColor);
            }
            catch
            {
                return System.Drawing.Color.Black;  // Returnerer svart som fallback
            }
        }





        public static void GenererMeshFraPunkter(List<Point3d> punkterMesh, string BergmodellNavn, string BergmodellLagNavn)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;

            using (DocumentLock docLock = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    LayerTableRecord layerRecord;
                    if (!layerTable.Has(BergmodellLagNavn))
                    {
                        layerTable.UpgradeOpen();
                        layerRecord = new LayerTableRecord
                        {
                            Name = BergmodellLagNavn
                        };

                        layerTable.Add(layerRecord);
                        tr.AddNewlyCreatedDBObject(layerRecord, true);
                    }
                    else
                    {
                        layerRecord = tr.GetObject(layerTable[BergmodellLagNavn], OpenMode.ForWrite) as LayerTableRecord;
                    }

                    ObjectId tinSurfaceId = TinSurface.Create(db, BergmodellNavn);
                    TinSurface tinSurface = tr.GetObject(tinSurfaceId, OpenMode.ForWrite) as TinSurface;

                    foreach (Point3d punkt in punkterMesh)
                    {
                        tinSurface.AddVertex(punkt);
                    }

                    tinSurface.Rebuild();
                    tinSurface.LayerId = layerRecord.ObjectId;

                    // Set the style of the surface to display triangles and contours
                    ObjectId styleId = GetOrCreateSurfaceStyleWithContoursAndTriangles(db, tr);
                    if (styleId != ObjectId.Null)
                        tinSurface.StyleId = styleId;

                    tr.Commit();
                }
            }
        }

        private static ObjectId GetOrCreateSurfaceStyleWithContoursAndTriangles(Database db, Transaction tr)
        {
            string styleName = "ContoursAndTrianglesStyle";
            SurfaceStyleCollection styleColl = CivilApplication.ActiveDocument.Styles.SurfaceStyles;

            if (!styleColl.Contains(styleName))
            {
                ObjectId styleId = styleColl.Add(styleName);
                using (SurfaceStyle style = tr.GetObject(styleId, OpenMode.ForWrite) as SurfaceStyle)
                {
                    // Setup display styles as needed
                    style.GetDisplayStylePlan(SurfaceDisplayStyleType.Triangles).Visible = true;
                    //style.GetDisplayStylePlan(SurfaceDisplayStyleType.MajorContour).Visible = true;
                    style.GetDisplayStyleModel(SurfaceDisplayStyleType.Triangles).Visible = true;
                    //style.GetDisplayStyleModel(SurfaceDisplayStyleType.MajorContour).Visible = true;
                }
                return styleId;
            }
            else
            {
                return styleColl[styleName];
            }
        }



    }
}

