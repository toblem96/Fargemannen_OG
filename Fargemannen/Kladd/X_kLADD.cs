using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using Fargemannen;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fargemannen
{
    internal class X_kLADD
    {
    }

    /*
    public static void CreateWordDocument(string filePath, string text1, string text2, string prompts_1, string prompts_2)
    {
        // Første, ta et skjermbilde
        CaptureView();  // Sørger for at dette er definert for å fange riktig visning

        // Anta at filnavnet for skjermbildet er basert på den siste skjermbildeoperasjonen
        string screenshotPath = Directory.GetFiles(Path.GetTempPath(), "Screenshot_*.png").OrderByDescending(f => f).FirstOrDefault();

        using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());

            if (screenshotPath != null)
            {
                AddImageToDocument(mainPart, screenshotPath);
            }

            // Tekstseksjoner
            AddParagraph(mainPart.Document.Body, "Analyse av avstand mellom Bergmodell og Terrengmodell", text1);
            AddParagraph(mainPart.Document.Body, "Generering av Bergmodell", text2);
            AddParagraph(mainPart.Document.Body, "Prompts brukt:", $"{prompts_1}\n{prompts_2}");
        }
    }

    private static void AddImageToDocument(MainDocumentPart mainPart, string imagePath)
    {
        ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (FileStream stream = new FileStream(imagePath, FileMode.Open))
        {
            imagePart.FeedData(stream);
        }
        AddImageToBody(mainPart.GetIdOfPart(imagePart), mainPart.Document.Body);
    }

    private static void AddImageToBody(string relationshipId, DocumentFormat.OpenXml.Wordprocessing.Body body)
    {
        // Definere elementene som trengs for å legge til bildet
        var element =
             new Drawing(
                 new DW.Inline(
                     new DW.Extent() { Cx = 990000L, Cy = 792000L },
                     new DW.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                     new DW.DocProperties() { Id = (UInt32Value)1U, Name = "Picture 1" },
                     new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks() { NoChangeAspect = true }),
                     new A.Graphic(
                         new A.GraphicData(
                             new PIC.Picture(
                                 new PIC.NonVisualPictureProperties(
                                     new PIC.NonVisualDrawingProperties() { Id = (UInt32Value)0U, Name = "New Bitmap Image.png" },
                                     new PIC.NonVisualPictureDrawingProperties()),
                                 new PIC.BlipFill(
                                     new A.Blip(new A.BlipExtensionList(new A.BlipExtension() { Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}" }))
                                     {
                                         Embed = relationshipId,
                                         CompressionState = A.BlipCompressionValues.Print
                                     },
                                     new A.Stretch(new A.FillRectangle())),
                                 new PIC.ShapeProperties(
                                     new A.Transform2D(
                                         new A.Offset() { X = 0L, Y = 0L },
                                         new A.Extents() { Cx = 990000L, Cy = 792000L }),
                                     new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }))))));

        body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(element)));
    }

    private static DocumentFormat.OpenXml.Wordprocessing.Paragraph AddParagraph(DocumentFormat.OpenXml.Wordprocessing.Body body, string text)
    {
        DocumentFormat.OpenXml.Wordprocessing.Paragraph para = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
        DocumentFormat.OpenXml.Wordprocessing.Run run = para.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
        return para;
    }

    private void CaptureView()
    {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database db = doc.Database;
        Editor ed = doc.Editor;

        // Sjekk om det er noen polylinjer å behandle
        if (AnalyseZ.MiniPlListZ == null || AnalyseZ.MiniPlListZ.Count == 0)
        {
            ed.WriteMessage("\nIngen data tilgjengelig for å ta skjermbilde.");
            return;
        }

        // Kalkulerer minste og største koordinater fra MiniPlListZ
        double minX = AnalyseZ.MiniPlListZ.Min(poly => poly.Bounds.Value.MinPoint.X);
        double minY = AnalyseZ.MiniPlListZ.Min(poly => poly.Bounds.Value.MinPoint.Y);
        double maxX = AnalyseZ.MiniPlListZ.Max(poly => poly.Bounds.Value.MaxPoint.X);
        double maxY = AnalyseZ.MiniPlListZ.Max(poly => poly.Bounds.Value.MaxPoint.Y);

        Point3d firstCorner = new Point3d(minX, minY, 0);
        Point3d secondCorner = new Point3d(maxX, maxY, 0);

        // Beregner området for skjermbilde
        Extents3d extents = new Extents3d(firstCorner, secondCorner);

        // Setter opp visningen basert på beregnet område
        using (Transaction trans = db.TransactionManager.StartTransaction())
        {
            ViewTable viewTbl = (ViewTable)trans.GetObject(db.ViewTableId, OpenMode.ForRead);
            ViewTableRecord view = new ViewTableRecord
            {
                CenterPoint = new Point2d((firstCorner.X + secondCorner.X) / 2, (firstCorner.Y + secondCorner.Y) / 2),
                Height = (secondCorner.Y - firstCorner.Y),
                Width = (secondCorner.X - firstCorner.X)
            };

            ed.SetCurrentView(view);
            trans.Commit();
        }

        // Tar et skjermbilde av det definerte området og lagrer det
        string filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                       "Screenshot_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png");
        Autodesk.AutoCAD.ApplicationServices.Application.ScreenCapture(filename, new Point2d(firstCorner.X, firstCorner.Y), new Point2d(secondCorner.X, secondCorner.Y));
        ed.WriteMessage("\nScreenshot lagret: " + filename);
    }
    private static string GetIntervall()
    {
        {

            StringBuilder intervallInfo = new StringBuilder();
            intervallInfo.Clear();

            // Sorter intervaller basert på startverdi
            var sorterteIntervaller = Intervall.intervallListeZ.OrderBy(x => x.Start).ToList();

            foreach (var intervall in sorterteIntervaller)
            {
                // Anta at hvert intervall er aktivt og hent nødvendige detaljer
                string fargeHex = ColorTranslator.ToHtml(intervall.Farge); // Konverterer Color til Hex-kode
                intervallInfo.AppendLine($"Start: {intervall.Start}, Slutt: {intervall.Slutt}, Farge: {fargeHex}");
            }

            return intervallInfo.ToString();
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
                            double avstandTilBerg = 0, avstandTilTerreng = 0, differanse;

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
    /*
         private static void DelFirkantI1mX1m(Autodesk.AutoCAD.DatabaseServices.Polyline pl, double ruteStr)
         {
             Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
             Database acCurDb = doc.Database;



             using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
             {
                 using (doc.LockDocument())
                 {
                     string layerName = "UsynligLag";
                     try
                     {

                         LayerTable lt = null;


                         lt = (LayerTable)acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead);


                         //Må ha en docmuntlock her 
                         if (!lt.Has(layerName))
                         {
                             lt.UpgradeOpen();
                             LayerTableRecord ltr = new LayerTableRecord
                             {
                                 Name = layerName,
                                 IsOff = true // Dette slår av laget, gjør objekter på dette laget usynlige
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


                         foreach (DBPoint dbPoint in MidPoints)
                         {
                             Point3d currentMidPoint = dbPoint.Position;
                             double nearestDistance = double.MaxValue;

                             foreach (Point3d analysisPoint in WinAnalyseXY.punkterAnalyseXY)
                             {
                                 double distance = currentMidPoint.DistanceTo(analysisPoint);
                                     if (distance < nearestDistance)
                                     {
                                         nearestDistance = distance;
                                     }
                                 }

                                 lengdeVerdier.Add(Math.Round(nearestDistance));
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

       */

    /*
    
    public void test(List<PunktInfo> pointsToSymbol, List<string> metoder, double minGrenseBerg)
    {
        string stiTotalsondering = "D:\\Temp\\Totalsondering.dwg";
        string stiDreietrykksondering = "D:\\Temp\\dreietrykksondering.dwg";
        string stiTrykksondering = "D:\\Temp\\trykksondering.dwg";
        string stiPrøvesiere = "D:\\Temp\\prøvesiere.dwg";
        string stiPoretrykksmåler = "D:\\Temp\\poretrykksmåler.dwg";
        string stiVingeboring = "D:\\Temp\\vingeboring.dwg";
        string stiDreiesondering = "D:\\Temp\\dreiesondering.dwg";
        string stiPrøvegrop = "D:\\Temp\\prøvegrop.dwg";
        string stiFjellkontrollboring = "D:\\Temp\\fjellkontrollboring.dwg";
        string stiRamsondering = "D:\\Temp\\ramsondering.dwg";
        string stiEnkelSondring = "D:\\Temp\\enkelSondering.dwg";
        string stiFjellIDagen = "D:\\Temp\\fjellIDagen.dwg";
        string stiStansSyombol = "D:\\Temp\\stans.dwg";


        string layerSymbol = "Symboler";
        string layerSymbolStansILos = "Symboler for stans i løs";

        //Aktiviere dokumntet 
        Document doc = AcApp.DocumentManager.MdiActiveDocument;
        Editor ed = doc.Editor;

        //Starter Transaksjon 
        Transaction tr = doc.TransactionManager.StartTransaction();

        SjekkOgOpprettLag(doc.Database, tr, layerSymbol, doc);
        SjekkOgOpprettLag(doc.Database, tr, layerSymbolStansILos, doc);

        try
        {
            //låser dokumntet 
            using (doc.LockDocument())
            {
                //Oppretter en ny database 
                Database dbTotalsondering = new Database(false, false);
                Database dbDreietrykksondering = new Database(false, false);
                Database dbTrykksondering = new Database(false, false);
                Database dbPrøvesiere = new Database(false, false);
                Database dbPoretrykksmåler = new Database(false, false);
                Database dbVingeboring = new Database(false, false);
                Database dbDreiesondering = new Database(false, false);
                Database dbPrøvegrop = new Database(false, false);
                Database dbFjellkontrollboring = new Database(false, false);
                Database dbRamsondering = new Database(false, false);
                Database dbEnkelSondring = new Database(false, false);
                Database dbFjellIDagen = new Database(false, false);
                Database dbStansSyombol = new Database(false, false);




                string dwgTotalsondering = HostApplicationServices.Current.FindFile(stiTotalsondering, doc.Database, FindFileHint.Default);
                string dwgDreietrykksondering = HostApplicationServices.Current.FindFile(stiDreietrykksondering, doc.Database, FindFileHint.Default);
                string dwgTrykksondering = HostApplicationServices.Current.FindFile(stiTrykksondering, doc.Database, FindFileHint.Default);
                string dwgPrøvesiere = HostApplicationServices.Current.FindFile(stiPrøvesiere, doc.Database, FindFileHint.Default);
                string dwgPoretrykksmåler = HostApplicationServices.Current.FindFile(stiPoretrykksmåler, doc.Database, FindFileHint.Default);
                string dwgVingeboring = HostApplicationServices.Current.FindFile(stiVingeboring, doc.Database, FindFileHint.Default);
                string dwgDreiesondering = HostApplicationServices.Current.FindFile(stiDreiesondering, doc.Database, FindFileHint.Default);
                string dwgPrøvegrop = HostApplicationServices.Current.FindFile(stiPrøvegrop, doc.Database, FindFileHint.Default);
                string dwgFjellkontrollboring = HostApplicationServices.Current.FindFile(stiFjellkontrollboring, doc.Database, FindFileHint.Default);
                string dwgRamsondering = HostApplicationServices.Current.FindFile(stiRamsondering, doc.Database, FindFileHint.Default);
                string dwgEnkelSondring = HostApplicationServices.Current.FindFile(stiEnkelSondring, doc.Database, FindFileHint.Default);
                string dwgFjellIDagen = HostApplicationServices.Current.FindFile(stiFjellIDagen, doc.Database, FindFileHint.Default);
                string dwgStansSyombol = HostApplicationServices.Current.FindFile(stiStansSyombol, doc.Database, FindFileHint.Default);



                dbTotalsondering.ReadDwgFile(dwgTotalsondering, FileShare.Read, true, "");
                dbDreietrykksondering.ReadDwgFile(dwgDreietrykksondering, FileShare.Read, true, "");
                dbTrykksondering.ReadDwgFile(dwgTrykksondering, FileShare.Read, true, "");
                dbPrøvesiere.ReadDwgFile(dwgPrøvesiere, FileShare.Read, true, "");
                dbPoretrykksmåler.ReadDwgFile(dwgPoretrykksmåler, FileShare.Read, true, "");
                dbVingeboring.ReadDwgFile(dwgVingeboring, FileShare.Read, true, "");
                dbDreiesondering.ReadDwgFile(dwgDreiesondering, FileShare.Read, true, "");
                dbPrøvegrop.ReadDwgFile(dwgPrøvegrop, FileShare.Read, true, "");
                dbFjellkontrollboring.ReadDwgFile(dwgFjellkontrollboring, FileShare.Read, true, "");
                dbRamsondering.ReadDwgFile(dwgRamsondering, FileShare.Read, true, "");
                dbEnkelSondring.ReadDwgFile(dwgEnkelSondring, FileShare.Read, true, "");
                dbFjellIDagen.ReadDwgFile(dwgFjellIDagen, FileShare.Read, true, "");
                dbStansSyombol.ReadDwgFile(dwgStansSyombol, FileShare.Read, true, "");


                ObjectId BlkTotalsondering;
                ObjectId BlkDreietrykksondering;
                ObjectId BlkTrykksondering;
                ObjectId BlkPrøvesiere;
                ObjectId BlkPoretrykksmåler;
                ObjectId BlkVingeboring;
                ObjectId BlkDreiesondering;
                ObjectId BlkPrøvegrop;
                ObjectId BlkFjellkontrollboring;
                ObjectId BlkRamsondering;
                ObjectId BlkEnkelSondring;
                ObjectId BlkFjellIDagen;
                ObjectId BlkStansSyombol;


                BlkTotalsondering = doc.Database.Insert(dwgTotalsondering, dbTotalsondering, false);
                BlkDreietrykksondering = doc.Database.Insert(dwgDreietrykksondering, dbDreietrykksondering, false);
                BlkTrykksondering = doc.Database.Insert(dwgTrykksondering, dbTrykksondering, false);
                BlkPrøvesiere = doc.Database.Insert(dwgPrøvesiere, dbPrøvesiere, false);
                BlkPoretrykksmåler = doc.Database.Insert(dwgPoretrykksmåler, dbPoretrykksmåler, false);
                BlkVingeboring = doc.Database.Insert(dwgVingeboring, dbVingeboring, false);
                BlkDreiesondering = doc.Database.Insert(dwgDreiesondering, dbDreiesondering, false);
                BlkPrøvegrop = doc.Database.Insert(dwgPrøvegrop, dbPrøvegrop, false);
                BlkFjellkontrollboring = doc.Database.Insert(dwgFjellkontrollboring, dbFjellkontrollboring, false);
                BlkRamsondering = doc.Database.Insert(dwgRamsondering, dbRamsondering, false);
                BlkEnkelSondring = doc.Database.Insert(dwgEnkelSondring, dbEnkelSondring, false);
                BlkFjellIDagen = doc.Database.Insert(dwgFjellIDagen, dbFjellIDagen, false);
                BlkStansSyombol = doc.Database.Insert(dwgStansSyombol, dbStansSyombol, false);


                Dictionary<string, int> sonderingstypeTeller = new Dictionary<string, int>();

                //Test må gjøres noe med så alle er i en block
                foreach (PunktInfo punkt in pointsToSymbol)
                {
                    string sonderingType = punkt.GBUMetode;
                    Point3d plasseringsPunkt = punkt.Punkt;
                    ObjectId gjelendeKurve = new ObjectId();

                    if (sonderingstypeTeller.ContainsKey(sonderingType))
                    {
                        sonderingstypeTeller[sonderingType]++;
                    }
                    else
                    {
                        sonderingstypeTeller[sonderingType] = 1;
                    }


                    if (sonderingType.ToLower() == "totalsondering")
                    {
                        gjelendeKurve = BlkTotalsondering;
                    }
                    else if (sonderingType.ToLower() == "dreietrykksondering")
                    {
                        gjelendeKurve = BlkDreietrykksondering;
                    }
                    else if (sonderingType.ToLower() == "trykksondering")
                    {
                        gjelendeKurve = BlkTrykksondering;
                    }
                    else if (sonderingType.ToLower() == "prøveserie uspesifisert")
                    {
                        gjelendeKurve = BlkPrøvesiere;
                    }
                    else if (sonderingType.ToLower() == "Poretrykkmåling")
                    {
                        gjelendeKurve = BlkPoretrykksmåler;
                    }
                    else if (sonderingType.ToLower() == "vingeboring")
                    {
                        gjelendeKurve = BlkVingeboring;
                    }
                    else if (sonderingType.ToLower() == "dreiesondering uspesifisert")
                    {
                        gjelendeKurve = BlkDreiesondering;
                    }
                    else if (sonderingType.ToLower() == "prøvegropg")
                    {
                        gjelendeKurve = BlkPrøvegrop;
                    }
                    else if (sonderingType.ToLower() == "bergkontrollboring")
                    {
                        gjelendeKurve = BlkFjellkontrollboring;
                    }
                    else if (sonderingType.ToLower() == "ramsondering")
                    {
                        gjelendeKurve = BlkRamsondering;
                    }
                    else if (sonderingType.ToLower() == "enkel sondering")
                    {
                        gjelendeKurve = BlkEnkelSondring;
                    }
                    else if (sonderingType.ToLower() == "fjell i dagen")
                    {
                        gjelendeKurve = BlkFjellIDagen;
                    }
                    else
                    {
                        gjelendeKurve = BlkEnkelSondring;
                    }



                    string rapportID = punkt.RapportID;
                    string punktID = punkt.PunktID;
                    double terrengkvote = punkt.Terrengkvote;
                    double boreIFjell = punkt.BoreFjell;
                    double borILøs = punkt.BorLøs;
                    double bergKvote = terrengkvote - boreIFjell - borILøs;
                    bergKvote = Math.Round(bergKvote, 2);

                    string borILøs_borIFjell = borILøs.ToString() + " + " + boreIFjell.ToString();
                    string symbolID = rapportID + "_" + punktID;

                    BlockTableRecord nyBlockDef = new BlockTableRecord();



                    LayerTable layerTable = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
                    if (!layerTable.Has(layerSymbol))
                    {
                        // Oppretter nytt lag dersom det ikke finnes
                        LayerTableRecord newLayer = new LayerTableRecord();
                        newLayer.Name = layerSymbol;
                        layerTable.UpgradeOpen(); // Åpne lagtabellen for skriving
                        layerTable.Add(newLayer);
                        tr.AddNewlyCreatedDBObject(newLayer, true);
                    }


                    Point3d plasseringTerrengKvote = new Point3d(plasseringsPunkt.X + 1.8, plasseringsPunkt.Y + 0.12, plasseringsPunkt.Z); // Juster etter behov.
                    Point3d plasseringFjellKvote = new Point3d(plasseringsPunkt.X + 1.8, plasseringsPunkt.Y - 1.12, plasseringsPunkt.Z);
                    Point3d plasseringborILøs_borIFjell = new Point3d(plasseringsPunkt.X + 5, plasseringsPunkt.Y - 0.3, plasseringsPunkt.Z);
                    Point3d plasseringsymbolID = new Point3d(plasseringsPunkt.X - 1.8, plasseringsPunkt.Y - 0.5, plasseringsPunkt.Z);

                    // Opprett en tekstentitet.
                    DBText tekstEntitet_terrengkvote = new DBText()
                    {
                        Position = plasseringTerrengKvote,
                        TextString = terrengkvote.ToString(),
                        Height = 0.9 // Juster tekststørrelsen etter behov.    //0,9
                    };



                    DBText tekstEntitet_borILøs_borIFjell = new DBText()
                    {
                        Position = plasseringborILøs_borIFjell,
                        TextString = borILøs_borIFjell.ToString(),
                        Height = 0.9


                    };

                    DBText tekstEntitet_symbolID = new DBText()
                    {

                        TextString = symbolID.ToString(),
                        Height = 1.2

                    };

                    if (boreIFjell < minGrenseBerg)
                    {
                        ObjectId stanssymbol = new ObjectId();
                        stanssymbol = BlkStansSyombol;
                        Point3d plasseringStansSymbol = new Point3d(plasseringsPunkt.X + 3.2, plasseringsPunkt.Y - 0.9, plasseringsPunkt.Z); //x var 2

                        nyBlockDef.AppendEntity(new BlockReference(plasseringStansSymbol, stanssymbol));


                    }

                    else
                    {
                        DBText tekstEntitet_Fjellkvote = new DBText()
                        {
                            Position = plasseringFjellKvote,
                            TextString = bergKvote.ToString(),
                            Height = 0.9


                        };

                        nyBlockDef.AppendEntity(tekstEntitet_Fjellkvote);


                    }

                    // Angi Justify til BaseRight for å feste teksten til høyre side av posisjonen
                    tekstEntitet_symbolID.Justify = AttachmentPoint.BaseRight;
                    // Siden vi nå bruker Justify, må vi angi AlignmentPoint i stedet for Position
                    tekstEntitet_symbolID.AlignmentPoint = plasseringsymbolID;




                    // Midlertidig BlockTableRecord for å holde både symbol og tekst.

                    nyBlockDef.Name = $"Punkt_{punkt.PunktID}";


                    GenerteSymbol.Add($"Punkt_{punkt.PunktID}");

                    // Legger til symbol og tekst i den midlertidige BlockTableRecord.
                    nyBlockDef.AppendEntity(new BlockReference(plasseringsPunkt, gjelendeKurve));
                    nyBlockDef.AppendEntity(tekstEntitet_terrengkvote);
                    nyBlockDef.AppendEntity(tekstEntitet_borILøs_borIFjell);
                    nyBlockDef.AppendEntity(tekstEntitet_symbolID);
                    nyBlockDef.Origin = plasseringsPunkt;

                    // Legger den midlertidige BlockTableRecord til i blokktabellen og oppretter en blokkdefinisjon.
                    BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    if (!bt.Has($"Punkt_{punkt.PunktID}"))
                    {
                        if (!bt.Has(nyBlockDef.Name))
                        {
                            bt.UpgradeOpen(); // Åpner blokktabellen for skriving.
                            bt.Add(nyBlockDef);
                            tr.AddNewlyCreatedDBObject(nyBlockDef, true);
                        }

                        // Oppretter en ny blokkreferanse basert på den nylig opprettede blokkdefinisjonen.
                        ObjectId nyBlockDefId = bt[nyBlockDef.Name];

                        BlockReference nyBref = new BlockReference(plasseringsPunkt, nyBlockDefId);

                        if (boreIFjell < minGrenseBerg)
                        {
                            nyBref.Layer = layerSymbolStansILos;
                        }
                        else
                        {
                            nyBref.Layer = layerSymbol;
                        }




                        // Legger den nye blokkreferansen til modellområdet.
                        btr.AppendEntity(nyBref);
                        tr.AddNewlyCreatedDBObject(nyBref, true);

                    }




                }


                foreach (var par in sonderingstypeTeller)
                {
                    ed.WriteMessage($"\nAntall {par.Key}: {par.Value}");
                }
            }

            //Forplikter transaksjonen, som lagrer alle endringene gjort siden transaksjonen startet.
            tr.Commit();


        }
        finally
        {
            tr.Dispose();
        }
        */
}





