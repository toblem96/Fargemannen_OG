using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using System.Windows.Controls;
using Autodesk.Civil.ApplicationServices;
using System.Diagnostics;
using Autodesk.AutoCAD.Colors;
using Autodesk.Aec.Geometry;
using System.Web;
using Autodesk.Civil;
using Autodesk.AutoCAD.Customization;

namespace Fargemannen.Model
{
    internal class DukXYModel
    {
        public static List<double> VerdierXY = new List<double>();
        public static List<double> VerdierZ = new List<double>();
        public  void KjørDukPåXY(List<PunktInfo> PunkterForAnalyse, string layerBergmodell, string layerTerrengmodel, string analyseType)
        {


            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = doc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                using (doc.LockDocument())
                {
                    TinSurface bergmodell = GetMeshData.GetTinSurfaceFromLayerName(layerBergmodell, acTrans, acCurDb);

                    // Sjekker om bergmodell ikke er null
                    if (bergmodell != null)
                    {
                        // Hente antall vertices
                        int antallVertices = bergmodell.Vertices.Count;

                        // Skrive ut antallet vertices til kommandolinjen
                        doc.Editor.WriteMessage($"\nAntall vertices i bergmodellen: {antallVertices}");

                        int antallFaces = bergmodell.Triangles.Count;
                        doc.Editor.WriteMessage($"\nAntall faces i bergmodellen: {antallFaces}");

                    }

                    acTrans.Commit();
                }
            }
            RaffinerTIN();


            ProgressWindow progressWindow = new ProgressWindow(4);
            progressWindow.Show();

            for (int i = 0; i < 4; i++)
            {
                RaffinerTIN_30(progressWindow, i + 1);  // Kaller RaffinerTIN_30 funksjonen med progressWindow og nåværende iterasjon
                System.Threading.Thread.Sleep(1000); // Venter ett sekund mellom hver kjøring for å unngå å overbelaste med forespørsler
            }

            progressWindow.Complete();



            List<Point3d> midPoints = ProjectTinTrianglesToXY(layerBergmodell);




            List<Point3d> symbolPoints = SymbolPro(PunkterForAnalyse);

            foreach (Point3d leng in symbolPoints)
            {
                doc.Editor.WriteMessage($"\n{leng}");
            }


            if (analyseType == "XY")
            {

                List<double> closestDistances = CalculateClosestDistances(midPoints, symbolPoints);
                VerdierXY = closestDistances;

            }
            else
            {

                Document docc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Database acCurDbc = doc.Database;

                using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                {
                    using (doc.LockDocument())
                    {
                        CalculateElevationDifferences(acTrans, acCurDbc, layerTerrengmodel, layerBergmodell, midPoints );


                        acTrans.Commit();


                    }
                    
                }
            }







            //ColorizeTinSurfaceTriangles(closestDistances, layerBergmodell, interaller);

            doc.Editor.WriteMessage($"Koden kjørte hele veien");
        }

        public void FargeMesh(List<double> closestDistances, string layerBergmodell, List<ViewModel.Intervall> intervaller)
        {
            ColorizeTinSurfaceTriangles(closestDistances, layerBergmodell, intervaller);
        }

        public static void ColorizeTinSurfaceTriangles(List<double> closestDistances, string layerBergmodell, List<ViewModel.Intervall> intervaller)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            if (acDoc == null)
            {
                Application.ShowAlertDialog("Ingen aktivt dokument funnet. Åpne eller opprett et nytt dokument før du starter.");
                return;
            }

            Database acCurDb = acDoc.Database;
            ProgressWindow progressWindow = new ProgressWindow(closestDistances.Count);

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                using (acDoc.LockDocument())
                {
                    TinSurface bergmodell = GetTinSurfaceFromLayerName(layerBergmodell, acTrans, acCurDb);
                    List<TinSurfaceTriangle> triangleList = bergmodell.GetTriangles(false).ToList();

                    LayerTable lt = (LayerTable)acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)acTrans.GetObject(acCurDb.CurrentSpaceId, OpenMode.ForWrite);

