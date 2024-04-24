using Fargemannen.ViewModel;
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

namespace Fargemannen.View
{
    /// <summary>
    /// Interaction logic for AnalyseXY.xaml
    /// </summary>
    public partial class AnalyseXY : UserControl
    {
        public AnalyseXY()
        {
            InitializeComponent();
            DataContext = AnalyseXYViewModel.Instance;
        }
    }
}
