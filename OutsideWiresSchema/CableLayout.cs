using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    class CableLayout
    {
        private List<Point> startOffsets;
        private double skewOffset;
        private double gridStep;
        private Level level;
        private double minOffset;
        private double maxOffset;
        private Position skewDirection;
        public int verticalOffsetStep;
        public double verticalOffset;

        public Position SkewDirection
        {
            get
            {
                return skewDirection;
            }
        }

        public Level Level
        {
            get
            {
                return level;
            }
        }

        public double MinOffset
        {
            get
            {
                return minOffset;
            }
        }

        public double MaxOffset
        {
            get
            {
                return maxOffset;
            }
        }

        public List<Point> StartOffsets
        {
            get
            {
                return startOffsets;
            }
        }

        public List<Point> PlacedPoints { get; private set; }

        public int Id { get; private set; }

        public CableLayout(int id, Level level)
        {
            startOffsets = new List<Point>();
            Id = id;
            gridStep = 4;
            this.level = level;
            skewDirection = Position.Center;
            verticalOffsetStep = 0;
            minOffset = double.MaxValue;
            maxOffset = double.MinValue;
        }

        public void AddOffset(Point offset)
        {
            startOffsets.Add(offset);
            minOffset = Math.Min(minOffset, offset.X);
            maxOffset = Math.Max(maxOffset, offset.X);
        }

        public void SetSkewDirectionAndoffset(Position direction, double offset)
        {
            minOffset = startOffsets.Min(l => l.X);
            maxOffset = startOffsets.Max(l => l.X);
            if (direction == Position.Left)
            {
                minOffset -= offset;
                maxOffset -= offset;
            }
            if (direction == Position.Right)
            {
                minOffset += offset;
                maxOffset += offset;
            }
            skewDirection = direction;
            skewOffset = offset;
        }

        public List<int> Place(Sheet sheet, Graphic graphic, Point placePosition)
        {
            double lineHeight = 0.2;
            List<int> ids = new List<int>();
            int sheetId = sheet.Id;
            int colorIndex = 16;
            double endOrdinate = (level == Level.Top) ? sheet.MoveUp(placePosition.Y, verticalOffset) : sheet.MoveDown(placePosition.Y, verticalOffset);
            List<Point> placedPoints = new List<Point>(startOffsets.Count);
            if (skewDirection == Position.Center )
                foreach (Point startOffset in startOffsets)
                {
                    double x = sheet.MoveRight(placePosition.X, startOffset.X);
                    double y = (level == Level.Top) ? sheet.MoveUp(placePosition.Y, startOffset.Y) : sheet.MoveDown(placePosition.Y, startOffset.Y);
                    ids.Add(graphic.CreateLine(sheetId, x, y, x, endOrdinate, lineHeight,colorIndex));
                    placedPoints.Add(new Point(x, endOrdinate));
                }
            else
                for (int i = 0; i < startOffsets.Count; i++)
                {
                    Point startOffset = startOffsets[i];
                    double xStart = sheet.MoveRight(placePosition.X, startOffset.X);
                    double yStart = (level == Level.Top) ? sheet.MoveUp(placePosition.Y, startOffset.Y) : sheet.MoveDown(placePosition.Y, startOffset.Y);
                    double yIntermediate = (level == Level.Top) ? sheet.MoveUp(yStart, gridStep) :  sheet.MoveDown(yStart, gridStep);
                    double xIntermediate = (skewDirection == Position.Left) ? sheet.MoveLeft(xStart, skewOffset) : sheet.MoveRight(xStart, skewOffset);
                    ids.Add(graphic.CreateLine(sheetId, xStart, yStart, xIntermediate, yIntermediate, lineHeight, colorIndex));
                    ids.Add(graphic.CreateLine(sheetId, xIntermediate, yIntermediate, xIntermediate, endOrdinate, lineHeight, colorIndex));
                    placedPoints.Add(new Point(xIntermediate, endOrdinate));
                }
            PlacedPoints = placedPoints;
            if (minOffset!=maxOffset)
                ids.Add(graphic.CreateLine(sheetId, sheet.MoveRight(placePosition.X, minOffset),endOrdinate, sheet.MoveRight(placePosition.X, maxOffset), endOrdinate, 0.5 ,colorIndex));
            return ids;
        }

    }
}
