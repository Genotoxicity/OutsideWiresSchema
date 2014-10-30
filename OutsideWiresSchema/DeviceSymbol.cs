using System.Collections.Generic;
using System.Linq;
using System.Windows;
using KSPE3Lib;
using System;

namespace OutsideConnectionsSchema
{
    class DeviceSymbol
    {
        private int id;
        private List<SymbolPin> topPins;
        private List<SymbolPin> bottomPins;
        private E3Font bigFont;
        private E3Font smallFont;
        private double nameWidth;
        private double topPinsWidth;
        private double bottomPinsWidth;
        private double topPinsHeight;
        private double bottomPinsHeight;
        private double gridStep;
        private double halfGridStep;
        private double skewLineOffset;
        private bool isTerminal;
        
        public List<int> ConnectionIds { get; private set; }

        public string Assignment { get; private set; }

        public string Name { get; private set; }

        public Size Size { get; private set; }

        public List<int> CableIds { get; private set; }

        public List<SymbolPin> TopPins
        {
            get
            {
                return topPins;
            }
        }

        public List<SymbolPin> BottomPins
        {
            get
            {
                return bottomPins;
            }
        }

        public DeviceSymbol(NormalDevice device, DevicePin pin )
        {
            id = device.Id;
            Assignment = String.Intern(device.Assignment);
            if (String.IsNullOrEmpty(Assignment) && device.GetAttributeValue("IncludeInOWS").Equals("1"))
                Assignment = String.Intern("AssignmentForConnectiongBox");
            Name = device.Name;
            isTerminal = device.IsTerminal();
            if (isTerminal)
            {
               pin.Id = device.PinIds.First();
               Name += ":" + pin.Name;
            }
            ConnectionIds = new List<int>();
            bigFont = new E3Font(alignment: Alignment.Left);
            smallFont = new E3Font(height: 2.5, alignment: Alignment.Left);
            gridStep = 4;
            halfGridStep = gridStep / 2;
            skewLineOffset = gridStep;
        }

        public void SetCableIds(Dictionary<int, DeviceConnection> deviceConnectionById)
        {
            List<int> ids = new List<int>();
            foreach (int connectionId in ConnectionIds)
            {
                int cableId = deviceConnectionById[connectionId].CableId;
                if (!ids.Contains(cableId))
                    ids.Add(cableId);
            }
            CableIds = ids;
        }

        public void SetPinsAndHeightAndNameWidth(Dictionary<int, DeviceConnection> deviceConnectionById, Dictionary<int, DeviceSymbol> deviceSymbolById, E3Text text)
        {
            GetPins(deviceConnectionById, deviceSymbolById, text);
            double height;
            if (isTerminal)
                height = 25;
            else
            {
                nameWidth = text.GetTextLength(Name, bigFont);
                topPinsHeight = GetPinsHeight(topPins, text);
                bottomPinsHeight = GetPinsHeight(bottomPins, text);
                height = topPinsHeight + bottomPinsHeight + RoundUp(bigFont.height) + 4;
            }
            Size = new Size(0, height);
        }

        public void CalculateLayout(double height)
        {
            double totalWidth;
            if (isTerminal)
            {
                totalWidth = gridStep;
                SetPinsHorizontalOffsets(bottomPins, halfGridStep);
                SetPinsHorizontalOffsets(topPins, halfGridStep);
            }
            else
            {
                topPinsWidth = topPins.Count * gridStep;
                bottomPinsWidth = bottomPins.Count * gridStep;
                totalWidth = Math.Max(Math.Max(topPinsWidth, bottomPinsWidth), RoundUp(nameWidth));
                SetPinsHorizontalOffsets(bottomPins, (totalWidth - bottomPinsWidth) /2 + halfGridStep);
                SetPinsHorizontalOffsets(topPins, (totalWidth - topPinsWidth) / 2 + halfGridStep);
            }
            Size = new Size(totalWidth, height);
        }

        private void SetPinsHorizontalOffsets(List<SymbolPin> pins,double firstPinHorizontalOffset)
        {
            double offset = firstPinHorizontalOffset;
            foreach (SymbolPin pin in pins)
            {
                pin.SetHorizontalOffset(offset);
                offset += gridStep;
            }
        }

