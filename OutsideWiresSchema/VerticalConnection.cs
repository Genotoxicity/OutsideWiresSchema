using System.Collections.Generic;
using System.Linq;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    class VerticalConnection
    {
        public List<int> TopGroupIds { get; private set; }
        public List<int> BottomGroupIds { get; private set; }
        private Dictionary<int, double> offsetByCableId;
        private List<CableSymbol> verticalCableSymbols;
        private List<SymbolGroup> topGroups;
        private List<SymbolGroup> bottomGroups;
        private double cablesWidthSum;
        private double minCablesWidth;
        private double gridStep;

        public List<CableSymbol> VerticalCableSymbols
        {
            get
            {
                return verticalCableSymbols;
            }
        }

        public List<SymbolGroup> TopGroups
        {
            get
            {
                return topGroups;
            }
        }

        public List<SymbolGroup> BottomGroups
        {
            get
            {
                return bottomGroups;
            }
        }

        public Dictionary<int, double> OffsetByCableId
        {
            get
            {
                return offsetByCableId;
            }
        }

        public double MinCablesWidth
        {
            get
            {
                return minCablesWidth;
            }
        }

        public VerticalConnection(List<int> topGroupIds, List<int> bottomGroupIds, double gridStep)
        {
            TopGroupIds = topGroupIds;
            BottomGroupIds = bottomGroupIds;
            this.gridStep = gridStep;
        }

        public void SetGroups(Dictionary<int, SymbolGroup> groupById)
        {
            topGroups = new List<SymbolGroup>(TopGroupIds.Count);
            foreach (int topGroupId in TopGroupIds)
                topGroups.Add(groupById[topGroupId]);
            bottomGroups = new List<SymbolGroup>(BottomGroupIds.Count);
            foreach (int bottomGroupId in BottomGroupIds)
                bottomGroups.Add(groupById[bottomGroupId]);
        }

        public void SetVerticalCableSymbols(Dictionary<int, CableSymbol> cableSymbolById)
        {
            verticalCableSymbols = BottomGroups.SelectMany(g => g.CableIds).Select(id => cableSymbolById[id]).Where(cableSymbol => cableSymbol.Orientation == Orientation.Vertical).ToList();
            Dictionary<int, int> topPositionById = new Dictionary<int, int>(TopGroupIds.Count);
            for (int i = 0; i < TopGroupIds.Count; i++)
                topPositionById.Add(TopGroupIds[i], i);
            Dictionary<int, int> bottomPositionById = new Dictionary<int, int>(BottomGroupIds.Count);
            for (int i = 0; i < BottomGroupIds.Count; i++)
                bottomPositionById.Add(BottomGroupIds[i], i);
            VerticalCableSymbolsComparer comparer = new VerticalCableSymbolsComparer(topGroups, bottomGroups, bottomPositionById, topPositionById);
            verticalCableSymbols.Sort(comparer);
            cablesWidthSum = verticalCableSymbols.Sum(vc => vc.Size.Width);
            minCablesWidth = cablesWidthSum + (verticalCableSymbols.Count - 1) * gridStep;
        }
        
        public void CalculateCableLayout(double offset, double groupsWidth)
        {
            offsetByCableId = new Dictionary<int,double>(verticalCableSymbols.Count);
            if (minCablesWidth >= groupsWidth)
            {
                offset += (groupsWidth - minCablesWidth) / 2;
                foreach (CableSymbol cableSymbol in verticalCableSymbols)
                {
                    offset += cableSymbol.Size.Width / 2;
                    offsetByCableId.Add(cableSymbol.CableId, offset);
                    offset += cableSymbol.Size.Width / 2 + gridStep;
                }
            }
            else
                foreach (CableSymbol cableSymbol in verticalCableSymbols)
                {
                    double halfPortion = groupsWidth * (cableSymbol.Size.Width / cablesWidthSum) / 2;
                    offset += halfPortion;
                    offsetByCableId.Add(cableSymbol.CableId, offset);
                    offset += halfPortion;
                }
        }

        private class VerticalCableSymbolsComparer : IComparer<CableSymbol>
        {
            private Dictionary<int, int> optimalBottomPositionById;
            private Dictionary<int, int> optimalTopPositionById;
            private List<SymbolGroup> topGroups;
            private List<SymbolGroup> bottomGroups;

            private NaturalSortingStringComparer stringComparer;

            public VerticalCableSymbolsComparer(List<SymbolGroup> topGroups, List<SymbolGroup> bottomGroups, Dictionary<int, int> bottomPositionById, Dictionary<int, int> topPositionById)
            {
                this.optimalBottomPositionById = bottomPositionById;
                this.optimalTopPositionById = topPositionById;
                this.topGroups = topGroups;
                this.bottomGroups = bottomGroups;
                stringComparer = new NaturalSortingStringComparer();
            }

            public int Compare(CableSymbol a, CableSymbol b)
            {
                int aBottomId = bottomGroups.First(g => g.CableIds.Contains(a.CableId)).Id;
                int bBottomId = bottomGroups.First(g => g.CableIds.Contains(b.CableId)).Id;
                int diff = optimalBottomPositionById[aBottomId] - optimalBottomPositionById[bBottomId];
                if (diff != 0)
                    return diff;
                int aTopId = topGroups.First(g => g.CableIds.Contains(a.CableId)).Id;
                int bTopId = topGroups.First(g => g.CableIds.Contains(b.CableId)).Id;
                diff = optimalTopPositionById[aTopId] - optimalTopPositionById[bTopId];
                if (diff != 0)
                    return diff;
                return stringComparer.Compare(a.CableName, b.CableName);
            }
        }
    }
}
