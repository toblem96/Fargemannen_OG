using OpenAI_API.Completions;
using OpenAI_API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using Autodesk.AutoCAD.EditorInput;
using System.Drawing;
using Autodesk.AutoCAD.ApplicationServices;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Documents;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Net.Http.Headers;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Controls;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;





namespace Fargemannen
{
   
    internal class Rapport
    {
      
        /*
        private static OpenAIAPI openAiApi;
        public static List<string> BoreMotoderBergmodell = new List<string>();
        public static int AntallBoringerBergmodell = 0;
        public static double MinGrenseIFjell = 0;
        public static string ruteStørresle = "";
        public static double minAr = 0;

        public static async Task GenererRapport()
        {
            var openAiApiKey = "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";
            APIAuthentication apiAuthentication = new APIAuthentication(openAiApiKey);
            openAiApi = new OpenAIAPI(apiAuthentication);

            string intervallInfo = GetIntervall();
            string bergmodellInfo = GetBergmodellInfo();
            string symbolInfo = GetSymbolInfo();
            string minÅr = GetMinAr();
            string ruteInfo = GetRuteInfo();
            string verdierZ = GetVerdierZ();
            string prompts_1 = Prompts_1();
            string prompts_2 = Prompts_2();

            string mergeText_1 = MergeText_1(intervallInfo, ruteInfo, verdierZ, prompts_1);
            string mergeText_2 = MergeText_2(bergmodellInfo, symbolInfo, minÅr, prompts_2);

            string model = "gpt-3.5-turbo-instruct";
            int maxTokens = 2500;

            var completionRequest_1 = new CompletionRequest
            {
                Prompt = mergeText_1,
                Model = model,
                MaxTokens = maxTokens
            };

            var completionResult_1 = await openAiApi.Completions.CreateCompletionAsync(completionRequest_1);
            var generatedText_1 = completionResult_1.Completions[0].Text;

            var completionRequest_2 = new CompletionRequest
            {
                Prompt = mergeText_2,
                Model = model,
                MaxTokens = maxTokens
            };

            var completionResult_2 = await openAiApi.Completions.CreateCompletionAsync(completionRequest_2);
            var generatedText_2 = completionResult_2.Completions[0].Text;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "Rapport_GEO.docx");

            CreateWordDocument(filePath, generatedText_1, generatedText_2, mergeText_1, mergeText_2);
        }

        private static void CreateWordDocument(string filePath, string text1, string text2, string prompts_1, string prompts_2)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                DocumentFormat.OpenXml.Wordprocessing.Body body = mainPart.Document.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Body());

                // Add first section with header
                var headingPar1 = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                var runHeading1 = headingPar1.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                runHeading1.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("Analyse av avstand mellom Bergmodell og Terrengmodell"));

                var para1 = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                var run1 = para1.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                run1.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text1));

                // Add second section with sub-header
                var headingPar2 = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                var runHeading2 = headingPar2.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                runHeading2.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("Generering av Bergmodell"));

                var para2 = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                var run2 = para2.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                run2.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text2));

                // Append prompts used
                var promptsPar = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                var runPrompts = promptsPar.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                runPrompts.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text($"Prompts brukt: \n{prompts_1}\n{prompts_2}"));
            }
        }
        private static string GetIntervall() 
        {
            {
                
                StringBuilder intervallInfo = new StringBuilder();
                intervallInfo.Clear();

                // Sorter intervaller basert på startverdi
                var sorterteIntervaller = Intervall.intervallListeZ.OrderBy(x => x.Start).ToList();

                foreach (var intervall in sorterteIntervaller)
                {
                    // Anta at hvert intervall er aktivt og hent nødvendige detaljer
                    string fargeHex = ColorTranslator.ToHtml(intervall.Farge); // Konverterer Color til Hex-kode
                    intervallInfo.AppendLine($"Start: {intervall.Start}, Slutt: {intervall.Slutt}, Farge: {fargeHex}");
                }

                return intervallInfo.ToString();
            }
        }

        private static string GetMinAr() 
        {
        
        return minAr.ToString();
        }

        private static string GetBergmodellInfo()
        {
            StringBuilder bergmodellInfo = new StringBuilder();

            // Beskriv hva bergmodellen består av
            bergmodellInfo.AppendLine("Bergmodellen er konstruert basert på følgende boremetoder og antall boringer:");

            // Legg til antall boringer i informasjonen
            bergmodellInfo.AppendLine($"Totalt antall boringer utført: {AntallBoringerBergmodell}");

            // Legg til detaljer om hver boremetode
            if (BoreMotoderBergmodell.Any())
            {
                bergmodellInfo.AppendLine("Boremetoder brukt:");
                foreach (var metode in BoreMotoderBergmodell)
                {
                    bergmodellInfo.AppendLine($"- {metode}");
                }
            }
            else
            {
                bergmodellInfo.AppendLine("Ingen boremetoder er definert.");
            }

            return bergmodellInfo.ToString();
        }


        private static string GetSymbolInfo()
        {
            StringBuilder symbolInfo = new StringBuilder();

            // Beskriv hva de forskjellige fargene representerer basert på grensen for boret i berg
            symbolInfo.AppendLine("Symbolinformasjon for borelengder:");
            symbolInfo.AppendLine($"Grense for å definere om det er boret i berg: {MinGrenseIFjell} meter");

            // Beskrivelse av farger
            string boretIFjellHex = ColorTranslator.ToHtml(SecondWinSymbol.selectedColor); // Konverterer Color til Hex-kode
            string ikkeBoretIFjellHex = ColorTranslator.ToHtml(SecondWinSymbol.selectedColor_minBerg); // Konverterer Color til Hex-kode

            symbolInfo.AppendLine($"Farge for symboler som har boret i berg (>= {MinGrenseIFjell} meter): {boretIFjellHex}");
            symbolInfo.AppendLine($"Farge for symboler som ikke har boret i berg (< {MinGrenseIFjell} meter): {ikkeBoretIFjellHex}");

            return symbolInfo.ToString();
        }

        private static string GetRuteInfo () 
        {
            string RuteInfo = $" Rute størrelse {WinBergModell.ruterStrZ}m x {WinBergModell.ruterStrZ}m";


        return RuteInfo;
        }

        private static string GetVerdierZ()
        {
            // Initialisere strengen som skal inneholde resultatet
            string VerdierZ = "Målingsverdier fra terrengmodellen til bergmodellen:\n";

            // Beregne og avrunde de nødvendige statistikkene
            double minZ = Math.Round(AnalyseZ.minVerdiZ);
            double maxZ = Math.Round(AnalyseZ.maxVerdiZ);
            double gjennomsnittZ = Math.Round(AnalyseZ.gjennomsnittVerdiZ);

            // Formaterer og legger til statistikkene i resultatstrengen
            VerdierZ += $"Minimumsverdi (Z): {minZ} m\n";
            VerdierZ += $"Maksimumsverdi (Z): {maxZ} m\n";
            VerdierZ += $"Gjennomsnittsverdi (Z): {gjennomsnittZ} m";

            return VerdierZ;
        }

        private static string Prompts_1()
        {
            return "Du er en geoteknikker som skal beskrive resultate av en analyse som har målt avstaden fra en gernert bergmodell til terrengmodell. Teksten skal beskrive hva bilde til analyse viser. Bilde viser et rutenett med forskjellige farger basert på avstaden " +
                   "I en sammenhengde tekt skal du beskrive hvilken type analyse dette er. Størrelsene på rutene. Intervallene start og slutt verdi, samt farge. Min, Max og gjennomsnitt verdi";
                  
        }
        private static string Prompts_2()
        {
            return "Du er en geoteknikker som skal beskrive hvordan en bergmodell er laget med tanke på hvilken borginer som er brukt, hvor mange, og om de har boret langt nok ned i fjell" +
                   "I en sammenhengde tekt skal du beskrive hvilken hvilken boringer som er brukt, hvor mange, og fra etter hvilket år. Du skal også forkalre hva de forskjellige fargene til symbolene er og forkalre syombler som ikke har boret i berg skaper litt usikkerhet";

        }

        private static string MergeText_1(string intervallInfo, string ruteInfo, string verdierZ, string prompts)
        {
            return $"{prompts}\n\n" +
                   "Intervall Informasjon:\n" + intervallInfo + "\n\n" +
                   "Rute Informasjon:\n" + ruteInfo + "\n\n" +
                   "Verdier Z:\n" + verdierZ;
        }

        private static string MergeText_2(string bergmodellInfo, string symbolInfo, string minAr, string prompts)
        {


            return $"{prompts}\n\n" +
                   "Bergmodell Informasjon:\n" + bergmodellInfo + "\n\n" +
                   "Symbol informasjon Informasjon:\n" + symbolInfo + "\n\n" +
                   "Bare brukt boringer etter:\n" + minAr; ;


        }


        */

    }









   
}
