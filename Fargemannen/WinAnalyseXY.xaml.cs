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
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;

using Microsoft.Win32;
using Autodesk.AutoCAD.Geometry;
using System.IO;
using Autodesk.AutoCAD.EditorInput;




using Autodesk.AutoCAD.Runtime;

using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Windows.Media.Animation;
using System.Security.Cryptography;

namespace Fargemannen
{
    /// <summary>
    /// Interaction logic for WinAnalyseXY.xaml
    /// </summary>
    public partial class WinAnalyseXY : Window
    {
        public WinAnalyseXY()
        {
            InitializeComponent();
        }

        private List<string> boreMetoder = new List<string>();
        private int minAr;
        public static double ruterStr;
        public static List<Point3d> punkterAnalyseXY = new List<Point3d>();


        private void OpenNewWindow_Click(object sender, RoutedEventArgs e)
        {
            SecondWinAnalyseXY secondWindow = new SecondWinAnalyseXY
            {
                Owner = this, // Setter dette vinduet som eier av det nye vinduet
                WindowStartupLocation = WindowStartupLocation.CenterOwner // Sentrerer det nye vinduet over eieren
            };
            secondWindow.Show(); // Viser det nye vinduet som en modal dialog
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
                    ruterStr = parsedValue;
                }
                else
                {
                    // Om du trenger å håndtere en feil hvor teksten ikke er et gyldig tall,
                    // for eksempel, tilbakestille til en standardverdi eller lignende, kan det gjøres her
                    // minAr = defaultValue; // Sett til standardverdi eller lignende
                }
            }

        }

        private void TegnFirkant_Click(object sender, RoutedEventArgs e) { }




        /*
        private void TegnFirkant_Click(object sender, RoutedEventArgs e)
        {

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


            processor.ProcessSOSIFilesIDagen(minAr, boreMetoder, DataHenter.FP_SosiIDagen, pointsToSymbol, punkterMesh);



            processor.ProcessTOTandKOFFiles(DataHenter.FP_Tot, DataHenter.FP_Kof, pointsToSymbol, punkterMesh);




            NullstillZVerdierOgLagreIAnalyseListe(punkterMesh);



            MakeringAvBerg.punktInfos = pointsToSymbol;
            Analyse.Start();

        }


        //KNAP FOR Å STARTE PROSSESEN 
  

        public static void NullstillZVerdierOgLagreIAnalyseListe(List<Point3d> punkterMesh)
        {
            foreach (var punkt in punkterMesh)
            {
                // Setter Z-verdien til 0 og oppretter et nytt Point3d-objekt
                Point3d nyttPunkt = new Point3d(punkt.X, punkt.Y, 0);
                punkterAnalyseXY.Add(nyttPunkt);
            }
        }
        */


    }
}

