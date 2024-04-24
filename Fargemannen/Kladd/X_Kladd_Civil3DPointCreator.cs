using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using System.Collections.Generic;
using System;
/*
namespace Fargemannen
{
    public class Civil3DPointCreator
    {
        public static void CreateCivil3DPoints(List<PunktInfo> pointsToSymbol)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // Få tilgang til eller opprett punktgruppen hvor nye punkter skal legges til
                ObjectId pointGroupId = GetOrCreatePointGroup(civilDoc, "Nye Punkter");

                foreach (var punktInfo in pointsToSymbol)
                {
                    // Konverter PunktInfo til et CogoPoint og legg det til i Civil 3D
                    Point3d point = new Point3d(punktInfo.Punkt.X, punktInfo.Punkt.Y, punktInfo.Punkt.Z);
                    ObjectId cogoPointId = CogoPoint.Create(civilDoc, point);
                    CogoPoint cogoPoint = trans.GetObject(cogoPointId, OpenMode.ForWrite) as CogoPoint;

                    // Sett beskrivelser og andre egenskaper basert på PunktInfo
                    cogoPoint.Description = punktInfo.GBUMetode; // Eksempel på å sette en beskrivelse

                    // Legg til punktet i den spesifikke punktgruppen
                    PointGroup pointGroup = trans.GetObject(pointGroupId, OpenMode.ForWrite) as PointGroup;
                    pointGroup.AddPoint(cogoPointId);
                }

                trans.Commit();
            }
        }

        private static ObjectId GetOrCreatePointGroup(CivilDocument civilDoc, string groupName)
        {
            ObjectId pointGroupId = ObjectId.Null;
            Database db = civilDoc.Database;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                PointGroupCollection pointGroups = PointGroupCollection.GetPointGroups(db);

                // Sjekk om punktgruppen allerede eksisterer
                foreach (ObjectId groupId in pointGroups)
                {
                    PointGroup group = trans.GetObject(groupId, OpenMode.ForRead) as PointGroup;
                    if (group.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                    {
                        pointGroupId = groupId;
                        break;
                    }
                }

                // Hvis gruppen ikke finnes, opprett den
                if (pointGroupId == ObjectId.Null)
                {
                    PointGroup group = new PointGroup(db, groupName);
                    pointGroupId = pointGroups.Add(group);
                    // Må lagre endringene gjort i transaksjonen
                    trans.AddNewlyCreatedDBObject(group, true);
                }

                // Husk å fullføre transaksjonen
                trans.Commit();
            }

            return pointGroupId;
        }
    }*/