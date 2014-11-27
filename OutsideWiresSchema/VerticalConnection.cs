using System.Collections.Generic;
using System.Linq;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    class VerticalConnection
    {
        public List<int> TopSymbolIds { get; private set; }
        public List<int> BottomSymbolIds { get; private set; }
        private Dictionary<int, double> offsetByCableId;
        private List<CableSymbol> verticalCableSymbols;
        private List<ISchemeSymbol> topSymbols;
        private List<ISchemeSymbol> bottomSymbols;
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

        public List<ISchemeSymbol> TopSymbols
        {
            get
            {
                return topSymbols;
            }
        }

        public List<ISchemeSymbol> BottomSymbols
        {
            get
            {
                return bottomSymbols;
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
            TopSymbolIds = topGroupIds;
            BottomSymbolIds = bottomGroupIds;
            this.gridStep = gridStep;
        }

        public void SetSymbols(Dictionary<int, ISchemeSymbol> symbolById)
        {
            topSymbols = new List<ISchemeSymbol>(TopSymbolIds.Count);
            foreach (int topGroupId in TopSymbolIds)
                topSymbols.Add(symbolById[topGroupId]);
            bottomSymbols = new List<ISchemeSymbol>(BottomSymbolIds.Count);
            foreach (int bottomGroupId in BottomSymbolIds)
                bottomSymbols.Add(symbolById[bottomGroupId]);
        }

        public void SetVerticalCableSymbols(Dictionary<int, CableSymbol> cableSymbolById)
        {
            verticalCableSymbols = BottomSymbols.SelectMany(g => g.CableIds).Select(id => cableSymbolById[id]).Where(cableSymbol => cableSymbol.Orientation == Orientation.Vertical).ToList();
            Dictionary<int, int> topPositionById = new Dictionary<int, int>(TopSymbolIds.Count);
            for (int i = 0; i < TopSymbolIds.Count; i++)
                topPositionById.Add(TopSymbolIds[i], i);
            Dictionary<int, int> bottomPositionById = new Dictionary<int, int>(BottomSymbolIds.Count);
            for (int i = 0; i < BottomSymbolIds.Count; i++)
                bottomPositionById.Add(BottomSymbolIds[i], i);
            VerticalCableSymbolsComparer comparer = new VerticalCableSymbolsComparer(topSymbols, bottomSymbols, bottomPositionById, topPositionById);
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
            private List<ISchemeSymbol> topGroups;
            private List<ISchemeSymbol> bottomGroups;

            private NaturalSortingStringComparer stringComparer;

            public VerticalCableSymbolsComparer(List<ISchemeSymbol> topSymbols, List<ISchemeSymbol> bottomSymbols, Dictionary<int, int> bottomPositionById, Dictionary<int, int> topPositionById)
            {
                this.optimalBottomPositionById = bottomPositionById;
                this.optimalTopPositionById = topPositionById;
                this.topGroups = topSymbols;
                this.bottomGroups = bottomSymbols;
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
