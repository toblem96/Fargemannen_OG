using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.AutoCAD.Geometry;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using System.Windows.Controls;
using Fargemannen.ViewModel;



namespace Fargemannen.Model
{
    public class ProsseseringAvFiler
    {

        public static Dictionary<string, Tuple<string, string>> PunkterPerPdf { get; private set; } = new Dictionary<string, Tuple<string, string>>();

        public static void ProcessSOSIFilesBor(int minAr, List<string> borremetoderSymbol, string sosiFilePath, List<PunktInfo> pointsToSymbol, List<Point3d> punkterMesh, string NummerType, string ProsjektType)
        {
            string data;
           

            try
            {
                data = File.ReadAllText(sosiFilePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Kunne ikke lese SOSI filen: {ex.Message}");
            }

            string[] lines = data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            double x = 0, y = 0, z = 0;
            bool nextLineHasCoordinates = false;
            int currentYear = 0;
            string MainID = "", currentPunktID = "X", currentGBUMethod = "";
            double terrengkvote = 0.0, bor_fjell = 0.0, bor_løs = 0.0;
            string rapportID = ProsjektType;

            foreach (var line in lines)
            {
                if (line.StartsWith(".PUNKT"))
                {
                    string punktIDWithColon = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                    MainID = punktIDWithColon.TrimEnd(':');
                    if (NummerType == "SOSINummer")
                    {
                        currentPunktID = MainID;
                    }
                }
                else if (line.StartsWith("..PDF_BORENUMMER") && NummerType != "SOSINummer")
                {
                    currentPunktID = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                }

                // Bestemme Rapport ID basert på ProsjektType
                if (ProsjektType == "PDF-nummer" && line.StartsWith("..PDF_KOMMUNE_INTERNT_FILNAVN"))
                {
                    rapportID = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                }
                else if (ProsjektType == "Saksnummer" && line.StartsWith("..ID_SAKSNUMMER"))
                {
                    rapportID = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                }

                // Andre feltbehandlinger
                if (line.Contains("..FJELLKOTE"))
                {
                    double.TryParse(line.Split(' ')[1], out z);
                }
                else if (line.StartsWith("..NØ"))
                {
                    nextLineHasCoordinates = true;
                }
                else if (line.StartsWith("..BORELENGDE"))
                {
                    double.TryParse(line.Split(' ')[1], out bor_løs);
                }
                else if (line.StartsWith("..RAPPORTDATO"))
                {
                    int.TryParse(line.Split(' ')[1], out currentYear);
                }
                else if (line.StartsWith("..BORET_LENGDE_I_BERG"))
                {
                    double.TryParse(line.Split(' ')[1], out bor_fjell);
                }
                else if (line.StartsWith("..TERRENGKOTE_HISTORISK"))
                {
                    double.TryParse(line.Split(' ')[1], out terrengkvote);
                }
                else if (line.StartsWith("..GBU_METODE"))
                {
                    var splitByQuotes = line.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries);
                    if (splitByQuotes.Length > 1)
                    {
                        var wordsInQuotes = splitByQuotes[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        currentGBUMethod = wordsInQuotes[0];
                    }
                    else
                    {
                        currentGBUMethod = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                    }

                    if (currentGBUMethod == "Bergkontrollboring")
                    {
                        currentGBUMethod = "Fjellkontrollboring";
                    }
                }

                // Lagring av punkter og metadata
                if (nextLineHasCoordinates)
                {
                    var coords = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (coords.Length >= 2 && double.TryParse(coords[0], out y) && double.TryParse(coords[1], out x))
                    {
                        x /= 100; // Konvertering hvis nødvendig
                        y /= 100;

                        if (currentYear >= minAr && borremetoderSymbol.Contains(currentGBUMethod))
                        {
                            Point3d point = new Point3d(x, y, z);
                            punkterMesh.Add(point);

                            PunktInfo info = new PunktInfo
                            {
                                MinPunktID = MainID,
                                Punkt = point,
                                RapportID = rapportID,
                                PunktID = currentPunktID,
                                GBUMetode = currentGBUMethod,
                                Terrengkvote = terrengkvote,
                                BoreFjell = bor_fjell,
                                BorLøs = bor_løs
                            };

                            pointsToSymbol.Add(info);
                        }
                        nextLineHasCoordinates = false;  // Resetter flagget
                    }
                }
            }
        }





        //BERG I DAGEN 

        public static void ProcessSOSIFilesIDagen(int minAr, List<string> borremetoderSymbol, string sosiFilePath, List<PunktInfo> pointsToSymbol, List<Point3d> punkterMesh, string NummerType, string ProsjektType)
        {
            string data;
            try
            {
                data = File.ReadAllText(sosiFilePath);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error reading file: " + e.Message, "File Read Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string[] lines = data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            double x = 0, y = 0, z = 0;
            bool nextLineHasCoordinates = false;
            int currentYear = 0;
            string MainID = "", currentPunktID = "X", currentGBUMethod = "";
            double terrengkvote = 0.0, bor_fjell = 0.0, bor_løs = 0.0;
            string rapportID = ProsjektType;

            foreach (var line in lines)
            {
                if (line.StartsWith(".PUNKT"))
                {
                    string punktIDWithColon = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                    MainID = punktIDWithColon.TrimEnd(':');
                    if (NummerType == "SOSINummer")
                    {
                        currentPunktID = MainID;
                    }
                }
                else if (line.StartsWith("..PDF_BORENUMMER") && NummerType != "SOSINummer")
                {
                    currentPunktID = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                }

                if (ProsjektType == "PDF-nummer" && line.StartsWith("..PDF_KOMMUNE_INTERNT_FILNAVN"))
                {
                    rapportID = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                }
                else if (ProsjektType == "Saksnummer" && line.StartsWith("..ID_SAKSNUMMER"))
                {
                    rapportID = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                }

                if (line.Contains("..FJELLKOTE"))
                {
                    string boreLengdeStr = line.Split(' ')[1];
                    double.TryParse(boreLengdeStr, out z);
                }
                else if (line.StartsWith("..NØ"))
                {
                    nextLineHasCoordinates = true;
                }
                else if (line.StartsWith("..BORELENGDE"))
                {
                    string bor_løsStr = line.Split(' ')[1];
                    double.TryParse(bor_løsStr, out bor_løs);
                }
                else if (line.StartsWith("..RAPPORTDATO"))
                {
                    string yearStr = line.Split(' ')[1];
                    int.TryParse(yearStr, out currentYear);
                }
                else if (line.StartsWith("..BORET_LENGDE_I_BERG"))
                {
                    string borelengdeIBergStr = line.Split(' ')[1];
                    double.TryParse(borelengdeIBergStr, out bor_fjell);
                }
                else if (line.StartsWith("..TERRENGKOTE_HISTORISK"))
                {
                    string terrengkoteHistStr = line.Split(' ')[1];
                    double.TryParse(terrengkoteHistStr, out terrengkvote);
                }
                else if (line.StartsWith("..RAPPORTID") && ProsjektType == "Standard")
                {
                    rapportID = line.Substring(line.IndexOf(' ') + 1).Trim('"', ' ');
                }

                if (nextLineHasCoordinates)
                {
                    var coords = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (coords.Length >= 2 && double.TryParse(coords[0], out y) && double.TryParse(coords[1], out x))
                    {
                        x /= 100; // Konvertering hvis nødvendig
                        y /= 100; // Konvertering hvis nødvendig

                        if (currentYear >= minAr && borremetoderSymbol.Contains(currentGBUMethod))
                        {
                            Point3d point = new Point3d(x, y, z);
                            punkterMesh.Add(point);

                            PunktInfo info = new PunktInfo
                            {
                                MinPunktID = MainID,
                                Punkt = point,
                                RapportID = rapportID,
                                PunktID = currentPunktID,
                                GBUMetode = currentGBUMethod,
                                Terrengkvote = terrengkvote,
                                BoreFjell = bor_fjell,
                                BorLøs = bor_løs
                            };

                            pointsToSymbol.Add(info);
                        }
                        nextLineHasCoordinates = false; // Resetter etter koordinater er lagret
                    }
                }
            }
        }


        //TOT OG KOF
        public static void ProcessTOTandKOFFiles(List<string> totFilePaths, string kofFilePath, List<PunktInfo> pointsToSymbol, List<Point3d> punkterMesh)
        {
            foreach (string totFilePath in totFilePaths)
            {
                // Les TOT-fil
                string totData = File.ReadAllText(totFilePath);
                string[] totLines = totData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                string currentPunktID = null;
                string currentGBUMethod = "Totalsondering";

                // Hente HK verdi (borrepunkt-ID) fra TOT-fil
                string rapportID = totLines[1].Split(',')[10].Split('=')[1];

                // Hente D verdi (borLeng) fra siste linje av TOT-fil
                string lastLine = totLines[totLines.Length - 1];
                string borLengStr = lastLine.Split(',')[0].Split('=')[1];
                double.TryParse(borLengStr, out double borLeng);

                // Hente D verdi (bor_løs) fra linjen hvor K=41
                double bor_løsKof = 0;
                foreach (string line in totLines)
                {
                    if (line.Contains("K=41"))
                    {
                        string borLosStr = line.Split(',')[0].Split('=')[1];
                        double.TryParse(borLosStr, out bor_løsKof);
                        break;
                    }
                }

                // Les KOF-fil
                string[] kofLines = File.ReadAllLines(kofFilePath);

                // Finn og hent data fra KOF-fil basert på currentPunktIDKof
                foreach (string line in kofLines)
                {
                    if (line.Contains(rapportID))
                    {
                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        double.TryParse(parts[3], out double xKof);
                        double.TryParse(parts[4], out double yKof);
                        double.TryParse(parts[5], out double terrengkote);

                        // Beregninger
                        double bor_fjellKof = borLeng - bor_løsKof;
                        double zKof = terrengkote - borLeng;

                        // Opprett nytt punkt
                        Point3d newPointKof = new Point3d(xKof, yKof, zKof);

                        // Opprett tuple
                        
                        punkterMesh.Add(newPointKof);




                        PunktInfo info = new PunktInfo
                        {
                            
                            Punkt = newPointKof,
                            RapportID = rapportID,
                            PunktID = currentPunktID,
                            GBUMetode = currentGBUMethod,
                            Terrengkvote = terrengkote,
                            BoreFjell = bor_fjellKof,
                            BorLøs = bor_løsKof
                        };

                        pointsToSymbol.Add(info);


                        break; // Går ut av KOF-filsøk-loop når matchende linje er funnet
                    }
                }
            }
        }




        public static void PDFpross()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            PunkterPerPdf.Clear();

            FileUploadViewModel fileViewModel = FileUploadViewModel.Instance;
            string sosiFilePath = fileViewModel.SosiFilePath;

            string data;
            try
            {
                data = File.ReadAllText(sosiFilePath);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error reading file: " + e.Message, "File Read Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string[] lines = data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            string currentPunktID = "", currentPdfRapportNummer = "", currentPrøvePath = "";

            foreach (var line in lines)
            {
                if (line.StartsWith(".PUNKT"))
                {
                    // Reset variables for each new punkt
                    currentPunktID = "";
                    currentPdfRapportNummer = "";
                    currentPrøvePath = "";

                    string punktIDWithColon = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                    currentPunktID = punktIDWithColon.TrimEnd(':');
                }
                else if (line.StartsWith("..PDF_KOMMUNE_INTERNT_FILNAVN") && !string.IsNullOrEmpty(currentPunktID))
                {
                    currentPdfRapportNummer = line.Split(' ').Last();
                }
                else if (line.StartsWith("..PDF_URL_PRØVE") && !string.IsNullOrEmpty(currentPunktID))
                {
                    currentPrøvePath = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last().Trim();
                    currentPrøvePath = Path.GetFileNameWithoutExtension(currentPrøvePath); // Retrieves the whole file name without extension
                }

                /*
                if (!string.IsNullOrEmpty(currentPunktID) && !string.IsNullOrEmpty(currentPdfRapportNummer))
                {
                    if (!PunkterPerPdf.ContainsKey(currentPunktID))
                    {
                        PunkterPerPdf[currentPunktID] = new Tuple<string, string>(currentPdfRapportNummer, currentPrøvePath);
                    }
                }
                */  
            }

            //Print all key-value pairs (Punkt-ID and associated file numbers)
            foreach (var item in PunkterPerPdf)
            {
                string melding = $"\nPunkt-ID: {item.Key}, Rapportnummer: {item.Value.Item1}, Prøvenummer: {item.Value.Item2}";
                doc.Editor.WriteMessage(melding);
            }
        }

        public static void HentPunkter(List<PunktInfo> pointsToSymbol, List<Point3d> punkterMesh, int minAr, List<string> boreMetoder, string NummerType, string ProsjektType)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            FileUploadViewModel fileViewModel = FileUploadViewModel.Instance;

            string sosiFilePath = fileViewModel.SosiFilePath;
            string sosiDagenFilePath = fileViewModel.SosidagenFilePath;

            try
            {
                ValidateInput(pointsToSymbol, punkterMesh, minAr, boreMetoder, sosiFilePath, sosiDagenFilePath);
                // Hvis validering er vellykket, prosesser filer
                ProcessSOSIFilesBor(minAr, boreMetoder, sosiFilePath, pointsToSymbol, punkterMesh, NummerType, ProsjektType);
                ProcessSOSIFilesIDagen(minAr, boreMetoder, sosiDagenFilePath, pointsToSymbol, punkterMesh, NummerType, ProsjektType);
            }
            catch (InvalidOperationException ex)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog($"Input valideringsfeil: {ex.Message}");
                return; // Avslutter metoden for å forhindre videre utførelse ved input feil
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage($"\nAutodesk AutoCAD Runtime Exception: {ex.Message}");
                return; // Avslutter metoden ved AutoCAD-spesifikke unntak
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nGenerell feil oppstod: {ex.Message}");
                return; // Avslutter metoden for alle andre typer unntak
            }
        }

        public static void ValidateInput(List<PunktInfo> pointsToSymbol, List<Point3d> punkterMesh, int minAr, List<string> boreMetoder, string sosiFilePath, string sosiDagenFilePath)
        {
       

            // Sjekker om 'minÅr' er et gyldig år
            if (minAr <= 0)
            {
                throw new InvalidOperationException("Feil: 'minÅr' må være et positivt tall og representere et gyldig år.");
            }

            // Sjekker om minst en av filstiene ikke er tom
            if (string.IsNullOrWhiteSpace(sosiFilePath) && string.IsNullOrWhiteSpace(sosiDagenFilePath))
            {
                throw new InvalidOperationException("Feil: Minst en av filstiene 'sosiFilePath' eller 'sosiDagenFilePath' må inneholde en gyldig sti.");
            }
        }

    }
    }




