using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fargemannen.ViewModel;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.AutoCAD.Customization;
using System.Windows.Data;
using Microsoft.Extensions.Logging;

namespace Fargemannen.Model
{
    public class KampIDagenModel
    {

        public static void SelectClosedPolylineAndManipulateSurface(string bergmodellLagNavn, string terrengModellLagNavn)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            Database db = doc.Database;

            // La brukeren velge en lukket polyline
            PromptEntityOptions opt = new PromptEntityOptions("\nVelg en lukket polyline:");
            opt.SetRejectMessage("\nKun lukkede polylines er tillatt.");
            opt.AddAllowedClass(typeof(Polyline), false);
            PromptEntityResult res = ed.GetEntity(opt);

            if (res.Status != PromptStatus.OK)
                return;

            Transaction tr = doc.TransactionManager.StartTransaction();

            using (tr)
            using (doc.LockDocument())
            {
                Polyline polyline = tr.GetObject(res.ObjectId, OpenMode.ForRead) as Polyline;
                if (!polyline.Closed)
                {
                    ed.WriteMessage("\nPolylinjen er ikke lukket.");
                    return;
                }

                // Hent punkter fra GridSurface og lagre i liste A
                List<Point3d> pointsInArea = GetPointsFromGridSurface(polyline, tr, db, terrengModellLagNavn);

                // Oppdater TinSurface
                UpdateTinSurface(polyline, pointsInArea, bergmodellLagNavn, tr, db);

                tr.Commit();
            }
        }

        private static List<Point3d> GetPointsFromGridSurface(Polyline boundary, Transaction tr, Database acCurDb, string terrengModellLagNavn)
        {
            List<Point3d> points = new List<Point3d>();
            
                
                List<GridSurface> terrengmodeller = GetSurfacesFromLayer(tr, acCurDb, terrengModellLagNavn);

                foreach (GridSurface terreng in terrengmodeller)
                {
                    GridSurfaceVertexCollection vertices = terreng.GetVertices(false);

                    foreach (GridSurfaceVertex vertex in vertices)
                    {
                        Point3d vertexPoint = new Point3d(vertex.Location.X, vertex.Location.Y, vertex.Location.Z);
                        if (IsPointInPolygon(boundary, vertexPoint))
                        {
                            points.Add(vertexPoint);
                        }
                    }
                }
            
            return points;
        }

        private static bool IsPointInPolygon(Polyline poly, Point3d pt)
        {
            int n = poly.NumberOfVertices;
            bool result = false;
            int j = n - 1;
            for (int i = 0; i < n; i++)
            {
                Point3d pi = poly.GetPoint3dAt(i);
                Point3d pj = poly.GetPoint3dAt(j);
                if ((pi.Y > pt.Y) != (pj.Y > pt.Y) &&
                    (pt.X < (pj.X - pi.X) * (pt.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
                {
                    result = !result;
                }
                j = i;
            }
            return result;
        }

        private static void UpdateTinSurface(Polyline boundary, List<Point3d> newPoints, string bergmodellLagNavn, Transaction tr, Database db)
        {
            TinSurface bergmodell = GetTinSurfaceFromLayerName(bergmodellLagNavn, tr, db);

            // Få alle vertices inne i polyline
            TinSurfaceVertex[] verticesInside = bergmodell.GetVerticesInsidePolylines(new ObjectIdCollection(new ObjectId[] { boundary.ObjectId }));

            if (verticesInside != null && verticesInside.Length > 0)
            {
                // Fjerne eksisterende vertices innenfor polyline
                foreach (TinSurfaceVertex vertex in verticesInside)
                {
                    if (vertex != null)
                    {
                        bergmodell.DeleteVertex(vertex);
                    }
                    else
                    {
                        // Logger at en vertex var null eller disposed, hvis du har logging mekanisme
           
                    }
                }
            }
            else
            {
                // Logger at ingen vertices ble funnet for fjerning
               
              
            }

            // Opprette Point3dCollection for nye punkter
            Point3dCollection pointCollection = new Point3dCollection();
           
            foreach (Point3d point in newPoints)
            {
                pointCollection.Add(point);
            }

            // Legge til nye vertices
            bergmodell.AddVertices(pointCollection);

            // Lagre endringer og bygge om surface
            bergmodell.Rebuild();
        }


        private static List<GridSurface> GetSurfacesFromLayer(Transaction tr, Database acCurDb, string terrengModellLagNavn)
        {
            List<GridSurface> surfaces = new List<GridSurface>();
            BlockTable blockTable = (BlockTable)tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
            BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            foreach (ObjectId objectId in modelSpace)
            {
                Autodesk.Civil.DatabaseServices.Entity entity = tr.GetObject(objectId, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Entity;
                if (entity != null && entity.Layer == terrengModellLagNavn && entity is GridSurface gridSurface)
                {
                    surfaces.Add(gridSurface);
                }

            }
            return surfaces;
        }


        private static TinSurface GetTinSurfaceFromLayerName(string layerName, Transaction trans, Database db)
        {
            TinSurface tinSurface = null;

            // Hent LayerTableRecord for det spesifiserte laget
            LayerTableRecord layer = GetLayerByName(layerName, trans, db);
            if (layer == null) return null;

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
    }
}
