using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Fargemannen.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Fargemannen.Model
{
    internal class LegdenModel
    {

        public void VelgFirkant(List<ViewModel.Intervall> Intervaller)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Be brukeren om å velge en polyline
            PromptEntityOptions peo = new PromptEntityOptions("\nVelg en firkant (Polyline): ");
            peo.SetRejectMessage("\nObjektet må være en Polyline.");
            peo.AddAllowedClass(typeof(Polyline), true);
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
            {
                ed.WriteMessage("Ingen gyldig polyline ble valgt.");
                return;
            }

            // Fortsett med å håndtere den valgte polylinen
            HandleSelectedPolyline(per.ObjectId, Intervaller);
        }


        private   void HandleSelectedPolyline(ObjectId polylineId, List<ViewModel.Intervall> Intervaller)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            using (doc.LockDocument())
            {
                Polyline pline = tr.GetObject(polylineId, OpenMode.ForRead) as Polyline;

                if (!pline.Closed)
                {
                    ed.WriteMessage("\nValgt polyline er ikke lukket. Velg en lukket polyline.");
                    return;
                }

                if (pline.NumberOfVertices < 2)
                {
                    ed.WriteMessage("\nPolylinen må ha minst to vertices.");
                    return;
                }
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
          
                double minX = Double.MaxValue;
                double maxX = Double.MinValue;
                double minY = Double.MaxValue;
                double maxY = Double.MinValue;

                // Iterate over each vertex to find the min/max X and Y
                for (int i = 0; i < pline.NumberOfVertices; i++)
                {
                    Point3d pt = pline.GetPoint3dAt(i);
                    minX = Math.Min(minX, pt.X);
                    maxX = Math.Max(maxX, pt.X);
                    minY = Math.Min(minY, pt.Y);
                    maxY = Math.Max(maxY, pt.Y);
                }

                double totalWidth = maxX - minX;
                double totalHeight = maxY - minY;
                double rowHeight = totalHeight / Intervaller.Count;  // Adjusted to use count of intervals list


                double rowWidth = totalWidth; // The row width is the same as the total width of the polyline

                for (int i = 0; i < Intervaller.Count; i++)
                {
                    ViewModel.Intervall interval = Intervaller[Intervaller.Count - 1 - i];
                    Point3d rectStart = new Point3d(minX, minY + rowHeight * i, 0);
                    Point3d rectEnd = new Point3d(maxX, minY + rowHeight * (i + 1), 0);

                    CreateColoredRectangle(doc, db, tr, rectStart, rectEnd, interval);
                    InsertIntervalText(doc, tr, rectStart, rectEnd, interval);
                }
                tr.Commit();
            }
        }


        private void CreateColoredRectangle(Document doc, Database db, Transaction tr, Point3d start, Point3d end, ViewModel.Intervall interval)
        {
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

            Polyline rect = new Polyline();
            rect.AddVertexAt(0, new Point2d(start.X, start.Y), 0, 0, 0);
            rect.AddVertexAt(1, new Point2d(end.X, start.Y), 0, 0, 0);
            rect.AddVertexAt(2, new Point2d(end.X, end.Y), 0, 0, 0);
            rect.AddVertexAt(3, new Point2d(start.X, end.Y), 0, 0, 0);
            rect.Closed = true;

            ObjectId rectId = btr.AppendEntity(rect);
            tr.AddNewlyCreatedDBObject(rect, true);

            Hatch hatch = new Hatch();
            btr.AppendEntity(hatch);
            tr.AddNewlyCreatedDBObject(hatch, true);
            hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            hatch.Associative = true;
            hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection(new ObjectId[] { rectId }));
            hatch.EvaluateHatch(true);
            hatch.Layer = interval.Navn;  // Ensure the layer is set correctly
            hatch.Transparency = new Transparency((byte)255);  // Set transparency of the hatch to 0 (fully opaque)
        }

        private void InsertIntervalText(Document doc, Transaction tr, Point3d start, Point3d end, ViewModel.Intervall interval)
        {
            if (doc == null || tr == null)
            {
                throw new ArgumentNullException("Document og/eller Transaction er null.");
            }

            if (interval == null)
            {
                throw new ArgumentNullException("Intervall er null, kan ikke sette inn tekst.");
            }

            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
            if (btr == null)
            {
                throw new InvalidOperationException("Kunne ikke hente BlockTableRecord for skriving.");
            }

            double rowHeight = Math.Abs(end.Y - start.Y);
            if (rowHeight == 0)
            {
                throw new InvalidOperationException("Start og sluttpunktet er på samme Y-nivå, radhøyden blir 0.");
            }

            double textHeight = rowHeight * 0.6;  // Sett tekstens høyde til 60% av radens høyde
            textHeight = Math.Max(textHeight, 0.1);  // Sikre at tekstens høyde ikke er for liten

            try
            {
                DBText text = new DBText();
                text.Position = new Point3d((start.X + end.X) / 2, (start.Y + end.Y) / 2, 0);
                text.Height = textHeight;
                text.TextString = $"{interval.StartVerdi}m - {interval.SluttVerdi}m";
                text.HorizontalMode = TextHorizontalMode.TextCenter;
                text.VerticalMode = TextVerticalMode.TextVerticalMid;
                text.AlignmentPoint = text.Position;

                btr.AppendEntity(text);
                tr.AddNewlyCreatedDBObject(text, true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Feil under innsetting av tekst: " + ex.Message, ex);
            }
        }

        public void VelgFirkantZ()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Be brukeren om å velge en polyline
            PromptEntityOptions peo = new PromptEntityOptions("\nVelg en firkant (Polyline): ");
            peo.SetRejectMessage("\nObjektet må være en Polyline.");
            peo.AddAllowedClass(typeof(Polyline), true);
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
            {
                ed.WriteMessage("Ingen gyldig polyline ble valgt.");
                return;
            }

            // Fortsett med å håndtere den valgte polylinen
            HandleSelectedPolylineZ(per.ObjectId);
        }


        private void HandleSelectedPolylineZ(ObjectId polylineId)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            using (doc.LockDocument())
            {
                Polyline pline = tr.GetObject(polylineId, OpenMode.ForRead) as Polyline;

                if (!pline.Closed)
                {
                    ed.WriteMessage("\nValgt polyline er ikke lukket. Velg en lukket polyline.");
                    return;
                }

                if (pline.NumberOfVertices < 2)
                {
                    ed.WriteMessage("\nPolylinen må ha minst to vertices.");
                    return;
                }
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                List<Intervall> activeIntervals = Intervall.intervallListeZ
                    .Where(i => !string.IsNullOrEmpty(i.LagNavn) && lt.Has(i.LagNavn))
                    .ToList();

                double minX = Double.MaxValue;
                double maxX = Double.MinValue;
                double minY = Double.MaxValue;
                double maxY = Double.MinValue;

                // Iterate over each vertex to find the min/max X and Y
                for (int i = 0; i < pline.NumberOfVertices; i++)
                {
                    Point3d pt = pline.GetPoint3dAt(i);
                    minX = Math.Min(minX, pt.X);
                    maxX = Math.Max(maxX, pt.X);
                    minY = Math.Min(minY, pt.Y);
                    maxY = Math.Max(maxY, pt.Y);
                }

                double totalWidth = maxX - minX;
                double totalHeight = maxY - minY;
                double rowHeight = totalHeight / activeIntervals.Count;  // Adjusted to use count of intervals list


                double rowWidth = totalWidth; // The row width is the same as the total width of the polyline

                for (int i = 0; i < activeIntervals.Count; i++)
                {
                    Intervall interval = activeIntervals[activeIntervals.Count - 1 - i];
                    Point3d rectStart = new Point3d(minX, minY + rowHeight * i, 0);
                    Point3d rectEnd = new Point3d(maxX, minY + rowHeight * (i + 1), 0);

                    CreateColoredRectangleZ(doc, db, tr, rectStart, rectEnd, interval);
                    InsertIntervalTextZ(doc, tr, rectStart, rectEnd, interval);
                }
                tr.Commit();
            }
        }


        private void CreateColoredRectangleZ(Document doc, Database db, Transaction tr, Point3d start, Point3d end, Intervall interval)
        {
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

            Polyline rect = new Polyline();
            rect.AddVertexAt(0, new Point2d(start.X, start.Y), 0, 0, 0);
            rect.AddVertexAt(1, new Point2d(end.X, start.Y), 0, 0, 0);
            rect.AddVertexAt(2, new Point2d(end.X, end.Y), 0, 0, 0);
            rect.AddVertexAt(3, new Point2d(start.X, end.Y), 0, 0, 0);
            rect.Closed = true;

            ObjectId rectId = btr.AppendEntity(rect);
            tr.AddNewlyCreatedDBObject(rect, true);

            Hatch hatch = new Hatch();
            btr.AppendEntity(hatch);
            tr.AddNewlyCreatedDBObject(hatch, true);
            hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            hatch.Associative = true;
            hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection(new ObjectId[] { rectId }));
            hatch.EvaluateHatch(true);
            hatch.Layer = interval.LagNavn;  // Ensure the layer is set correctly
            hatch.Transparency = new Transparency((byte)255);  // Set transparency of the hatch to 0 (fully opaque)
        }

        private void InsertIntervalTextZ(Document doc, Transaction tr, Point3d start, Point3d end, Intervall interval)
        {
            if (doc == null || tr == null)
            {
                throw new ArgumentNullException("Document og/eller Transaction er null.");
            }

            if (interval == null)
            {
                throw new ArgumentNullException("Intervall er null, kan ikke sette inn tekst.");
            }

            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
            if (btr == null)
            {
                throw new InvalidOperationException("Kunne ikke hente BlockTableRecord for skriving.");
            }

            double rowHeight = Math.Abs(end.Y - start.Y);
            if (rowHeight == 0)
            {
                throw new InvalidOperationException("Start og sluttpunktet er på samme Y-nivå, radhøyden blir 0.");
            }

            double textHeight = rowHeight * 0.6;  // Sett tekstens høyde til 60% av radens høyde
            textHeight = Math.Max(textHeight, 0.1);  // Sikre at tekstens høyde ikke er for liten

            try
            {
                DBText text = new DBText();
                text.Position = new Point3d((start.X + end.X) / 2, (start.Y + end.Y) / 2, 0);
                text.Height = textHeight;
                text.TextString = $"{interval.Start}m - {interval.Slutt}m";
                text.HorizontalMode = TextHorizontalMode.TextCenter;
                text.VerticalMode = TextVerticalMode.TextVerticalMid;
                text.AlignmentPoint = text.Position;

                btr.AppendEntity(text);
                tr.AddNewlyCreatedDBObject(text, true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Feil under innsetting av tekst: " + ex.Message, ex);
            }
        }





    }
}

