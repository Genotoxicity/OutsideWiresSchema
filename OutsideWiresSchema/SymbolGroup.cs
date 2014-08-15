using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    class SymbolGroup
    {
        private double gridStep;
        private double width;
        private string assignment;
        private Dictionary<int, int> matePositionByCableId;
        private int groupPosition;
        private List<DeviceSymbol> deviceSymbols;
        private Dictionary<int, CableLayout> cableLayoutById;
        private Dictionary<int, double> bottomCableVerticalOffsetByStep;
        private Dictionary<int, double> topCableVerticalOffsetByStep;

        public double LeftMargin { get; private set; }

        public double RightMargin { get; private set; }

        public double TopMargin { get; private set; }

        public double BottomMargin { get; private set; }

        public double SymbolsWidth
        {
            get
            {
                return width;
            }
        }

        public double Height
        {
            get
            {
                return TopMargin + BottomMargin;
            }
        }

        public int Id { get; private set; }

        public List<int> CableIds { get; private set; }

        public List<int> LoopIds { get; private set; }

        public Level Level { get; private set; }

        public Dictionary<int, CableLayout> CableLayoutById
        {
            get
            {
                return cableLayoutById;
            }
        }

        public List<DeviceSymbol> DeviceSymbols
        {
            get
            {
                return deviceSymbols;
            }
        }

        public Point PlacedPosition { get; private set; }

        public SymbolGroup(int id)
        {
            deviceSymbols = new List<DeviceSymbol>();
            CableIds = new List<int>();
            LoopIds = new List<int>();
            Id = id;
            width = 0;
            gridStep = 4;
        }

        public bool TryAddSymbol(DeviceSymbol deviceSymbol)
        {
            if (deviceSymbols.Count == 0)
            {
                deviceSymbols.Add(deviceSymbol);
                assignment = deviceSymbol.Assignment;
                CableIds = deviceSymbol.CableIds;
                return true;
            }
            if (!assignment.Equals(deviceSymbol.Assignment))
                return false;
            if (!CableIds.Intersect<int>(deviceSymbol.CableIds).Any<int>())
                return false;
            if (IsConnectionBetweenSymbolsExist(deviceSymbol))
                return false;
            deviceSymbols.Add(deviceSymbol);
            foreach (int cableId in deviceSymbol.CableIds)
                if (!CableIds.Contains(cableId))
                    CableIds.Add(cableId);
            return true;
        }

        private bool IsConnectionBetweenSymbolsExist(DeviceSymbol deviceSymbol)
        {
            foreach (DeviceSymbol thisDeviceSymbol in deviceSymbols)
                if (thisDeviceSymbol.ConnectionIds.Intersect<int>(deviceSymbol.ConnectionIds).Any<int>())
                    return true;
            return false;
        }

        public void SetPosition()
        {
            if (LoopIds.Count > 0 || !String.IsNullOrEmpty(assignment))
                Level = Level.Bottom;
            else
                Level = Level.Top;
        }

        public void Place(Point placePosition, Sheet sheet, Graphic graphic, Group group, E3Text text)
        {
            PlacedPosition = placePosition;
            List<int> ids = new List<int>();
            foreach (CableLayout cableLayout in cableLayoutById.Values)
                if (cableLayout.Level == Level.Bottom)
                    ids.AddRange(cableLayout.Place(sheet, graphic, placePosition));
                else
                    ids.AddRange(cableLayout.Place(sheet, graphic, placePosition));
            foreach (DeviceSymbol symbol in deviceSymbols)
            {
                ids.AddRange(symbol.Place(sheet, graphic, text, placePosition));
                placePosition.X = sheet.MoveRight(placePosition.X, symbol.Size.Width);
            }
            group.CreateGroup(ids);
        }

        public void CalculateGroupLayout(SchemeLayout layout, double height, Dictionary<int, CableSymbol> cableSymbolById, Dictionary<int, CableInfo> cableInfoById)
        {
            SortSymbols();
            cableLayoutById = new Dictionary<int, CableLayout>(CableIds.Count);
            foreach (DeviceSymbol deviceSymbol in deviceSymbols)
            {
                deviceSymbol.CalculateLayout(height);
                foreach (SymbolPin topPin in deviceSymbol.TopPins)
                    foreach (int cableId in topPin.CableIds)
                    {
                        if (!cableLayoutById.ContainsKey(cableId))
                            cableLayoutById.Add(cableId, new CableLayout(cableId, Level.Top));
                        cableLayoutById[cableId].AddOffset(new Point(width + topPin.HorizontalOffset, height));
                    }
                foreach (SymbolPin bottomPin in deviceSymbol.BottomPins)
                    foreach (int cableId in bottomPin.CableIds)
                    {
                        if (!cableLayoutById.ContainsKey(cableId))
                            cableLayoutById.Add(cableId, new CableLayout(cableId, Level.Bottom));
                        cableLayoutById[cableId].AddOffset(new Point(width + bottomPin.HorizontalOffset, 0));
                    }
                width += deviceSymbol.Size.Width;
            }
            matePositionByCableId = layout.GetMatePositionByCableId(Id);
            groupPosition = layout.GetGroupPosition(Id);
            AdjustCableLayouts(cableLayoutById, cableSymbolById, cableInfoById);
            TopMargin = height;
            BottomMargin = (bottomCableVerticalOffsetByStep.Count > 0) ? bottomCableVerticalOffsetByStep.Last().Value : 0;
            double minCablesOffset = cableLayoutById.Values.Min(cl=>cl.MinOffset);
            LeftMargin = (minCablesOffset > 0) ? 0 : -minCablesOffset;
            double maxCablesOffset = cableLayoutById.Values.Max(cl => cl.MaxOffset);
            RightMargin = (width > maxCablesOffset) ? width : maxCablesOffset;
        }

        private void SortSymbols()
        {
            NaturalSortingStringComparer stringComparer = new NaturalSortingStringComparer();
            deviceSymbols.Sort((ds1, ds2) => stringComparer.Compare(ds1.Name, ds2.Name));
        }

        private void AdjustCableLayouts(Dictionary<int, CableLayout> cableLayoutById, Dictionary<int, CableSymbol> cableSymbolById, Dictionary<int, CableInfo> cableInfoById)
        {
            List<CableLayout> topLayouts = new List<CableLayout>();
            List<CableLayout> bottomLeftHorizontalCableLayouts = new List<CableLayout>();
            List<CableLayout> bottomRightHorizontalCableLayouts = new List<CableLayout>();
            List<CableLayout> bottomVerticalCableLayouts = new List<CableLayout>();
            foreach (int cableId in CableIds)
            { 
                CableLayout cableLayout = cableLayoutById[cableId];
                if (cableLayout.Level == Level.Top)
                    topLayouts.Add(cableLayout);
                else
                {
                    if (cableSymbolById[cableId].Orientation == Orientation.Vertical)
                        bottomVerticalCableLayouts.Add(cableLayout);
                    else
                        if (matePositionByCableId[cableId] < groupPosition)
                            bottomLeftHorizontalCableLayouts.Add(cableLayout);
                        else
                            bottomRightHorizontalCableLayouts.Add(cableLayout);
                }
            }
            VerticalCableLayoutComparer verticalComparer = new VerticalCableLayoutComparer(cableInfoById, matePositionByCableId);
            topLayouts.Sort(verticalComparer);
            bottomVerticalCableLayouts.Sort(verticalComparer);
            HorizontalCableLayoutComparer horizontalComparer = new HorizontalCableLayoutComparer(cableInfoById, matePositionByCableId, groupPosition);
            bottomRightHorizontalCableLayouts.Sort(horizontalComparer);
            bottomLeftHorizontalCableLayouts.Sort(horizontalComparer);
            int totalCablesCount = bottomLeftHorizontalCableLayouts.Count + bottomRightHorizontalCableLayouts.Count + bottomVerticalCableLayouts.Count;
            List<CableLayout> bottomLayouts = new List<CableLayout>(totalCablesCount);
            bottomLayouts.AddRange(bottomLeftHorizontalCableLayouts);
            bottomLayouts.AddRange(bottomVerticalCableLayouts);
            bottomLayouts.AddRange(bottomRightHorizontalCableLayouts);
            SetLayoutsSkewDirectionAndOffset(cableLayoutById, topLayouts);
            SetLayoutsSkewDirectionAndOffset(cableLayoutById, bottomLayouts);
            bottomCableVerticalOffsetByStep = GetCableVerticalOffsetByStep(bottomLayouts);
            topCableVerticalOffsetByStep = GetCableVerticalOffsetByStep(topLayouts);
            foreach (CableLayout cableLayout in cableLayoutById.Values)
                cableLayout.verticalOffset = (cableLayout.Level == Level.Bottom) ? bottomCableVerticalOffsetByStep[cableLayout.verticalOffsetStep] : topCableVerticalOffsetByStep[cableLayout.verticalOffsetStep];
        }

        private void SetLayoutsSkewDirectionAndOffset(Dictionary<int, CableLayout> cableLayoutById, List<CableLayout> cableLayouts)
        {
            List<List<int>> layoutIdsWithRepeatedOffsets = new List<List<int>>();
            for (int i = 0; i < cableLayouts.Count - 1; i++)
            {
                CableLayout firstLayout = cableLayouts[i];
                List<Point> firstOffsets = firstLayout.StartOffsets;
                int firstOffsetsCount = firstOffsets.Count;
                for (int j = i + 1; j < cableLayouts.Count; j++)
                {
                    CableLayout secondLayout = cableLayouts[j];
                    if (firstOffsets.Intersect(secondLayout.StartOffsets).Count() == firstOffsetsCount)
                        AddIdsWithRepeatedOffsetToList(layoutIdsWithRepeatedOffsets, firstLayout.Id, secondLayout.Id);
                }
            }
            double skewOffset;
            if (layoutIdsWithRepeatedOffsets.Count == 1)
            {
                List<int> ids = layoutIdsWithRepeatedOffsets.First();
                CableLayout firstLayout = cableLayoutById[ids.First()];
                skewOffset = (firstLayout.StartOffsets.Max(p => p.X) - firstLayout.StartOffsets.Min(p => p.X)) + gridStep;
            }
            else
                skewOffset = gridStep * 1.5;
            foreach (List<int> ids in layoutIdsWithRepeatedOffsets)
                for (int i = 0; i < ids.Count; i++)
                {
                    Position direction;
                    switch (i)
                    {
                        case 0: direction = Position.Left; break;
                        case 1: direction = Position.Center; break;
                        default: direction = Position.Right; break;
                    }
                    cableLayoutById[ids[i]].SetSkewDirectionAndoffset(direction, skewOffset);
                }
        }

        private static void AddIdsWithRepeatedOffsetToList(List<List<int>> layoutIdsWithRepeatedOffsets, int firstId, int secondId)
        {
            foreach (List<int> ids in layoutIdsWithRepeatedOffsets)
                if (ids.Contains(firstId))
                {
                    if (!ids.Contains(secondId))
                        ids.Add(secondId);
                    return;
                }
            layoutIdsWithRepeatedOffsets.Add(new List<int>() { firstId, secondId });
        }

        private Dictionary<int, double> GetCableVerticalOffsetByStep(List<CableLayout> layouts)
        {
            Dictionary<int, double> cableVerticalOffsetByStep = new Dictionary<int, double>();
            if (layouts.Count == 0)
                return cableVerticalOffsetByStep;
            Dictionary<int, List<int>> cablePositionsByStep = GetCablePositionsByStep(layouts);
            double previousOffset = layouts.First().StartOffsets.First().Y;
            for (int step = 0; step < cablePositionsByStep.Count; step++)
            {
                List<int> cableIdsWithText = GetCableIdsWithText(layouts, cablePositionsByStep, step);
                double offset;
                if (cableIdsWithText.Count == 0)
                {
                    offset = previousOffset + gridStep;
                    if (step == 0)
                        offset = Math.Max(offset, gridStep * 2);
                }
                else
                {
                    List<SymbolPin> connectedPins = GetConnectedPins(layouts.First().Level);
                    double maxTextLength = (connectedPins.Count > 0) ? connectedPins.Max(p => p.SignalTextWidth) : 0;
                    offset = previousOffset + maxTextLength + 2; // 2 миллиметра зазора для текста
                    if (step == 0)
                        offset = Math.Max(maxTextLength + gridStep, gridStep * 2) + previousOffset +2;
                }
                cableVerticalOffsetByStep[step] = offset;
                previousOffset = offset;
            }
            return cableVerticalOffsetByStep;
        }

        private static List<int> GetCableIdsWithText(List<CableLayout> layouts, Dictionary<int, List<int>> cablePositionsByStep, int step)
        {
            List<int> cableIdsWithText = new List<int>();
            foreach (int position in cablePositionsByStep[step])
                if (layouts[position].SkewDirection == Position.Center)
                    cableIdsWithText.Add(layouts[position].Id);
            return cableIdsWithText;
        }

        private Dictionary<int, List<int>> GetCablePositionsByStep(List<CableLayout> layouts)
        {
            List<int> optimalVerticalSteps = GetOptimalVerticalStep(layouts);
            Dictionary<int, List<int>> cablePositionsByStep = new Dictionary<int, List<int>>();
            for (int i = 0; i < layouts.Count; i++)
            {
                int step = optimalVerticalSteps[i];
                layouts[i].verticalOffsetStep = step;
                if (!cablePositionsByStep.ContainsKey(step))
                    cablePositionsByStep.Add(step, new List<int>());
                cablePositionsByStep[step].Add(i);
            }
            return cablePositionsByStep;
        }

        private List<int> GetOptimalVerticalStep(List<CableLayout> layouts)
        {
            int layoutCount = layouts.Count;
            List<int> optimalStep = new List<int>(layoutCount);
            List<int> steps = new List<int>(layoutCount);
            for (int i = 0; i < layoutCount; i++)
                steps.Add(0);
            int maxStepCount = layoutCount;
            int lastIndex = maxStepCount - 1;
            int minFine = int.MaxValue;
            do
            {
                for (int i = 0; i < layoutCount; i++)
                    layouts[i].verticalOffsetStep = steps[i];
                if (GetLayoutIntersectionCount(layouts) == 0)
                {
                    int fine = GetFine(layouts);
                    if (fine < minFine)
                    {
                        minFine = fine;
                        optimalStep = new List<int>(steps);
                    }
                }
            }
            while (NextPermutationWithRepetition(steps, maxStepCount, lastIndex));
            return optimalStep;
        }

        private List<SymbolPin> GetConnectedPins(Level level)
        {
            List<SymbolPin> connectedPins = new List<SymbolPin>();
            if (level == Level.Bottom)
            {
                foreach (DeviceSymbol deviceSymbol in deviceSymbols)
                    foreach (SymbolPin symbolPin in deviceSymbol.BottomPins)
                        connectedPins.Add(symbolPin);
            }
            if (level == Level.Top)
            {
                foreach (DeviceSymbol deviceSymbol in deviceSymbols)
                    foreach (SymbolPin symbolPin in deviceSymbol.TopPins)
                        connectedPins.Add(symbolPin);
            }
            return connectedPins;
        }

        private static int GetLayoutIntersectionCount(List<CableLayout> layouts)
        {
            int intersectionCount = 0;
            for (int i = 0; i < layouts.Count-1; i++)
            {
                CableLayout firstLayout = layouts[i];
                for (int j = i + 1; j < layouts.Count; j++)
                    if (IsLayoutIntersect(firstLayout, layouts[j]))
                        intersectionCount++;
            }
            return intersectionCount;
        }

        private static bool IsLayoutIntersect(CableLayout firstLayout, CableLayout secondLayout)
        {
            if (firstLayout.verticalOffsetStep != secondLayout.verticalOffsetStep)
                return false;
            double firstMin = firstLayout.MinOffset;
            double firstMax = firstLayout.MaxOffset;
            double secondMin = secondLayout.MinOffset;
            double secondMax = secondLayout.MaxOffset;
            return (firstMax > secondMin && firstMin < secondMax) || (secondMax > firstMin && secondMin < firstMax);
        }

        /// <summary>
        /// Функция вычисляет штраф за некрасивое расположение, "критерии красоты" - 1. Скошенные кабеля должны быть выше 2. Чем левее, тем выше
        /// </summary>
        /// <param name="layouts"></param>
        /// <returns></returns>
        private static int GetFine(List<CableLayout> layouts)
        {
            int fine = 0;
            for (int i=0; i < layouts.Count; i++)
            {
                CableLayout layout = layouts[i];
                fine += (int)Math.Abs(layout.MaxOffset - layout.MinOffset) * layout.verticalOffsetStep * 1000;  // +1 чтобы не было вырожденного случая умножения на 0.
                fine += layout.verticalOffsetStep * 10 * (i + 1);
                if (layout.SkewDirection != Position.Center)
                    fine += layout.verticalOffsetStep * 100 * (i+1);
            }
            return fine;
        }

        private bool NextPermutationWithRepetition(List<int> steps, int maxStepCount, int lastIndex)
        {
            steps[lastIndex]++;
            if (steps[lastIndex] < maxStepCount)
                return true;
            else
            {
                steps[lastIndex] = 0;
                if (lastIndex > 0)
                    return NextPermutationWithRepetition(steps, maxStepCount, --lastIndex);
            }
            return false;
        }

        private class VerticalCableLayoutComparer : IComparer<CableLayout>
        {
            private Dictionary<int, int> matePositionByCableId;
            private Dictionary<int, CableInfo> cableInfoById;
            private NaturalSortingStringComparer stringComparer;

            public VerticalCableLayoutComparer(Dictionary<int, CableInfo> cableInfoById, Dictionary<int, int> matePositionByCableId)
            {
                this.matePositionByCableId = matePositionByCableId;
                this.cableInfoById = cableInfoById;
                stringComparer = new NaturalSortingStringComparer();
            }

            public int Compare(CableLayout a, CableLayout b)
            {
                int aId = a.Id;
                int bId = b.Id;
                int aMatePosition = matePositionByCableId[aId];
                int bMatePosition = matePositionByCableId[bId];
                if (aMatePosition < bMatePosition)
                    return -1;
                if (aMatePosition > bMatePosition)
                    return 1;
                return stringComparer.Compare(cableInfoById[a.Id].Name, cableInfoById[b.Id].Name);
            }
        }

        private class HorizontalCableLayoutComparer : IComparer<CableLayout>
        {
            private Dictionary<int, int> matePositionByCableId;
            private Dictionary<int, CableInfo> cableInfoById;
            private NaturalSortingStringComparer stringComparer;
            private int groupPosition;

            public HorizontalCableLayoutComparer(Dictionary<int, CableInfo> cableInfoById, Dictionary<int, int> matePositionByCableId, int groupPosition)
            {
                this.matePositionByCableId = matePositionByCableId;
                this.cableInfoById = cableInfoById;
                this.groupPosition = groupPosition;
                stringComparer = new NaturalSortingStringComparer();
            }

            public int Compare(CableLayout a, CableLayout b)
            {
                int aId = a.Id;
                int bId = b.Id;
                int aDistance = Math.Abs(groupPosition - matePositionByCableId[aId]);
                int bDistance = Math.Abs(groupPosition - matePositionByCableId[bId]);
                if (aDistance < bDistance)
                    return -1;
                if (aDistance > bDistance)
                    return 1;
                return stringComparer.Compare(cableInfoById[a.Id].Name, cableInfoById[b.Id].Name);
            }
        }
    }
}
