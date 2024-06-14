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

    }
}
