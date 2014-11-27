using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    class AssignmentReferenceSymbol : ISchemeSymbol
    {
        private int id;
        private List<int> cableIds;
        private double height;
        private double margin;
        private double triangleHeight;
        private double triangleBaseLength;
        private string description;
        private double descriptionVerticalMargin;
        private E3Font font;

        public int Id
        {
            get
            {
                return id;
            }
        }

        public double OutlineHeight
        {
            get
            {
                return height;
            }
        }

        public double OutlineWidth
        {
            get
            {
                return height;
            }
        }

        public Level Level
        {
            get
            {
                return Level.Bottom;
            }
        }

        public List<int> CableIds
        {
            get
            {
                return cableIds;
            }
        }

        public double Height
        {
            get
            {
                return height;
            }
        }

        public double TopMargin
        {
            get
            {
                return margin;
            }
        }

        public double BottomMargin
        {
            get
            {
                return margin;
            }
        }

        public double LeftMargin
        {
            get
            {
                return margin;
            }
        }

        public double RightMargin
        {
            get
            {
                return margin;
            }
        }

        public Dictionary<int, CableLayout> CableLayoutById { get; private set; }

        public Point PlacedPosition { get; private set; }

        public AssignmentReferenceSymbol(string assignment, int id, int cableId, E3Text text)
        {
            this.id = id;
            triangleHeight = 4;
            triangleBaseLength = 2;
            descriptionVerticalMargin = 2;
            description ="В "+ assignment;
            font = new E3Font();
            double descriptionLength = text.GetTextLength(description, font);
            height = triangleHeight + descriptionVerticalMargin + font.height;
            margin = Math.Max(triangleBaseLength, descriptionLength) / 2;
            cableIds = new List<int>() { cableId };
            CableLayout cableLayout = new CableLayout(cableId, Level.Bottom);
            cableLayout.AddOffset(new Point(0,0));
            CableLayoutById = new Dictionary<int, CableLayout>() { {cableId, cableLayout} };
        }

        public void Place(Point placePosition, Sheet sheet, Graphic graphic, Group group, E3Text text)
        {
            PlacedPosition = placePosition;
            double width = 0.2;
            List<int> ids = new List<int>(5);
            double halfBaseLength = triangleBaseLength / 2;
            Point leftBottom = new Point(sheet.MoveLeft(placePosition.X, halfBaseLength), placePosition.Y);
            Point rightBottom = new Point(sheet.MoveRight(placePosition.X, halfBaseLength), placePosition.Y);
            Point top = new Point(placePosition.X, sheet.MoveUp(placePosition.Y, triangleHeight));
            int sheetId = sheet.Id;
            ids.Add(graphic.CreateLine(sheetId, leftBottom.X, leftBottom.Y, rightBottom.X, rightBottom.Y, width));
            ids.Add(graphic.CreateLine(sheetId, leftBottom.X, leftBottom.Y, top.X, top.Y, width));
            ids.Add(graphic.CreateLine(sheetId, rightBottom.X, rightBottom.Y, top.X, top.Y, width));
            ids.Add(text.CreateText(sheetId, description, top.X, sheet.MoveUp(top.Y, descriptionVerticalMargin), font));
            ids.AddRange(CableLayoutById.Values.First().Place(sheet, graphic, placePosition));
            group.CreateGroup(ids);

        }
    }
}