/*
public void CreateCivil3DPoints(List<PunktInfo> pointsToSymbol)
{
    //INITIALISERER AUTOCAD- OG DATABASEDOKUMENTOBJEKTER 

    //Henter det aktive dokumentet i Civil 3D - Dette må gjøre for å kunne arbeide med tegningen. 
    Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

    //Henter databasen til det aktive dokumentet - Dette må gjøres får å få tilgang på ny geometri/Legge til geometri
    Database db = doc.Database;

    //Låser dokumnetet. Passer på at det er bare denne koden som har tilgang på dokumentet, se det ikke skjer noen konfilkter 
    using (DocumentLock docLock = doc.LockDocument())
    {

        //Starter en transaskjon av dokumentet databasen. Blir ikke Commitet for sluttet. 
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            //Henter BlockTable ForRead som gjør at kan hente inforasjonen inni det bestmete BlockTablet
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

            //Henter BlockTable ForRead som gjør at kan ednre inforasjonen inni det bestmete BlockTablet
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            // Oppretter et nytt DBPoint og legger det til i modellrommet
            Point3d pointLocation = new Point3d(10.0, 20.0, 0.0); // Eksempelkoordinater
            DBPoint newPoint = new DBPoint(pointLocation); //DBPoint represnterer et punkt i AutoCAD databasen
            newPoint.SetDatabaseDefaults(); //Setter Standrard regler
            btr.AppendEntity(newPoint); //Legger det til i BlockTabaletet
            tr.AddNewlyCreatedDBObject(newPoint, true); //Legger til punktet i transaskjons vinduet

            // Forplikter endringene til databasen
            tr.Commit();
        }
    }
}

public void HenteUtPunkter(List<PunktInfo> pointsToSymbol)
{
    Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
    Database db = doc.Database;


    List<Point3d> point3Ds = new List<Point3d>();

    point3Ds = pointsToSymbol.Select(p => p.Punkt).ToList();
    foreach (Point3d point in point3Ds)
    {
        DBPoint newPoint = new DBPoint(point);

        using (DocumentLock docLock = doc.LockDocument())
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                //Henter BlockTable ForRead som gjør at kan ednre inforasjonen inni det bestmete BlockTablet
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);


                newPoint.SetDatabaseDefaults(); //Setter Standrard regler
                btr.AppendEntity(newPoint); //Legger det til i BlockTabaletet
                tr.AddNewlyCreatedDBObject(newPoint, true); //Legger til punktet i transaskjons vinduet

                // Forplikter endringene til databasen
                tr.Commit();

            }
        }
    }

}

public void GenererMeshFraPunkter(List<PunktInfo> punktListe)
{
    Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
    Database db = doc.Database;
    CivilDocument civilDoc = CivilApplication.ActiveDocument;

    using (DocumentLock docLock = doc.LockDocument())
    {
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            // Oppretter en ny TIN-surface
            TinSurface tinSurface = TinSurface.Create(civilDoc, "BergmodellMesh");

            // Går gjennom hvert punkt og legger det til i TIN-surface
            foreach (PunktInfo punktInfo in punktListe)
            {
                Point3d punkt = punktInfo.Punkt;
                tinSurface.AddVertex(new TinSurfaceVertex(punkt.X, punkt.Y, punkt.Z));
            }

            // Rebuild surface for å oppdatere meshet med de nye punktene
            tinSurface.Rebuild();

            // Forplikter endringene til databasen
            tr.Commit();
        }
    }
}







 public partial class MainWinBergModell : Window
    {
        //setter opp variabler 
        public List<string> boreMetoder = new List<string>();
        public int minAr;
        public string filepath;
     

        public MainWinBergModell()
        {
            InitializeComponent();
        }


        

        //KANPP FOR BRUKERE FOR Å VELGE SOSI-FIL 
        private void Button_VelgSosiFil(object sender, RoutedEventArgs e)
        {
            //Setter opp ny fildialg som brukes for å åpne en ny filvaglgmeny, som er innebygd.
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.Filter = "SOSI-filer | *.SOS";
            

            //Lagerer reultet av brukerens handling inn i resultat
            bool? succsess = fileDialog.ShowDialog();

            //hvis det er handling
            if (succsess == true)
            {
                // Brukeren valgte en fil og trykket OK
                filepath = fileDialog.FileName;


                //Viser hvilken filer som er lastet opp og hjør det sånn at bare filvanvnet vises 
                string filename = fileDialog.SafeFileName;
                tbInfo.Text = filename;

            }
            else
            {
                //Fikk ikke noe filepath
            }
        }




        //TEXTBOX FOR Å HENTE UT_MIN AR
        private void TextBox_MinAr(object sender, TextChangedEventArgs e)
        {
            {
                //konventere sender til TextBox
                TextBox textBox = sender as TextBox;

                //Hvis den ikke er null
                if (textBox != null)
                {
                    // Forsøk å konvertere tekst til en int
                    bool result = int.TryParse(textBox.Text, out int parsedValue);
                    if (result)
                    {
                        // Teksten er en gyldig int, oppdater minAr
                        minAr = parsedValue;

                        minArInfo.Text = minAr.ToString();

                    }
                    else
                    {
                        // Teksten er ikke en gyldig int, håndter det på ønsket måte
                        // For eksempel, du kan vise en feilmelding eller nullstille minAr
                        // minAr = 0; // Nullstill minAr eller håndter feilen
                    }
                }
            }
        }

        


        //KNAPP FOR Å ÅPNE CHECKLIST TIL BORRINGENE 
        private void Button_ShowCheckListBoringer(object sender, RoutedEventArgs e)
        {
            lstSondering.Visibility = lstSondering.Visibility == System.Windows.Visibility.Visible ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

        }

        //LASTER OPP ALLE CHECKBOXENE TIL EN STRINGLISTE
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //Konventerer sender til CheckBox
            CheckBox chk = sender as CheckBox;

            //Sjekker at koventeringen var vellyket, og at noe er sjekket
            if (chk != null && chk.IsChecked == true)
            {
                //Legger til i boreMetoder
                boreMetoder.Add(chk.Tag.ToString());

                //Ser at det ligger noe i listen 
                boreMetoderInfo.Text = boreMetoder[0];
            }
        }

        //FJERNER ALLE IKKESJEKKEDE BOKSER FRA LISTEN 
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox chk = sender as CheckBox;
            if (chk != null)
            {
                boreMetoder.Remove(chk.Tag.ToString());
            }
        }
     

        



        //KNAP FOR Å STARTE PROSSESEN 
        private void StartProcessing_Click(object sender, RoutedEventArgs e)
        {
            // Anta at disse variablene er definert og satt tidligere i din klasse:
            // int minAr;
            // List<string> borremetoderSymbol;
            // string sosiFilePath;

            // Opprette listen hvor resultater vil bli lagt til
            List<PunktInfo> pointsToSymbol = new List<PunktInfo>();
            List<Point3d> punkterMesh = new List<Point3d>();

            // Opprett en instans av din SOSIFileProcessor_Boring
            var processor = new Fargemannen.SOSIFileProcessor_Boring();

            // Kall metoden med de nødvendige verdiene
            processor.ProcessSOSIFiles(minAr, boreMetoder, filepath, pointsToSymbol, punkterMesh);

            // Gjør noe med pointsToSymbol etter prosessering om nødvendig
            // For eksempel, oppdatere UI eller lagre resultatene

            GenererMeshFraPunkter(punkterMesh);
        }

        public void GenererMeshFraPunkter(List<Point3d> punkterMesh)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;

            // Anta at punkterMesh er tilgjengelig her som en List<Point3d>

            using (DocumentLock docLock = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ObjectId tinSurfaceId = TinSurface.Create(db, "Bergmodell");
                    TinSurface tinSurface = tr.GetObject(tinSurfaceId, OpenMode.ForWrite) as TinSurface;

             
                        foreach (Point3d punkt in punkterMesh)
                        {
                            // Legger til punkter til TinSurface
                            tinSurface.AddVertex(punkt);
                        }

                        // Rebuild surface for å oppdatere meshet med de nye punktene
                        tinSurface.Rebuild();

                        // Forplikter endringene til databasen
                        tr.Commit();
                    }
                }
            }
        }


    }


<Window x:Class="Fargemannen.MainWinSymbol"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             xmlns:local="clr-namespace:Fargemannen"
             mc:Ignorable="d" Height="436" Width="483">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="309*"/>
            <ColumnDefinition Width="491*"/>
        </Grid.ColumnDefinitions>

        <!--Knapp for filfalg sosi -->
        <Button Content="BORHULL" HorizontalAlignment="Left" Margin="27,52,0,0" VerticalAlignment="Top" RenderTransformOrigin="-3.868,-0.686" Click="Button_VelgSosiFil" Width="91"/>

        <!--Tekstsboks for å hente minÅr -->
        <TextBox HorizontalAlignment="Left" Margin="27,130,0,0" TextWrapping="Wrap" Text="TextBox" VerticalAlignment="Top" Width="120" TextChanged="TextBox_MinAr"/>

        <!-- Knapp for å vise sjekkliste -->
        <Button Content="Vis Sjekkliste" HorizontalAlignment="Left" VerticalAlignment="Top" Click="Button_ShowCheckListBoringer" Margin="27,197,0,0"/>

        <!-- Sjekklisten -->
        <ListBox x:Name="lstSondering" SelectionMode="Multiple" Margin="0,455,20,-435">
            <CheckBox Content="Enkel sondering" Tag="Enkel Sondering" Checked="CheckBox_Checked" Unchecked="CheckBox_Unchecked"/>
            <CheckBox Content="Ikke angitt" Tag="Ikke Angitt" Checked="CheckBox_Checked" Unchecked="CheckBox_Unchecked"/>
            <CheckBox Content="Dreietrykksondering" Tag="Dreietrykksondering" Checked="CheckBox_Checked" Unchecked="CheckBox_Unchecked"/>
            <CheckBox Content="Dreiesondering uspesifisert" Tag="DreiesonderingUspesifisert" Checked="CheckBox_Checked" Unchecked="CheckBox_Unchecked"/>
            <CheckBox Content="Vingeboring" Tag="Vingeboring" Checked="CheckBox_Checked" Unchecked="CheckBox_Unchecked"/>
            <CheckBox Content="Totalsondering" Tag="Totalsondering" Checked="CheckBox_Checked" Unchecked="CheckBox_Unchecked"/>
            <TextBlock TextWrapping="Wrap" Name="boreMetoderInfo" Text="TextBlock"/>
        </ListBox>

        <!-- Startknapp for prossesen-->
        <Button Content="Start Prossesering" Click="StartProcessing_Click" Grid.Column="1" Margin="0,0,182,0"/>
        
        <!-- Informasjon om hvilken filer som er lastet opp -->
        <TextBlock HorizontalAlignment="Left" Margin="27,81,0,0" TextWrapping="Wrap" Name="tbInfo" Text="TextBlock" VerticalAlignment="Top"/>
        
        <!-- Informajon om hvilk min_ar verdier som kommer-->
        <TextBlock HorizontalAlignment="Left" Margin="27,153,0,0" TextWrapping="Wrap" Name="minArInfo" Text="TextBlock" VerticalAlignment="Top" RenderTransformOrigin="-0.35,-0.944"/>

    </Grid>
</Window>




*/