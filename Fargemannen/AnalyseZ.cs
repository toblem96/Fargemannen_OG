using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Civil.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.Runtime;
using System.Windows.Controls;
using System.Diagnostics;
using Autodesk.Civil;



namespace Fargemannen
{
    internal class AnalyseZ
    {
        public static List<double> VerdierZ = new List<double>();
        public static List<Autodesk.AutoCAD.DatabaseServices.Polyline> MiniPlListZ = new List<Autodesk.AutoCAD.DatabaseServices.Polyline>();
        public static List<DBPoint> MidPointsZ = new List<DBPoint>(); // Liste for å lagre midtpunkter
        public static int gjennomsiktighetZ = 0;

        public static double minVerdiZ;
        public static double maxVerdiZ;
        public static double gjennomsnittVerdiZ;


        public static void Start()
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
                DelFirkantI1mX1m(pl, WinBergModell.ruterStrZ);
                BeregnVerdier();
            }
        }


        private static void DelFirkantI1mX1m(Autodesk.AutoCAD.DatabaseServices.Polyline pl, double ruteStr)
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
                    

                    CalculateElevationDifferences(acTrans, acCurDb, layerName);
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



        public static void CalculateElevationDifferences(Transaction acTrans, Database acCurDb, string layerName)
        {

            VerdierZ.Clear();

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

            foreach (DBPoint midPointEntity in MidPointsZ)
            {
                Point3d midPoint = midPointEntity.Position;

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
                    differanse =Math.Round( minAvstandTilTerreng - avstandTilBerg);
                    differanse = Math.Round(Math.Max(0, differanse));  // Setter differansen til 0 hvis den er negativ
                    VerdierZ.Add(differanse);
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



            string melding = $"Operasjonen var vellykket. Ruter med størrelse {ruteStr}m x {ruteStr}m er generert";

            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog(melding);
        }


        public static void BeregnVerdier()
        {
            minVerdiZ = VerdierZ.Min();
            maxVerdiZ = VerdierZ.Max();
            gjennomsnittVerdiZ = VerdierZ.Average();
        }

        public static void PlasserFirkanterIIntervallLayersOgFyllMedFargeZ()
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

                    int processedItems = 0;
                    try
                    {
                        foreach (var lengde in VerdierZ)
                        {
                            Autodesk.AutoCAD.DatabaseServices.Polyline pl = MiniPlListZ[processedItems];
                            foreach (var intervall in Intervall.intervallListeZ)
                            {
                                if (lengde >= intervall.Start && lengde <= intervall.Slutt)
                                {
                                    ProcessLayerAndHatch(lt, btr, acTrans, intervall, pl);
                                    break; // Exit the loop once the correct interval is processed
                                }
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

        private static void ProcessLayerAndHatch(LayerTable lt, BlockTableRecord btr, Transaction acTrans, Intervall intervall, Autodesk.AutoCAD.DatabaseServices.Polyline pl)
        {
            if (!lt.Has(intervall.LagNavn))
            {
                lt.UpgradeOpen();  // Oppgraderer LayerTable til skrivemodus
                LayerTableRecord ltr = new LayerTableRecord
                {
                    Name = intervall.LagNavn  // Setter navnet på det nye laget
                };
                ObjectId layerId = lt.Add(ltr);
                acTrans.AddNewlyCreatedDBObject(ltr, true);  // Legger til det nye laget i transaksjonen
            }

            // Henter det eksisterende eller nyopprettede laget for modifikasjon
            LayerTableRecord ltrExisting = (LayerTableRecord)acTrans.GetObject(lt[intervall.LagNavn], OpenMode.ForWrite);
            ltrExisting.Color = Autodesk.AutoCAD.Colors.Color.FromColor(intervall.Farge);
            ltrExisting.IsPlottable = true;  // Sørger for at laget er plottbart


            byte transparencyValue = (byte)(255 * (1 - Analyse.gjennomsiktighet / 100.0)); // Korrekt beregning for transparens
            ltrExisting.Transparency = new Transparency(transparencyValue);

            // Oppretter et nytt Hatch-objekt
            Hatch hatch = new Hatch();
            btr.AppendEntity(hatch);
            acTrans.AddNewlyCreatedDBObject(hatch, true);
            hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            hatch.Associative = true;
            hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection(new ObjectId[] { pl.ObjectId }));
            hatch.EvaluateHatch(true);
            hatch.Layer = intervall.LagNavn;  // Setter hatchets lag til intervallens lag


            DrawOrderTable dot = (DrawOrderTable)acTrans.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite);
            dot.MoveToBottom(new ObjectIdCollection(new ObjectId[] { hatch.ObjectId }));
        }



    }
}

















