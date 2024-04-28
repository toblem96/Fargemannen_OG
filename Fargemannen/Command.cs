using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Civil.Runtime;

using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil;
using Autodesk.Civil.DataShortcuts;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.AutoCAD.Runtime;

using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using System.Windows.Interop;
using Autodesk.AutoCAD.DatabaseServices;
using Microsoft.ApplicationInsights;


using Autodesk.AutoCAD.Geometry;
using Fargemannen.ViewModel;




[assembly: CommandClass(typeof(Fargemannen.MyCommands))]
namespace Fargemannen
{
    public class MyCommands
    {   

        public static List<PunktInfo> symbolSjekk = new List<PunktInfo>();


        [CommandMethod("AnalyseZ")]
        public void HeloWorld()
        {
            // Oppretter en ny instans av MainWinSymbol Window
            WinBergModell bergWindow = new WinBergModell();

            // Oppretter en WindowInteropHelper for å sette eieren av WPF-vinduet
            WindowInteropHelper helper = new WindowInteropHelper(bergWindow)
            {
                Owner = Application.MainWindow.Handle
            };

            // Viser WPF-vinduet
            bergWindow.Show();
        }




        [CommandMethod("Symboler")]
        public void Symboler()
        {
            // Oppretter en ny instans av MainWinSymbol Window
            Symboler.WinSymbol symbolWindow = new Symboler.WinSymbol();

            // Oppretter en WindowInteropHelper for å sette eieren av WPF-vinduet
            WindowInteropHelper helper = new WindowInteropHelper(symbolWindow)
            {
                Owner = Application.MainWindow.Handle
            };

            // Viser WPF-vinduet
            symbolWindow.Show();
        }
        [CommandMethod("AnalyseXY")]
        public void AnalyseXY() 
        {

            WinAnalyseXY winAnalyseXY = new WinAnalyseXY();

            WindowInteropHelper helper = new WindowInteropHelper(winAnalyseXY)
            {
                Owner = Application.MainWindow.Handle
            };
            winAnalyseXY.Show();
        }

        [CommandMethod("Rapport")]
        public void Rapport()
        {

            GenRapport GenRapport = new GenRapport();

            WindowInteropHelper helper = new WindowInteropHelper(GenRapport)
            {
                Owner = Application.MainWindow.Handle
            };
            GenRapport.Show();
        }



        [CommandMethod("Data")]
        public void Data()
        {

            DataHenter DataHenter = new DataHenter();

            WindowInteropHelper helper = new WindowInteropHelper(DataHenter)
            {
                Owner = Application.MainWindow.Handle
            };
            DataHenter.Show();
        }

        [CommandMethod("Fargemannen")]
        public void KjørFargemannen()
        {

            View.MainWindow MainWindow = new View.MainWindow();

            WindowInteropHelper helper = new WindowInteropHelper(MainWindow)
            {
                Owner = Application.MainWindow.Handle
            };
                MainWindow.Show();
        }



        


        [CommandMethod("PDFBRILLER")]
        public void HentBlockNavn()
        {
            Fargemannen.ApplicationInsights.AppInsights.TrackEvent("HentBlockNavn Used");


            if (FileUploadViewModel.Instance.ReportFiles.Count == 0 && FileUploadViewModel.Instance.SampleResultFiles.Count == 0)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Ingen PDF-mapper er lagt til");
                return;
            }

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Forespør brukeren om å selektere et objekt
            PromptSelectionResult result = ed.GetSelection();
            if (result.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nIngen objekter ble valgt.");
                return;
            }

            SelectionSet set = result.Value;
            using (Transaction trans = doc.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject obj in set)
                {
                    Autodesk.AutoCAD.DatabaseServices.DBObject dbObj = trans.GetObject(obj.ObjectId, OpenMode.ForRead);
                    if (dbObj is BlockReference blockRef)
                    {
                        BlockTableRecord blockDef = trans.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                        string[] navnDeler = blockDef.Name.Split('_');

                        if (navnDeler.Length > 1)
                        {
                            string punktNummer = navnDeler[1];
                            ed.WriteMessage($"\nSjekker punkt nummer: {punktNummer}");

                            if (Model.ProsseseringAvFiler.PunkterPerPdf.TryGetValue(punktNummer, out Tuple<string, string> filbaner))
                            {
                                string rapportFilbane = filbaner.Item1;
                                string prøveFilbane = filbaner.Item2;

                                ed.WriteMessage($"\nRapport Filbane: {rapportFilbane}, Prøve Filbane: {prøveFilbane}");

                                if (FileUploadViewModel.Instance.ReportFiles.TryGetValue(rapportFilbane, out string fullRapportPath))
                                {
                                    System.Diagnostics.Process.Start(fullRapportPath);
                                    ed.WriteMessage($"\nÅpner rapport PDF: {fullRapportPath}");
                                }
                                else
                                {
                                    ed.WriteMessage("\nFant ikke rapport PDF stien.");
                                }

                                if (FileUploadViewModel.Instance.SampleResultFiles.TryGetValue(prøveFilbane, out string fullPrøvePath))
                                {
                                    System.Diagnostics.Process.Start(fullPrøvePath);
                                    ed.WriteMessage($"\nÅpner prøveresultat PDF: {fullPrøvePath}");
                                }
                                else
                                {
                                    ed.WriteMessage("\nFant ikke prøveresultat PDF stien.");
                                }
                            }
                            else
                            {
                                ed.WriteMessage("\nIngen filer funnet for dette punktet i PDF-databasen.");
                            }
                        }
                        else
                        {
                            ed.WriteMessage("\nBlokken har ikke et gyldig navn for oppdeling.");
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\nDet selekterte objektet er ikke en blokkreferanse.");
                    }
                }
                trans.Commit();
            }
        }




        [CommandMethod("sjekkEXCEL")]
      public void sjekkboring()
        {

            EXsjekk.kjørTestRapport();




        }
        [CommandMethod("DukPåMeshXYY")]
        public void LagDukXY()
        {
            string metode = "XY";

            DukPåMesh.KjørDukPåMesh(metode);




        }
        [CommandMethod("DukPåMeshZ")]
        public void LagDukZ()
        {

            string metode = "Z";
            DukPåMesh.KjørDukPåMesh(metode);




        }




    }
}
