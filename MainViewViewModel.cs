using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace multiangular
{
    internal class MainViewViewModel
    {
        private ExternalCommandData _commandData;
        public DelegateCommand SaveCommand { get; }
        public int Quantity { get; set; }
        public double Diameter { get; set; }
        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            SaveCommand = new DelegateCommand(OnSaveCommand);
            Quantity = 6;
            Diameter = 1000;
        }

        private void OnSaveCommand()
        {
            Document doc = _commandData.Application.ActiveUIDocument.Document;

            List<Level> listLevel = new FilteredElementCollector(doc)
             .OfClass(typeof(Level))
             .OfType<Level>()
             .ToList();

            Level level1 = listLevel
                .Where(el => el.Name == "Уровень 1")
                .FirstOrDefault();
            Level level2 = listLevel
                .Where(el => el.Name == "Уровень 2")
                .FirstOrDefault();

            if (Quantity % 2 != 0 || Quantity < 4)
            {
                TaskDialog.Show("ошибка", "введите чётное значение >= 4");
            }
            else
            {
                var walls = AddWalls(doc, level1, level2);
                RaiseCloseRequest();
            }
        }

        public List<Wall> AddWalls(Document doc, Level level1, Level level2)
        {
            double n = Quantity;
            double r = UnitUtils.ConvertToInternalUnits(Diameter / 2, UnitTypeId.Millimeters);
            double a = 360 / n;
            double R = r / Math.Cos(Math.PI / n);
            double step = 360 / n;
            double x0 = 50;

            List<XYZ> points = new List<XYZ>();

            while (a <= 360 + step)
            {
                double b = (a * (Math.PI)) / 180;
                points.Add(new XYZ(x0 + R * Math.Cos(b), R * Math.Sin(b), 0));
                a += step;
            }

            List<Wall> walls = new List<Wall>();
            Transaction ts = new Transaction(doc, "walls create");
            ts.Start();
            for (int i = 0; i < n; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }
            ts.Commit();

            //тип соединения не удалось реализовать этим способом
            //foreach (Wall wall in walls)
            //{
            //    LocationCurve locationCurve = wall.Location as LocationCurve;
            //    using (Transaction st = new Transaction(doc, "тип соединения"))
            //    {
            //        st.Start();
            //        locationCurve.set_JoinType(1, JoinType.Miter);
            //        locationCurve.set_JoinType(0, JoinType.Miter);
            //        st.Commit();
            //    }
            //}
            return walls;
        }

        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}
    
