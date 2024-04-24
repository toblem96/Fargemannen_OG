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
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Windows.Forms; // Husk å legge til referanse til System.Windows.Forms.
using System.Drawing; // Og System.Drawing.
using Color = System.Drawing.Color;

namespace Fargemannen
{
    /// <summary>
    /// Interaction logic for SecondWinSymbol.xaml
    /// </summary>
    public partial class SecondWinSymbol : Window
    {
        public static System.Drawing.Color selectedColor = System.Drawing.Color.White;           // Setter standard fargen valgt for symbolene til hvit
        public static System.Drawing.Color selectedColor_minBerg = System.Drawing.Color.White;
        public static double rotasjonAngle;                        // Lager en statisk variabel for rotasjonsvinkelen
        public static double newScale;

        public SecondWinSymbol()
        {
            InitializeComponent();


            // Initialiserer UI-komponenter med lagrede statiske verdier
            System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor);
            colorDisplay.Fill = new SolidColorBrush(wpfColor);

            System.Windows.Media.Color wpfColorMinBerg = SymbolStyle.ConvertToMediaColor(selectedColor_minBerg);
            colorDisplayMinFjell.Fill = new SolidColorBrush(wpfColorMinBerg);

            sliderRotasjon.Value = rotasjonAngle * (180.0 / Math.PI);  // Konverterer radianer til grader for slideren
            sliderSkala.Value = newScale;

        }

        private void FerdigButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }




        private void Farge(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectedColor = colorDialog.Color;
                SymbolStyle.EndreLagFarge("Symboler", selectedColor);

                System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor);
                colorDisplay.Fill = new SolidColorBrush(wpfColor);
            }
        }

        private void FargeMinFjell(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectedColor_minBerg = colorDialog.Color;
                SymbolStyle.EndreLagFarge("Symboler for stans i løs", selectedColor_minBerg);

                System.Windows.Media.Color wpfColor = SymbolStyle.ConvertToMediaColor(selectedColor_minBerg);
                colorDisplayMinFjell.Fill = new SolidColorBrush(wpfColor);
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            rotasjonAngle = e.NewValue * (Math.PI / 180);  // Lagrer i radianer
            SymbolStyle.RoterBlokker(SymbolProsseser.GenerteSymbol, rotasjonAngle);
        }

        private void Slider_SkalaEndret(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            newScale = e.NewValue;
            SymbolStyle.EndreSkala(SymbolProsseser.GenerteSymbol,newScale);
        }
    }

}


