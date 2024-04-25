using System.Collections.Generic;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Fargemannen.ViewModel;
using Fargemannen;
using System.Runtime.CompilerServices;
using System.Windows;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using System.Linq;
using System.Windows.Media;
using System.Windows.Forms;
using Fargemannen.Model;
using System.Drawing;
using System;

namespace Fargemannen.ViewModel
{

    public class AnalyseXYViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SonderingTypeXY> _sonderingTypesXY;
        private ObservableCollection<Intervall> _intervaller = new ObservableCollection<Intervall>();
        private int _minYear = 1990;
        private int _RuteStørresle = 1;
        private double _gjennomsnittVerdiXY;
        private double _minVerdiXY;
        private double _maxVerdiXY;
        private double _totalProsent;



        private static AnalyseXYViewModel _instance;
        public static AnalyseXYViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AnalyseXYViewModel();
                }
                return _instance;
            }
        }
       
        public ObservableCollection<Intervall> Intervaller
        {
            get => _intervaller;
            set
            {
                if (_intervaller != value)
                {
                    _intervaller = value;
                    OnPropertyChanged(nameof(Intervaller));
                }
            }
        }

        public ObservableCollection<SonderingTypeXY> SonderingTypesXY
        {
            get => _sonderingTypesXY;
            set
            {
                _sonderingTypesXY = value;
                OnPropertyChanged(nameof(SonderingTypesXY)); //ENDRET HER: Sørger for at endringer i listen oppdaterer UI
            }
        }

        public int MinYear
        {
            get => _minYear;
            set
            {
                if (_minYear != value)
                {
                    _minYear = value;
                    OnPropertyChanged(nameof(MinYear));
                }
            }
        }

        public int RuteStørresle
        {
            get => _RuteStørresle;
            set
            {
                if (_RuteStørresle != value)
                {
                    _RuteStørresle = value;
                    OnPropertyChanged(nameof(RuteStørresle));
                }
            }
        }
   

        public double MinVerdiXY
        {
            get => _minVerdiXY;
            set
            {
                if (_minVerdiXY != value)
                {
                    _minVerdiXY = value;
                    OnPropertyChanged(nameof(MinVerdiXY));
                }
            }
        }

        public double MaxVerdiXY
        {
            get => _maxVerdiXY;
            set
            {
                if (_maxVerdiXY != value)
                {
                    _maxVerdiXY = value;
                    OnPropertyChanged(nameof(MaxVerdiXY));
                }
            }
        }

        public double TotalProsent
        {
            get => _totalProsent;
            set
            {
                if (_totalProsent != value)
                {
                    _totalProsent = value;
                    OnPropertyChanged(nameof(TotalProsent));
                }
            }
        }
        public ICommand UpdatePercentagesCommand { get; private set; }
        public ICommand VelgAnalyseXYCommand { get; private set; }
        public ICommand ChooseColorCommand { get; private set; }
        public AnalyseXYViewModel()
        {


            SonderingTypesXY = new ObservableCollection<SonderingTypeXY>
        {
            new SonderingTypeXY { Name = "Totalsondering", IsChecked = true },
            new SonderingTypeXY { Name = "Dreietrykksondering", IsChecked = false },
            new SonderingTypeXY { Name = "Trykksondering", IsChecked = false },
            new SonderingTypeXY { Name = "Prøveserie", IsChecked = true },
            new SonderingTypeXY { Name = "Poretrykksmåler", IsChecked = false },
            new SonderingTypeXY { Name = "Vingeboring", IsChecked = false },
            new SonderingTypeXY { Name = "Fjellkontrollboring", IsChecked = false },
            new SonderingTypeXY { Name = "Dreiesondering", IsChecked = false },
            new SonderingTypeXY { Name = "Prøvegrop", IsChecked = false },
            new SonderingTypeXY { Name = "Ramsondering", IsChecked = false },
            new SonderingTypeXY { Name = "Enkel", IsChecked = false },
            new SonderingTypeXY { Name = "Fjellidagen", IsChecked = true }
        };
            VelgAnalyseXYCommand = new RelayCommand(SetAnalyseXYOmeråde);
            ChooseColorCommand = new RelayCommand<Intervall>(ChooseColor);
           
            
            UpdateIntervalsAndCalculatePercentages();

            UpdatePercentagesCommand = new RelayCommand(UpdateIntervalsAndCalculatePercentages);

            foreach (var intervall in Intervaller)
            {
                intervall.OnIntervallChanged = UpdateIntervalsAndCalculatePercentages;
            }

            // Ny metode for å re-regne intervallene
            

            Intervaller.Add(new Intervall { Navn = "Intervall_1", StartVerdi = 0, SluttVerdi = 10, Farge = "#1f7f00" });
            Intervaller.Add(new Intervall { Navn = "Intervall_2", StartVerdi = 11, SluttVerdi = 20, Farge = "#00ff00" });
            Intervaller.Add(new Intervall { Navn = "Intervall_3", StartVerdi = 11, SluttVerdi = 20, Farge = "#ffff00" });
            Intervaller.Add(new Intervall { Navn = "Intervall_4", StartVerdi = 11, SluttVerdi = 20, Farge = "#ffbf00" });
            Intervaller.Add(new Intervall { Navn = "Intervall_5", StartVerdi = 11, SluttVerdi = 20, Farge = "#ff3f00" });


        }

        private void ChooseColor(Intervall intervall)
        {
            var colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                intervall.Farge = System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(colorDialog.Color.ToArgb()));
            }
        }

        private void FetchValues()
        {
          
            MinVerdiXY = Fargemannen.Model.AnalyseXYModel.minVerdiXY;
            MaxVerdiXY = Fargemannen.Model.AnalyseXYModel.maxVerdiXY;
        }



        private void RecalculateIntervals()
        {
            var lengdeVerdier = Fargemannen.Model.AnalyseXYModel.lengdeVerdier;
            if (lengdeVerdier == null || lengdeVerdier.Count == 0)
            {
                foreach (var intervall in Intervaller)
                {
                    intervall.Prosent = 0;  // Sørger for at prosenten settes til 0 hvis det ikke er noen verdier
                }
                return;
            }

            int totalLengder = lengdeVerdier.Count;
            foreach (var intervall in Intervaller)
            {
                int countInInterval = lengdeVerdier.Count(x => x >= intervall.StartVerdi && x < intervall.SluttVerdi + 0.1);
                intervall.Prosent = (double)countInInterval / totalLengder * 100;
                intervall.OnPropertyChanged(nameof(Intervall.Prosent)); // Sørger for å utløse PropertyChanged for Prosent
            }
        }



        public void RecalculateTotalPercentage()
        {
            TotalProsent = Intervaller.Sum(intervall => intervall.Prosent);
            OnPropertyChanged(nameof(TotalProsent)); // Sørger for å oppdatere UI med den nye totalen
        }

        private void FyllVerdier()
        {
            var lengdeVerdier = Fargemannen.Model.AnalyseXYModel.lengdeVerdier;
            if (lengdeVerdier == null || lengdeVerdier.Count == 0)
                return; // Ingen verdier å prosessere

            double minVerdi = 0;  // Starter alltid på 0
            double maxVerdi = lengdeVerdier.Max();


            int antallIntervaller = Intervaller.Count;
            double totalRange = maxVerdi - minVerdi + 1; // +1 for å tillate siste intervall å slutte på x.9
            double intervallStørrelse = Math.Floor(totalRange / antallIntervaller);

            for (int i = 0; i < antallIntervaller; i++)
            {
                var intervall = Intervaller[i];
                intervall.StartVerdi = minVerdi + i * intervallStørrelse;
                intervall.SluttVerdi = intervall.StartVerdi + intervallStørrelse - 0.1; // Justering for å ende på x.9

                // Sørger for at sluttverdien av siste intervall korrekt ender på x.9 av den høyeste verdien
                if (i == antallIntervaller - 1 && intervall.SluttVerdi < maxVerdi)
                {
                    intervall.SluttVerdi = maxVerdi - 0.1; // Justerer siste intervall hvis det ikke dekker helt til maxVerdi
                }

                intervall.Farge = intervall.Farge;  // Oppdaterer farge hvis nødvendig, kan utløse OnPropertyChanged
            }
            int totalLengder = lengdeVerdier.Count;
            foreach (var intervall in Intervaller)
            {
                int countInInterval = lengdeVerdier.Count(x => x >= intervall.StartVerdi && x < intervall.SluttVerdi + 0.1); // +0.1 for å inkludere grenseverdien
                intervall.Prosent = (double)countInInterval / totalLengder * 100;
            }
        }

        private void UpdateIntervalsAndCalculatePercentages()
        {
            var lengdeVerdier = Fargemannen.Model.AnalyseXYModel.lengdeVerdier;
            if (lengdeVerdier == null || lengdeVerdier.Count == 0)
            {
                foreach (var intervall in Intervaller)
                {
                    intervall.Prosent = 0;  // Setter prosent til 0 hvis ingen verdier finnes
                }
                return;
            }

            double minVerdi = 0; // Starter på 0
            double maxVerdi = lengdeVerdier.Max();
            double totalRange = maxVerdi - minVerdi + 1; // +1 for å inkludere siste verdi i intervall
            double intervallStørrelse = Math.Floor(totalRange / Intervaller.Count);

            int totalLengder = lengdeVerdier.Count;

            for (int i = 0; i < Intervaller.Count; i++)
            {
                var intervall = Intervaller[i];
                intervall.StartVerdi = minVerdi + i * intervallStørrelse;
                intervall.SluttVerdi = intervall.StartVerdi + intervallStørrelse - 0.1; // -0.1 for å ikke overlappe med neste intervall

                if (i == Intervaller.Count - 1 && intervall.SluttVerdi < maxVerdi)
                {
                    intervall.SluttVerdi = maxVerdi; // Justerer siste intervall for å dekke alle verdier
                }

                int countInInterval = lengdeVerdier.Count(x => x >= intervall.StartVerdi && x <= intervall.SluttVerdi);
                intervall.Prosent = (double)countInInterval / totalLengder * 100;

                intervall.OnPropertyChanged(nameof(Intervall.Prosent)); // Oppdaterer UI for hver endring
            }
        }


        /*
         * Se om den er like den over eller ikke, skal oppdatere en TOT:prossent, Husk at View ikke er oppdatert 
         * 
         * 
        private void UpdateIntervalsAndCalculatePercentages()
        {
            var lengdeVerdier = Fargemannen.Model.AnalyseXYModel.lengdeVerdier;
            double totalPercent = 0;

            if (lengdeVerdier == null || lengdeVerdier.Count == 0)
            {
                foreach (var intervall in Intervaller)
                {
                    intervall.Prosent = 0;
                    intervall.OnPropertyChanged(nameof(Intervall.Prosent));
                }
                TotalProsent = 0;  // Setter totalprosent til 0 hvis ingen verdier
                return;
            }

            int totalLengder = lengdeVerdier.Count;

            foreach (var intervall in Intervaller)
            {
                var countInInterval = lengdeVerdier.Count(x => x >= intervall.StartVerdi && x <= intervall.SluttVerdi);
                var currentProsent = (double)countInInterval / totalLengder * 100;
                intervall.Prosent = currentProsent;
                totalPercent += currentProsent;
                intervall.OnPropertyChanged(nameof(Intervall.Prosent));
            }

            TotalProsent = totalPercent;  // Oppdaterer den totale prosenten
            OnPropertyChanged(nameof(TotalProsent));
        }
    }
        */





    private void SetAnalyseXYOmeråde()
        {
            var selectedTypesXY = SonderingTypesXY.Where(x => x.IsChecked).Select(x => x.Name).ToList();


            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            List<PunktInfo> pointsToSymbol = new List<PunktInfo>();
            List<Point3d> punkterMesh = new List<Point3d>();
            string NummerType = "";
            string ProjectType = "";



            ProsseseringAvFiler.HentPunkter(pointsToSymbol, punkterMesh, MinYear, selectedTypesXY, NummerType, ProjectType);

             var PunkterMesh  = NullstillZVerdierOgLagreIAnalyseListe(punkterMesh);
            Fargemannen.Model.AnalyseXYModel.Start(PunkterMesh, RuteStørresle);
            UpdateIntervalsAndCalculatePercentages();
            FetchValues();


            var sortedValues = Fargemannen.Model.AnalyseXYModel.lengdeVerdier.OrderBy(x => x).ToList();

            // Print hver verdi på en ny linje i AutoCAD's kommandolinje
            foreach (double l in sortedValues)
            {
                ed.WriteMessage($"\n{l:F2}");  // Formaterer tall til 2 desimaler for bedre leselighet
            }




            ed.WriteMessage(Fargemannen.Model.AnalyseXYModel.lengdeVerdier.Count.ToString());
        }


        //KNAP FOR Å STARTE PROSSESEN 


        public static List<Point3d> NullstillZVerdierOgLagreIAnalyseListe(List<Point3d> punkterMesh)
        {
            List<Point3d> PunkterMesh = new List<Point3d>();
            foreach (var punkt in punkterMesh)
            {
                // Setter Z-verdien til 0 og oppretter et nytt Point3d-objekt
                Point3d nyttPunkt = new Point3d(punkt.X, punkt.Y, 0);
                PunkterMesh.Add(nyttPunkt);
                
            }
            return PunkterMesh;

        }

    

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SonderingTypeXY : INotifyPropertyChanged
    {
        private bool _isChecked;

        public string Name { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

     


    }
    public class Intervall : INotifyPropertyChanged
    {
        public string Navn { get; set; }
        private double _startVerdi;
        private double _sluttVerdi;
        private string _farge;
        private SolidColorBrush _brush;
        private double _prosent;

        public Action OnIntervallChanged;
        public double StartVerdi
        {
            get => _startVerdi;
            set
            {
                if (_startVerdi != value)
                {
                    _startVerdi = value;
                    OnPropertyChanged(nameof(StartVerdi));
                    CalculateAndUpdatePercentage();
                }
            }
        }

        public double SluttVerdi
        {
            get => _sluttVerdi;
            set
            {
                if (_sluttVerdi != value)
                {
                    _sluttVerdi = value;
                    OnPropertyChanged(nameof(SluttVerdi));
                    CalculateAndUpdatePercentage();  // Kall oppdateringsmetoden
                }
            }
        }

        public double Prosent
        {
            get => _prosent;
            set
            {
                if (_prosent != value)
                {
                    _prosent = value;
                    OnPropertyChanged(nameof(Prosent));
                }
            }
        }

        public string Farge
        {
            get => _farge;
            set
            {
                if (_farge != value)
                {
                    _farge = value;
                    Brush = new SolidColorBrush(ConvertToColor(_farge));
                    OnPropertyChanged(nameof(Farge));
                    OnPropertyChanged(nameof(Brush));
                }
            }
        }

        public SolidColorBrush Brush
        {
            get => _brush;
            set
            {
                if (_brush != value)
                {
                    _brush = value;
                    OnPropertyChanged(nameof(Brush));
                }
            }
        }
  

        private System.Windows.Media.Color ConvertToColor(string colorStr)
        {
            if (string.IsNullOrEmpty(colorStr))
                return System.Windows.Media.Colors.Transparent; // Use transparent if no color specified

            try
            {
                return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorStr);
            }
            catch
            {
                return System.Windows.Media.Colors.Black; // Use black as fallback if conversion fails
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CalculateAndUpdatePercentage()
        {
            var lengdeVerdier = Fargemannen.Model.AnalyseXYModel.lengdeVerdier;
            if (lengdeVerdier == null || lengdeVerdier.Count == 0)
            {
                Prosent = 0;
            }
            else
            {
                int totalLengder = lengdeVerdier.Count;
                int countInInterval = lengdeVerdier.Count(x => x >= StartVerdi && x < SluttVerdi + 0.1);
                Prosent = (double)countInInterval / totalLengder * 100;
            }
            AnalyseXYViewModel.Instance.RecalculateTotalPercentage();

        }
    }
}

