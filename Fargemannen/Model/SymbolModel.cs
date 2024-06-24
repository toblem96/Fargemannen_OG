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

using System.Reflection;

using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

using Autodesk.AutoCAD.Runtime;

using System.Windows.Forms;
using Microsoft.ApplicationInsights;
namespace Fargemannen.Model
{
    public static class SymbolModel
    {

        public static List<string> GenerteSymbol { get; } = new List<string>();



        public static void PrintValgtBoring(List<PunktInfo> pointsToSymbol, List<string> boreMetoder)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Dictionary<string, int> boringCount = new Dictionary<string, int>();

            foreach (var punkt in pointsToSymbol)
            {
                if (boringCount.ContainsKey(punkt.GBUMetode))
                {
                    boringCount[punkt.GBUMetode]++;
                }
                else
                {
                    boringCount[punkt.GBUMetode] = 1;
                }
            }
            ed.WriteMessage("\nAntall forekomster av hver GBUMetode:\n");
            foreach (var pair in boringCount)
            {
                ed.WriteMessage($"GBUMetode: {pair.Key}, Antall: {pair.Value}\n");
            }
        }

        public static void ListResourceNames()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            foreach (string resourceName in resourceNames)
            {
                ed.WriteMessage($"\n{resourceName}\n");
            }
        }


        public static void test(List<PunktInfo> pointsToSymbol, List<string> metoder, double minGrenseBerg, System.Windows.Media.Color selectedColor, System.Windows.Media.Color selectedColor_minBerg)
        {
            var telemetryClient = new TelemetryClient();
            try 
            { 
            SlettAlleBlokkerOgLag();

            var assembly = System.Reflection.Assembly.GetExecutingAssembly(); // Corrected with the proper using directive

            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            ListResourceNames();


            var dwgFiles = new Dictionary<string, string>
            {
                ["totalsondering"] = "Fargemannen.Resources.Totalsondering.dwg",
                ["cpt"] = "Fargemannen.Resources.CPT.dwg",
                ["dreietrykksondering"] = "Fargemannen.Resources.Dreietrykksondering.dwg",
                ["prøveserie"] = "Fargemannen.Resources.prøveserie.dwg",
                ["poretrykkmåling"] = "Fargemannen.Resources.poretrykksmåler.dwg",
                ["vingeboring"] = "Fargemannen.Resources.vingeboring.dwg",
                ["dreiesondering"] = "Fargemannen.Resources.dreiesondering.dwg",
                ["prøvegrop"] = "Fargemannen.Resources.prøvegrop.dwg",
                ["fjellkontrollboring"] = "Fargemannen.Resources.fjellkontrollboring.dwg",
                ["ramsondering"] = "Fargemannen.Resources.ramsondering.dwg",
                ["enkel"] = "Fargemannen.Resources.enkelSondering.dwg",
                ["fjellidagen"] = "Fargemannen.Resources.fjellIDagen.dwg",
                ["stans"] = "Fargemannen.Resources.stans.dwg",
                ["stack_dreiesondering"] = "Fargemannen.Resources.STACK_dreiesondering.dwg",
                ["stack_dreietrykksondering"] = "Fargemannen.Resources.STACK_Dreietrykksondering.dwg",
                ["stack_cpt"] = "Fargemannen.Resources.STACK_CPT.dwg",
                ["stack_vingeboring"] = "Fargemannen.Resources.STACK_vingeboring.dwg",
                ["stack_ramsondering"] = "Fargemannen.Resources.STACK_ramsondering.dwg",
                ["stack_prøveserie"] = "Fargemannen.Resources.STACK_prøveserie.dwg",
                ["stack_prøvegrop"] = "Fargemannen.Resources.STACK_prøvegrop.dwg",
                ["stack_poretrykkmåling"] = "Fargemannen.Resources.STACK_poretrykksmåler.dwg",
                ["stack_fjellkontrollboring"] = "Fargemannen.Resources.STACK_fjellkontrollboring.dwg",
                ["stack_enkel"] = "Fargemannen.Resources.STACK_enkelSondering.dwg",
                
            };

           
                using (var tr = db.TransactionManager.StartTransaction())
                using (doc.LockDocument())
                {
                    var layerSymbol = "Symboler";
                    var layerSymbolStansILos = "Symboler for stans i løs";

                    SjekkOgOpprettLag(db, tr, layerSymbol,doc, selectedColor);
                    SjekkOgOpprettLag(db, tr, layerSymbolStansILos,doc, selectedColor_minBerg);

                    var databases = new Dictionary<string, Database>();
                    var blockRefs = new Dictionary<string, ObjectId>();

                    foreach (var file in dwgFiles)
                    {
                        using (Stream stream = assembly.GetManifestResourceStream(file.Value))
                        {
                            if (stream == null)
                                throw new FileNotFoundException($"Innebygd DWG-fil '{file.Value}' ble ikke funnet.");

                            // Create a temporary file to write the stream into
                            string tempFile = System.IO.Path.GetTempFileName();
                            using (var fileStream = File.Create(tempFile))
                            {
                                stream.CopyTo(fileStream);  
                            }

                            databases[file.Key] = new Database(false, false);
                            databases[file.Key].ReadDwgFile(tempFile, FileShare.Read, true, ""); 
                            blockRefs[file.Key] = db.Insert(tempFile, databases[file.Key], false);

                            //File.Delete(tempFile); // Clean up the temp file
                        }
                    }

                    foreach (var punkt in pointsToSymbol)
                    {
                        ProcessPunkt(punkt, metoder, minGrenseBerg, blockRefs, db, tr, layerSymbol, layerSymbolStansILos);
                    }

                    tr.Commit();
                        ed.Regen();
                    }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception acEx)
            {
                // Sporer AutoCAD-spesifikke unntak
                telemetryClient.TrackException(acEx, new Dictionary<string, string> {
            {"Message", "AutoCAD Runtime Exception in SymbolModel.test"},
            {"Method", "SymbolModel.test"},
            {"Severity", "High"}
        });
                System.Windows.MessageBox.Show($"AutoCAD Runtime-feil: {acEx.Message}", "Runtime Feil", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.Exception ex)
            {
                // Sporer generelle unntak
                telemetryClient.TrackException(ex, new Dictionary<string, string> {
            {"Message", "Unexpected Exception in SymbolModel.test"},
            {"Method", "SymbolModel.test"},
            {"Severity", "Critical"}
        });
                System.Windows.MessageBox.Show($"En uventet feil oppstod: {ex.Message}", "System Feil", MessageBoxButton.OK, MessageBoxImage.Error);
            }

         
        }

        private static void ProcessPunkt(PunktInfo punkt, List<string> metoder, double minGrenseBerg, Dictionary<string, ObjectId> blockRefs, Database db, Transaction tr, string layerSymbol, string layerSymbolStansILos)
        {

            var sonderingType = punkt.GBUMetode.ToLower();

            if (!blockRefs.ContainsKey(sonderingType))
            {
                sonderingType = "enkel";
            }
                


            Point3d plasseringsPunkt = new Point3d(punkt.Punkt.X, punkt.Punkt.Y, punkt.Punkt.Z);



            ObjectId gjelendeKurve = blockRefs[sonderingType];
            ObjectId stansSymbol = blockRefs["stans"];
            string symbolID = punkt.RapportID + "_" + punkt.PunktID;

            double borIBerg = punkt.BoreFjell;


            using (var btr = new BlockTableRecord())
            {
                btr.Name = $"Punkt_{punkt.MinPunktID}";
                if (string.IsNullOrWhiteSpace(btr.Name))
                    throw new ArgumentException("Navnet på blokk tabell record er tomt eller inneholder bare hvite tegn.", nameof(btr.Name));

                try
                {
                    InsertSymbolAndText(plasseringsPunkt, gjelendeKurve, stansSymbol, punkt, tr, db, layerSymbol, layerSymbolStansILos, minGrenseBerg, symbolID, borIBerg, blockRefs);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    System.Windows.MessageBox.Show($"En AutoCAD-feil oppstod: {ex.Message}", "AutoCAD Feil", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }

        }



        private static void InsertSymbolAndText(Point3d placementPoint, ObjectId gjeldendeKurve, ObjectId stansSymbol, PunktInfo punkt, Transaction tr, Database db, string layerSymbol, string layerSymbolStansILos, double minGrenseBerg, string symbolID, double borIBerg, Dictionary<string, ObjectId> blockRefs)
        {
            try
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);

                if (bt == null)
                {
                    throw new InvalidOperationException("Kan ikke åpne BlockTable fra databasen.");
                }

                var btr = new BlockTableRecord();
                btr.Name = $"Punkt_{punkt.MinPunktID}";

                GenerteSymbol.Add(btr.Name);

                if (bt.Has(btr.Name))
                {
                    throw new InvalidOperationException($"En blokk med navn '{btr.Name}' finnes allerede.");
                }

                // Insert the main symbol and text
                BlockReference symbolRef = new BlockReference(placementPoint, gjeldendeKurve)
                {
                    Layer = punkt.BoreFjell < minGrenseBerg ? layerSymbolStansILos : layerSymbol
                };

                DBText textEntityGroundQuote = new DBText
                {
                    Position = new Point3d(placementPoint.X + 1.8, placementPoint.Y + 0.12, placementPoint.Z),
                    TextString = punkt.Terrengkvote.ToString("F2"),
                    Height = 0.9
                };

                DBText textEntitySymbolID = new DBText
                {
                    Position = new Point3d(placementPoint.X - 1.8, placementPoint.Y - 0.5, placementPoint.Z),
                    TextString = symbolID,
                    Height = 1.2,
                    Justify = AttachmentPoint.BaseRight,
                    AlignmentPoint = new Point3d(placementPoint.X - 1.8, placementPoint.Y - 0.7, placementPoint.Z)
                };

                btr.AppendEntity(symbolRef);
                btr.AppendEntity(textEntityGroundQuote);
                btr.AppendEntity(textEntitySymbolID);
                btr.Origin = placementPoint;

                if (punkt.GBUMetode.ToLower() != "fjellidagen")
                {
                    DBText textEntityLooseRockFirmRock = new DBText
                    {
                        Position = new Point3d(placementPoint.X + 6, placementPoint.Y - 0.3, placementPoint.Z),
                        TextString = $"{punkt.BorLøs:F2}+{punkt.BoreFjell:F2}",
                        Height = 0.9
                    };

                    btr.AppendEntity(textEntityLooseRockFirmRock);

                    if (borIBerg < minGrenseBerg)
                    {
                        Point3d plasseringStansSymbol = new Point3d(placementPoint.X + 3.2, placementPoint.Y - 0.9, placementPoint.Z);
                        btr.AppendEntity(new BlockReference(plasseringStansSymbol, stansSymbol));
                    }
                    else
                    {
                        DBText tekstEntitet_Fjellkvote = new DBText()
                        {
                            Position = new Point3d(placementPoint.X + 1.8, placementPoint.Y - 1.12, placementPoint.Z),
                            TextString = punkt.Punkt.Z.ToString("F2"),
                            Height = 0.9
                        };

                        btr.AppendEntity(tekstEntitet_Fjellkvote);
                    }
                }

                // Handle stack bor symbols (only symbols, no text)
                if (punkt.StackBor != null && punkt.StackBor.Any())
                {
                    for (int i = 0; i < punkt.StackBor.Count; i++)
                    {
                        Point3d stackPlacementPoint = new Point3d(placementPoint.X, placementPoint.Y + 3.2 * (i + 1), placementPoint.Z);

                        // Determine the symbol type for stack bor
                        string stackSonderingType = punkt.StackBor[i].ToLower();
                        if (!blockRefs.ContainsKey(stackSonderingType))
                        {
                            stackSonderingType = "enkel";
                        }
                        ObjectId stackGjeldendeKurve = blockRefs[stackSonderingType];

                        BlockReference stackSymbolRef = new BlockReference(stackPlacementPoint, stackGjeldendeKurve)
                        {
                            Layer = punkt.BoreFjell < minGrenseBerg ? layerSymbolStansILos : layerSymbol
                        };

                        btr.AppendEntity(stackSymbolRef);
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
                    ScaleFactors = new Scale3d(Model.SymbolHandlers.Scale),
                    Rotation = Model.SymbolHandlers.rotation,
                };

                modelSpace.AppendEntity(newBlockRef);
                tr.AddNewlyCreatedDBObject(newBlockRef, true);
            }
            catch (System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"En feil oppstod: {ex.Message}", "Feil", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        public static void SjekkOgOpprettLag(Database db, Transaction tr, string layerName, Document doc, System.Windows.Media.Color farge)
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

        public static void SlettAlleBlokkerOgLag()
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
                          
                        }

                    }

                    // Delete blocks
                    foreach (var block in blocksToDelete)
                    {
                        if (!block.IsErased)
                        {
                            block.UpgradeOpen();
                            block.Erase();
                           
                        }

                    }

                    tr.Commit();
                   
                }

                
            }
        }
    }



}


