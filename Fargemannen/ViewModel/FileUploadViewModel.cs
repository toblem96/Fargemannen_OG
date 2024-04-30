using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Autodesk.AutoCAD.Customization;
using Microsoft.Win32;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using System.Runtime.CompilerServices;

using GalaSoft.MvvmLight.Command;
using System.Windows.Controls;
using System.IO;
using System.Collections.ObjectModel;

namespace Fargemannen.ViewModel
{
    public class FileUploadViewModel : INotifyPropertyChanged
    {
        private static FileUploadViewModel instance;

        private string _fullSosiFilePath;
        private string _fullsosidagenFilePath;
        private string _fullKofFilePath;
       
        private string _reportFolderPath;
        private string _sampleResultsFolderPath;
        public Dictionary<string, string> ReportFiles { get; private set; }
        public Dictionary<string, string> SampleResultFiles { get; private set; }

        private string _errorMessage;
        public List<string> TotPaths;
   


        public string SosiFilePath
        {
            get => _fullSosiFilePath;
            set
            {
                if (_fullSosiFilePath == value) return;

                _fullSosiFilePath = value; // Lagrer den fullstendige stien
                OnPropertyChanged(nameof(SosiFilePath));
                OnPropertyChanged(nameof(DisplaySosiFilePath)); // Oppdater visningssti når full sti endres
                ClearError();
            }
        }
        public string DisplaySosiFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_fullSosiFilePath))
                {
                    return ""; // Returnerer en tom streng hvis stien er null eller tom
                }

                var parts = _fullSosiFilePath.Split('\\');
                return parts.Length > 1 ? string.Join("\\", parts.Skip(parts.Length - 2)) : _fullSosiFilePath;
            }
        }

        public string SosidagenFilePath
        {
            get => _fullsosidagenFilePath;
            set
            {
               
                if (_fullsosidagenFilePath == value) return;
                _fullsosidagenFilePath = value;
                OnPropertyChanged(nameof(SosidagenFilePath));
                OnPropertyChanged(nameof(DisplaySosidagenFilePath));
                ClearError();
            }
        }

        public string DisplaySosidagenFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_fullsosidagenFilePath))
                {
                    return ""; // Returnerer en tom streng hvis stien er null eller tom
                }

                var parts = _fullsosidagenFilePath.Split('\\');
                return parts.Length > 1 ? string.Join("\\", parts.Skip(parts.Length - 2)) : _fullsosidagenFilePath;
            }
        }

        public string KofFilePath
        {
            get => _fullKofFilePath;
            set
            {
               
                if (_fullKofFilePath == value) return;
                _fullKofFilePath = value;
                OnPropertyChanged(nameof(KofFilePath));
                OnPropertyChanged(nameof(DisplayKofFilePath));
                ClearError();
            }
        }
        public string DisplayKofFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_fullKofFilePath))
                {
                    return ""; // Returnerer en tom streng hvis stien er null eller tom
                }

                var parts = _fullKofFilePath.Split('\\');
                return parts.Length > 1 ? string.Join("\\", parts.Skip(parts.Length - 2)) : _fullKofFilePath;
            }
        }
        public ObservableCollection<KeyValuePair<string, string>> TotFilePaths { get; private set; }


   
        public List<string> GetTotFilePathsAsList()
        {
            return new List<string>(TotPaths);
        }
        public void UpdateTotPaths()
        {
            TotPaths = TotFilePaths.Select(pair => pair.Key).ToList();  // Kopierer bare stiene
            OnPropertyChanged(nameof(TotPaths));
        }


        private string _infoTot;
        public string InfoTot
        {
            get => _infoTot;
            set
            {
                _infoTot = value;
                OnPropertyChanged(nameof(InfoTot));
            }
        }
        public string DisplayReportFolderPath
        {
            get => Path.GetFileName(_reportFolderPath);
        }
        public string ReportFolderPath
        {
            get => _reportFolderPath;
            set
            {
                if (_reportFolderPath != value)
                {
                    _reportFolderPath = value;
                    OnPropertyChanged(nameof(ReportFolderPath));
                    OnPropertyChanged(nameof(DisplayReportFolderPath));  // Trigger oppdatering av visning
                    ClearError();
                    Model.ProsseseringAvFiler.PDFpross();
                    UpdateReportFilesDictionary();
                }
            }
        }
        public string SampleResultsFolderPath
        {
            get => _sampleResultsFolderPath;
            set
            {
                if (_sampleResultsFolderPath != value)
                {
                    _sampleResultsFolderPath = value;
                    OnPropertyChanged(nameof(SampleResultsFolderPath));
                    OnPropertyChanged(nameof(DisplaySampleResultsFolderPath));  // Trigger oppdatering av visning
                    ClearError();
                    Model.ProsseseringAvFiler.PDFpross();
                    UpdateSampleResultsFilesDictionary();
                }
            }
        }

        public string DisplaySampleResultsFolderPath
        {
            get => Path.GetFileName(_sampleResultsFolderPath);
        }
     
        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }
        
        public static FileUploadViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new FileUploadViewModel();
                    System.Diagnostics.Debug.WriteLine("FileUploadViewModel instantiated");  // Diagnostisk utskrift
                }
                return instance;
            }
        }
  

        public ICommand UploadSosiCommand { get; private set; }
        public ICommand UploadSosidagenCommand { get; private set; }
        public ICommand UploadKofCommand { get; private set; }
        public ICommand UploadTotCommand { get; private set; }
        public ICommand UploadReportFolderCommand { get; private set; }
        public ICommand UploadSampleResultsFolderCommand { get; private set; }

        public FileUploadViewModel()
        {
            UploadSosiCommand = new RelayCommand(UploadSosi);
            UploadSosidagenCommand = new RelayCommand(UploadSosidagen);
            UploadKofCommand = new RelayCommand(UploadKof);
            UploadTotCommand = new RelayCommand(UploadTot);
            UploadReportFolderCommand = new RelayCommand(UploadReportFolder);
            UploadSampleResultsFolderCommand = new RelayCommand(UploadSampleResultsFolder);

            ReportFiles = new Dictionary<string, string>();
            SampleResultFiles = new Dictionary<string, string>();
            TotPaths = new List<string>();
            TotFilePaths = new ObservableCollection<KeyValuePair<string, string>>();  // Initialiserer samlingen

        }
        private void UploadReportFolder()
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                ReportFolderPath = folderDialog.SelectedPath;
        }

        private void UploadSampleResultsFolder()
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                SampleResultsFolderPath = folderDialog.SelectedPath;
        }

        private void UpdateReportFilesDictionary()
        {
            ReportFiles.Clear();
            foreach (var filePath in Directory.GetFiles(ReportFolderPath, "*.pdf"))
            {
                var fileName = Path.GetFileName(filePath);
                fileName = fileName.Substring(0, fileName.Length - 4);  // Remove the last four characters (.PDF)
                if (!ReportFiles.ContainsKey(fileName))
                    ReportFiles.Add(fileName, filePath);
            }
            //PrintDictionaryContents(ReportFiles, "Rapport Files");
        }

        private void UpdateSampleResultsFilesDictionary()
        {
            SampleResultFiles.Clear();
            foreach (var filePath in Directory.GetFiles(SampleResultsFolderPath, "*.pdf"))
            {
                var fileName = Path.GetFileName(filePath);
                fileName = fileName.Substring(0, fileName.Length - 4);  // Remove the last four characters (.PDF)
                if (!SampleResultFiles.ContainsKey(fileName))
                    SampleResultFiles.Add(fileName, filePath);
            }
            //PrintDictionaryContents(SampleResultFiles, "Sample Result Files");
        }
        private void UploadSosi()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "SOSI-filer|*.sos"
            };

            if (fileDialog.ShowDialog() == true)
            {
                SosiFilePath = fileDialog.FileName;  // Change to FileName to store the full path

               
            }
            Fargemannen.ApplicationInsights.AppInsights.TrackEvent("Opplastet SOSI-filer");
        }


        private void UploadSosidagen()
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "SOSI-filer|*.sos"  // Placeholder, replace with actual extension
            };

            if (fileDialog.ShowDialog() == true)
            {
                SosidagenFilePath = fileDialog.FileName;
            }
            Fargemannen.ApplicationInsights.AppInsights.TrackEvent("Opplastet SosiIdagen-filer");
        }

        private void UploadKof()
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "KOF-filer|*.kof"  // Placeholder, replace with actual extension
            };

            if (fileDialog.ShowDialog() == true)
            {
                KofFilePath = fileDialog.FileName;
            }
            Fargemannen.ApplicationInsights.AppInsights.TrackEvent("Opplastet KOF-filer");
        }

        private void UploadTot()
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "TOT-filer|*.TOT",
                Multiselect = true
            };

            bool? success = fileDialog.ShowDialog();

            if (success == true)
            {
                TotFilePaths.Clear();  // Tøm listen før nye filer legges til
                foreach (string filePath in fileDialog.FileNames)
                {
                    var fileName = Path.GetFileName(filePath);
                    TotFilePaths.Add(new KeyValuePair<string, string>(filePath, fileName));
                }
                Fargemannen.ApplicationInsights.AppInsights.TrackEvent("Opplastet TOT-filer");
            }
            else
            {
                TotFilePaths.Clear();
                TotFilePaths.Add(new KeyValuePair<string, string>("", "Ingen filer ble valgt."));
            }
            UpdateTotPaths();  // Du kan beholde denne hvis du trenger en liste av bare stiene et annet sted
        }


        private void PrintDictionaryContents(Dictionary<string, string> dictionary, string dictionaryName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            ed.WriteMessage($"\nContents of {dictionaryName}:\n");
            foreach (KeyValuePair<string, string> entry in dictionary)
            {
                ed.WriteMessage($"Key: {entry.Key}, Value: {entry.Value}\n");
            }
        }

        private void ClearError()
        {
            ErrorMessage = "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
