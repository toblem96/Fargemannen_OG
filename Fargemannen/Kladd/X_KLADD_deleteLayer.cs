using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Fargemannen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fargemannen
{
    internal class X_KLADD_deleteLayer
    {

        public static void EraseLayer()
        {
            // Få den gjeldende dokumentet og databasen
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start en transaksjon
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Åpne Layer-tabellen for lesing
                LayerTable acLyrTbl = acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                // Angi navnet på laget du ønsker å slette
                string sLayerName = "Symboler"; // Endre dette til navnet på laget du vil slette

                if (acLyrTbl.Has(sLayerName))
                {
                    // Sjekk om det er trygt å slette laget
                    ObjectIdCollection acObjIdColl = new ObjectIdCollection();
                    acObjIdColl.Add(acLyrTbl[sLayerName]);
                    acCurDb.Purge(acObjIdColl);

                    if (acObjIdColl.Count > 0)
                    {
                        LayerTableRecord acLyrTblRec = acTrans.GetObject(acObjIdColl[0], OpenMode.ForWrite) as LayerTableRecord;

                        try
                        {
                            // Slett det urefererte laget
                            acLyrTblRec.Erase(true);

                            // Lagre endringene og avslutt transaksjonen
                            acTrans.Commit();
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception Ex)
                        {
                            // Laget kunne ikke slettes
                            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Feil:\n" + Ex.Message);
                        }
                    }
                    else
                    {
                        Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Laget '" + sLayerName + "' kan ikke slettes fordi det fortsatt er i bruk eller er det aktive laget.");
                    }
                }
                else
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Laget '" + sLayerName + "' finnes ikke.");
                }
            }
        }

        public static void DeactivateLayer()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            string layerName = "Symboler"; // Navnet på laget du ønsker å deaktivere


            using (DocumentLock docLock = acDoc.LockDocument())
            {
                using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                {
                    LayerTable layerTable = acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                    if (layerTable.Has(layerName))
                    {
                        ObjectId layerId = layerTable[layerName];
                        LayerTableRecord layerRecord = acTrans.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                        // Deaktiver laget ved å sette IsOff til true
                        layerRecord.IsOff = true;

                        acTrans.Commit();
                        Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog($"Laget '{layerName}' har blitt deaktivert.");
                    }
                    else
                    {
                        Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog($"Laget '{layerName}' finnes ikke.");
                    }
                }
            }
        }
    }
}
