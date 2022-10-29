using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;

namespace Lab5PickFilter
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

                // Pick a point
                XYZ point = sel.PickPoint("Please pick a point to place group");

                // Place the group
                Transaction trans = new Transaction(doc);
                trans.Start("Lab5");
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
