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
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace Fargemannen
{
    /// <summary>
    /// Interaction logic for SecondWinAnalyseXY.xaml
    /// </summary>
    public partial class SecondWinAnalyseXY : Window
    {
        public SecondWinAnalyseXY()
        {
            InitializeComponent();
            OppdaterTextBox();
            Intervall.intervallListe.Clear();

         

        }
        private void FerdigButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public int int1_start;
        public int int1_slutt;
        public int int2_start;  
        public int int2_slutt;
        public int int3_start;
        public int int3_slutt;
        public int int4_start;
        public int int4_slutt;
        public int int5_start;
        public int int5_slutt;

        public System.Drawing.Color farge1;
        public System.Drawing.Color farge2;
        public System.Drawing.Color farge3;
        public System.Drawing.Color farge4;
        public System.Drawing.Color farge5;

  




        private void FargeInt1(object sender, RoutedEventArgs e)
        {
            // Opprett en ny instans av ColorDialog.
            ColorDialog colorDialog = new ColorDialog();

            // Vis dialogen og sjekk om brukeren klikker OK.
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Få fargen valgt av brukeren.
                System.Drawing.Color selectedColor = colorDialog.Color;

                // Lagre den valgte fargen i den offentlige variabelen
                farge1 = selectedColor;

                // Konverter fargen til en WPF-brukbar farge
                System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor);
                UpdateLayerColor("Intervall1", selectedColor);
                // Oppdater WPF UI-element med den konverterte fargen
                colorDisplay_1.Fill = new SolidColorBrush(wpfColor);
            }
        }
        private void FargeInt2(object sender, RoutedEventArgs e)
        {
            // Opprett en ny instans av ColorDialog.
            ColorDialog colorDialog = new ColorDialog();

            // Vis dialogen og sjekk om brukeren klikker OK.
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Få fargen valgt av brukeren.
                System.Drawing.Color selectedColor = colorDialog.Color;

                // Lagre den valgte fargen i den offentlige variabelen
                farge2 = selectedColor;

                // Konverter fargen til en WPF-brukbar farge
                System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor);


                UpdateLayerColor("Intervall2", selectedColor);
                // Oppdater WPF UI-element med den konverterte fargen
                colorDisplay_2.Fill = new SolidColorBrush(wpfColor);
            }
        }
        private void FargeInt3(object sender, RoutedEventArgs e)
        {
            // Opprett en ny instans av ColorDialog.
            ColorDialog colorDialog = new ColorDialog();

            // Vis dialogen og sjekk om brukeren klikker OK.
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Få fargen valgt av brukeren.
                System.Drawing.Color selectedColor = colorDialog.Color;

                // Lagre den valgte fargen i den offentlige variabelen
                farge3 = selectedColor;

                // Konverter fargen til en WPF-brukbar farge
                System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor);

                UpdateLayerColor("Intervall3", selectedColor);
                // Oppdater WPF UI-element med den konverterte fargen
                colorDisplay_3.Fill = new SolidColorBrush(wpfColor);
            }
        }
        private void FargeInt4(object sender, RoutedEventArgs e)
        {
            // Opprett en ny instans av ColorDialog.
            ColorDialog colorDialog = new ColorDialog();

            // Vis dialogen og sjekk om brukeren klikker OK.
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Få fargen valgt av brukeren.
                System.Drawing.Color selectedColor = colorDialog.Color;

                // Lagre den valgte fargen i den offentlige variabelen
                farge4 = selectedColor;

                // Konverter fargen til en WPF-brukbar farge
                System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor);

                UpdateLayerColor("Intervall4", selectedColor);

                // Oppdater WPF UI-element med den konverterte fargen
                colorDisplay_4.Fill = new SolidColorBrush(wpfColor);
            }
        }
        private void FargeInt5(object sender, RoutedEventArgs e)
        {
            // Opprett en ny instans av ColorDialog.
            ColorDialog colorDialog = new ColorDialog();

            // Vis dialogen og sjekk om brukeren klikker OK.
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Få fargen valgt av brukeren.
                System.Drawing.Color selectedColor = colorDialog.Color;

                // Lagre den valgte fargen i den offentlige variabelen
                farge5 = selectedColor;

                // Konverter fargen til en WPF-brukbar farge
                System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor);

                UpdateLayerColor("Intervall5", selectedColor);

                // Oppdater WPF UI-element med den konverterte fargen
                colorDisplay_5.Fill = new SolidColorBrush(wpfColor);
            }
        }

        private void BoxInt1_start(object sender, TextChangedEventArgs e)
        {
            
            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int1_start = parsedValue;
            }
        }

        private void BoxInt1_slutt(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int1_slutt = parsedValue;
                boxInt2Start.Text = (parsedValue+1).ToString(); 
                int2_start = parsedValue + 1;
            }
        }
        private void BoxInt2_start(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int2_start = parsedValue;
            }
        }

        private void BoxInt2_slutt(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int2_slutt = parsedValue;

                boxInt3Start.Text = (parsedValue + 1).ToString();
                int3_start = parsedValue+1;
            }
        }
        private void BoxInt3_start(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int3_start = parsedValue;
            }
        }

        private void BoxInt3_slutt(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int3_slutt = parsedValue;

                boxInt4Start.Text = (parsedValue + 1).ToString();
                int4_start = parsedValue + 1;

            }
        }
        private void BoxInt4_start(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int4_start = parsedValue;
            }
        }

        private void BoxInt4_slutt(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int4_slutt = parsedValue;

                boxInt5Start.Text = (parsedValue + 1).ToString();
                int5_start = parsedValue + 1;
            }
        }
        private void BoxInt5_start(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int5_start = parsedValue;
            }
        }

        private void BoxInt5_slutt(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int5_slutt = parsedValue;
            }
        }
        private void OppdaterTextBox()
        {
            MinVerdiTextBox.Text = Analyse.minVerdiXY.ToString();
            AvgVerdiTextBox.Text = Analyse.gjennomsnittVerdiXY.ToString();
            MaxVerdiTextBox.Text = Analyse.maxVerdiXY.ToString();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            try
            {
            Intervall.LeggTilIntervall(int1_start, int1_slutt, farge1, "Intervall1");
            Intervall.LeggTilIntervall(int2_start, int2_slutt, farge2, "Intervall2");
            Intervall.LeggTilIntervall(int3_start, int3_slutt, farge3, "Intervall3");
            Intervall.LeggTilIntervall(int4_start, int4_slutt, farge4, "Intervall4");
            Intervall.LeggTilIntervall(int5_start, int5_slutt, farge5, "Intervall5");


            Analyse.PlasserFirkanterIIntervallLayersOgFyllMedFarge();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Analyse.gjennomsiktighet = (int)e.NewValue;  // Oppdaterer global transparensverdi
            /*
            // Tegningsdokument og databaseobjekter
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Start en transaksjon for å oppdatere lagene
            using (Transaction tr = db.TransactionManager.StartTransaction())
            using (doc.LockDocument())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                foreach (Intervall intervall in Intervall.intervallListe)
                {
                    if (!string.IsNullOrEmpty(intervall.LagNavn) && lt.Has(intervall.LagNavn))
                    {
                        LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[intervall.LagNavn], OpenMode.ForWrite);
                        byte transparencyValue = (byte)(255 * (1 - Analyse.gjennomsiktighet / 100.0));
                        ltr.Transparency = new Transparency(transparencyValue);
                        
                    }
                }

                tr.Commit();
                doc.Editor.Regen();
            }
            
            Autodesk.AutoCAD.ApplicationServices.Application.UpdateScreen();  
            */
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Legend legend = new Legend();
            legend.VelgFirkant();
        }

        private void UpdateLayerColor(string layerName, System.Drawing.Color selectedColor)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            using(doc.LockDocument())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                if (lt.Has(layerName))
                {
                    LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);
                    ltr.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(selectedColor.R, selectedColor.G, selectedColor.B);
                    tr.Commit();
                    
                }
                else
                {
                   
                }
            }
            doc.Editor.Regen();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MakeringAvBerg.KjørMakeringAvBerg();
        }
    }
}