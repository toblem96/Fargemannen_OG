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
        public List<double> lengdeVerdierXY; 



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
        private string _BergmodellLagNavn = "Bergmodell";

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
                double roundedValue = Math.Round(value);
                if (_totalProsent != roundedValue)
                {
                    _totalProsent = roundedValue;
                    OnPropertyChanged(nameof(TotalProsent));
                }
            }
        }
        public ICommand UpdatePercentagesCommand { get; private set; }
        public ICommand VelgAnalyseXYCommand { get; private set; }
        public ICommand ChooseColorCommand { get; private set; }
        public ICommand OppdaterTotalProsetCommand { get; private set; }
        public ICommand KjørFargekartCommand { get; private set; }
        public ICommand KjørLegendCommand { get; private set; }
        public ICommand KjørDukCommand { get; private set; }


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
            OppdaterTotalProsetCommand = new RelayCommand(RecalculateTotalPercentage);
            KjørFargekartCommand = new RelayCommand(LagFargekart);
            KjørLegendCommand = new RelayCommand(LagLegend);
            KjørDukCommand = new RelayCommand(KjørDuk);
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
        public void KjørDuk() 
        {
            var intervallListe = AnalyseXYViewModel.Instance.GetIntervallListe();
            var selectedTypesXY = SonderingTypesXY.Where(x => x.IsChecked).Select(x => x.Name).ToList();


            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            List<PunktInfo> pointsToSymbol = new List<PunktInfo>();
            List<Point3d> punkterMesh = new List<Point3d>();
            string NummerType = "";
            string ProjectType = "";


            Fargemannen.ApplicationInsights.AppInsights.TrackEvent("Analyse XY");
            ProsseseringAvFiler.HentPunkter(pointsToSymbol, punkterMesh, MinYear, selectedTypesXY, NummerType, ProjectType);

            DukXYModel Duk = new DukXYModel();
            //Duk.KjørDukPåXY(pointsToSymbol, intervallListe, BergmodellLagNavn);

        }
        public void LagLegend() 
        {
            var intervallListe = AnalyseXYViewModel.Instance.GetIntervallListe();

            LegdenModel legdenModel = new LegdenModel();
            legdenModel.VelgFirkant(intervallListe);
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
                    string layerName = $"Intervall_{i}";
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
            MinVerdiXY = lengdeVerdierXY.Min();
            MaxVerdiXY = lengdeVerdierXY.Max();
        }

        public void RecalculateTotalPercentage()
        {
            TotalProsent = Math.Round(Intervaller.Sum(intervall => intervall.Prosent));
            OnPropertyChanged(nameof(TotalProsent)); // Sørger for å oppdatere UI med den nye totalen
        }

     
        private void UpdateIntervalsAndCalculatePercentages()
        {
            var lengdeVerdier = lengdeVerdierXY;
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
                intervall.Prosent = Math.Round((double)countInInterval / totalLengder * 100);

                intervall.OnPropertyChanged(nameof(Intervall.Prosent)); // Oppdaterer UI for hver endring
                RecalculateTotalPercentage();
            }
        }


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

            if (IsFargekartSelected)
            {
                var PunkterMesh = NullstillZVerdierOgLagreIAnalyseListe(punkterMesh);
                Fargemannen.Model.AnalyseXYModel.Start(PunkterMesh, RuteStørresle);
            
           
                lengdeVerdierXY = AnalyseXYModel.lengdeVerdier;
                UpdateIntervalsAndCalculatePercentages();
                FetchValues();
                var sortedValues = Fargemannen.Model.AnalyseXYModel.lengdeVerdier.OrderBy(x => x).ToList();
                ed.WriteMessage(Fargemannen.Model.AnalyseXYModel.lengdeVerdier.Count.ToString());

            }
            else 
            {
                string placeHolder = "HvaEr";
                string analysetype = "XY";
                DukXYModel Duk = new DukXYModel();
                Duk.KjørDukPåXY(pointsToSymbol, BergmodellLagNavn, placeHolder, analysetype);
                lengdeVerdierXY = Model.DukXYModel.VerdierXY;
                UpdateIntervalsAndCalculatePercentages();
                FetchValues();
                var sortedValues = Fargemannen.Model.AnalyseXYModel.lengdeVerdier.OrderBy(x => x).ToList();
            }
        }
        public List<Intervall> GetIntervallListe()
        {
            return Intervaller.ToList();
        }


        public void LagFargekart()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            var intervallListe = AnalyseXYViewModel.Instance.GetIntervallListe();

            if (IsFargekartSelected)
            {
                Model.AnalyseXYModel.PlasserFirkanterIIntervallLayersOgFyllMedFarge(SliderValue, intervallListe);
            }
            else
            {
                DukXYModel Duk = new DukXYModel();
                Duk.FargeMesh(lengdeVerdierXY, BergmodellLagNavn, intervallListe);
            }

            ed.WriteMessage($"{SliderValue}");
        }


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
                    //CalculateAndUpdatePercentage();
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
                // Rund av verdien til nærmeste hele tall før du sammenligner og setter den
                double roundedValue = Math.Round(value);
                if (_prosent != roundedValue)
                {
                    _prosent = roundedValue;
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
                   //UpdateLayerColor(Navn, Farge);
                }
            }
        }

        private void UpdateLayerColor(string layerName, string colorHex)
        {
            if (!string.IsNullOrEmpty(layerName))
            {
                Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;

                using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                {
                    using (acDoc.LockDocument())
                    {
                        LayerTable lt = (LayerTable)acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead);
                        if (lt.Has(layerName))  // Sjekk om laget eksisterer
                        {
                            LayerTableRecord ltr = (LayerTableRecord)acTrans.GetObject(lt[layerName], OpenMode.ForWrite);
                            ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColor(ConvertHexToDrawingColor(colorHex));
                            OnPropertyChanged(nameof(Brush));
                            acTrans.Commit();  // Commit kun hvis endringer er gjort
                            acDoc.Editor.Regen();
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
            
            var lengdeVerdier = Model.DukXYModel.VerdierXY;
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

