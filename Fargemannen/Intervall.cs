using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Fargemannen
{
    internal class Intervall
    {
        public int Start { get; set; }
        public int Slutt { get; set; }
        public System.Drawing.Color Farge { get; set; }
        public string LagNavn { get; set; }





      public static List<Intervall> intervallListe = new List<Intervall>(); 
     

        // Metode for å legge til et nytt intervall basert på brukerinput
        public static void LeggTilIntervall(int start, int slutt, System.Drawing.Color farge, string lagNavn)
        {
            Intervall nyttIntervall = new Intervall()
            {
                Start = start,
                Slutt = slutt,
                Farge = farge,
                LagNavn = lagNavn
            };

            intervallListe.Add(nyttIntervall);
        }


        public static List<Intervall> intervallListeZ = new List<Intervall>();
        public static void LeggTilIntervallZ(int start, int slutt, System.Drawing.Color farge, string lagNavn)
        {
            Intervall nyttIntervall = new Intervall()
            {
                Start = start,
                Slutt = slutt,
                Farge = farge,
                LagNavn = lagNavn
            };

            intervallListeZ.Add(nyttIntervall);
        }
    }

}

