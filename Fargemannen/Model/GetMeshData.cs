using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil;
using Autodesk.Civil.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Fargemannen.ViewModel;
using Fargemannen;
using System.Runtime.CompilerServices;
using System.Windows;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using System.Windows.Media;
using System.Windows.Forms;
using Fargemannen.Model;
using System.Drawing;

using Autodesk.AutoCAD.Colors;

namespace Fargemannen.Model
{
    public class GetMeshData
    {




        public static TinSurface GetTinSurfaceFromLayerName(string layerName, Transaction trans, Database db)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            TinSurface tinSurface = null;

            // Hent LayerTableRecord for det spesifiserte laget
            LayerTableRecord layer = GetLayerByName(layerName, trans, db);
            if (layer == null)
            {
                ed.WriteMessage("Laget ble ikke funnet: " + layerName);
            }

            // Søk gjennom database for å finne TinSurface objekter på det spesifiserte laget
            var bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
            var btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            foreach (ObjectId objId in btr)
            {
                var obj = trans.GetObject(objId, OpenMode.ForRead);
                var surface = obj as TinSurface;
                if (surface != null && surface.Layer == layerName)
                {
                    tinSurface = surface;
                    break; // Anta at det kun finnes én TinSurface per lag
                }
            }

            if (tinSurface == null)
            {
                ed.WriteMessage("Ingen bergmodell funnet i laget: " + layerName);
            }

            return tinSurface;
        }


        private static LayerTableRecord GetLayerByName(string layerName, Transaction trans, Database db)
        {
            LayerTable lt = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(layerName))
            {
                ObjectId layerId = lt[layerName];
                return (LayerTableRecord)trans.GetObject(layerId, OpenMode.ForRead);
            }
            return null;
        }
        private static List<Point2d> ExtractBorders(Transaction acTrans, GridSurface surface)
        {
            List<Point2d> borderPoints = new List<Point2d>();
            ObjectIdCollection borderIds = surface.ExtractBorder(SurfaceExtractionSettingsType.Plan);
            foreach (ObjectId borderId in borderIds)
            {
                Polyline3d polyline3d = acTrans.GetObject(borderId, OpenMode.ForRead) as Polyline3d;
                if (polyline3d != null)
                {
                    foreach (ObjectId vertexId in polyline3d)
                    {
                        PolylineVertex3d vertex = acTrans.GetObject(vertexId, OpenMode.ForRead) as PolylineVertex3d;
                        borderPoints.Add(new Point2d(vertex.Position.X, vertex.Position.Y));
                    }
                }
            }
            return borderPoints;
        }

        private static List<Point2d> ExtractBordersFromTinSurface(Transaction acTrans, TinSurface surface)
        {
            List<Point2d> borderPoints = new List<Point2d>();
            if (surface == null)
                return borderPoints; // Returner tom liste hvis overflaten ikke er gyldig

            // Ekstraher grensene fra TinSurface
            ObjectIdCollection borderIds = surface.ExtractBorder(SurfaceExtractionSettingsType.Plan);
            Console.WriteLine("# of surface borders: " + borderIds.Count);

            foreach (ObjectId borderId in borderIds)
            {
                Polyline3d border = acTrans.GetObject(borderId, OpenMode.ForRead) as Polyline3d;
                if (border != null)
                {
                    Console.WriteLine("Surface border vertices:");
                    foreach (ObjectId vertexId in border)
                    {
                        PolylineVertex3d vertex = acTrans.GetObject(vertexId, OpenMode.ForRead) as PolylineVertex3d;
                        if (vertex != null)
                        {
                            borderPoints.Add(new Point2d(vertex.Position.X, vertex.Position.Y));
                            Console.WriteLine(String.Format("  - Border vertex at: {0}", vertex.Position.ToString()));
                        }
                    }
                }
            }

            return borderPoints;
        }


    }

}
