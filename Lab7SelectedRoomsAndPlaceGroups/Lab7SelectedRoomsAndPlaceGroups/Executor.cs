﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;

namespace Lab7SelectedRoomsAndPlaceGroups
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Executor : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get application and document objects
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;
            try
            {
                // Define a reference Object to accept the pick result
                Reference pickedRef = null;

                // Pick a group
                Selection sel = uiApp.ActiveUIDocument.Selection;
                GroupPickFilter selFilter = new GroupPickFilter();
                pickedRef = sel.PickObject(ObjectType.Element, selFilter, "Please select a group");
                Element elem = doc.GetElement(pickedRef);
                Group group = elem as Group;

                // Get the group's center point
                XYZ origin = GetElementCenter(group);

                // Get the room that the picked group is located in
                Room room = GetRoomOfGroup(doc, origin);

                // Get the room's center point
                XYZ sourceCenter = GetRoomCenter(room);

                // Ask the user to pick target rooms
                RoomPickFilter roomPickFilter = new RoomPickFilter();
                IList<Reference> rooms = sel.PickObjects(ObjectType.Element, roomPickFilter,
                    "Select target rooms for duplicate furniture group");

                // Place furniture in each of the rooms
                Transaction trans = new Transaction(doc);
                trans.Start("Lab7");
                PlaceFurnitureInRooms(doc, rooms, sourceCenter, group.GroupType, origin);
                trans.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Return an element's center point coordinates.
        /// </summary>
        public XYZ GetElementCenter(Element elem)
        {
            BoundingBoxXYZ bounding = elem.get_BoundingBox(null);
            XYZ center = (bounding.Min + bounding.Max) / 2;
            return center;
        }

        /// <summary>
        /// Return the room in which the given point is located
        /// </summary>
        Room GetRoomOfGroup(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            Room room = null;
            foreach (Element elem in collector)
            {
                room = elem as Room;
                if (room != null && room.IsPointInRoom(point))
                {
                    break;
                }
            }

            return room;
        }

        /// <summary>
        /// Return a room's center point coordinates.
        /// Z value is equal to the bottom of the room.
        /// </summary>
        public XYZ GetRoomCenter(Room room)
        {
            // Get the room center point
            XYZ boundCenter = GetElementCenter(room);
            LocationPoint locPoint = (LocationPoint)room.Location;
            XYZ roomCenter = new XYZ(boundCenter.X, boundCenter.Y, locPoint.Point.Z);
            return roomCenter;
        }

        /// <summary>
        /// Copy the group to each of the provided rooms. The position
        /// at which the group should be placed is based on the target
        /// room's center point: it should have the same offset from
        /// this point as the original had from the center of its room
        /// </summary>
        public void PlaceFurnitureInRooms(Document doc, IList<Reference> rooms, XYZ sourceCenter, GroupType gt,
            XYZ groupOrigin)
        {
            XYZ offset = groupOrigin - sourceCenter;
            XYZ offsetXY = new XYZ(offset.X, offset.Y, 0);
            foreach (Reference r in rooms)
            {
                if (doc.GetElement(r) is Room roomTarget)
                {
                    XYZ roomCenter = GetRoomCenter(roomTarget);
                    doc.Create.PlaceGroup(roomCenter + offsetXY, gt);
                }
            }
        }

        /// <summary>
        /// Filter to constrain picking to model groups. Only model groups
        /// are highlighted and can be selected when cursor is hovering.
        /// </summary>
        public class GroupPickFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                return elem.Category != null && elem.Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_IOSModelGroups);
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
        /// <summary>
        /// Filter to constrain picking to rooms
        /// </summary>
        public class RoomPickFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                return elem.Category != null && elem.Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_Rooms);
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
    }
}