        public List<int> Place(Sheet sheet, Graphic graphic, E3Text text, Point position)
        {
            int sheetId = sheet.Id;
            if (isTerminal)
                return CreateTerminalSymbol(sheet, text, graphic, sheetId, position);
            else
                return CreateDeviceSymbol(sheet, text, graphic, sheetId, position);
        }

        private List<int> CreateDeviceSymbol(Sheet sheet, E3Text text, Graphic graphic, int sheetId, Point position )
        {
            List<int> groupIds = new List<int>((topPins.Count + bottomPins.Count) * 2 + 2);
            E3Font bigFont = new E3Font(alignment: Alignment.Left);
            groupIds.Add(CreateOutline(sheet, graphic, sheetId, position));
            double xText = sheet.MoveRight(position.X, (Size.Width - nameWidth) / 2);
            double spaceForName = Size.Height - bottomPinsHeight - topPinsHeight;
            double offsetForName = (spaceForName - bigFont.height) / 2;
            double yText = sheet.MoveUp(position.Y, bottomPinsHeight + offsetForName);
            groupIds.Add(text.CreateText(sheetId, Name, xText, yText, bigFont));
            double top = sheet.MoveUp(position.Y, Size.Height);
            double pinBottom = sheet.MoveDown(top, topPinsHeight);
            List<int> topPinIds = DrawPins(sheet, graphic, text, topPins, topPinsHeight, topPinsWidth, sheetId, position.X, pinBottom, top, Level.Top);
            double pinTop = sheet.MoveUp(position.Y, bottomPinsHeight);
            List<int> bottomPinIds = DrawPins(sheet, graphic, text, bottomPins, bottomPinsHeight, bottomPinsWidth, sheetId, position.X, position.Y, pinTop, Level.Bottom);
            groupIds.AddRange(topPinIds);
            groupIds.AddRange(bottomPinIds);
            return groupIds;
        }

        private List<int> CreateTerminalSymbol(Sheet sheet, E3Text text, Graphic graphic, int sheetId, Point position)
        {
            List<int> groupIds;
            E3Font smallFont = new E3Font(height:2.5, alignment: Alignment.Left);
            int outlineId = CreateOutline(sheet, graphic, sheetId, position);
            double offset = 0.5;
            double xText = sheet.MoveRight(sheet.MoveRight(position.X, halfGridStep), smallFont.height / 2 - 0.2);
            if (!String.IsNullOrEmpty(Assignment))
            {
                double yAssignmentText = sheet.MoveUp(position.Y, offset);
                groupIds = new List<int>(3);
                groupIds.Add(text.CreateVerticalText(sheetId, Assignment, xText, yAssignmentText, smallFont));
            }
            else
                groupIds = new List<int>(2);
            groupIds.Add(outlineId);
            nameWidth = text.GetTextLength(Name, smallFont);
            double top = sheet.MoveUp(position.Y, Size.Height);
            double yText = sheet.MoveDown(top, offset + nameWidth);
            groupIds.Add(text.CreateVerticalText(sheetId, Name, xText, yText, smallFont));
            foreach (SymbolPin topPin in topPins)
            {
                int signalTextId = DrawSignalAndSetConnectionPoint(topPin, Level.Top, sheet, text, sheetId, position.Y, top, position.X);
                groupIds.Add(signalTextId);
            }
            foreach (SymbolPin bottomPin in bottomPins)
            {
                int signalTextId = DrawSignalAndSetConnectionPoint(bottomPin, Level.Bottom, sheet, text, sheetId, position.Y, top, position.X);
                groupIds.Add(signalTextId);
            }
            return groupIds;
        }

        private int CreateOutline(Sheet sheet, Graphic graphic, int sheetId, Point placePosition)
        {
            double x1 = placePosition.X;
            double y1 = placePosition.Y;
            double x2 = sheet.MoveRight(x1, Size.Width);
            double y2 = sheet.MoveUp(y1, Size.Height);
            return graphic.CreateRectangle(sheetId, x1, y1, x2, y2);
        }

