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
        private ObservableCollection<IntervallZ> _intervallerZ = new ObservableCollection<IntervallZ>();
        private int _minYear = 1990;
        private int _RuteStørresle = 1;
        private double _minVerdiZ;
        private double _maxVerdiZ;
        private double _totalProsent;
        private string _BergmodellNavn = "Bergmodell";
        private string _BergmodellLagNavn = "Bergmodell_Lag";
        private string _TerrengModellLagNavn = "C-TOPO-GRID";
        public List<double> VerdierZ;
        public List<PunktInfo> PunkterMedInfoZ;



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
        public ObservableCollection<IntervallZ> IntervallerZ
        {
            get => _intervallerZ;
            set
            {
                if (_intervallerZ != value)
                {
                    _intervallerZ = value;
                    OnPropertyChanged(nameof(IntervallerZ));
                }
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

        //Type visualiseirng 
        private bool _isFargekartSelected = true;
        private bool _isMeshDukSelected = false;

        public bool IsFargekartSelected
        {
            get => _isFargekartSelected;
            set
            {
                if (_isFargekartSelected != value)
                {
                    _isFargekartSelected = value;
                    OnPropertyChanged(nameof(IsFargekartSelected));

                    if (value) // Hvis Fargekart er valgt, sett Mesh Duk til false
                    {
                        if (_isMeshDukSelected) // Unngå rekursiv oppdatering hvis allerede false
                        {
                            IsMeshDukSelected = false;
                        }
                    }
                }
            }
        }

        public bool IsMeshDukSelected
        {
            get => _isMeshDukSelected;
            set
            {
                if (_isMeshDukSelected != value)
                {
                    _isMeshDukSelected = value;
                    OnPropertyChanged(nameof(IsMeshDukSelected));

                    if (value) // Hvis Mesh Duk er valgt, sett Fargekart til false
                    {
                        if (_isFargekartSelected) // Unngå rekursiv
                        {
                            IsFargekartSelected = false;
                        }
                    }
                }
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
        public string TerrengModellLagNavn
        {
            get => _TerrengModellLagNavn;
            set
            {
                if (value != _TerrengModellLagNavn)

                    _TerrengModellLagNavn = value;
                OnPropertyChanged(nameof(TerrengModellLagNavn));
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
        private int _sliderValue = 50; // Startverdi som et eksempel

        public int SliderValue
        {
            get => _sliderValue;
            set
            {
                if (_sliderValue != value)
                {
                    _sliderValue = value;
                    OnPropertyChanged(nameof(SliderValue));
                    UpdateLayerTransparency(value);
                }
            }
        }

        public double MaxVerdiZ
        {
            get => _maxVerdiZ;
            set
            {
                if (_maxVerdiZ != value)
                {
                    _maxVerdiZ = value;
                    OnPropertyChanged(nameof(MaxVerdiZ));
                }
            }
        }
        public double MinVerdiZ
        {
            get => _minVerdiZ;
            set
            {
                if (_minVerdiZ != value)
                {
                    _minVerdiZ = value;
                    OnPropertyChanged(nameof(MinVerdiZ));
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
        public ICommand VelgAnalyseZCommand { get; private set; }
        public ICommand ChooseColorCommand { get; private set; }
        public ICommand OppdaterTotalProsetCommand { get; private set; }
        public ICommand KjørFargekartCommand { get; private set; }

        public ICommand LagBergmodellCommand { get; private set; }
        public ICommand KjørLegendZCommand { get; private set; }
        public ICommand MarkerBergCommand { get; private set; }

        public ICommand KampCommand { get; private set; }
        public AnalyseZViewModel()
        {


            SonderingTypesZ = new ObservableCollection<SonderingTypeZ>
        {
            new SonderingTypeZ { Name = "Totalsondering", IsChecked = true },
            new SonderingTypeZ { Name = "Dreietrykksondering", IsChecked = true },
            new SonderingTypeZ { Name = "Fjellkontrollboring", IsChecked = true },
            new SonderingTypeZ { Name = "Fjellidagen", IsChecked = true }
        };
            VelgAnalyseZCommand = new RelayCommand(SetAnalyseZOmeråde);
            LagBergmodellCommand = new RelayCommand(GenererBergmodell);
            ChooseColorCommand = new RelayCommand<Intervall>(ChooseColor);
            OppdaterTotalProsetCommand = new RelayCommand(RecalculateTotalPercentage);
            KjørFargekartCommand = new RelayCommand(LagFargekart);
            KjørLegendZCommand = new RelayCommand(LagLegend);
            MarkerBergCommand = new RelayCommand(KjørMarkeringAvBerg);
            KampCommand = new RelayCommand(KjørKamp);

            UpdateIntervalsAndCalculatePercentages();

            foreach (var intervall in IntervallerZ)
            {
                intervall.OnIntervallChanged = UpdateIntervalsAndCalculatePercentages;
            }

            // Ny metode for å re-regne intervallene


            IntervallerZ.Add(new IntervallZ { Navn = "IntervallZ_1", StartVerdi = 0, SluttVerdi = 10, Farge = "#f7e7b8" });
            IntervallerZ.Add(new IntervallZ { Navn = "IntervallZ_2", StartVerdi = 11, SluttVerdi = 20, Farge = "#e5cb97" });
            IntervallerZ.Add(new IntervallZ { Navn = "IntervallZ_3", StartVerdi = 11, SluttVerdi = 20, Farge = "#d1ad79" });
            IntervallerZ.Add(new IntervallZ { Navn = "IntervallZ_4", StartVerdi = 11, SluttVerdi = 20, Farge = "#ac7749" });
            IntervallerZ.Add(new IntervallZ { Navn = "IntervallZ_5", StartVerdi = 11, SluttVerdi = 20, Farge = "#995f39" });




        }
        private void KjørKamp()
        {
            Model.KampIDagenModel.SelectClosedPolylineAndManipulateSurface(BergmodellLagNavn, TerrengModellLagNavn);
        }
        private void KjørMarkeringAvBerg()
        {
            Model.MarkeringAvBergModel.MakeringBerg(PunkterMedInfoZ, 3, RuteStørresle, "Z");
        }

        public void LagLegend()
        {
            var intervallListeZ = AnalyseZViewModel.Instance.GetIntervallListe();

            LegdenModel legdenModel = new LegdenModel();
           legdenModel.VelgFirkantZ(intervallListeZ);
        }

        private void UpdateLayerTransparency(int transparencyPercent)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            using (acDoc.LockDocument())
            {
                LayerTable lt = (LayerTable)acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead);
                BlockTable bt = (BlockTable)acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);

                byte transparencyValue = (byte)(255 * (1 - transparencyPercent / 100.0));  // Beregner transparensverdien

                for (int i = 1; i <= 5; i++)
                {
                    string layerName = $"IntervallZ_{i}";
                    if (lt.Has(layerName))
                    {
                        LayerTableRecord ltr = (LayerTableRecord)acTrans.GetObject(lt[layerName], OpenMode.ForWrite);
                        ltr.Transparency = new Transparency(transparencyValue);
                        ltr.IsPlottable = true;  // Sørger for at laget er plottbart

                        // Gå gjennom alle blokker for å finne og oppdatere hatcher
                        foreach (ObjectId btrId in bt)
                        {
                            BlockTableRecord btr = (BlockTableRecord)acTrans.GetObject(btrId, OpenMode.ForRead);
                            foreach (ObjectId entId in btr)
                            {
                                Entity ent = (Entity)acTrans.GetObject(entId, OpenMode.ForRead);
                                if (ent.Layer == layerName && ent is Hatch)
                                {
                                    ent.UpgradeOpen();  // Gjør entiteten skrivbar
                                    Hatch hatch = ent as Hatch;
                                    hatch.Transparency = new Transparency(transparencyValue);  // Setter transparensen
                                    hatch.DowngradeOpen();  // Nedgraderer tilgangen tilbake til les
                                }
                            }
                        }
                    }
                }

                acTrans.Commit();
                acDoc.Editor.Regen();// Lagrer endringene i databasen
            }
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

            MinVerdiZ = Math.Round(Fargemannen.Model.AnalyseZModel.minVerdiZ);
            MaxVerdiZ = Math.Round(Fargemannen.Model.AnalyseZModel.maxVerdiZ);

            if(MinVerdiZ < 0)
            {
                MinVerdiZ = 0;
            }
        }
        public void RecalculateTotalPercentage()
        {
            TotalProsent = IntervallerZ.Sum(intervall => intervall.Prosent);
            OnPropertyChanged(nameof(TotalProsent)); // Sørger for å oppdatere UI med den nye totalen
        }
        private void UpdateIntervalsAndCalculatePercentages()
        {

            if (VerdierZ == null)
            {
          
                return;  // Avbryt videre behandling siden VerdierZ er null
            }

            var lengdeVerdier = VerdierZ.Where(v => v != -999).ToList();

            if (lengdeVerdier == null || lengdeVerdier.Count == 0)
            {
                foreach (var intervall in IntervallerZ)
                {
                    intervall.Prosent = 0;  // Setter prosent til 0 hvis ingen verdier finnes
                }
                return;
            }

            double minVerdi = lengdeVerdier.Min(); // Oppdaterer minVerdi til å være minimum av gyldige verdier
            double maxVerdi = lengdeVerdier.Max();
            double totalRange = maxVerdi - minVerdi + 1; // +1 for å inkludere siste verdi i intervall
            double intervallStørrelse = Math.Floor(totalRange / IntervallerZ.Count);

            int totalLengder = lengdeVerdier.Count;

            for (int i = 0; i < IntervallerZ.Count; i++)
            {
                var intervall = IntervallerZ[i];
                intervall.StartVerdi = minVerdi + i * intervallStørrelse;
                intervall.SluttVerdi = intervall.StartVerdi + intervallStørrelse - 0.1; // -0.1 for å ikke overlappe med neste intervall

                if (i == IntervallerZ.Count - 1 && intervall.SluttVerdi < maxVerdi)
                {
                    intervall.SluttVerdi = maxVerdi; // Justerer siste intervall for å dekke alle verdier
                }

                int countInInterval = lengdeVerdier.Count(x => x >= intervall.StartVerdi && x <= intervall.SluttVerdi);
                intervall.Prosent = (double)countInInterval / totalLengder * 100;

                intervall.OnPropertyChanged(nameof(Intervall.Prosent)); // Oppdaterer UI for hver endring
                RecalculateTotalPercentage();
                }
            
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


            ed.WriteMessage(punkterMesh.Count.ToString());
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

            PunkterMedInfoZ = pointsToSymbol;

            if (IsFargekartSelected)
            {
                Fargemannen.Model.AnalyseZModel.Start(punkterMesh, RuteStørresle, TerrengModellLagNavn, BergmodellLagNavn);
                VerdierZ = AnalyseZModel.VerdierZ;
                UpdateIntervalsAndCalculatePercentages();
                FetchValues();
                
            }
            else
            {
               
                string analysetype = "Z";
                DukXYModel Duk = new DukXYModel();
                Duk.KjørDukPåXY(pointsToSymbol, BergmodellLagNavn, TerrengModellLagNavn, analysetype);
                VerdierZ = Model.DukXYModel.VerdierZ;
                UpdateIntervalsAndCalculatePercentages();
                FetchValues();

            }
         


        }

        public List<IntervallZ> GetIntervallListe()
        {
            return IntervallerZ.ToList();
        }
        public void LagFargekart()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            var intervallListe = AnalyseZViewModel.Instance.GetIntervallListe();

            if(IsFargekartSelected)
            {
                Model.AnalyseZModel.PlasserFirkanterIIntervallLayersOgFyllMedFargeZ(SliderValue, intervallListe);

            }
            else
            {
                DukXYModel Duk = new DukXYModel();
                Duk.FargeMeshZ(VerdierZ, BergmodellLagNavn, intervallListe);
            }

          
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

    public class IntervallZ : INotifyPropertyChanged
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
                    CalculateAndUpdatePercentage();
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
                    
                    if (!string.IsNullOrEmpty(Navn))
                    {
                        Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                        Database acCurDb = acDoc.Database;

                        using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                        {
                            using (acDoc.LockDocument())
                            {
                                LayerTable lt = (LayerTable)acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead);

                                if (lt.Has(Navn))  // Sjekk om laget eksisterer
                                {
                                    LayerTableRecord ltrExisting = (LayerTableRecord)acTrans.GetObject(lt[Navn], OpenMode.ForWrite);
                                    ltrExisting.Color = Autodesk.AutoCAD.Colors.Color.FromColor(ConvertHexToDrawingColor(Farge));
                                    OnPropertyChanged(nameof(Brush));
                                    acTrans.Commit();  // Commit kun hvis endringer er gjort
                                    acDoc.Editor.Regen();
                                }
                                else
                                {

                                }
                            }
                        }
                    }
                    
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
            List<double> lengdeVerdier = new List<double>();
            
            if(Model.DukXYModel.VerdierZ.Count == 0)
            {
                lengdeVerdier = Model.AnalyseZModel.VerdierZ.Where(v => v != -999).ToList();
            }
            else
            {
                lengdeVerdier = Model.DukXYModel.VerdierZ.Where(v => v != -999).ToList();
            }
          

            if (lengdeVerdier == null || lengdeVerdier.Count == 0)
            {
                Prosent = 0; // Setter prosent til 0 hvis ingen gyldige verdier finnes
            }
            else
            {
                int totalLengder = lengdeVerdier.Count;
                int countInInterval = lengdeVerdier.Count(x => x >= StartVerdi && x < SluttVerdi + 0.1);
                Prosent = (double)countInInterval / totalLengder * 100;
            }
        }
        private static System.Drawing.Color ConvertHexToDrawingColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor))
                return System.Drawing.Color.Transparent;  // Returnerer transparent hvis ingen farge er spesifisert

            try
            {
                return System.Drawing.ColorTranslator.FromHtml(hexColor);
            }
            catch
            {
                return System.Drawing.Color.Black;  // Returnerer svart som fallback
            }
        }
    }
}

