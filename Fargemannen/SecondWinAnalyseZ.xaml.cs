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
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
namespace Fargemannen
{
    /// <summary>
    /// Interaction logic for SecondWinAnalyseZ.xaml
    /// </summary>
    public partial class SecondWinAnalyseZ : Window
    {
        public SecondWinAnalyseZ()
        {
            InitializeComponent();
            OppdaterTextBoxZ();
            Intervall.intervallListeZ.Clear();
        }

        private void FerdigButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public int int1_startZ;
        public int int1_sluttZ;
        public int int2_startZ;
        public int int2_sluttZ;
        public int int3_startZ;
        public int int3_sluttZ;
        public int int4_startZ;
        public int int4_sluttZ;
        public int int5_startZ;
        public int int5_sluttZ;

        public System.Drawing.Color farge1Z;
        public System.Drawing.Color farge2Z;
        public System.Drawing.Color farge3Z;
        public System.Drawing.Color farge4Z;
        public System.Drawing.Color farge5Z;


        private void FargeInt1Z(object sender, RoutedEventArgs e)
        {
            // Opprett en ny instans av ColorDialog.
            ColorDialog colorDialog = new ColorDialog();

            // Vis dialogen og sjekk om brukeren klikker OK.
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Få fargen valgt av brukeren.
                System.Drawing.Color selectedColor = colorDialog.Color;

                // Lagre den valgte fargen i den offentlige variabelen
                farge1Z = selectedColor;

                // Konverter fargen til en WPF-brukbar farge
                System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor);
                UpdateLayerColor("Intervall1_z", selectedColor);
                // Oppdater WPF UI-element med den konverterte fargen
                colorDisplay_1.Fill = new SolidColorBrush(wpfColor);
            }
        }
        private void FargeInt2Z(object sender, RoutedEventArgs e)
        {
            // Opprett en ny instans av ColorDialog.
            ColorDialog colorDialog = new ColorDialog();

            // Vis dialogen og sjekk om brukeren klikker OK.
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Få fargen valgt av brukeren.
                System.Drawing.Color selectedColor = colorDialog.Color;

                // Lagre den valgte fargen i den offentlige variabelen
                farge2Z = selectedColor;

                // Konverter fargen til en WPF-brukbar farge
                System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor);

                UpdateLayerColor("Intervall2_z", selectedColor);
                // Oppdater WPF UI-element med den konverterte fargen
                colorDisplay_2.Fill = new SolidColorBrush(wpfColor);
            }
        }
        private void FargeInt3Z(object sender, RoutedEventArgs e)
        {
            // Opprett en ny instans av ColorDialog.
            ColorDialog colorDialog = new ColorDialog();

            // Vis dialogen og sjekk om brukeren klikker OK.
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Få fargen valgt av brukeren.
                System.Drawing.Color selectedColor = colorDialog.Color;

                // Lagre den valgte fargen i den offentlige variabelen
                farge3Z = selectedColor;

                // Konverter fargen til en WPF-brukbar farge
                System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor);

                UpdateLayerColor("Intervall3_z", selectedColor);

                // Oppdater WPF UI-element med den konverterte fargen
                colorDisplay_3.Fill = new SolidColorBrush(wpfColor);
            }
        }
        private void FargeInt4Z(object sender, RoutedEventArgs e)
        {
            // Opprett en ny instans av ColorDialog.
            ColorDialog colorDialog = new ColorDialog();

            // Vis dialogen og sjekk om brukeren klikker OK.
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Få fargen valgt av brukeren.
                System.Drawing.Color selectedColor = colorDialog.Color;

                // Lagre den valgte fargen i den offentlige variabelen
                farge4Z = selectedColor;

                // Konverter fargen til en WPF-brukbar farge
                System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor);

                UpdateLayerColor("Intervall4_z", selectedColor);

                // Oppdater WPF UI-element med den konverterte fargen
                colorDisplay_4.Fill = new SolidColorBrush(wpfColor);
            }
        }
        private void FargeInt5Z(object sender, RoutedEventArgs e)
        {
            // Opprett en ny instans av ColorDialog.
            ColorDialog colorDialog = new ColorDialog();

            // Vis dialogen og sjekk om brukeren klikker OK.
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Få fargen valgt av brukeren.
                System.Drawing.Color selectedColor = colorDialog.Color;

                // Lagre den valgte fargen i den offentlige variabelen
                farge5Z = selectedColor;

                // Konverter fargen til en WPF-brukbar farge
                System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor);

                UpdateLayerColor("Intervall5_z", selectedColor);

                // Oppdater WPF UI-element med den konverterte fargen
                colorDisplay_5.Fill = new SolidColorBrush(wpfColor);
            }
        }

        private void BoxInt1_startZ(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int1_startZ = parsedValue;
            }
        }

        private void BoxInt1_sluttZ(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int1_sluttZ = parsedValue;
                boxInt2StartZ.Text = (parsedValue + 1).ToString();
                int2_startZ = parsedValue + 1;

            }
        }
        private void BoxInt2_startZ(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int2_startZ = parsedValue;
            }
        }

        private void BoxInt2_sluttZ(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int2_sluttZ = parsedValue;
                boxInt3StartZ.Text = (parsedValue + 1).ToString();
                int3_startZ = parsedValue + 1;

            }
        }
        private void BoxInt3_startZ(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int3_startZ = parsedValue;
            }
        }

        private void BoxInt3_sluttZ(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int3_sluttZ = parsedValue;

                boxInt4StartZ.Text = (parsedValue + 1).ToString();
                int4_startZ = parsedValue + 1;
            }
        }
        private void BoxInt4_startZ(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int4_startZ = parsedValue;
            }
        }

        private void BoxInt4_sluttZ(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int4_sluttZ = parsedValue;

                boxInt5StartZ.Text = (parsedValue + 1).ToString();
                int5_startZ = parsedValue + 1;
            }
        }
        private void BoxInt5_startZ(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int5_startZ = parsedValue;
            }
        }

        private void BoxInt5_sluttZ(object sender, TextChangedEventArgs e)
        {

            // Forsøker å konvertere teksten fra TextBox til en int
            if (int.TryParse((sender as System.Windows.Controls.TextBox).Text, out int parsedValue))
            {
                // Setter int1_start til den konverterte verdien hvis konverteringen lykkes
                int5_sluttZ = parsedValue;
            }
        }
        private void OppdaterTextBoxZ()
        {
            MinVerdiTextBox.Text = AnalyseZ.minVerdiZ.ToString();
            AvgVerdiTextBox.Text = AnalyseZ.gjennomsnittVerdiZ.ToString();
            MaxVerdiTextBox.Text = AnalyseZ.maxVerdiZ.ToString();

        }

        private void Button_ClickZ(object sender, RoutedEventArgs e)
        {

            try
            {
                Intervall.LeggTilIntervallZ(int1_startZ, int1_sluttZ, farge1Z, "Intervall1_Z");
                Intervall.LeggTilIntervallZ(int2_startZ, int2_sluttZ, farge2Z, "Intervall2_Z");
                Intervall.LeggTilIntervallZ(int3_startZ, int3_sluttZ, farge3Z, "Intervall3_Z");
                Intervall.LeggTilIntervallZ(int4_startZ, int4_sluttZ, farge4Z, "Intervall4_Z");
                Intervall.LeggTilIntervallZ(int5_startZ, int5_sluttZ, farge5Z, "Intervall5_Z");


                AnalyseZ.PlasserFirkanterIIntervallLayersOgFyllMedFargeZ();
                Rapport.GenererRapport();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void Slider_ValueChangedZ(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AnalyseZ.gjennomsiktighetZ = (int)e.NewValue;


        }

        private void UpdateLayerColor(string layerName, System.Drawing.Color selectedColor)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            using (doc.LockDocument())
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
           
                Legend legend = new Legend();
                legend.VelgFirkantZ();
            
        }
    }
}