        private List<int> DrawPins(Sheet sheet, Graphic graphic, E3Text text, List<SymbolPin> pins, double pinsHeight, double pinsWidth, int sheetId, double xLeft, double pinBottom, double pinTop, Level position)
        {
            List<int> graphicIds = new List<int>(pins.Count * 2 + 1);
            double pinLeft = sheet.MoveRight(xLeft, (Size.Width - pinsWidth) / 2);
            foreach (SymbolPin pin in pins)
            {
                double pinRight = sheet.MoveRight(pinLeft, gridStep);
                int pinOutlineId = graphic.CreateRectangle(sheetId, pinLeft, pinBottom, pinRight, pinTop);
                graphicIds.Add(pinOutlineId);
                double textWidth = text.GetTextLength(pin.Name, smallFont);
                double xPinText = sheet.MoveRight(sheet.MoveRight(pinLeft, halfGridStep), smallFont.height / 2 - 0.2);
                double yPinText = sheet.MoveUp(pinBottom, (pinsHeight - textWidth) / 2);
                graphicIds.Add(text.CreateVerticalText(sheetId, pin.Name, xPinText, yPinText, smallFont));
                int signalTextId = DrawSignalAndSetConnectionPoint(pin, position, sheet, text, sheetId, pinBottom, pinTop, pinLeft);
                graphicIds.Add(signalTextId);
                pinLeft = pinRight;
            }
            return graphicIds;
        }

        private int DrawSignalAndSetConnectionPoint(SymbolPin pin, Level position, Sheet sheet, E3Text text, int sheetId, double pinBottom, double pinTop, double pinLeft)
        {
            double xSignalText = sheet.MoveRight(pinLeft, 1);
            double ySignalText;
            E3Font font = new E3Font(height: smallFont.height);
            if (position == Level.Top)
            {
                ySignalText = sheet.MoveUp(pinTop, skewLineOffset);
                font.alignment = Alignment.Left;
            }
            else
            {
                ySignalText = sheet.MoveDown(pinBottom, skewLineOffset);
                font.alignment = Alignment.Right;
            }
            return text.CreateVerticalText(sheetId, pin.Signal, xSignalText, ySignalText, font);
        }

        private void GetPins(Dictionary<int, DeviceConnection> deviceConnectionById, Dictionary<int, DeviceSymbol> deviceSymbolById, E3Text text)
        {
            Dictionary<string, SymbolPin> topPinByName = new Dictionary<string, SymbolPin>();
            Dictionary<string, SymbolPin>  bottomPinByName = new Dictionary<string, SymbolPin>();
            foreach (int connectionId in ConnectionIds)
            {
                DeviceConnection connection = deviceConnectionById[connectionId];
                int connectedId;
                string pinName;
                if (id == connection.StartDeviceId)
                {
                    connectedId = connection.EndDeviceId;
                    pinName = connection.StartPinName;
                }
                else
                {
                    connectedId = connection.StartDeviceId;
                    pinName = connection.EndPinName;
                }
                string connectedAssignment = deviceSymbolById[connectedId].Assignment;
                if (!String.IsNullOrEmpty(Assignment) && String.IsNullOrEmpty(connectedAssignment))
                {
                    if (!topPinByName.ContainsKey(pinName))
                    {
                        SymbolPin pin = new SymbolPin(pinName, connection.Signal, text, smallFont);
                        topPinByName.Add(pinName, pin);
                    }
                    topPinByName[pinName].CableIds.Add(connection.CableId);
                }
                else
                {
                    if (!bottomPinByName.ContainsKey(pinName))
                    {
                        SymbolPin pin = new SymbolPin(pinName, connection.Signal, text, smallFont);
                        bottomPinByName.Add(pinName, pin);
                    }
                    bottomPinByName[pinName].CableIds.Add(connection.CableId);
                }
            }
            topPins = topPinByName.Values.ToList();
            bottomPins = bottomPinByName.Values.ToList();
            NaturalSortingStringComparer stringComparer = new NaturalSortingStringComparer();
            topPins.Sort((p1,p2)=>stringComparer.Compare(p1.Name, p2.Name));
            bottomPins.Sort((p1, p2) => stringComparer.Compare(p1.Name, p2.Name));
        }

        private double GetPinsHeight(List<SymbolPin> pins, E3Text text)
        {
            double offset = 2;
            return (pins.Count > 0) ? RoundUp(pins.Max(p => text.GetTextLength(p.Name, smallFont)) + offset) : 0;
        }

        private static double RoundUp(double value)
        {
            return ((int)value / 2 + 1) * 2;
        }
    }
}