using System;
using System.Collections.Generic;
using System.Windows;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    class CableSymbol
    {
        private CableInfo cableInfo;
        private Orientation orientation;
        private int connectionColorIndex = 16;
        private double diameter = 10;
        private double smallOffset = 1;
        private double bigOffset = 1.5;
        private double lineHeight = 0.2;
        private double ovalLength;
        private double nameTextWidth;
        private double typeTextWidth;
        private double lengthTextWidth;
        private double smallFontHeight;
        private double bigFontHeight;
        private bool isCircle;

        public int CableId { get; private set; }

        public Size Size { get; private set; }

        public double Diameter
        {
            get
            {
                return diameter;
            }
        }

        public Orientation Orientation
        {
            get
            {
                return orientation;
            }
        }

        public String CableName
        {
            get
            {
                return cableInfo.Name;
            }
        }

        public Point PlacedPosition { get; private set; }

        public CableSymbol(CableInfo cableInfo, E3Text text, Orientation orientation)
        {
            this.cableInfo = cableInfo;
            this.orientation = orientation;
            CableId = cableInfo.Id;
            ovalLength = 0;
            smallFontHeight = 2;
            bigFontHeight = 3;
            isCircle = true;
            E3Font smallFont = new E3Font(height: smallFontHeight);
            E3Font bigFont = new E3Font(height: 3);
            nameTextWidth = text.GetTextLength(cableInfo.Name, bigFont);
            typeTextWidth = text.GetTextLength(cableInfo.Type, smallFont);
            lengthTextWidth = text.GetTextLength(cableInfo.Length, smallFont);
            Size = GetSize();
        }

        private Size GetSize()
        {
            if (nameTextWidth + 2 > diameter)
            {
                isCircle = false;
                ovalLength = nameTextWidth + 2 - diameter;
            }
            double width, height;
            double thickness = Math.Max(diameter, (smallFontHeight + bigOffset) * 2);
            if (orientation == Orientation.Horizontal)
            {
                width = diameter + typeTextWidth + lengthTextWidth + ovalLength + smallOffset * 2;
                height = thickness;
            }
            else
            {
                height = diameter + smallOffset + Math.Max(typeTextWidth, lengthTextWidth);
                width = thickness + ovalLength;
            }
            return new Size(width, height);
        }

        public void Place(Sheet sheet, Graphic graphic, E3Text text, Group group, Point placePosition)
        {
            E3Font smallFont = new E3Font(height: smallFontHeight, alignment: Alignment.Left);
            E3Font bigFont = new E3Font(height: bigFontHeight, alignment: Alignment.Left); 
            List<int> groupIds;
            if (orientation == Orientation.Horizontal)
                groupIds = PlaceHorizontally(sheet, graphic, text, placePosition, smallFont, bigFont);
            else
                groupIds = PlaceVertically(sheet, graphic, text, placePosition, smallFont, bigFont);
            group.CreateGroup(groupIds);
        }

        private List<int> PlaceHorizontally(Sheet sheet, Graphic graphic, E3Text text, Point placePosition, E3Font smallFont, E3Font bigFont)
        {
            List<int> groupIds = new List<int>(6);
            int sheetId = sheet.Id;
            double x = sheet.MoveLeft(placePosition.X, Size.Width / 2);
            double y = sheet.MoveDown(placePosition.Y, bigOffset + smallFontHeight);
            groupIds.Add(text.CreateText(sheetId, cableInfo.Type, x, y, smallFont));
            double x2 = sheet.MoveRight(x, typeTextWidth + smallOffset);
            groupIds.Add(graphic.CreateLine(sheetId, x, placePosition.Y, x2, placePosition.Y, lineHeight,connectionColorIndex));
            double radius = diameter / 2;
            double halfNameTextWidth = nameTextWidth / 2;
            x = sheet.MoveRight(x2, radius - halfNameTextWidth + ovalLength / 2);
            y = sheet.MoveDown(placePosition.Y, bigFontHeight / 2);
            groupIds.Add(text.CreateText(sheetId, cableInfo.Name, x, y, bigFont));
            x = sheet.MoveRight(x2, radius);
            if (isCircle)
            {
                groupIds.Add(graphic.CreateCircle(sheetId, x, placePosition.Y, radius));
                x = sheet.MoveRight(x, radius);
            }
            else
            {
                groupIds.Add(graphic.CreateArc(sheetId, x, placePosition.Y, radius, 90, 270));
                double y1 = sheet.MoveUp(placePosition.Y, radius);
                double y2 = sheet.MoveDown(placePosition.Y, radius);
                x2 = sheet.MoveRight(x, ovalLength);
                groupIds.Add(graphic.CreateLine(sheetId, x, y1, x2, y1));
                groupIds.Add(graphic.CreateLine(sheetId, x, y2, x2, y2));
                groupIds.Add(graphic.CreateArc(sheetId, x2, placePosition.Y, radius, 270, 90));
                x = sheet.MoveRight(x2, radius);
            }
            x2 = sheet.MoveRight(x, smallOffset + lengthTextWidth);
            groupIds.Add(graphic.CreateLine(sheetId, x, placePosition.Y, x2, placePosition.Y, lineHeight,connectionColorIndex));
            x = sheet.MoveRight(x, smallOffset);
            y = sheet.MoveUp(placePosition.Y, bigOffset);
            groupIds.Add(text.CreateText(sheetId, cableInfo.Length, x, y, smallFont));
            PlacedPosition = placePosition;
            return groupIds;
        }

        private List<int> PlaceVertically(Sheet sheet, Graphic graphic, E3Text text, Point placePosition, E3Font smallFont, E3Font bigFont)
        {
            List<int> groupIds = new List<int>(5);
            int sheetId = sheet.Id;
            double radius = diameter / 2;
            double x, y2;
            double y = sheet.MoveUp(placePosition.Y, Size.Height / 2 - radius);
            if (isCircle)
            {
                groupIds.Add(graphic.CreateCircle(sheetId, placePosition.X, y, radius));
            }
            else
            { 
                x = sheet.MoveLeft(placePosition.X, ovalLength / 2);
                groupIds.Add(graphic.CreateArc(sheetId, x, y, radius, 90, 270));
                double x2 = sheet.MoveRight(x, ovalLength);
                groupIds.Add(graphic.CreateArc(sheetId, x2, y, radius, 270, 90));
                y = sheet.MoveDown(y, radius);
                y2 = sheet.MoveUp(y, 2 * radius);
                groupIds.Add(graphic.CreateLine(sheetId, x, y, x2, y));
                groupIds.Add(graphic.CreateLine(sheetId, x, y2, x2, y2));
                y = sheet.MoveUp(y, radius);
            }
            x = sheet.MoveLeft(placePosition.X, nameTextWidth / 2);
            y = sheet.MoveDown(y, bigFontHeight / 2);
            groupIds.Add(text.CreateText(sheetId, cableInfo.Name, x,y, bigFont));
            y = sheet.MoveDown(y, radius - bigFontHeight/2);
            y2 = sheet.MoveDown(y, smallOffset + Math.Max(typeTextWidth, lengthTextWidth));
            groupIds.Add(graphic.CreateLine(sheetId, placePosition.X, y, placePosition.X, y2, lineHeight,connectionColorIndex));
            y = sheet.MoveDown(y, smallOffset+typeTextWidth);
            x = sheet.MoveLeft(placePosition.X, bigOffset);
            groupIds.Add(text.CreateVerticalText(sheetId, cableInfo.Type, x, y, smallFont));
            x = sheet.MoveRight(placePosition. X, bigOffset + smallFontHeight);
            y = sheet.MoveUp(y, typeTextWidth - lengthTextWidth);
            groupIds.Add(text.CreateVerticalText(sheetId, cableInfo.Length, x, y, smallFont));
            PlacedPosition = placePosition;
            return groupIds;
        }
    }
}
