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
using DocumentFormat.OpenXml.Spreadsheet;
using System.Globalization;



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
                if ((rapportID == "PDF-nummer") || (rapportID == "Saksnummer"))
                {
                    rapportID = "";
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
                        x /= 100; 
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
            int currentYear = 10;
            string MainID = "", currentPunktID = "", currentGBUMethod = "Fjellidagen";
            double terrengkvote = 0.0, bor_fjell = 0.0, bor_løs = 0.0;
            string rapportID = ProsjektType;

            foreach (var line in lines)
            {
                if (line.StartsWith(".PUNKT"))
                {
                    // Resett alle variabler for nytt punkt, inkludert rapportID satt til ProsjektType
                    rapportID = ProsjektType;
                    MainID = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last().TrimEnd(':') + ".FJELL";
                    currentPunktID = (NummerType == "SOSINummer") ? MainID : "";
                    terrengkvote = 0.0;
                    bor_fjell = 0.0;
                    bor_løs = 0.0;
                    currentYear = 10;
                    nextLineHasCoordinates = false;
                }
                else if (line.StartsWith("..PDF_BORENUMMER") && NummerType != "SOSINummer")
                {
                    currentPunktID = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last() + ".FJELL";
                }

                if ((ProsjektType == "PDF-nummer") && line.StartsWith("..PDF_KOMMUNE_INTERNT_FILNAVN"))
                {
                    rapportID = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last() ;
                }
                else if ((ProsjektType == "Saksnummer") && line.StartsWith("..ID_SAKSNUMMER"))
                {
                    rapportID = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                }
                if ((rapportID == "PDF-nummer") || (rapportID == "Saksnummer"))
                {
                    rapportID = "";
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
                        x /= 100; // Conversion if necessary
                        y /= 100; // Conversion if necessary
                        nextLineHasCoordinates = false; // Reset after coordinates are saved

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
                    }
                }
            }
        }



        //TOT OG KOF
        public static void ProcessTOTandKOFFiles(List<string> totFilePaths, string kofFilePath, List<PunktInfo> pointsToSymbol, List<Point3d> punkterMesh)
        {
            int i=0;
            foreach (string totFilePath in totFilePaths)
            {
                // Les TOT-fil
                string totData = File.ReadAllText(totFilePath);
                string[] totLines = totData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                
                string MainID = "TOT_"+i;
                i++;
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
                            MinPunktID = MainID,
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
            string sosiDagenFilePath = fileViewModel.SosidagenFilePath;

            // Les og prosesser hver fil
            ReadAndProcessFile(sosiFilePath, false);
            ReadAndProcessFile(sosiDagenFilePath, true);

            // Hjelpemetode for å lese og prosessere en gitt filsti
            void ReadAndProcessFile(string filePath, bool isDagenFile)
            {
                string data;
                try
                {
                    data = File.ReadAllText(filePath);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error reading file: " + e.Message, "File Read Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string[] lines = data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                string currentPunktID = "", currentPdfRapportNummer = "", currentPrøvePath = "";
                bool hasValidPdfData = false;

                foreach (var line in lines)
                {
                    if (line.StartsWith(".PUNKT"))
                    {
                        // Legg til gyldige data før reset hvis de er gyldige
                        if (hasValidPdfData && !string.IsNullOrEmpty(currentPunktID) && !PunkterPerPdf.ContainsKey(currentPunktID))
                        {
                            PunkterPerPdf[currentPunktID] = new Tuple<string, string>(currentPdfRapportNummer, currentPrøvePath);
                        }
                        currentPunktID = "";
                        currentPdfRapportNummer = "";
                        currentPrøvePath = "";
                        hasValidPdfData = false;  // Reset PDF data validity flag

                        string punktIDWithColon = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                        currentPunktID = punktIDWithColon.TrimEnd(':');

                        // Legger til "_FJELL" hvis data er fra sosiDagenFilePath
                        if (isDagenFile)
                        {
                            currentPunktID += ".FJELL";
                        }
                    }
                    else if (line.StartsWith("..PDF_KOMMUNE_INTERNT_FILNAVN") && !string.IsNullOrEmpty(currentPunktID))
                    {
                        currentPdfRapportNummer = line.Split(' ').Last();
                        hasValidPdfData = true;
                    }
                    else if (line.StartsWith("..PDF_URL_PRØVE") && !string.IsNullOrEmpty(currentPunktID))
                    {
                        currentPrøvePath = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last().Trim();
                        currentPrøvePath = Path.GetFileNameWithoutExtension(currentPrøvePath);
                        hasValidPdfData = true;
                    }
                 
                }

                // Add any remaining valid data
                if (!string.IsNullOrEmpty(currentPunktID) && !PunkterPerPdf.ContainsKey(currentPunktID))
                {
                    PunkterPerPdf[currentPunktID] = new Tuple<string, string>(currentPdfRapportNummer, currentPrøvePath);
                }
            }
        }





        public static void HentPunkter(List<PunktInfo> pointsToSymbol, List<Point3d> punkterMesh, int minAr, List<string> boreMetoder, string NummerType, string ProsjektType)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            FileUploadViewModel fileViewModel = FileUploadViewModel.Instance;

            string sosiFilePath = fileViewModel.SosiFilePath;
            string sosiDagenFilePath = fileViewModel.SosidagenFilePath;
            List<string> totFilesPaths = fileViewModel.TotPaths;
            string kofFilPath = fileViewModel.KofFilePath;

            foreach (string tot in totFilesPaths)
            {
                ed.WriteMessage(tot);
            }

            
            if (string.IsNullOrWhiteSpace(sosiDagenFilePath) && boreMetoder.Contains("Fjellidagen"))
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Du har valgt fjell i dagen, men ikke lagt til en relevant SOSI-fil");
                return;

            }


            try
            {
                foreach (var metode in boreMetoder)
                {
                    ed.WriteMessage("\nBoremetode: " + metode);
                }

               // ValidateInput(pointsToSymbol, punkterMesh, minAr, kofFilPath, sosiFilePath, sosiDagenFilePath);

                if (!string.IsNullOrWhiteSpace(sosiFilePath))
                {
                    ProcessSOSIFilesBor(minAr, boreMetoder, sosiFilePath, pointsToSymbol, punkterMesh, NummerType, ProsjektType);
                }
                if (!string.IsNullOrWhiteSpace(sosiDagenFilePath))
                {
                    ProcessSOSIFilesIDagen(minAr, boreMetoder, sosiDagenFilePath, pointsToSymbol, punkterMesh, NummerType, ProsjektType);
                }
                if (!string.IsNullOrWhiteSpace(kofFilPath))
                {
                    ProcessTOTandKOFFiles(totFilesPaths, kofFilPath, pointsToSymbol, punkterMesh);
                }
                var folderData = fileViewModel.FolderData; // Sørg for at denne er riktig initialisert i ViewModel
                HentBorepunktData(folderData, pointsToSymbol);









            }
            catch (InvalidOperationException ex)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog($"Input valideringsfeil: {ex.Message}");
                return; // Avslutter metoden for å forhindre videre utførelse ved input feil
            }

            
        
        }


        public static void ValidateInput(List<PunktInfo> pointsToSymbol, List<Point3d> punkterMesh, int minAr, string kof, string sosiFilePath, string sosiDagenFilePath)
        {
       

            // Sjekker om 'minÅr' er et gyldig år
            if (minAr <= 0)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Minimums grensen for År er ikke gyldig");
                return;
            }

            // Sjekker om minst en av filstiene ikke er tom
            if (string.IsNullOrWhiteSpace(sosiFilePath) && string.IsNullOrWhiteSpace(sosiDagenFilePath) && string.IsNullOrWhiteSpace(kof))
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Ingen gyldige filstier er lastet opp");
                return;
            }
        }
        public static void HentBorepunktData(Dictionary<int, FolderData> folderData, List<PunktInfo> pointsToSymbol)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            foreach (var folder in folderData)
            {
                foreach (var filePath in folder.Value.FilePaths)
                {
                    if (Directory.Exists(filePath))
                    {
                        var files = Directory.GetFiles(filePath);
                        foreach (var file in files)
                        {
                           // ed.WriteMessage(file);
                            var punktObjekter = LesOgProsesserFil(file, folder.Value.Prefix);
                            var punktObjekteMedMetode = GiReelGbuMetode(punktObjekter);
                            var punktInfo = PrioritereSymbol(punktObjekteMedMetode);
           
                            if (punktInfo != null)
                            {
                                pointsToSymbol.Add(punktInfo);
                                PrintPunktTilAutoCAD(punktInfo);
                            }
                        }
                    }
                    else if (File.Exists(filePath))
                    {
                        var punktObjekter = LesOgProsesserFil(filePath, folder.Value.Prefix);
                        var punktObjekteMedMetode = GiReelGbuMetode(punktObjekter);
                        var punktInfo = PrioritereSymbol(punktObjekteMedMetode);

                        if (punktInfo != null)
                        {
                            pointsToSymbol.Add(punktInfo);
                            PrintPunktTilAutoCAD(punktInfo);
                        }
                    }
                }
            }
        }
        private static void PrintPunktTilAutoCAD(PunktInfo punktInfo)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            ed.WriteMessage($"\nPunkt lagt til: PunktID = {punktInfo.PunktID}");
            ed.WriteMessage($"\tPunkt = {punktInfo.Punkt}");
            ed.WriteMessage($"\tGBUMetode = {punktInfo.GBUMetode}");
            ed.WriteMessage($"\tTerrengkvote = {punktInfo.Terrengkvote}");
            ed.WriteMessage($"\tBorLøs = {punktInfo.BorLøs}");
            ed.WriteMessage($"\tBoreFjell = {punktInfo.BoreFjell}");
            ed.WriteMessage($"\tStackBor = {string.Join(", ", punktInfo.StackBor)}");
            ed.WriteMessage($"\tPrefiks = {punktInfo.RapportID}");
        }

        private static List<PunktInfo> LesOgProsesserFil(string filePath, string prefix)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            List<PunktInfo> punktObjekter = new List<PunktInfo>();
            try
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length < 4) return null;

                var punktID = Path.GetFileNameWithoutExtension(filePath).Split('_').Last();
                punktID = punktID.Split(' ').First();
                var y = double.Parse(lines[0]);
                var x = double.Parse(lines[1]);
                var z1 = double.Parse(lines[2]);
               
                var terrengkvote = z1;

                var gbuMetode = "";
                var borLøs = 0.0;
                var boreFjell = 0.0;
                var stackBor = new List<string>();
                double identifiedValue = 0.0;
                var starCounter = 0;
                var funnet43 = false;
                var totalBoreLengde = 0.0;

                // Søk etter linjer som inneholder 43 i den femte kolonnen

                for (int i = 0; i < lines.Length; i++)  
                {
                    var parts = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                   
                    if (parts.Length >= 5 && parts[4] == "43")
                    {
                        borLøs = double.Parse(parts[0], CultureInfo.InvariantCulture);
                        funnet43 = true;
                        continue;
                    }

                    if (parts[0] == "*")
                    {
                        starCounter++;

                        if (starCounter == 3)
                        {
                            gbuMetode = lines[i + 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];
                        }
                        if (starCounter >= 4)
                        {
                            totalBoreLengde = double.Parse(lines[i - 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0]);
                            if (!funnet43)
                            {
                                borLøs = totalBoreLengde;
                            }
                            else { boreFjell = totalBoreLengde - borLøs; }


                            var punkt = new Point3d(x, y, terrengkvote-totalBoreLengde);
                            PunktInfo punktInfo = new PunktInfo
                            {
                                PunktID = punktID,
                                RapportID = prefix,
                                MinPunktID = punktID,
                                Punkt = punkt,
                                GBUMetode = gbuMetode,
                                Terrengkvote = terrengkvote,
                                BorLøs = borLøs,
                                BoreFjell = boreFjell,
                                StackBor = stackBor
                            };
                            punktObjekter.Add(punktInfo);
                            
                            gbuMetode = "";
                            borLøs = 0.0;
                            boreFjell = 0.0;
                            stackBor = new List<string>();
                            funnet43 = false;
                            //ed.WriteMessage("\n -------- objekt er laget!");

                            if (i + 2 < lines.Length && lines[i + 2] != "*" && !string.IsNullOrEmpty(lines[i + 1]) && lines[i + 1].Length > 4)
                            {
                                gbuMetode = lines[i + 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];
                            }

                          
                        }
                    }
                }
                
                return punktObjekter;

            }
            catch (Exception ex)
            {
                // Håndter eventuelle unntak
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                return null;
            }
        }


        private static PunktInfo PrioritereSymbol(List<PunktInfo> punktObjekter)
        {
            PunktInfo punktInfo = new PunktInfo();

            if (punktObjekter.Count == 0)
            {
                return punktInfo;  // Return an empty object if the list is empty
            }

            if (punktObjekter.Count == 1)
            {
                return punktObjekter[0];  // Return the only object if the list has one element
            }

            // Define priority order for GBUMetode
            List<string> priorityOrder = new List<string> { "totalsondering", "fjellkontrollboring", "dreietrykksondering" };

            PunktInfo prioritizedPunkt = null;

            foreach (var priority in priorityOrder)
            {
                prioritizedPunkt = punktObjekter.FirstOrDefault(p => p.GBUMetode == priority);
                if (prioritizedPunkt != null)
                {
                    break;
                }
            }

            if (prioritizedPunkt == null)
            {
                prioritizedPunkt = punktObjekter[0];  // Fallback to the first element if no prioritized method found
            }

            prioritizedPunkt.StackBor = new List<string>();

            // Add GBUMetode of non-prioritized points to StackBor
            foreach (var punkt in punktObjekter)
            {
                if (punkt != prioritizedPunkt)
                {
                    // Sjekk om GBUMetode inneholder minst tre bokstaver
                    if (punkt.GBUMetode.Count(char.IsLetter) >= 3)
                    {
                        prioritizedPunkt.StackBor.Add("STACK_" + punkt.GBUMetode);
                    }
                }
            }

            return prioritizedPunkt;
        }

        private static List<PunktInfo> GiReelGbuMetode(List<PunktInfo> punktObjekter)
        {
            //Bør legge inn feilmelding om noen blir kventert til enkel ettersom det ikke finnerrring
            foreach (var punktIfno in punktObjekter)
            {
                if (punktIfno.GBUMetode == "26")
                {
                    punktIfno.GBUMetode = "fjellkontrollboring";
                }
                else if (punktIfno.GBUMetode == "25")
                {
                    punktIfno.GBUMetode = "totalsondering";
                }
                else if (punktIfno.GBUMetode == "23")
                {
                    punktIfno.GBUMetode = "dreietrykksondering";
                }
                else if (punktIfno.GBUMetode == "21")
                {
                    punktIfno.GBUMetode = "dreiesondering";
                }
                else if (punktIfno.GBUMetode == "22")
                {
                    punktIfno.GBUMetode = "enkel";
                }
                else if (punktIfno.GBUMetode == "7")
                {
                    punktIfno.GBUMetode = "CPT";
                }
            }

            return punktObjekter;
        }
    }
}




