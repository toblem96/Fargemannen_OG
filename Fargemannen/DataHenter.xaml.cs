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
using System.Windows.Forms;

namespace Fargemannen
{
    /// <summary>
    /// Interaction logic for DataHenter.xaml
    /// </summary>
    public partial class DataHenter : Window
    {
        public DataHenter()
        {
            InitializeComponent();
        }

        public static string FP_SosiIDagen;
        public static string FP_SosiBor;
        public static string FP_Kof;

        public static List<string> FP_Tot { get; } = new List<string>();
        public static Dictionary<string, string> FilStierPDF { get; private set; } = new Dictionary<string, string>();


        private void Button_VelgSosiFilBor(object sender, RoutedEventArgs e)
        {
            //Setter opp ny fildialg som brukes for å åpne en ny filvaglgmeny, som er innebygd.
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();

            //Filter etter en spesiellt øsnkse 
            fileDialog.Filter = "SOSI-filer | *.SOS";

            //Lagerer reultet av brukerens handling inn i resultat
            bool? succsess = fileDialog.ShowDialog();

            //hvis det er handling
            if (succsess == true)
            {
                // Brukeren valgte en fil og trykket OK
                FP_SosiBor = fileDialog.FileName;

                //ProsseseringAvFiler.PDFpross(FP_SosiBor);
                //Viser hvilken filer som er lastet opp og hjør det sånn at bare filvanvnet vises 
                string filename = fileDialog.SafeFileName;
                InfoSosiBor.Text = filename;

            }
            else
            {
                //Fikk ikke noe filepath
            }
        }



        //BUTTON --> FP_SosiIDagen
        private void Button_VelgSosiFilIDagen(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();

            fileDialog.Filter = "SOSI-filer | *.SOS";

            bool? succsess = fileDialog.ShowDialog();

            if (succsess == true)
            {
                FP_SosiIDagen = fileDialog.FileName;

                string filename = fileDialog.SafeFileName;
                InfoIDagen.Text = filename;

            }
            else
            {

            }
        }



        //BUTTON --> FP_Tot
        private void Button_VelgTotFil(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "TOT-filer | *.TOT",
                Multiselect = true
            };

            bool? success = fileDialog.ShowDialog();

            if (success == true)
            {
                FP_Tot.Clear(); // Forsikre deg om at listen er tom før du legger til nye filer
                foreach (string fileName in fileDialog.FileNames)
                {
                    FP_Tot.Add(fileName);
                }

                // Oppdater InfoTot TextBlock med navnene på de valgte filene
                InfoTot.Text = string.Join(Environment.NewLine, FP_Tot.Select(System.IO.Path.GetFileName));
            }
            else
            {
                // Valgfritt: Oppdater InfoTot med en melding om at ingen filer ble valgt
                InfoTot.Text = "Ingen filer ble valgt.";
            }
        }



        //BUTTON --> FP_Kof
        private void Button_VelgKofFil(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "KOF-filer | *.KOF"
            };

            bool? success = fileDialog.ShowDialog();

            if (success == true)
            {
                FP_Kof = fileDialog.FileName;  // Rettet variabelen til å sette KOF-filens sti

                string filename = fileDialog.SafeFileName;
                InfoKof.Text = filename;
            }
            else
            {

            }
        }


      
       

        private void Button_VelgPDF(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            {
                FilStierPDF.Clear(); // Tømmer Dictionary før ny bruk
                string[] filer = Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.pdf");

                foreach (var fil in filer)
                {
                    string filnavnUtenUtvidelse = System.IO.Path.GetFileNameWithoutExtension(fil);
                    FilStierPDF[filnavnUtenUtvidelse] = fil;
                }


            }
        }


        private void Button_Avlustt(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
