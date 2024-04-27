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


namespace Fargemannen.Model
{
    internal class AnalyseXYModel
    {
        public static List<double> lengdeVerdier = new List<double>();
        public static List<Autodesk.AutoCAD.DatabaseServices.Polyline> MiniPlList = new List<Autodesk.AutoCAD.DatabaseServices.Polyline>();
        public static List<DBPoint> MidPoints = new List<DBPoint>(); // Liste for å lagre midtpunkter
        public static int gjennomsiktighet = 0;

        public static double minVerdiXY;
        public static double maxVerdiXY;
        public static double gjennomsnittVerdiXY;

        public static void Start(List<Point3d> borPunkter, double ruteStr)
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
                DelFirkantI1mX1m(pl, ruteStr, borPunkter);
                BeregnVerdier();
            }
        }

        private static void DelFirkantI1mX1m(Autodesk.AutoCAD.DatabaseServices.Polyline pl, double ruteStr, List<Point3d> borPunkter)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = doc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                using (doc.LockDocument())
                {
                    string layerName = "UsynligLag";
                    OpprettEllerOppdaterLayer(acTrans, acCurDb, layerName);




                    BlockTable bt = (BlockTable)acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)acTrans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    (double minX, double minY, double maxX, double maxY) = FinnGrenser(pl);

                    GenererRuterOgMidtpunkter(acTrans, btr, layerName, ruteStr, minX, minY, maxX, maxY);

                    lengdeVerdier.Clear();

                    BeregnOgLagreNærmesteAvstander(MidPoints, lengdeVerdier, borPunkter);
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
        private static void BeregnOgLagreNærmesteAvstander(List<DBPoint> MidPoints, List<double> lengdeVerdier, List<Point3d> borPunkter)
        {

            foreach (DBPoint dbPoint in MidPoints)
            {
                Point3d currentMidPoint = dbPoint.Position;
                double nearestDistance = double.MaxValue;

                foreach (Point3d analysisPoint in borPunkter)
                {
                    double distance = currentMidPoint.DistanceTo(analysisPoint);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                    }
                }

                lengdeVerdier.Add(Math.Round(nearestDistance, 1));
            }
        }

        private static void GenererRuterOgMidtpunkter(Transaction acTrans, BlockTableRecord btr, string layerName, double ruteStr, double minX, double minY, double maxX, double maxY)
        {

            MiniPlList.Clear();
            MidPoints.Clear();


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
                    MiniPlList.Add(miniPl);
                    btr.AppendEntity(miniPl);
                    acTrans.AddNewlyCreatedDBObject(miniPl, true);


                    Point3d midPoint = new Point3d((x + x + ruteStr) / 2, (y + y + ruteStr) / 2, 0);

                    DBPoint midPointEntity = new DBPoint(midPoint);
                    midPointEntity.Layer = layerName;
                    MidPoints.Add(midPointEntity);
                    btr.AppendEntity(midPointEntity);
                    acTrans.AddNewlyCreatedDBObject(midPointEntity, true);
                }
            }



            string melding = $"Operasjonen var vellykket. Ruter med størrelse {ruteStr}m x {ruteStr}m er generert";

            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog(melding);
        }


        public static void BeregnVerdier()
        {

            minVerdiXY = Math.Round(lengdeVerdier.Min());
            maxVerdiXY = Math.Round(lengdeVerdier.Max());
            gjennomsnittVerdiXY = Math.Round(lengdeVerdier.Average());


        }

        public static void PlasserFirkanterIIntervallLayersOgFyllMedFarge(double gjennomsiktighet, List<Fargemannen.ViewModel.Intervall> intervaller)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (acDoc == null)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Ingen aktivt dokument funnet. Åpne eller opprett et nytt dokument før du starter.");
                return;
            }

            Database acCurDb = acDoc.Database;
            ProgressWindow progressWindow = new ProgressWindow(lengdeVerdier.Count);  // Set up the progress window with the number of steps equal to the number of lengths

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
                        foreach (var lengde in lengdeVerdier)
                        {
                            Autodesk.AutoCAD.DatabaseServices.Polyline pl = MiniPlList[processedItems];
                            foreach (var intervall in intervaller)
                            {
                                if (lengde >= intervall.StartVerdi && lengde <= intervall.SluttVerdi)
                                {
                                    ProcessLayerAndHatch(lt, btr, acTrans, intervall, pl, gjennomsiktighet);
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

        private static void ProcessLayerAndHatch(LayerTable lt, BlockTableRecord btr, Transaction acTrans, Fargemannen.ViewModel.Intervall intervall, Autodesk.AutoCAD.DatabaseServices.Polyline pl, double gjennomsiktighet)
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


            byte transparencyValue = (byte)(255 * (1 - gjennomsiktighet / 100.0)); // Korrekt beregning for transparens
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



        }
        private static void ClearLayers(Transaction acTrans, LayerTable lt, BlockTable btr, List<Fargemannen.ViewModel.Intervall> intervaller)
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
                            Entity entity = (Entity)acTrans.GetObject(objId, OpenMode.ForRead);
                            if (entity.Layer == intervall.Navn)
                            {
                                allObjects.Add(objId);
                            }
                        }
                    }

                    foreach (ObjectId objId in allObjects)
                    {
                        DBObject obj = acTrans.GetObject(objId, OpenMode.ForWrite, true);
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


    }
}
