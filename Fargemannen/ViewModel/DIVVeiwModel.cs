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

using System.IO; // For Path handling

namespace Fargemannen.ViewModel
{
    public class DIVVeiwModel : INotifyPropertyChanged
    {
        private ObservableCollection<SonderingTypeXL> _sonderingTypesXL;

        private static DIVVeiwModel _instance;
        public static DIVVeiwModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DIVVeiwModel();
                }
                return _instance;
            }
        }
        public ObservableCollection<SonderingTypeXL> SonderingTypesXL
        {
            get => _sonderingTypesXL;
            set
            {
                _sonderingTypesXL = value;
                OnPropertyChanged(nameof(SonderingTypesXL)); //ENDRET HER: Sørger for at endringer i listen oppdaterer UI
            }
        }
        private string _reportName;
        private int _intValue;
        private string _savePath;
        private string _BergmodellLagNavn = "Bergmodell_Lag";
        private string _TerrengModellLagNavn = "C-TOPO-GRID";

        public string ReportName
        {
            get => _reportName;
            set
            {
                _reportName = value;
                OnPropertyChanged(nameof(ReportName));
            }
        }

        public int IntValue
        {
            get => _intValue;
            set
            {
                _intValue = value;
                OnPropertyChanged(nameof(IntValue));
            }
        }

        public string SavePath
        {
            get => _savePath;
            set
            {
                _savePath = value;
                OnPropertyChanged(nameof(SavePath));
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

        // Legg til en kommando for å åpne SaveFileDialog

        public ICommand SaveCommand { get; private set; }
        public ICommand kjørRapport { get; private set; }

        public DIVVeiwModel()
        {
           

            SonderingTypesXL = new ObservableCollection<SonderingTypeXL>
        {
            new SonderingTypeXL { Name = "Totalsondering", IsChecked = true },
            new SonderingTypeXL { Name = "Dreietrykksondering", IsChecked = false },
            new SonderingTypeXL { Name = "Trykksondering", IsChecked = false },
            new SonderingTypeXL { Name = "Prøveserie", IsChecked = true },
            new SonderingTypeXL { Name = "Poretrykksmåler", IsChecked = false },
            new SonderingTypeXL { Name = "Vingeboring", IsChecked = false },
            new SonderingTypeXL { Name = "Fjellkontrollboring", IsChecked = false },
            new SonderingTypeXL { Name = "Dreiesondering", IsChecked = false },
            new SonderingTypeXL { Name = "Prøvegrop", IsChecked = false },
            new SonderingTypeXL { Name = "Ramsondering", IsChecked = false },
            new SonderingTypeXL { Name = "Enkel", IsChecked = false },
            new SonderingTypeXL { Name = "Fjellidagen", IsChecked = true }
        };

            SaveCommand = new RelayCommand(ExecuteSaveCommand);
            kjørRapport = new RelayCommand(KjørEXCL);
        }

        private void ExecuteSaveCommand()
        {
            // Opprett en instans av FolderBrowserDialog
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            // Sett eventuelle egenskaper for dialogen, for eksempel beskrivelsen
            folderBrowserDialog.Description = "Velg en mappe for å lagre rapporten";

            // Vis dialogen og sjekk om brukeren klikker OK
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Lagre den valgte mappen
                SavePath = folderBrowserDialog.SelectedPath;
            }
        }


        LegdenModel legdenModel = new LegdenModel();

        
        public void KjørEXCL()
        {

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            ed.WriteMessage(SavePath);
            ed.WriteMessage(ReportName);
            ed.WriteMessage(IntValue.ToString());
            ed.WriteMessage(TerrengModellLagNavn);
            ed.WriteMessage(BergmodellLagNavn);

            var selectedTypesZ = SonderingTypesXL.Where(x => x.IsChecked).Select(x => x.Name).ToList();


           
            List<PunktInfo> pointsToSymbol = new List<PunktInfo>();
            List<Point3d> punkterMesh = new List<Point3d>();
            string NummerType = "";
            string ProjectType = "";
            int MinYear = 1;


            ed.WriteMessage(punkterMesh.Count.ToString());
            ProsseseringAvFiler.HentPunkter(pointsToSymbol, punkterMesh, MinYear, selectedTypesZ, NummerType, ProjectType);

            EXCEL eXCEL = new EXCEL();
            eXCEL.kjørTestRapport(pointsToSymbol, SavePath, IntValue, ReportName, TerrengModellLagNavn, BergmodellLagNavn);

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
    public class SonderingTypeXL : INotifyPropertyChanged
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