/*

private static void DelFirkantI1mX1m(Autodesk.AutoCAD.DatabaseServices.Polyline pl, double ruteStr)
{
    Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
    Database acCurDb = doc.Database;



    using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
    {
        using (doc.LockDocument())
        {
            string layerName = "UsynligLagZ";
            try
            {

                LayerTable lt = null;


                lt = (LayerTable)acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead);



                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen();
                    LayerTableRecord ltr = new LayerTableRecord
                    {
                        Name = layerName,
                        IsOff = false // Dette slår av laget, gjør objekter på dette laget usynlige
                    };

                    lt.Add(ltr);
                    acTrans.AddNewlyCreatedDBObject(ltr, true);
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Feil under opprettelse eller oppdatering av lag: " + e.Message);
                // Du kan vurdere å logge feilen til en loggfil eller database for videre analyse
                return; // Avslutter funksjonen tidlig hvis en feil oppstår
            }






            BlockTable bt = (BlockTable)acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
            BlockTableRecord btr = (BlockTableRecord)acTrans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            try
            {
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    Point2d pt = pl.GetPoint2dAt(i);
                    minX = Math.Min(minX, pt.X);
                    minY = Math.Min(minY, pt.Y);
                    maxX = Math.Max(maxX, pt.X);
                    maxY = Math.Max(maxY, pt.Y);
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Feil ved behandling av polyline punkter: " + e.Message);
                return;
            }



            try
            {
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

                try
                {
                    // Anta at funksjoner for å hente TinSurface og GridSurface basert på lag-navn eksisterer
                    TinSurface bergmodell = GetTinSurfaceFromLayerName("Bergmodell", acTrans, acCurDb);
                    //GridSurface terrengmodell = GetGridSurfaceFromLayerName("C-TOPO-GRID", acTrans, acCurDb);

                    List<GridSurface> surfaces = new List<GridSurface>();

                        // Få tilgang til lagtabellen
                        LayerTable layerTable = (LayerTable)acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead);




                        // Få tilgang til modellrommet
                        BlockTable blockTable = (BlockTable)acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord modelSpace = (BlockTableRecord)acTrans.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                        // Iterer gjennom alle objekter i modellrommet
                        foreach (ObjectId objectId in modelSpace)
                        {
                            Autodesk.Civil.DatabaseServices.Entity entity = acTrans.GetObject(objectId, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Entity;

                            // Sjekk om objektet er på det angitte laget og er en GridSurface
                            if (entity != null && entity.Layer == layerName && entity is GridSurface)
                            {
                                surfaces.Add((GridSurface)entity);
                            }
                        }



                    foreach (DBPoint midPointEntity in MidPointsZ)
                    {
                        Point3d midPoint = midPointEntity.Position;
                        double avstandTilBerg = 0,  avstandTilTerreng = 0, differanse;

                        double minAvstandTilTerreng = double.MaxValue;
                        foreach (var terrengmodell in surfaces)
                        {
                            avstandTilTerreng = terrengmodell.FindElevationAtXY(midPoint.X, midPoint.Y) - midPoint.Z;
                            if (avstandTilTerreng < minAvstandTilTerreng)
                            {
                                minAvstandTilTerreng = avstandTilTerreng; // Oppdaterer hvis en mindre avstand er funnet
                            }
                        }


                        avstandTilBerg = bergmodell.FindElevationAtXY(midPoint.X, midPoint.Y) - midPoint.Z;




                            differanse = Math.Round(Math.Abs(avstandTilTerreng - avstandTilBerg));


                        VerdierZ.Add(differanse);
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception e)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Feil under beregning av avstander: " + e.Message);
                    return;
                }







            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Feil under oppretting eller tillegg av polyline: " + e.Message);
                return;
            }

            acTrans.Commit();
            doc.Database.TransactionManager.QueueForGraphicsFlush();
            doc.Editor.Regen();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Firkanten ble delt inn i " + ruteStr + "m x " + ruteStr + "m seksjoner vellykket.");
        }
    }


}



public static void BeregnVerdier()
{

    minVerdiZ = Math.Round(VerdierZ.Min());
    maxVerdiZ = Math.Round(VerdierZ.Max());
    gjennomsnittVerdiZ= Math.Round(VerdierZ.Average());


}

public static void PlasserFirkanterIIntervallLayersOgFyllMedFargeZ()
{
    Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
    Database acCurDb = acDoc.Database;

    try
    {
        using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
        {
            using (acDoc.LockDocument())
            {
                LayerTable lt = acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead) as LayerTable;
                BlockTableRecord btr = acTrans.GetObject(acCurDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                if (VerdierZ.Count == 0 || MiniPlListZ.Count == 0)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Ingen verdier funnet i VerdierZ eller MiniPlListZ.");
                    return;
                }

                for (int i = 0; i < VerdierZ.Count; i++)
                {
                    double lengde = VerdierZ[i];
                    Autodesk.AutoCAD.DatabaseServices.Polyline pl = MiniPlListZ[i];

                    bool intervallFunnet = false;
                    foreach (var intervall in Intervall.intervallListeZ)
                    {
                        if (lengde >= intervall.Start && lengde <= intervall.Slutt)
                        {
                            intervallFunnet = true;

                            if (!lt.Has(intervall.LagNavn))
                            {
                                lt.UpgradeOpen();
                                LayerTableRecord ltr = new LayerTableRecord();
                                ltr.Name = intervall.LagNavn;
                                lt.Add(ltr);
                                acTrans.AddNewlyCreatedDBObject(ltr, true);
                            }

                            LayerTableRecord ltrExisting = acTrans.GetObject(lt[intervall.LagNavn], OpenMode.ForWrite) as LayerTableRecord;
                            Autodesk.AutoCAD.Colors.Color acColor = Autodesk.AutoCAD.Colors.Color.FromColor(intervall.Farge);
                            ltrExisting.Color = acColor;

                            Hatch hatch = new Hatch();
                            btr.AppendEntity(hatch);
                            acTrans.AddNewlyCreatedDBObject(hatch, true);
                            hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                            hatch.Associative = true;
                            hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection(new ObjectId[] { pl.ObjectId }));
                            hatch.EvaluateHatch(true);
                            hatch.Layer = intervall.LagNavn;

                            byte transparencyValue = (byte)((AnalyseZ.gjennomsiktighetZ / 100.0) * 255);
                            hatch.Transparency = new Transparency(transparencyValue);

                            DrawOrderTable dot = acTrans.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;
                            ObjectIdCollection objIds = new ObjectIdCollection(new ObjectId[] { hatch.ObjectId });
                            dot.MoveToBottom(objIds);
                            break;
                        }
                    }

                    if (!intervallFunnet)
                    {
                        Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog($"Ingen intervall funnet for verdien: {lengde}");
                    }
                }
            }

            acTrans.Commit();
        }
    }
    catch (System.Exception ex)
    {
        Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog($"En feil oppstod: {ex.Message}");
    }
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
*/