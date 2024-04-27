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
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;

namespace Fargemannen.ViewModel
{
    public class AnalyseZViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SonderingTypeZ> _sonderingTypesZ;
        private ObservableCollection<Intervall> _intervaller = new ObservableCollection<Intervall>();
        private int _minYear = 1990;
        private int _RuteStørresle = 1;
        private double _gjennomsnittVerdiZ;
        private double _minVerdiZ;
        private double _maxVerdiZ;
        private double _totalProsent;
        private string _BergmodellNavn = "Bergmodell";
        private string _BergmodellLagNavn = "Bergmodell";



        private static AnalyseZViewModel _instance;
        public static AnalyseZViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AnalyseZViewModel();
                }
                return _instance;
            }
        }
        public ObservableCollection<SonderingTypeZ> SonderingTypesZ
        {
            get => _sonderingTypesZ;
            set
            {
                _sonderingTypesZ = value;
                OnPropertyChanged(nameof(SonderingTypesZ)); //ENDRET HER: Sørger for at endringer i listen oppdaterer UI
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

        public string BergmodellNavn
        {
            get => _BergmodellNavn;
            set
            {
                if(value != _BergmodellNavn)
                    _BergmodellNavn = value;
                OnPropertyChanged(nameof(BergmodellNavn));
            }
        }
        public string BergmodellLagNavn
        {
            get => _BergmodellLagNavn;
            set
            {
                if (value != _BergmodellLagNavn)
                {
                    _BergmodellLagNavn = value;
                    OnPropertyChanged(nameof(BergmodellLagNavn));
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

        public ICommand UpdatePercentagesCommand { get; private set; }
        public ICommand VelgAnalyseZCommand { get; private set; }
        public ICommand ChooseColorCommand { get; private set; }
        public ICommand OppdaterTotalProsetCommand { get; private set; }
        public ICommand KjørFargekartCommand { get; private set; }

        public ICommand LagBergmodellCommand { get; private set; }


        public AnalyseZViewModel()
        {


            SonderingTypesZ = new ObservableCollection<SonderingTypeZ>
        {
            new SonderingTypeZ { Name = "Totalsondering", IsChecked = true },
            new SonderingTypeZ { Name = "Dreietrykksondering", IsChecked = false },
            new SonderingTypeZ { Name = "Trykksondering", IsChecked = false },
            new SonderingTypeZ { Name = "Prøveserie", IsChecked = true },
            new SonderingTypeZ { Name = "Poretrykksmåler", IsChecked = false },
            new SonderingTypeZ { Name = "Vingeboring", IsChecked = false },
            new SonderingTypeZ { Name = "Fjellkontrollboring", IsChecked = false },
            new SonderingTypeZ { Name = "Dreiesondering", IsChecked = false },
            new SonderingTypeZ  { Name = "Prøvegrop", IsChecked = false },
            new SonderingTypeZ { Name = "Ramsondering", IsChecked = false },
            new SonderingTypeZ { Name = "Enkel", IsChecked = false },
            new SonderingTypeZ { Name = "Fjellidagen", IsChecked = true }
        };
            VelgAnalyseZCommand = new RelayCommand(SetAnalyseZOmeråde);
            LagBergmodellCommand = new RelayCommand(GenererBergmodell);




            // Ny metode for å re-regne intervallene


        }
        public void GenererBergmodell()
        {
            var selectedTypesZ = SonderingTypesZ.Where(x => x.IsChecked).Select(x => x.Name).ToList();


            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            List<PunktInfo> pointsToSymbol = new List<PunktInfo>();
            List<Point3d> punkterMesh = new List<Point3d>();
            string NummerType = "";
            string ProjectType = "";



            ProsseseringAvFiler.HentPunkter(pointsToSymbol, punkterMesh, MinYear, selectedTypesZ, NummerType, ProjectType);

            Fargemannen.Model.AnalyseZModel.GenererMeshFraPunkter(punkterMesh, BergmodellNavn, BergmodellLagNavn);

        }

        private void SetAnalyseZOmeråde()
        {
            var selectedTypesZ = SonderingTypesZ.Where(x => x.IsChecked).Select(x => x.Name).ToList();


            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            List<PunktInfo> pointsToSymbol = new List<PunktInfo>();
            List<Point3d> punkterMesh = new List<Point3d>();
            string NummerType = "";
            string ProjectType = "";



            ProsseseringAvFiler.HentPunkter(pointsToSymbol, punkterMesh, MinYear, selectedTypesZ, NummerType, ProjectType);

         


            var sortedValues = Fargemannen.Model.AnalyseZModel.VerdierZ.OrderBy(x => x).ToList();

            // Print hver verdi på en ny linje i AutoCAD's kommandolinje
            foreach (double l in sortedValues)
            {
                ed.WriteMessage($"\n{l:F2}");  // Formaterer tall til 2 desimaler for bedre leselighet
            }




            ed.WriteMessage(Fargemannen.Model.AnalyseXYModel.lengdeVerdier.Count.ToString());
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }


    public class SonderingTypeZ : INotifyPropertyChanged
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
