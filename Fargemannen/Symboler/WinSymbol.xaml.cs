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
using Autodesk.AutoCAD.EditorInput;


using Autodesk.AutoCAD.Runtime;

using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Windows.Media.Animation;
using System.Security.Cryptography;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.AutoCAD.AcSeamless;
namespace Fargemannen.Symboler
{
    /// <summary>
    /// Interaction logic for WinSymbol.xaml
    /// </summary>
    public partial class WinSymbol : Window
    {

        private List<string> boreMetoder_1 = new List<string>();
        private int minAr;
        private double minBor;
       
        public WinSymbol()
        {
            InitializeComponent();
        }


        private void OpenNewWindow_Click(object sender, RoutedEventArgs e)
        {
            SecondWinSymbol secondWindow = new SecondWinSymbol
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
            if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text)) 
            {
                if (!int.TryParse(textBox.Text, out int parsedValue))
                {
                    MessageBox.Show("Vennligst oppgi et gyldig heltall.", "Ugyldig Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Background = System.Windows.Media.Brushes.LightPink; 
                }
                else
                {
                    minAr = parsedValue;
                    textBox.Background = System.Windows.Media.Brushes.White; 
                }
            }
            else
            {
                textBox.Background = System.Windows.Media.Brushes.White;
            }

        }


        private void MinBorFjell(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (!int.TryParse(textBox.Text, out int parsedValue))
                {
                    MessageBox.Show("Vennligst oppgi et gyldig heltall.", "Ugyldig Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Background = System.Windows.Media.Brushes.LightPink;
                }
                else
                {
                    minBor = parsedValue;
                    textBox.Background = System.Windows.Media.Brushes.White; 
                }
            }
            else
            {
                textBox.Background = System.Windows.Media.Brushes.White;
            }
        }



        //LASTER OPP ALLE CHECKBOXENE TIL EN STRINGLISTE
        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            //Konventerer sender til CheckBox
            CheckBox chk = sender as CheckBox;

            //Sjekker at koventeringen var vellyket, og at noe er sjekket
            if (chk != null && chk.IsChecked == true)
            {
                //Legger til i boreMetoder
                boreMetoder_1.Add(chk.Tag.ToString());

                //Ser at det ligger noe i listen 
              

            }
        }

        //FJERNER ALLE IKKESJEKKEDE BOKSER FRA LISTEN 
        private void CheckBox_Unchecked_1(object sender, RoutedEventArgs e)
        {
            CheckBox chk = sender as CheckBox;
            if (chk != null)
            {
                boreMetoder_1.Remove(chk.Tag.ToString());
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
          

            // Opprette listen hvor resultater vil bli lagt til
            List<PunktInfo> pointsToSymbol = new List<PunktInfo>();
            List<Point3d> punkterMesh = new List<Point3d>();

 /*

            // Få referanse til det aktive dokumentet
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

            // Opprett en instans av din SOSIFileProcessor_Boring
            var processor = new Fargemannen.ProsseseringAvFiler();

            try
            {
                // Kall metoden med de nødvendige verdiene
                processor.ProcessSOSIFilesBor(minAr, boreMetoder_1, DataHenter.FP_SosiBor, pointsToSymbol, punkterMesh);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                // Skriv feilmelding til AutoCADs kommandolinje
                doc.Editor.WriteMessage($"\nFeil ved prosessering av SOSI boringsfiler: {ex.Message}");
            }

            try
            {
                processor.ProcessSOSIFilesIDagen(minAr, boreMetoder_1, DataHenter.FP_SosiIDagen, pointsToSymbol, punkterMesh);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                doc.Editor.WriteMessage($"\nFeil ved prosessering av SOSI i dagens filer: {ex.Message}");
            }

            try
            {
                processor.ProcessTOTandKOFFiles(DataHenter.FP_Tot, DataHenter.FP_Kof, pointsToSymbol, punkterMesh);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                doc.Editor.WriteMessage($"\nFeil ved prosessering av TOT og KOF filer: {ex.Message}");
            }

            Rapport.MinGrenseIFjell = minBor;
            MakeringAvBerg.minBor = minBor;

            PrintValgtBoring(pointsToSymbol);
            SymbolProsseser symbolProsseser = new SymbolProsseser();

            
            symbolProsseser.test(pointsToSymbol, boreMetoder_1, minBor);

      



        }


        public void PrintValgtBoring(List<PunktInfo> pointsToSymbol) 
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            Dictionary<string,int > boringCount = new Dictionary<string,int>();
            var processor = new Fargemannen.ProsseseringAvFiler();

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

            // Utlisting av boreMetoder_1
            ed.WriteMessage("\nInnhold i boreMetoder_1:\n");
            foreach (var metode in boreMetoder_1)
            {
                ed.WriteMessage($"{metode}\n");
            }
            */
        }
    }
}

        