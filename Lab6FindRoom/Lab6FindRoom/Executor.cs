using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;

namespace Lab6FindRoom
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

                // Pick a point
                XYZ point = sel.PickPoint("Please pick a point to place group");

                // Place the group
                Transaction trans = new Transaction(doc);
                trans.Start("Lab6");
                doc.Create.PlaceGroup(point, group.GroupType);
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

        public XYZ GetElementCenter(Element elem)
        {
            BoundingBoxXYZ bounding = elem.get_BoundingBox(null);
            XYZ center = (bounding.Min + bounding.Max) / 2;
            return center;
        }

        // Return the room in which the given point is located
        Room GetRoomOfGroup(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            Room room = null;
            foreach (Element elem in collector)
            {
                room = elem as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        break;
                    }
                }
            }

            return room;
        }

        // Filter to constrain picking to model groups. Only model groups
        // are highlighted and can be selected when cursor is hovering.
        public class GroupPickFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                return elem.Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_IOSModelGroups);
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
    }
}
