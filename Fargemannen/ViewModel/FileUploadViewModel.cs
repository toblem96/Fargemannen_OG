﻿using System;
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

namespace Fargemannen.ViewModel
{
    public class FileUploadViewModel : INotifyPropertyChanged
    {
        private static FileUploadViewModel instance;
        private string _sosiFilePath;
        private string _sosidagenFilePath;
        private string _kofFilePath;
        private string _totFilePath;
        private string _reportFolderPath;
        private string _sampleResultsFolderPath;
        public Dictionary<string, string> ReportFiles { get; private set; }
        public Dictionary<string, string> SampleResultFiles { get; private set; }

        private string _errorMessage;


        public string SosiFilePath
        {
            get => _sosiFilePath;
            set
            {
                if (_sosiFilePath == value) return;
                _sosiFilePath = value;
                OnPropertyChanged(nameof(SosiFilePath));
                ClearError();
                System.Diagnostics.Debug.WriteLine($"SosiFilePath updated: {_sosiFilePath}");  // Diagnostisk utskrift
            }
        }

        public string SosidagenFilePath
        {
            get => _sosidagenFilePath;
            set
            {
                _sosidagenFilePath = value;
                OnPropertyChanged(nameof(SosidagenFilePath));
                ClearError();
            }
        }

        public string KofFilePath
        {
            get => _kofFilePath;
            set
            {
                _kofFilePath = value;
                OnPropertyChanged(nameof(KofFilePath));
                ClearError();
            }
        }

        public string TotFilePath
        {
            get => _totFilePath;
            set
            {
                _totFilePath = value;
                OnPropertyChanged(nameof(TotFilePath));
                ClearError();
            }
        }
        public string ReportFolderPath
        {
            get => _reportFolderPath;
            set
            {
                _reportFolderPath = value;
                OnPropertyChanged(nameof(ReportFolderPath));
                ClearError();
                Model.ProsseseringAvFiler.PDFpross();
                UpdateReportFilesDictionary();
            }
        }

        public string SampleResultsFolderPath
        {
            get => _sampleResultsFolderPath;
            set
            {
                _sampleResultsFolderPath = value;
                OnPropertyChanged(nameof(SampleResultsFolderPath));
                ClearError();
                Model.ProsseseringAvFiler.PDFpross();
                UpdateSampleResultsFilesDictionary();
            }
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
            PrintDictionaryContents(ReportFiles, "Rapport Files");
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
            PrintDictionaryContents(SampleResultFiles, "Sample Result Files");
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
            else
            {
                ErrorMessage = "Ingen SOSI fil ble valgt.";
            }
            doc.Editor.WriteMessage(SosiFilePath + "------------------------------------");
        }

        private void ClearError()
        {
            ErrorMessage = "";
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
        }

        private void UploadTot()
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "TOT-filer|*.tot"  // Placeholder, replace with actual extension
            };

            if (fileDialog.ShowDialog() == true)
            {
                TotFilePath = fileDialog.FileName;
            }
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
