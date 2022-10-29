using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Lab1PlaceGroup
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

            // Define a reference Object to accept the pick result
            Reference pickedRef = null;

            // Pick a group
            Selection sel = uiApp.ActiveUIDocument.Selection;
            pickedRef = sel.PickObject(ObjectType.Element, "Please select a group");
            Element elem = doc.GetElement(pickedRef);
            Group group = elem as Group;

            // Pick a point
            XYZ point = sel.PickPoint("Please pick a point to place group");

            // Place the group
            Transaction trans = new Transaction(doc);
            trans.Start("Lab1");
            doc.Create.PlaceGroup(point, group.GroupType);
            trans.Commit();

            return Result.Succeeded;
        }
    }
}