                    int index = 0;

                    foreach (var lengde in closestDistances)
                    {
                        TinSurfaceTriangle triangle = triangleList[index];


                        foreach (var intervall in intervaller)
                        {
                            if (lengde >= intervall.StartVerdi && lengde <= intervall.SluttVerdi)
                            {

                                ProcessLayerAndHatch(lt, btr, acTrans, intervall, triangle);
                                break; // Exit the loop once the correct interval is processed
                            }
                        }
                        index++;
                    }

                    acTrans.Commit();
                }
            }

            progressWindow.Complete();  // Lukker fremdriftsindikatoren etter ferdigstillelse
        }

        private static void ProcessLayerAndHatch(LayerTable lt, BlockTableRecord btr, Transaction acTrans, ViewModel.Intervall intervall, TinSurfaceTriangle triangle)
        {
            ObjectId layerId;

            // Sjekker om laget allerede eksisterer i LayerTable
            if (!lt.Has(intervall.Navn))
            {
                lt.UpgradeOpen();  // Oppgraderer LayerTable til skrivemodus
                LayerTableRecord ltr = new LayerTableRecord
                {
                    Name = intervall.Navn  // Setter navnet på det nye laget
                };
                layerId = lt.Add(ltr);
                acTrans.AddNewlyCreatedDBObject(ltr, true);  // Legger til det nye laget i transaksjonen
            }
            else
            {
                layerId = lt[intervall.Navn];  // Henter ObjectId for det eksisterende laget
            }

            LayerTableRecord ltrExisting = (LayerTableRecord)acTrans.GetObject(layerId, OpenMode.ForWrite);
            ltrExisting.Color = Autodesk.AutoCAD.Colors.Color.FromColor(ConvertHexToDrawingColor(intervall.Farge));
            ltrExisting.IsPlottable = true;  // Sørger for at laget er plottbart

            // Oppretter en Polyline3d som definerer omkretsen av trianglet
            Polyline3d pl = new Polyline3d(Poly3dType.SimplePoly, new Point3dCollection
    {
        triangle.Vertex1.Location,
        triangle.Vertex2.Location,
        triangle.Vertex3.Location,
        triangle.Vertex1.Location  // Lukker polygonet ved å gjenta første punktet
    }, true);

            btr.AppendEntity(pl);
            acTrans.AddNewlyCreatedDBObject(pl, true);
            pl.LayerId = layerId;  // Setter Polyline til å være på det spesifikke laget

            /*
            // Oppretter et nytt Hatch-objekt
            Hatch hatch = new Hatch();
            btr.AppendEntity(hatch);
            acTrans.AddNewlyCreatedDBObject(hatch, true);
            hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            hatch.Associative = true;  // Gjør hatchingen assosiativ til boundary (Polyline3d)
            hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection(new ObjectId[] { pl.ObjectId }));
            hatch.EvaluateHatch(true);  // Beregner og genererer hatching basert på definerte loops
            hatch.Layer = ltrExisting.Name;  // Setter hatchets lag til intervallens lag
            hatch.Color = Autodesk.AutoCAD.Colors.Color.FromColor(intervall.Farge); // Setter farge på hatch (valgfritt hvis lagfarge er tilstrekkelig)
            */

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



        public static List<double> CalculateClosestDistances(List<Point3d> midPoints, List<Point3d> symbolPoints)
        {
            var distances = new List<double>();


            foreach (var midPoint in midPoints)
            {
                var closestDistance = symbolPoints
                                      .Select(symbolPoint => midPoint.DistanceTo(symbolPoint))  // Beregn avstanden fra midPoint til hver symbolPoint
                                      .Min();  // Finn den minste avstanden

                distances.Add(closestDistance);
            }

            return distances;
        }

        private static List<Point3d> SymbolPro(List<PunktInfo> PunkterForAnalyse)
        {
            List<Point3d> symbolPoints = new List<Point3d>();

            foreach (PunktInfo punkt in PunkterForAnalyse)
            {
                Point3d mmpunkt = new Point3d(punkt.Punkt.X, punkt.Punkt.Y, 0);
                symbolPoints.Add(mmpunkt);



            }
            return symbolPoints;

        }


        private static void RaffinerTIN()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = doc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                using (doc.LockDocument())
                {
                    TinSurface tinSurface = GetTinSurfaceFromLayerName("Bergmodell", acTrans, acCurDb);

                    var newPoints = new Point3dCollection();


                    var allTriangles = new List<TinSurfaceTriangle>(tinSurface.Triangles);
                    foreach (var tri in allTriangles)
                    {

                        // Få koordinater for hjørnene i trekanten
                        Point3d p1 = tri.Vertex1.Location;
                        Point3d p2 = tri.Vertex2.Location;
                        Point3d p3 = tri.Vertex3.Location;

                        // Beregn midtpunktene på hver kant
                        Point3d midP1P2 = new Point3d((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, (p1.Z + p2.Z) / 2);
                        Point3d midP2P3 = new Point3d((p2.X + p3.X) / 2, (p2.Y + p3.Y) / 2, (p2.Z + p3.Z) / 2);
                        Point3d midP1P3 = new Point3d((p1.X + p3.X) / 2, (p1.Y + p3.Y) / 2, (p1.Z + p3.Z) / 2);

                        // Legg til midtpunktene i samlingen hvis de ikke allerede finnes
                        if (!newPoints.Contains(midP1P2)) newPoints.Add(midP1P2);
                        if (!newPoints.Contains(midP2P3)) newPoints.Add(midP2P3);
                        if (!newPoints.Contains(midP1P3)) newPoints.Add(midP1P3);
                    }

                    // Legg til de nye punktene til TIN-surface
                    foreach (Point3d point in newPoints)
                    {
                        tinSurface.AddVertex(point);
                    }

                    int antallVertices = tinSurface.Vertices.Count;

                    // Skrive ut antallet vertices til kommandolinjen
                    doc.Editor.WriteMessage($"\nAntall vertices i bergmodellen: {antallVertices}");

                    int antallFaces = tinSurface.Triangles.Count;
                    doc.Editor.WriteMessage($"\nAntall faces i bergmodellen: {antallFaces}");
                }
                acTrans.Commit();
            }
        }
        private static void RaffinerTIN_30(ProgressWindow progressWindow, int currentIteration)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = doc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                using (doc.LockDocument())
                {
                    TinSurface tinSurface = GetTinSurfaceFromLayerName("Bergmodell", acTrans, acCurDb);

                    var allTriangles = new List<TinSurfaceTriangle>(tinSurface.Triangles);
                    var triangleAreas = allTriangles.Select(tri => new
                    {
                        Triangle = tri,
                        Area = 0.5 * (tri.Vertex1.Location.DistanceTo(tri.Vertex2.Location) +
                                     tri.Vertex2.Location.DistanceTo(tri.Vertex3.Location) +
                                     tri.Vertex3.Location.DistanceTo(tri.Vertex1.Location))
                    }).ToList();

                    var sortedTriangles = triangleAreas.OrderByDescending(x => x.Area).ToList();

                    int numTrianglesToRefine = (int)(0.3 * sortedTriangles.Count);
                    var largestTriangles = sortedTriangles.Take(numTrianglesToRefine).Select(x => x.Triangle);

                    var newPoints = new Point3dCollection();

                    foreach (var tri in largestTriangles)
                    {
                        Point3d p1 = tri.Vertex1.Location;
                        Point3d p2 = tri.Vertex2.Location;
                        Point3d p3 = tri.Vertex3.Location;

                        Point3d midP1P2 = new Point3d((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, (p1.Z + p2.Z) / 2);
                        Point3d midP2P3 = new Point3d((p2.X + p3.X) / 2, (p2.Y + p3.Y) / 2, (p2.Z + p3.Z) / 2);
                        Point3d midP1P3 = new Point3d((p1.X + p3.X) / 2, (p1.Y + p3.Y) / 2, (p1.Z + p3.Z) / 2);

                        if (!newPoints.Contains(midP1P2)) newPoints.Add(midP1P2);
                        if (!newPoints.Contains(midP2P3)) newPoints.Add(midP2P3);
                        if (!newPoints.Contains(midP1P3)) newPoints.Add(midP1P3);
                    }

                    foreach (Point3d point in newPoints)
                    {
                        tinSurface.AddVertex(point);
                    }

                    int antallVertices = tinSurface.Vertices.Count;
                    doc.Editor.WriteMessage($"\nAntall vertices i bergmodellen etter raffinering: {antallVertices}");

                    int antallFaces = tinSurface.Triangles.Count;
                    doc.Editor.WriteMessage($"\nAntall faces i bergmodellen etter raffinering: {antallFaces}");

                    // Oppdaterer progress-vinduet basert på nåværende iterasjon av løkken
                    progressWindow.UpdateProgress(currentIteration);
                }
                acTrans.Commit();
            }
        }

        private static List<Point3d> ProjectTinTrianglesToXY(string layerBermodell)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = doc.Database;

            List<Point3d> midPoints = new List<Point3d>();
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                using (doc.LockDocument())
                {
                    TinSurface tinSurface = GetTinSurfaceFromLayerName(layerBermodell, acTrans, acCurDb);

                    var newPoints = new Point3dCollection();

                    var allTriangles = new List<TinSurfaceTriangle>(tinSurface.Triangles);
                    foreach (var tri in allTriangles)
                    {
                        Point3d p1 = tri.Vertex1.Location;
                        Point3d p2 = tri.Vertex2.Location;
                        Point3d p3 = tri.Vertex3.Location;


                        // Beregner midtpunktet for trianglet
                        double midX = (p1.X + p2.X + p3.X) / 3;
                        double midY = (p1.Y + p2.Y + p3.Y) / 3;
                        Point3d midpoint = new Point3d(midX, midY, 0);  // Set Z = 0 for projeksjon på XY-planet
                        midPoints.Add(midpoint);

                    }
                }
                acTrans.Commit();
            }

            return midPoints;
        }

        private static List<string> HenteLagNavn(string metode)
        {
            List<string> lagNavn = new List<string>();

            if (metode == "XY")
            {
                lagNavn.Add("Intervall1");
                lagNavn.Add("Intervall2");
                lagNavn.Add("Intervall3");
                lagNavn.Add("Intervall4");
                lagNavn.Add("Intervall5");

            }
            else
            {
                lagNavn.Add("Intervall1_Z");
                lagNavn.Add("Intervall2_Z");
                lagNavn.Add("Intervall3_Z");
                lagNavn.Add("Intervall4_Z");
                lagNavn.Add("Intervall5_Z");
            }


            return lagNavn;
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

        public static void CalculateElevationDifferences(Transaction acTrans, Database acCurDb, string terrengModellLagNavn, string bergmodellLagNavn, List<Point3d> MidPointsZ)
        {
            VerdierZ.Clear();

            // Hent bergmodell og terrengmodeller
            TinSurface bergmodell = GetTinSurfaceFromLayerName(bergmodellLagNavn, acTrans, acCurDb);
            List<GridSurface> terrengmodeller = GetSurfacesFromLayer(acTrans, acCurDb, terrengModellLagNavn);

            // Hent grenser for bergmodellen

            List<Point2d> bergGrenser = ExtractBordersFromTinSurface(acTrans, bergmodell);

            // Hent grenser for hver terrengmodell
            List<List<Point2d>> terrengBorders = terrengmodeller.Select(surface => ExtractBorders(acTrans, surface)).ToList();

            foreach (Point3d midPoint in MidPointsZ)
            {
              
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




    }
}
    

