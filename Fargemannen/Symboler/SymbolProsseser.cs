using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Autodesk.AutoCAD.Geometry;

using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;





using System.Windows.Media.Animation;
using System.Security.Cryptography;
using System.Net;
using System.Windows.Forms;
using Autodesk.AutoCAD.Internal.Calculator;
using Autodesk.Windows;


namespace Fargemannen
{
    internal class SymbolProsseser
    {
        public static List<string> GenerteSymbol { get; } = new List<string>();


        public void test(List<PunktInfo> pointsToSymbol, List<string> metoder, double minGrenseBerg)

        {

            SlettAlleBlokkerOgLag();

            string basePath = "D:\\Temp\\";
            var dwgFiles = new Dictionary<string, string>
            {
                ["totalsondering"] = "Totalsondering.dwg",
                ["dreietrykksondering"] = "dreietrykksondering.dwg",
                ["trykksondering"] = "trykksondering.dwg",
                ["prøveserie"] = "prøveserie.dwg",
                ["poretrykkmåling"] = "poretrykksmåler.dwg",
                ["vingeboring"] = "vingeboring.dwg",
                ["dreiesondering"] = "dreiesondering.dwg",
                ["prøvegrop"] = "prøvegrop.dwg",
                ["fjellkontrollboring"] = "fjellkontrollboring.dwg",
                ["ramsondering"] = "ramsondering.dwg",
                ["enkel"] = "enkelSondering.dwg",
                ["fjellidagen"] = "fjellIDagen.dwg",
                ["stans"] = "stans.dwg"
            };

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;


            try
            {
                using (var tr = db.TransactionManager.StartTransaction())
                using (doc.LockDocument())
                {
                    var layerSymbol = "Symboler";
                    var layerSymbolStansILos = "Symboler for stans i løs";



                    SjekkOgOpprettLag(db, tr, layerSymbol, doc, SecondWinSymbol.selectedColor);
                    SjekkOgOpprettLag(db, tr, layerSymbolStansILos, doc, SecondWinSymbol.selectedColor_minBerg);

                    var databases = new Dictionary<string, Database>();
                    var blockRefs = new Dictionary<string, ObjectId>();

                    foreach (var file in dwgFiles)
                    {
                        string filePath = basePath + file.Value;
                        try
                        {
                            databases[file.Key] = new Database(false, false);
                            string dwgPath = HostApplicationServices.Current.FindFile(filePath, db, FindFileHint.Default);
                            if (string.IsNullOrEmpty(dwgPath))
                            {
                                throw new FileNotFoundException($"DWG filen '{filePath}' ble ikke funnet.");
                            }

                            databases[file.Key].ReadDwgFile(dwgPath, FileShare.Read, true, "");
                            blockRefs[file.Key] = db.Insert(dwgPath, databases[file.Key], false);
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Feil ved lesing av DWG-fil: {ex.Message}", "Feil", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            continue;
                        }
                    }

                    foreach (var punkt in pointsToSymbol)
                    {
                        ProcessPunkt(punkt, metoder, minGrenseBerg, blockRefs, db, tr, layerSymbol, layerSymbolStansILos);
                    }

                    tr.Commit();
                }

            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                System.Windows.MessageBox.Show($"AutoCAD Runtime-feil: {ex.Message}", "Runtime Feil", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"En uventet feil oppstod: {ex.Message}", "System Feil", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            doc.Editor.Regen();
        }

        private void ProcessPunkt(PunktInfo punkt, List<string> metoder, double minGrenseBerg, Dictionary<string, ObjectId> blockRefs, Database db, Transaction tr, string layerSymbol, string layerSymbolStansILos)
        {
      
            var sonderingType = punkt.GBUMetode.ToLower();

            if (!blockRefs.ContainsKey(sonderingType))
                throw new KeyNotFoundException($"Ingen blokkreferanse funnet for sonderingstypen '{sonderingType}'.");

            Point3d plasseringsPunkt = new Point3d(punkt.Punkt.X, punkt.Punkt.Y, 0);
            


            ObjectId gjelendeKurve = blockRefs[sonderingType];
            ObjectId stansSymbol = blockRefs["stans"];
            string symbolID = punkt.RapportID + "_" + punkt.PunktID;
        
            double borIBerg = punkt.BoreFjell;


            using (var btr = new BlockTableRecord())
            {
                btr.Name = $"Punkt_{punkt.PunktID}";
                if (string.IsNullOrWhiteSpace(btr.Name))
                    throw new ArgumentException("Navnet på blokk tabell record er tomt eller inneholder bare hvite tegn.", nameof(btr.Name));

                try
                {
                    InsertSymbolAndText(plasseringsPunkt, gjelendeKurve, stansSymbol, punkt, tr, db, layerSymbol, layerSymbolStansILos, minGrenseBerg, symbolID, borIBerg);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    System.Windows.MessageBox.Show($"En AutoCAD-feil oppstod: {ex.Message}", "AutoCAD Feil", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            
        }


       
        private void InsertSymbolAndText(Point3d placementPoint, ObjectId gjeldendeKurve, ObjectId stansSymbol, PunktInfo punkt, Transaction tr, Database db, string layerSymbol, string layerSymbolStansILos, double minGrenseBerg, string symbolID, double borIBerg)
        {
            try
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);

                if (bt == null)
                {
                    throw new InvalidOperationException("Kan ikke åpne BlockTable fra databasen.");
                }

                var btr = new BlockTableRecord();
                btr.Name = $"Punkt_{punkt.PunktID}";

                GenerteSymbol.Add(btr.Name);

                if (bt.Has(btr.Name))
                {
                    throw new InvalidOperationException($"En blokk med navn '{btr.Name}' finnes allerede.");
                }
                placementPoint = new Point3d(placementPoint.X, placementPoint.Y, 100);
                BlockReference symbolRef = new BlockReference(placementPoint, gjeldendeKurve)
                {
                    Layer = punkt.BoreFjell < minGrenseBerg ? layerSymbolStansILos : layerSymbol
                };

                DBText textEntityGroundQuote = new DBText
                {
                    Position = new Point3d(placementPoint.X + 1.8, placementPoint.Y + 0.12, 100),
                    TextString = punkt.Terrengkvote.ToString("F2"),
                    Height = 0.9
                };

                DBText textEntitySymbolID = new DBText
                {
                    Position = new Point3d(placementPoint.X - 1.8, placementPoint.Y - 0.5, 100),
                    TextString = symbolID,
                    Height = 1.2,
                    Justify = AttachmentPoint.BaseRight,
                    AlignmentPoint = new Point3d(placementPoint.X - 1.8, placementPoint.Y - 0.7, 100)
                };

                if (punkt.GBUMetode.ToLower() == "fjellidagen")
                {
                    btr.AppendEntity(symbolRef);
                    btr.AppendEntity(textEntityGroundQuote);
                    btr.AppendEntity(textEntitySymbolID);
                    btr.Origin = placementPoint;
                }
                else
                {
                    DBText textEntityLooseRockFirmRock = new DBText
                    {
                        Position = new Point3d(placementPoint.X + 6, placementPoint.Y - 0.3, 100),
                        TextString = $"{punkt.BoreFjell:F2}+{punkt.BorLøs:F2}",
                        Height = 0.9
                    };

                    btr.AppendEntity(symbolRef);
                    btr.AppendEntity(textEntityGroundQuote);
                    btr.AppendEntity(textEntityLooseRockFirmRock);
                    btr.AppendEntity(textEntitySymbolID);
                    btr.Origin = placementPoint;

                    if (borIBerg < minGrenseBerg)
                    {
                        Point3d plasseringStansSymbol = new Point3d(placementPoint.X + 3.2, placementPoint.Y - 0.9, 100);
                        btr.AppendEntity(new BlockReference(plasseringStansSymbol, stansSymbol));
                    }
                    else
                    {
                        DBText tekstEntitet_Fjellkvote = new DBText()
                        {
                            Position = new Point3d(placementPoint.X + 1.8, placementPoint.Y - 1.12, 100),
                            TextString = punkt.Punkt.Z.ToString("F2"),
                            Height = 0.9
                        };

                        btr.AppendEntity(tekstEntitet_Fjellkvote);
                    }
                }

                bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);

                BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                if (modelSpace == null)
                {
                    throw new InvalidOperationException("Kan ikke åpne ModelSpace for skriving.");
                }

                BlockReference newBlockRef = new BlockReference(placementPoint, btr.Id)
                {
                    Layer = punkt.BoreFjell < minGrenseBerg ? layerSymbolStansILos : layerSymbol,
                    ScaleFactors = new Scale3d(SecondWinSymbol.newScale, SecondWinSymbol.newScale, SecondWinSymbol.newScale),
                    Rotation = SecondWinSymbol.rotasjonAngle
                };
                

                modelSpace.AppendEntity(newBlockRef);
                tr.AddNewlyCreatedDBObject(newBlockRef, true);
            }
            catch (System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"En feil oppstod: {ex.Message}", "Feil", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

       
        public void SjekkOgOpprettLag(Database db, Transaction tr, string layerName, Document doc, System.Drawing.Color farge)
        {
            // Få tilgang til LayerTable fra den gjeldende databasen
   
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            using (doc.LockDocument())
            {
                // Sjekk om laget eksisterer
                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen(); // For å tillate endringer

                    Autodesk.AutoCAD.Colors.Color fargeCon = Autodesk.AutoCAD.Colors.Color.FromColor(farge);
                    // Oppretter et nytt lag
                    LayerTableRecord ltr = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = fargeCon
                    };

                    // Legger det nye laget til LayerTable
                    lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);

                    lt.DowngradeOpen(); // Nedgraderer tilgangen etter endringene
                }
            }
        }

        public void SlettAlleBlokkerOgLag()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            GenerteSymbol.Clear();

            using (var tr = db.TransactionManager.StartTransaction())
            using (doc.LockDocument())
            {
                // Prepare for deleting layers
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
                var layersToDelete = lt.Cast<ObjectId>()
                    .Select(id => (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead))
                    .Where(ltr => ltr.Name.StartsWith("Symboler"))
                    .ToList();

                // Prepare for deleting blocks
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                var blocksToDelete = bt.Cast<ObjectId>()
                    .Select(id => (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead))
                    .Where(btr => btr.Name.StartsWith("Punkt_"))
                    .ToList();

                int totalActions = layersToDelete.Count + blocksToDelete.Count;
                using (ProgressWindow progressWindow = new ProgressWindow(totalActions))
                {
                   

                    // Delete layers
                    foreach (var layer in layersToDelete)
                    {
                        if (!layer.IsErased)
                        {
                            layer.UpgradeOpen();
                            layer.Erase();
                            ed.WriteMessage($"Layer {layer.Name} slettet.\n");
                        }
                      
                    }

                    // Delete blocks
                    foreach (var block in blocksToDelete)
                    {
                        if (!block.IsErased)
                        {
                            block.UpgradeOpen();
                            block.Erase();
                            ed.WriteMessage($"Block {block.Name} slettet.\n");
                        }
                     
                    }

                    tr.Commit();
                    ed.WriteMessage("\nAlle blokker og lag slettet.\n");
                }

                ed.WriteMessage("\nAlle blokker og lag slettet.\n");
            }
        }
    }
}

    

