using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using System.IO;


namespace Fargemannen
{
    /// <summary>
    /// Interaction logic for WinBegModell.xaml
    /// </summary>
    public partial class WinBergModell : Window
    {

        //Generelt
        private List<string> boreMetoder = new List<string>();
        private int minAr;
        public static double ruterStrZ;
        
        public WinBergModell()
        {
            InitializeComponent();

      
        }

        //TEXTBOX FOR Å HENTE UT_MIN AR
        private void TextBox_MinAr(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                if (int.TryParse(textBox.Text, out int parsedValue))
                {
                    // Her kan du gjøre noe med den parsede verdien, om nødvendig
                    // For eksempel, oppdatere en variabel eller tilstand innad i applikasjonen
                    minAr = parsedValue;
                }
                else
                {
                    // Om du trenger å håndtere en feil hvor teksten ikke er et gyldig tall,
                    // for eksempel, tilbakestille til en standardverdi eller lignende, kan det gjøres her
                    // minAr = defaultValue; // Sett til standardverdi eller lignende
                }
            }
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


        private void TextBox_Str(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                if (double.TryParse(textBox.Text, out double parsedValue))
                {
                    // Her kan du gjøre noe med den parsede verdien, om nødvendig
                    // For eksempel, oppdatere en variabel eller tilstand innad i applikasjonen
                    ruterStrZ = parsedValue;
                }
                else
                {
                    // Om du trenger å håndtere en feil hvor teksten ikke er et gyldig tall,
                    // for eksempel, tilbakestille til en standardverdi eller lignende, kan det gjøres her
                    // minAr = defaultValue; // Sett til standardverdi eller lignende
                }
            }

        }

        private void OpenNewWindow_Click(object sender, RoutedEventArgs e)
        {
            SecondWinAnalyseZ secondWindow = new SecondWinAnalyseZ
            {
                Owner = this, // Setter dette vinduet som eier av det nye vinduet
                WindowStartupLocation = WindowStartupLocation.CenterOwner // Sentrerer det nye vinduet over eieren
            };
            secondWindow.Show(); // Viser det nye vinduet som en modal dialog
        }


        private void TegnFirkant_Click(object sender, RoutedEventArgs e)
        {
            AnalyseZ.Start();
        }

     
        //KNAP FOR Å STARTE PROSSESEN 
        private void Button_GenererBergmodell(object sender, RoutedEventArgs e)
        { }
         /*
        {
            

            // Opprette listen hvor resultater vil bli lagt til
            List<PunktInfo> pointsToSymbol = new List<PunktInfo>();
            List<Point3d> punkterMesh = new List<Point3d>();

            int antallPunkterSosiBor = 0;
            int antallPunkterSosiIDagen = 0;
            int antallPunkterKofTot = 0;

            // Få referanse til det aktive dokumentet
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

            // Opprett en instans av din SOSIFileProcessor_Boring
            var processor = new Fargemannen.ProsseseringAvFiler();

    
             processor.ProcessSOSIFilesBor(minAr, boreMetoder, DataHenter.FP_SosiBor, pointsToSymbol, punkterMesh);
    

            antallPunkterSosiBor = punkterMesh.Count;

         
            processor.ProcessSOSIFilesIDagen(minAr, boreMetoder, DataHenter.FP_SosiIDagen, pointsToSymbol, punkterMesh);
      
              
            

            antallPunkterSosiIDagen = punkterMesh.Count - antallPunkterSosiBor;

           
           processor.ProcessTOTandKOFFiles(DataHenter.FP_Tot, DataHenter.FP_Kof, pointsToSymbol, punkterMesh);
        

            antallPunkterKofTot = punkterMesh.Count - antallPunkterSosiBor - antallPunkterSosiIDagen;



            Rapport.AntallBoringerBergmodell = punkterMesh.Count;
            Rapport.BoreMotoderBergmodell = boreMetoder;
            Rapport.minAr = minAr;

            EXsjekk.symbolSjekk = pointsToSymbol;

            DukPåMesh.punkterForAnalyse = pointsToSymbol;
            GenererMeshFraPunkter(punkterMesh, antallPunkterSosiBor, antallPunkterSosiIDagen, antallPunkterKofTot);

                */
        }

    /*
    public void GenererMeshFraPunkter(List<Point3d> punkterMesh, int antallPunkterSosiBor, int antallPunkterSosiIDagen, int antallPunkterKofTot)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;

            using (DocumentLock docLock = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    string lagNavn = "Bergmodell";
                    LayerTableRecord layerRecord;

                    if (!layerTable.Has(lagNavn))
                    {
                        layerTable.UpgradeOpen();
                        layerRecord = new LayerTableRecord
                        {
                            Name = lagNavn
                        };

                        layerTable.Add(layerRecord);
                        tr.AddNewlyCreatedDBObject(layerRecord, true);
                    }
                    else
                    {
                        layerRecord = tr.GetObject(layerTable[lagNavn], OpenMode.ForWrite) as LayerTableRecord;
                    }

                    ObjectId tinSurfaceId = TinSurface.Create(db, lagNavn);
                    TinSurface tinSurface = tr.GetObject(tinSurfaceId, OpenMode.ForWrite) as TinSurface;

                    foreach (Point3d punkt in punkterMesh)
                    {
                        // Logger detaljer om hvert punkt som legges til
                        doc.Editor.WriteMessage($"\nLegger til punkt: X={punkt.X}, Y={punkt.Y}, Z={punkt.Z}");
                        tinSurface.AddVertex(punkt);
                    }

                    // Rebuild surface for å oppdatere meshet med de nye punktene
                    tinSurface.Rebuild();
                    tinSurface.LayerId = layerRecord.ObjectId;

                    tr.Commit();
                }
            }

            int totaltAntallPunkter = antallPunkterSosiBor + antallPunkterSosiIDagen + antallPunkterKofTot;
            string melding = $"Totalt antall punkter brukt: {totaltAntallPunkter}.\n" +
                             $"- SOSI Borehull: {antallPunkterSosiBor}\n" +
                             $"- SOSI i Dagen: {antallPunkterSosiIDagen}\n" +
                             $"- KOF/TOT: {antallPunkterKofTot}";

            doc.Editor.WriteMessage("\n" + melding);
        }

        */
}

