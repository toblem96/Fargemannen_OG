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
using System;
using Microsoft.ApplicationInsights;



namespace Fargemannen.ViewModel
{
    public class SymbolsViewModel : INotifyPropertyChanged
    {

        private ObservableCollection<SonderingType> _sonderingTypes;
        private int _minYear = 1990;
        private int _minDrillingDepth = 2;
        
        private Color _normalSymbolColor = Colors.Black;
        private Color _minDrillingSymbolColor = Colors.Black;
        private double _rotation = 0;
        private double _scale = 1;
        private bool _usePDFProject = true;
        private bool _useCaseProject;
        private bool _useCustomProject;
        private string _customProjectName;
        private string _projectType;

        private bool _useSOSINumber;
        private bool _usePDFNumber = true;
        private string _nummerType;




        private static SymbolsViewModel _instance;
        public static SymbolsViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SymbolsViewModel();
                }
                return _instance;
            }
        }
        public bool UsePDFProject
        {
            get => _usePDFProject;
            set
            {
                SetProjectType(ref _usePDFProject, value, "PDF-nummer");
                UpdateProjectType();
            }
        }

        public bool UseCaseProject
        {
            get => _useCaseProject;
            set
            {
                SetProjectType(ref _useCaseProject, value, "Saksnummer");
                UpdateProjectType();
            }
        }

        public bool UseCustomProject
        {
            get => _useCustomProject;
            set
            {
                SetProjectType(ref _useCustomProject, value, CustomProjectName);
                UpdateProjectType();
            }
        }

        public string CustomProjectName
        {
            get => _customProjectName;
            set
            {
                if (_customProjectName != value)
                {
                    _customProjectName = value;
                    OnPropertyChanged(nameof(CustomProjectName));
                    if (_useCustomProject)
                    {
                        UpdateProjectType();
                    }
                }
            }
        }

        public string ProjectType
        {
            get => _projectType;
            private set
            {
                if (_projectType != value)
                {
                    _projectType = value;
                    OnPropertyChanged(nameof(ProjectType));
                }
            }
        }

        private void SetProjectType(ref bool projectFlag, bool value, string type)
        {
            if (projectFlag != value)
            {
                projectFlag = value;
                OnPropertyChanged(nameof(UsePDFProject));
                OnPropertyChanged(nameof(UseCaseProject));
                OnPropertyChanged(nameof(UseCustomProject));

                if (value) // Only update if the new value is true
                {
                    UsePDFProject = type == "PDF-nummer";
                    UseCaseProject = type == "Saksnummer";
                    UseCustomProject = type == CustomProjectName;
                    ProjectType = type;
                }
                else
                {
                    UpdateProjectType();  // Reevaluate the project type if the current type is being deactivated
                }
            }
        }

        private void UpdateProjectType()
        {
            if (UsePDFProject)
                ProjectType = "PDF-nummer";
            else if (UseCaseProject)
                ProjectType = "Saksnummer";
            else if (UseCustomProject)
                ProjectType = CustomProjectName;
            else
                ProjectType = "PDF-nummer";  // Reset to null if no project type is selected
        }
       
        public bool UseSOSINumber
        {
            get => _useSOSINumber;
            set
            {
                _useSOSINumber = value;
                OnPropertyChanged(nameof(UseSOSINumber));
                UpdateNummerType();
            }
        }

        public bool UsePDFNumber
        {
            get => _usePDFNumber;
            set
            {
                _usePDFNumber = value;
                OnPropertyChanged(nameof(UsePDFNumber));
                UpdateNummerType();
            }
        }

        public string NummerType
        {
            get => _nummerType;
            private set
            {
                _nummerType = value;
                OnPropertyChanged(nameof(NummerType));
            }
        }

        private void UpdateNummerType()
        {
            if (UseSOSINumber)
                NummerType = "SOSINummer";
            else if (UsePDFNumber)
                NummerType = "PDFNummer";
        }

        public ObservableCollection<SonderingType> SonderingTypes
        {
            get => _sonderingTypes;
            set
            {
                _sonderingTypes = value;
                OnPropertyChanged(nameof(SonderingTypes)); //ENDRET HER: Sørger for at endringer i listen oppdaterer UI
            }
        }
        public Color NormalSymbolColor
        {
            get => _normalSymbolColor;
            set
            {
                if (_normalSymbolColor != value)
                {
                    _normalSymbolColor = value;
                    OnPropertyChanged(nameof(NormalSymbolColor));
                    OnPropertyChanged(nameof(NormalSymbolColorBrush));
                    UpdateSymbolColor();  // Oppdater farge når brukeren velger en ny
                }
            }
        }
        public Color minDrillingSymbolColor
        {
            get => _minDrillingSymbolColor;
            set
            {
                if (_minDrillingSymbolColor != value)
                {
                    _minDrillingSymbolColor = value;
                    OnPropertyChanged(nameof(minDrillingSymbolColor));
                    OnPropertyChanged(nameof(minDrillingSymbolColorBrush));
                    UpdateMinSymbolColor();  // Oppdater farge når brukeren velger en ny
                }
            }
        }

        public double Rotation
        {
            get => _rotation;
            set
            {
                if (_rotation != value)
                {
                    _rotation = value;
                    OnPropertyChanged(nameof(Rotation));
                    UpdateSymbolRotation();  // Oppdater rotasjon når brukeren justerer slideren
                }
            }
        }

        public double Scale
        {
            get => _scale;
            set
            {
                if (_scale != value)
                {
                    _scale = value;
                    OnPropertyChanged(nameof(Scale));
                    UpdateSymbolScale();  // Oppdater skala når brukeren justerer slideren
                }
            }
        }

        public Brush NormalSymbolColorBrush => new SolidColorBrush(NormalSymbolColor);
        public Brush minDrillingSymbolColorBrush => new SolidColorBrush(minDrillingSymbolColor);

        public ICommand ChooseNormalColorCommand { get; private set; }
        public ICommand minDrillingSymbolColorCommand { get; private set; }
        public ICommand GenerateSymbolsCommand { get; private set; }


        private void ChooseNormalColor()
        {
            var colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                NormalSymbolColor = Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
            }
        }
        private void ChooseminDrillingSymbolColor()
        {
            var colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                minDrillingSymbolColor = Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
            }
        }

        private void UpdateSymbolColor()
        {
            // Konverterer System.Windows.Media.Color til System.Drawing.Color
            System.Drawing.Color drawColor = System.Drawing.Color.FromArgb(NormalSymbolColor.A, NormalSymbolColor.R, NormalSymbolColor.G, NormalSymbolColor.B);
            SymbolHandlers.EndreLagFarge("Symboler", drawColor);  // "LayerName" bør erstattes med faktisk lag navn
        }

        private void UpdateMinSymbolColor()
        {
            // Konverterer System.Windows.Media.Color til System.Drawing.Color
            System.Drawing.Color drawColor = System.Drawing.Color.FromArgb(minDrillingSymbolColor.A, minDrillingSymbolColor.R, minDrillingSymbolColor.G, minDrillingSymbolColor.B);
            SymbolHandlers.EndreLagFarge("Symboler for stans i løs", drawColor);  // "LayerName" bør erstattes med faktisk lag navn
        }

        private void UpdateSymbolRotation()
        {
            SymbolHandlers.RoterBlokker(Model.SymbolModel.GenerteSymbol, Rotation);
        }

        private void UpdateSymbolScale()
        {
            SymbolHandlers.EndreSkala(Model.SymbolModel.GenerteSymbol, Scale);
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

    public int MinDrillingDepth
    {
        get => _minDrillingDepth;
        set
        {
            if (_minDrillingDepth != value)
            {
                _minDrillingDepth = value;
                OnPropertyChanged(nameof(MinDrillingDepth));
            }
        }
    }


 



    private SymbolsViewModel()
    {
            _usePDFProject = true; // Set PDF Project as the default type on initialization
            UpdateProjectType();

            SonderingTypes = new ObservableCollection<SonderingType>
        {
            new SonderingType { Name = "Totalsondering", IsChecked = true },
            new SonderingType { Name = "Dreietrykksondering", IsChecked = false },
            new SonderingType { Name = "Trykksondering", IsChecked = false },
            new SonderingType { Name = "Prøveserie", IsChecked = true },
            new SonderingType { Name = "Poretrykksmåler", IsChecked = false },
            new SonderingType { Name = "Vingeboring", IsChecked = false },
            new SonderingType { Name = "Fjellkontrollboring", IsChecked = false },
            new SonderingType { Name = "Dreiesondering", IsChecked = false },
            new SonderingType { Name = "Prøvegrop", IsChecked = false },
            new SonderingType { Name = "Ramsondering", IsChecked = false },
            new SonderingType { Name = "Enkel", IsChecked = false },
            new SonderingType { Name = "Fjellidagen", IsChecked = true }
        };

        GenerateSymbolsCommand = new RelayCommand(ExecuteGenerateSymbols);
        ChooseNormalColorCommand = new RelayCommand(ChooseNormalColor);
        minDrillingSymbolColorCommand = new RelayCommand(ChooseminDrillingSymbolColor);
    }


        private void ExecuteGenerateSymbols()
        {
            var telemetryClient = new TelemetryClient();  // Pass inn din Instrumentation Key her om nødvendig

            try
            {
                // Spor bruk av knappen
                Fargemannen.ApplicationInsights.AppInsights.TrackEvent("Knapp generer Symbol");
               
                var selectedTypes = SonderingTypes.Where(x => x.IsChecked).Select(x => x.Name).ToList();

                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;

                List<PunktInfo> pointsToSymbol = new List<PunktInfo>();
                List<Point3d> punkterMesh = new List<Point3d>();

                ProsseseringAvFiler.HentPunkter(pointsToSymbol, punkterMesh, MinYear, selectedTypes, NummerType, ProjectType);

                Fargemannen.ApplicationInsights.AppInsights.TrackMetric("Number of Symbols Generated", pointsToSymbol.Count);

                SymbolModel.PrintValgtBoring(pointsToSymbol, selectedTypes);
                SymbolModel.test(pointsToSymbol, selectedTypes, MinDrillingDepth, NormalSymbolColor, minDrillingSymbolColor);

            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string>
    {
        { "Error Message", ex.Message },
        { "StackTrace", ex.StackTrace },
                    { "Function Name", nameof(ExecuteGenerateSymbols) }
    };
                Fargemannen.ApplicationInsights.AppInsights.TrackException(ex, properties);

                //


            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

    public class SonderingType : INotifyPropertyChanged
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



}