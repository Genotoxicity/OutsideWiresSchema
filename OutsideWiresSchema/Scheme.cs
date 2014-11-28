using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    class Scheme
    {
        private int connectionColorIndex = 16;
        private double radius = 1;
        private double gridStep;
        private double topGroupsOffset;
        private double bottomGroupsOffset;
        private double height;
        private double bottomGroupBottomMargin;
        private double topGroupBottomMargin;
        private List<VerticalConnection> verticalConnections;
        private Dictionary<int, double> topGroupHorizontalOffsetById;
        private Dictionary<int, double> bottomGroupHorizontalOffsetById;
        private Dictionary<int, DeviceGroup> deviceGroupById;
        private Dictionary<int, List<int>> symbolIdsByCableId;
        private Dictionary<int, int> optimalBottomPositionById;
        private Dictionary<int, int> optimalTopPositionById;
        private Dictionary<int, CableInfo> cableInfoById;
        private Dictionary<int, CableSymbol> cableSymbolById;

        public Size Size { get; private set; }

        public List<int> AllDeviceIds
        {
            get
            {
                List<int> ids = new List<int>();
                foreach (DeviceGroup deviceGroup in deviceGroupById.Values)
                        ids.AddRange(deviceGroup.DeviceSymbols.Select(ds => ds.Id));
                ids.AddRange(symbolIdsByCableId.Keys);
                return ids;
            }
        }

        public Scheme(Dictionary<int, DeviceGroup> deviceGroupById, Dictionary<int, List<int>> deviceGroupIdsByCableId, Dictionary<int, CableInfo> cableInfoById, E3Text text)
        {
            this.deviceGroupById = deviceGroupById;
            this.symbolIdsByCableId = deviceGroupIdsByCableId;
            gridStep = 4;
            height = 272 * 0.618;
            this.cableInfoById = cableInfoById;
            double minIntersectionCount = Int32.MaxValue;
            List<int> bottomSymbolIds = new List<int>();
            List<int> topSymbolIds = new List<int>();
            foreach (DeviceGroup deviceGroup in deviceGroupById.Values)
                if (deviceGroup.Level == Level.Top)
                    topSymbolIds.Add(deviceGroup.Id);
                else
                    bottomSymbolIds.Add(deviceGroup.Id);
            List<Tuple<int, int>> topToTopConnections, bottomToBottomConnections, topToBottomConnections;   // список соединений между групами, если группы на разных уровнях - первый член верхняя группа, второй - нижняя
            GetConnections(deviceGroupById, deviceGroupIdsByCableId, out topToTopConnections, out bottomToBottomConnections, out topToBottomConnections);
            Dictionary<int, Dictionary<int, int>> connectedBottomPositionByIdByTopId = GetConnectedPositionByIdByCenterId(Level.Top, topToBottomConnections);
            Dictionary<int, Dictionary<int, int>> connectedTopPositionByIdByBottomId = GetConnectedPositionByIdByCenterId(Level.Bottom, topToBottomConnections);
            int totalPermutationCount = 10000000;
            foreach (List<int> bottomPermutation in Permutate(bottomSymbolIds, bottomSymbolIds.Count))
            {
                Dictionary<int, int> bottomPositionById = new Dictionary<int, int>(bottomPermutation.Count);
                for (int i = 0; i < bottomPermutation.Count; i++)
                    bottomPositionById.Add(bottomPermutation[i], i);
                int bottomIntersectionCount = GetLevelIntersectionCount(bottomPositionById, bottomToBottomConnections);
                foreach (List<int> topPermutation in Permutate(topSymbolIds, topSymbolIds.Count))
                {

                    Dictionary<int, int> topPositionById = new Dictionary<int, int>(topPermutation.Count);
                    for (int i = 0; i < topPermutation.Count; i++)
                        topPositionById.Add(topPermutation[i], i);
                    int intersectionCount = bottomIntersectionCount;
                    intersectionCount += GetLevelIntersectionCount(topPositionById, topToTopConnections);
                    intersectionCount += GetInterLevelIntersectionCount(topToBottomConnections, topPositionById, bottomPositionById, connectedBottomPositionByIdByTopId, connectedTopPositionByIdByBottomId);
                    if (intersectionCount < minIntersectionCount)
                    {
                        minIntersectionCount = intersectionCount;
                        optimalTopPositionById = topPositionById;
                        optimalBottomPositionById = bottomPositionById;
                    }
                    totalPermutationCount--;
                    if (totalPermutationCount == 0)
                        break;
                }
                if (totalPermutationCount == 0)
                    break;
            }
            cableSymbolById = GetCableSymbolById(topToBottomConnections, text);
            CalculateLayout(text);
        }

        private Dictionary<int, Dictionary<int, int>> GetConnectedPositionByIdByCenterId(Level centerGroupLevel, List<Tuple<int, int>> topToBottomConnections)
        {
            IEnumerable<int> centerIds = topToBottomConnections.Select(c => (centerGroupLevel == Level.Top) ? c.Item1 : c.Item2).Distinct();
            NaturalSortingStringComparer stringComparer = new NaturalSortingStringComparer();
            Dictionary<int, Dictionary<int, int>> connectedPositionByIdByCenterId = new Dictionary<int, Dictionary<int, int>>();
            foreach (int centerId in centerIds)
            {
                DeviceGroup centerGroup = deviceGroupById[centerId];
                List<int> connectedIds = topToBottomConnections.Where(c => ((centerGroupLevel == Level.Top) ? c.Item1 : c.Item2) == centerId).Select(c => (centerGroupLevel == Level.Top) ? c.Item2 : c.Item1).ToList();
                int connectedCount = connectedIds.Count();
                if (connectedCount > 1)
                {
                    Dictionary<string, List<int>> groupIdsByPinName = new Dictionary<string, List<int>>(connectedCount);
                    foreach (int connectedId in connectedIds)
                    {
                        IEnumerable<int> commonCableIds = deviceGroupById[connectedId].CableIds.Intersect(centerGroup.CableIds);
                        IEnumerable<SymbolPin> bottomPins = centerGroup.DeviceSymbols.SelectMany(ds => ds.BottomPins);
                        IEnumerable<SymbolPin> topPins = centerGroup.DeviceSymbols.SelectMany(ds => ds.TopPins);
                        List<SymbolPin> pins = new List<SymbolPin>(bottomPins.Count() + topPins.Count());
                        pins.AddRange(bottomPins);
                        pins.AddRange(topPins);
                        List<string> commonPinNames = pins.Where(p => p.CableIds.Intersect(commonCableIds).Any()).Select(p => p.Name).ToList();
                        commonPinNames.Sort(stringComparer);
                        string leftPinName = commonPinNames.First();
                        if (!groupIdsByPinName.ContainsKey(leftPinName))
                            groupIdsByPinName.Add(leftPinName, new List<int>(){connectedId});
                        else
                            groupIdsByPinName[leftPinName].Add(connectedId);
                    }
                    if (groupIdsByPinName.Count > 1)
                    {
                        List<string> sortedLeftPinNames = groupIdsByPinName.Keys.ToList();
                        sortedLeftPinNames.Sort(stringComparer);
                        Dictionary<int, int> positionById = new Dictionary<int, int>(connectedCount);
                        for (int position = 0; position < sortedLeftPinNames.Count; position++)
                        {
                            string pinName = sortedLeftPinNames[position];
                            groupIdsByPinName[pinName].ForEach(id => positionById.Add(id, position));
                        }
                        connectedPositionByIdByCenterId.Add(centerId, positionById);
                    }
                }
            }
            return connectedPositionByIdByCenterId;
        }

        private static void GetConnections(Dictionary<int, DeviceGroup> deviceGroupById, Dictionary<int, List<int>> devoceGroupIdsIdsByCableId, out List<Tuple<int, int>> topToTopConnections, out List<Tuple<int, int>> bottomToBottomConnections, out List<Tuple<int, int>> topToBottomConnections)
        {
            topToTopConnections = new List<Tuple<int, int>>();
            bottomToBottomConnections = new List<Tuple<int, int>>();
            topToBottomConnections = new List<Tuple<int, int>>();
            foreach (List<int> symbolIds in devoceGroupIdsIdsByCableId.Values)
            {
                int firstId = symbolIds[0];
                int secondId = symbolIds[1];
                Level firstLevel = deviceGroupById[firstId].Level;
                Level secondLevel = deviceGroupById[secondId].Level;
                if (firstLevel == secondLevel)
                {
                    if (firstLevel == Level.Top)
                        topToTopConnections.Add(new Tuple<int, int>(firstId, secondId));
                    else
                        bottomToBottomConnections.Add(new Tuple<int, int>(firstId, secondId));
                }
                else
                {
                    if (firstLevel == Level.Top)
                        topToBottomConnections.Add(new Tuple<int, int>(firstId, secondId));
                    else
                        topToBottomConnections.Add(new Tuple<int, int>(secondId, firstId));
                }
            }
        }

        public static IEnumerable<List<int>> Permutate(List<int> sequence, int count)
        {
            if (count <= 1)
                yield return sequence;
            else
            {
                for (int i = 0; i < count; i++)
                {
                    foreach (var perm in Permutate(sequence, count - 1))
                        yield return perm;
                    int temp = sequence[count - 1];
                    sequence.RemoveAt(count - 1);
                    sequence.Insert(0, temp);
                }
            }
        }

        private static int GetLevelIntersectionCount(Dictionary<int, int> positionById, List<Tuple<int, int>> oneLevelConnections)
        {
            int totalIntersection = 0;
            oneLevelConnections.ForEach(olc=>totalIntersection += Math.Abs(positionById[olc.Item1] - positionById[olc.Item2]) - 1);
            return totalIntersection;
        }

        private static int GetInterLevelIntersectionCount(List<Tuple<int, int>> topToBottomConnections, Dictionary<int, int> topPositionById, Dictionary<int, int> bottomPositionById, Dictionary<int, Dictionary<int, int>> connectedBottomPositionByIdByTopId, Dictionary<int, Dictionary<int, int>> connectedTopPositionByIdByBottomId)
        {
            int totalIntersection = 0;
            int count = topToBottomConnections.Count;
            for (int i = 0; i < count - 1; i++)
            {
                Tuple<int, int> firstConnection = topToBottomConnections[i];
                int firstTopPosition = topPositionById[firstConnection.Item1];
                int firstBottomPosition = bottomPositionById[firstConnection.Item2];
                for (int j = i + 1; j < count; j++)
                {
                    Tuple<int, int> secondConnection = topToBottomConnections[j];
                    int secondTopPosition = topPositionById[secondConnection.Item1];
                    int secondBottomPosition = bottomPositionById[secondConnection.Item2];
                    if (firstTopPosition > secondTopPosition && firstBottomPosition < secondBottomPosition)
                        totalIntersection++;
                    if (firstTopPosition < secondTopPosition && firstBottomPosition > secondBottomPosition)
                        totalIntersection++;
                }
            }
            totalIntersection += GetConnectedGroupIntersectionCount(bottomPositionById, connectedBottomPositionByIdByTopId);
            totalIntersection += GetConnectedGroupIntersectionCount(topPositionById, connectedTopPositionByIdByBottomId);
            return totalIntersection;
        }

        private static int GetConnectedGroupIntersectionCount(Dictionary<int, int> positionById, Dictionary<int, Dictionary<int, int>> connectedPositionByIdByCenterId)
        {
            int intersection = 0;
            foreach (int centerId in connectedPositionByIdByCenterId.Keys)
            {
                Dictionary<int, int> neededPositionById = connectedPositionByIdByCenterId[centerId];
                List<int> ids = neededPositionById.Keys.ToList();
                int idsCount = ids.Count;
                for (int i = 0; i < idsCount - 1; i++)
                {
                    int firstId = ids[i];
                    int firstNeededPosition = neededPositionById[firstId];
                    int firstPosition = positionById[firstId];
                    for (int j = i + 1; j < idsCount; j++)
                    {
                        int secondId = ids[j];
                        int secondNeededPosition = neededPositionById[secondId];
                        int secondPosition = positionById[secondId];
                        if ((firstNeededPosition > secondNeededPosition && firstPosition < secondPosition) || (firstNeededPosition < secondNeededPosition && firstPosition > secondPosition))
                            intersection++;
                    }
                }
            }
            return intersection;
        }

        public int GetGroupPosition(int groupId)
        {
            return optimalTopPositionById.ContainsKey(groupId) ? optimalTopPositionById[groupId] : optimalBottomPositionById[groupId];
        }

        public Dictionary<int, int> GetMatePositionByCableId(int groupId)
        {
            Dictionary<int, int> matePositionByCableId = new Dictionary<int, int>(symbolIdsByCableId.Count);
            foreach (int cableId in symbolIdsByCableId.Keys)
            {
                List<int> groups = symbolIdsByCableId[cableId];
                int mateId = (groups[0] == groupId) ? groups[1] : groups[0];
                if (optimalBottomPositionById.ContainsKey(mateId))
                    matePositionByCableId.Add(cableId, optimalBottomPositionById[mateId]);
                else
                    matePositionByCableId.Add(cableId, optimalTopPositionById[mateId]);
            }
            return matePositionByCableId;
        }

        private Dictionary<int, CableSymbol> GetCableSymbolById(List<Tuple<int, int>> topToBottomConnections, E3Text text)
        {
            Dictionary<int, CableSymbol> cableSymbolById = new Dictionary<int, CableSymbol>(symbolIdsByCableId.Count);
            foreach (int cableId in symbolIdsByCableId.Keys)
            {
                Orientation orientation = GetCableOrientation(symbolIdsByCableId[cableId], topToBottomConnections);
                cableSymbolById.Add(cableId,new CableSymbol(cableInfoById[cableId], text, orientation));
            }
            return cableSymbolById;
        }

        private static Orientation GetCableOrientation(List<int> symbolIds, List<Tuple<int, int>> topToBottomConnections)
        {
            foreach (Tuple<int, int> connection in topToBottomConnections)
                if ((connection.Item1 == symbolIds[0] && connection.Item2 == symbolIds[1]) || (connection.Item1 == symbolIds[1] && connection.Item2 == symbolIds[0]))
                    return Orientation.Vertical;
            return Orientation.Horizontal;
        }

        public void CalculateLayout(E3Text text)
        {
            List<int> topGroupIds = new List<int>(optimalTopPositionById.Keys);
            topGroupIds.Sort((g1, g2) => optimalTopPositionById[g1].CompareTo(optimalTopPositionById[g2]));
            List<int> bottomGroupIds = new List<int>(optimalBottomPositionById.Keys);
            bottomGroupIds.Sort((g1, g2) => optimalBottomPositionById[g1].CompareTo(optimalBottomPositionById[g2]));
            List<DeviceGroup> topSymbols = new List<DeviceGroup>(topGroupIds.Count);
            topGroupIds.ForEach(id=>topSymbols.Add(deviceGroupById[id]));
            List<DeviceGroup> bottomSymbols = new List<DeviceGroup>(bottomGroupIds.Count);
            bottomGroupIds.ForEach(id=>bottomSymbols.Add(deviceGroupById[id]));
            double maxBottomGroupHeight = bottomSymbols.Max(g => g.OutlineHeight);
            bottomSymbols.ForEach(g => g.CalculateLayout(this, maxBottomGroupHeight, cableSymbolById, cableInfoById, text));
            List<CableSymbol> bottomHorizontalCableSymbols = GetGroupCableSymbols(bottomSymbols).FindAll(cs => cs.Orientation == Orientation.Horizontal);
            double maxBottomCableHeight = (bottomHorizontalCableSymbols.Count > 0) ? (bottomHorizontalCableSymbols.Max(cs => cs.Size.Height) + gridStep) : 0;
            bottomGroupBottomMargin = bottomSymbols.Max(g => g.BottomMargin);
            bottomGroupsOffset = maxBottomCableHeight + bottomGroupBottomMargin;
            double bottomHeight = bottomSymbols.Max(g => g.Height) + maxBottomCableHeight;
            if (topGroupIds.Count > 0)
            {
                double maxTopGroupHeight = topSymbols.Max(g => g.OutlineHeight);
                topSymbols.ForEach(g=>g.CalculateLayout(this, maxTopGroupHeight, cableSymbolById, cableInfoById, text));
                topGroupBottomMargin = topSymbols.Max(g => g.BottomMargin);
                topGroupsOffset = height - maxTopGroupHeight;
                AdjustVerticalConnections(topSymbols, bottomSymbols);
                double topWidth = topGroupHorizontalOffsetById.Values.Sum() + topSymbols.Last().RightMargin;
                double bottomWidth = bottomGroupHorizontalOffsetById.Values.Sum() + bottomSymbols.Last().RightMargin;
                double totalWidth = Math.Max(topWidth, bottomWidth);
                double totalHeight = height;
                Size = new Size(totalWidth, totalHeight);
            }
            else
            {
                topGroupHorizontalOffsetById = new Dictionary<int, double>(0);
                bottomGroupHorizontalOffsetById = GetMinHorizontalOffsetsBetweenGroups(bottomSymbols);
                double bottomWidth = bottomGroupHorizontalOffsetById.Values.Sum() + bottomSymbols.Last().RightMargin;
                Size = new Size(bottomWidth, bottomHeight);
            }
        }

        private void AdjustVerticalConnections(List<DeviceGroup> topSymbols, List<DeviceGroup> bottomSymbols)
        {
            Dictionary<int, double> minTopGroupHorizontalOffsetById = GetMinHorizontalOffsetsBetweenGroups(topSymbols);
            Dictionary<int, double> minBottomGroupHorizontalOffsetById = GetMinHorizontalOffsetsBetweenGroups(bottomSymbols);
            topGroupHorizontalOffsetById = new Dictionary<int, double>(minTopGroupHorizontalOffsetById);
            bottomGroupHorizontalOffsetById = new Dictionary<int, double>(minBottomGroupHorizontalOffsetById);
            verticalConnections = GetVerticalConnections(topSymbols, bottomSymbols);
            if (verticalConnections.Count > 0)
            {
                MoveTopGroupCentersToBottomGroupCenters();
                IEnumerable<int> topGroupIds = topSymbols.Select(g => g.Id);
                IEnumerable<int> bottomGroupIds = bottomSymbols.Select(g => g.Id);
                MoveStartNotConnectedTopGroupsToConnectedTopGroups(topGroupIds, minTopGroupHorizontalOffsetById);
                if (verticalConnections.Count > 1)
                {
                    for (int i = 1; i < verticalConnections.Count; i++)
                    {
                        int firstConnectionLastTopGroupId = verticalConnections[i - 1].TopSymbolIds.Last();
                        int secondConnectionFirstTopGroupId = verticalConnections[i].TopSymbolIds.First();
                        double firstConnectionLastTopGroupMinOffset = GetOffsetBeforeGroup(firstConnectionLastTopGroupId, minTopGroupHorizontalOffsetById);
                        double secondConnectionFirstTopGroupMinOffset = GetOffsetBeforeGroup(secondConnectionFirstTopGroupId, minTopGroupHorizontalOffsetById);
                        double firstConnectionLastTopGroupOffset = GetOffsetBeforeGroup(firstConnectionLastTopGroupId, topGroupHorizontalOffsetById);
                        double secondConnectionFirstTopGroupOffset = GetOffsetBeforeGroup(secondConnectionFirstTopGroupId, topGroupHorizontalOffsetById);
                        double minDistance = secondConnectionFirstTopGroupMinOffset - firstConnectionLastTopGroupMinOffset;
                        double distance = secondConnectionFirstTopGroupOffset - firstConnectionLastTopGroupOffset;
                        double distanceDifference = minDistance - distance;
                        if (distanceDifference > 0)
                        {
                            topGroupHorizontalOffsetById[secondConnectionFirstTopGroupId] += distanceDifference;
                            int firstConnectionLastBottomGroupId = verticalConnections[i - 1].BottomSymbolIds.Last();
                            int secondConnectionFirstBottomGroupId = verticalConnections[i].BottomSymbolIds.First();
                            MoveGroupsOffsets(bottomGroupIds, bottomGroupHorizontalOffsetById, firstConnectionLastBottomGroupId, secondConnectionFirstBottomGroupId,distanceDifference);
                        }
                        if (distanceDifference < 0)
                            MoveOffsetsBetweenGroups(topGroupIds, topGroupHorizontalOffsetById, firstConnectionLastTopGroupId, secondConnectionFirstTopGroupId, Math.Abs(distanceDifference));
                    }
                }
                AdjustVerticalConnectionCables(topGroupIds, bottomGroupIds);
            }
        }

        private void AdjustVerticalConnectionCables(IEnumerable<int> topGroupIds, IEnumerable<int> bottomGroupIds)
        {
            if (verticalConnections.Count > 1)
            {
                int lastCalculatedIndex = -1;
                for (int i = 1; i < verticalConnections.Count; i++)
                {
                    VerticalConnection leftConnection = verticalConnections[i - 1];
                    double leftGroupsWidth, leftOffset;
                    GetVerticalConnectionGroupsWidthAndOffset(leftConnection, out leftGroupsWidth, out leftOffset);
                    double leftDifference = leftConnection.MinCablesWidth - leftGroupsWidth;
                    double leftRightBorderOfsset = (leftDifference <= 0) ? (leftOffset + leftGroupsWidth) : (leftOffset + leftGroupsWidth + leftDifference / 2);

                    VerticalConnection rightConnection = verticalConnections[i];
                    double rightGroupsWidth, rightOffset;
                    GetVerticalConnectionGroupsWidthAndOffset(rightConnection, out rightGroupsWidth, out rightOffset);
                    double rightDifference = rightConnection.MinCablesWidth - rightGroupsWidth;
                    double rightLeftBorderOfsset = (rightDifference <= 0) ? rightOffset : (rightOffset - rightDifference / 2);

                    double difference = rightLeftBorderOfsset - leftRightBorderOfsset;
                    if (difference < gridStep)
                    {
                        double distanceDifference = gridStep - difference;
                        rightOffset += distanceDifference;
                        MoveGroupsOffsets(topGroupIds, topGroupHorizontalOffsetById, leftConnection.TopSymbolIds.Last(), rightConnection.TopSymbolIds.First(), distanceDifference);
                        MoveGroupsOffsets(bottomGroupIds, bottomGroupHorizontalOffsetById, leftConnection.BottomSymbolIds.Last(), rightConnection.BottomSymbolIds.First(), distanceDifference);
                    }
                    if (lastCalculatedIndex != i - 1)
                    {
                        leftConnection.CalculateCableLayout(leftOffset, leftGroupsWidth);
                        lastCalculatedIndex = i - 1;
                    }
                    if (lastCalculatedIndex != i)
                    {
                        rightConnection.CalculateCableLayout(rightOffset, rightGroupsWidth);
                        lastCalculatedIndex = i;
                    }
                }
            }
            else
            {
                VerticalConnection connection = verticalConnections.First();
                double groupsWidth, offset;
                GetVerticalConnectionGroupsWidthAndOffset(connection, out groupsWidth, out offset);
                connection.CalculateCableLayout(offset, groupsWidth);
            }
        }

        private void MoveGroupsOffsets(IEnumerable<int> groupIds, Dictionary<int, double> horizontalOffsetById, int firstGroupId, int secondGroupId, double difference)
        {
            MoveOffsetsBetweenGroups(groupIds, horizontalOffsetById, firstGroupId, secondGroupId, difference);
            horizontalOffsetById[secondGroupId] += difference;
        }

        private void MoveOffsetsBetweenGroups(IEnumerable<int> groupIds, Dictionary<int, double> horizontalOffsetById, int firstGroupId, int secondGroupId, double difference)
        {
            List<int> betweenGroupIds = GetBetweenGroupIds(groupIds, firstGroupId, secondGroupId);
            int betweenCount = betweenGroupIds.Count;
            if (betweenCount > 0)
            {
                double additionalOffset = difference / (betweenGroupIds.Count + 1);
                betweenGroupIds.ForEach(id=>topGroupHorizontalOffsetById[id] += additionalOffset);
                horizontalOffsetById[secondGroupId] -= additionalOffset * betweenCount;
            }
        }

        private void GetVerticalConnectionGroupsWidthAndOffset(VerticalConnection connection, out double width, out double offset)
        {
            double topWidth, bottomWidth;
            double firstTopGroupOffset = GetOffsetBeforeGroup(connection.TopSymbolIds.First(), topGroupHorizontalOffsetById);
            if (connection.TopSymbolIds.First() != connection.TopSymbolIds.Last())
            {
                double lastTopGroupOffset = GetOffsetBeforeGroup(connection.TopSymbolIds.Last(), topGroupHorizontalOffsetById);
                topWidth = lastTopGroupOffset - firstTopGroupOffset + connection.TopSymbols.First().LeftMargin + connection.TopSymbols.Last().RightMargin;
            }
            else
                topWidth = connection.TopSymbols.First().LeftMargin + connection.TopSymbols.Last().RightMargin;
            double firstBottomGroupOffset = GetOffsetBeforeGroup(connection.BottomSymbolIds.First(), bottomGroupHorizontalOffsetById);
            if (connection.BottomSymbolIds.First() != connection.BottomSymbolIds.Last())
            {
                double lastBottomGroupOffset = GetOffsetBeforeGroup(connection.BottomSymbolIds.Last(), bottomGroupHorizontalOffsetById);
                bottomWidth = lastBottomGroupOffset - firstBottomGroupOffset + connection.BottomSymbols.First().LeftMargin + connection.BottomSymbols.Last().RightMargin;
            }
            else
                bottomWidth = connection.BottomSymbols.First().LeftMargin + connection.BottomSymbols.Last().RightMargin;
            width = Math.Max(topWidth, bottomWidth);
            offset = Math.Min(firstTopGroupOffset, firstBottomGroupOffset);
        }

        private void MoveStartNotConnectedTopGroupsToConnectedTopGroups(IEnumerable<int> topGroupIds, Dictionary<int, double> minTopGroupHorizontalOffsetById)
        {
            VerticalConnection firstVerticalConnection = verticalConnections.First();
            int firstMovedTopGroupId = firstVerticalConnection.TopSymbolIds.First();
            if (firstMovedTopGroupId != topGroupIds.First())
            {
                double minOffset = minTopGroupHorizontalOffsetById[firstMovedTopGroupId];
                double offset = topGroupHorizontalOffsetById[firstMovedTopGroupId];
                double offsetDifference = offset - minOffset;
                topGroupHorizontalOffsetById[topGroupIds.First()] += offsetDifference;
                topGroupHorizontalOffsetById[firstMovedTopGroupId] = minOffset;
            }
        }

        private void MoveTopGroupCentersToBottomGroupCenters()
        {
            foreach (VerticalConnection verticalConnection in verticalConnections)
            {
                List<int> connectionBottomGroupIds = verticalConnection.BottomSymbolIds;
                int firstConnectionBottomGroupId = connectionBottomGroupIds.First();
                int lastConnectionBottomGroupId = connectionBottomGroupIds.Last();
                double offsetBeforeFirstConnectionBottomGroup = GetOffsetBeforeGroup(firstConnectionBottomGroupId, bottomGroupHorizontalOffsetById);
                double connectionBottomGroupWidth;
                if (firstConnectionBottomGroupId == lastConnectionBottomGroupId)
                    connectionBottomGroupWidth = deviceGroupById[firstConnectionBottomGroupId].OutlineWidth;
                else
                    connectionBottomGroupWidth = GetOffsetBeforeGroup(lastConnectionBottomGroupId, bottomGroupHorizontalOffsetById) + deviceGroupById[lastConnectionBottomGroupId].OutlineWidth - offsetBeforeFirstConnectionBottomGroup;
                double connectionBottomGroupCenteroffset = offsetBeforeFirstConnectionBottomGroup + connectionBottomGroupWidth / 2;
                List<int> connectionTopGroupIds = verticalConnection.TopSymbolIds;
                int firstConnectionTopGroupId = connectionTopGroupIds.First();
                int lastConnectionTopGroupId = connectionTopGroupIds.Last();
                double offsetBeforeFirstConnectionTopGroup = GetOffsetBeforeGroup(firstConnectionTopGroupId, topGroupHorizontalOffsetById);
                double connectionTopGroupWidth;
                if (firstConnectionTopGroupId == lastConnectionTopGroupId)
                    connectionTopGroupWidth = deviceGroupById[firstConnectionTopGroupId].OutlineWidth;
                else
                    connectionTopGroupWidth = GetOffsetBeforeGroup(lastConnectionTopGroupId, topGroupHorizontalOffsetById) + deviceGroupById[lastConnectionTopGroupId].OutlineWidth - offsetBeforeFirstConnectionTopGroup;
                double connectionTopGroupCenteroffset = offsetBeforeFirstConnectionTopGroup + connectionTopGroupWidth / 2;
                topGroupHorizontalOffsetById[firstConnectionTopGroupId] += (connectionBottomGroupCenteroffset - connectionTopGroupCenteroffset);
            }
        }

        private static List<int> GetBetweenGroupIds(IEnumerable<int> groupIds, int firstGroupId, int secondGroupId)
        {
            List<int> betweenGroupIds = new List<int>();
            int previousId = -1;
            bool isBetween = false;
            foreach (int groupId in groupIds)
            {
                if (previousId == firstGroupId)
                    isBetween = true;
                if (groupId == secondGroupId)
                    break;
                if (isBetween)
                    betweenGroupIds.Add(groupId);
                previousId = groupId;
            }
            return betweenGroupIds;
        }

        private List<CableSymbol> GetGroupCableSymbols(List<DeviceGroup> symbols)
        {
            List<int> cableIds = new List<int>();
            symbols.ForEach(g=>cableIds.AddRange(g.CableIds));
            cableIds = cableIds.Distinct().ToList();
            List<CableSymbol> cableSymbols = new List<CableSymbol>(cableIds.Count);
            cableIds.ForEach(cableId=>cableSymbols.Add(cableSymbolById[cableId]));
            return cableSymbols;
        }

        private static double GetOffsetBeforeGroup(int firstGroupId, Dictionary<int, double> horizontalOffsetById)
        {
            double offsetBeforeFirstGroup = 0;
            foreach (int groupId in horizontalOffsetById.Keys)
            {
                offsetBeforeFirstGroup += horizontalOffsetById[groupId];
                if (groupId == firstGroupId)
                    break;
            }
            return offsetBeforeFirstGroup;
        }

        private List<VerticalConnection> GetVerticalConnections(List<DeviceGroup> topSymbols, List<DeviceGroup> bottomSymbols)
        {
            List<VerticalConnection> verticalConnections = new List<VerticalConnection>();
            foreach (DeviceGroup topGroup in topSymbols)
            {
                List<int> mateBottomGroupId = bottomSymbols.FindAll(g => g.CableIds.Intersect(topGroup.CableIds).Any()).Select(bg=>bg.Id).ToList();
                if (mateBottomGroupId.Count == 0)
                    continue;
                if (verticalConnections.Count == 0)
                    verticalConnections.Add(new VerticalConnection(new List<int>() { topGroup.Id }, mateBottomGroupId, gridStep));
                else
                {
                    VerticalConnection lastVerticalConnection = verticalConnections.Last();
                    if (lastVerticalConnection.BottomSymbolIds.SequenceEqual(mateBottomGroupId))
                        lastVerticalConnection.TopSymbolIds.Add(topGroup.Id);
                    else
                        verticalConnections.Add(new VerticalConnection(new List<int>() { topGroup.Id }, mateBottomGroupId, gridStep));
                }
            }
            verticalConnections.ForEach(vc => vc.SetSymbols(deviceGroupById));
            verticalConnections.ForEach(vc => vc.SetVerticalCableSymbols(cableSymbolById));
            return verticalConnections;
        }

        private Dictionary<int, double> GetMinHorizontalOffsetsBetweenGroups(List<DeviceGroup> symbols)
        {
            int groupCount = symbols.Count;
            List<IEnumerable<int>> symbolConnectingCables = new List<IEnumerable<int>>(groupCount - 1); // кабели между групами, по порядку
            Dictionary<int, double>  horizontalOffsetById = new Dictionary<int, double>(groupCount);
            horizontalOffsetById.Add(symbols.First().Id, symbols.First().LeftMargin);
            if (groupCount > 1)
            {
                for (int i = 1; i < groupCount; i++)
                {
                    DeviceGroup leftSymbol = symbols[i - 1];
                    DeviceGroup rightSymbol = symbols[i];
                    symbolConnectingCables.Add(leftSymbol.CableIds.Intersect(rightSymbol.CableIds));
                    double offset = leftSymbol.RightMargin + rightSymbol.LeftMargin + gridStep;
                    foreach (int cableId in symbolConnectingCables[i - 1])
                    {
                        double cableOffset = cableSymbolById[cableId].Size.Width + leftSymbol.RightMargin + rightSymbol.LeftMargin + gridStep * 2;
                        offset = Math.Max(cableOffset, offset);
                    }
                    horizontalOffsetById.Add(symbols[i].Id, offset);
                }
            }
            return horizontalOffsetById;
        }

        public void Place(Sheet sheet, Graphic graphic, Group e3group, E3Text text, Point placePosition)
        {
            double startAbsciss = sheet.MoveLeft(placePosition.X, Size.Width / 2);
            double startOrdinate = sheet.MoveDown(placePosition.Y, Size.Height / 2);
            double bottomGroupOrdinate = sheet.MoveUp(startOrdinate, bottomGroupsOffset);
            double topGroupOrdinate = sheet.MoveUp(startOrdinate, topGroupsOffset);
            PlaceSymbols(sheet, graphic, e3group, text, topGroupHorizontalOffsetById, startAbsciss, topGroupOrdinate);
            PlaceSymbols(sheet, graphic, e3group, text, bottomGroupHorizontalOffsetById, startAbsciss, bottomGroupOrdinate);
            PlaceCableSymbols(sheet, graphic, e3group, text, startAbsciss, topGroupOrdinate, bottomGroupOrdinate);
            PlaceConnectionLines(sheet, graphic, topGroupOrdinate, bottomGroupOrdinate);
        }

        private void PlaceSymbols(Sheet sheet, Graphic graphic, Group e3group, E3Text text, Dictionary<int, double> horizontalOffsetById, double startAbsciss, double groupOrdinate)
        {
            double offset = 0;
            foreach (int id in horizontalOffsetById.Keys)
            {
                DeviceGroup symbol = deviceGroupById[id];
                offset += horizontalOffsetById[id];
                double absciss = sheet.MoveRight(startAbsciss, offset);
                symbol.Place(new Point(absciss, groupOrdinate), sheet, graphic, e3group, text);
            }
        }

        private void PlaceCableSymbols(Sheet sheet, Graphic graphic, Group e3group, E3Text text, double startAbsciss, double topGroupOrdinate, double bottomGroupOrdinate)
        { 
            foreach (int cableId in cableSymbolById.Keys)
            {
                CableSymbol cableSymbol = cableSymbolById[cableId];
                if (cableSymbol.Orientation == Orientation.Horizontal)
                {
                    List<int> groupIds = symbolIdsByCableId[cableId];
                    groupIds.Sort((id1,id2) => GetGroupPosition(id1).CompareTo(GetGroupPosition(id2)));
                    DeviceGroup firstSymbol = deviceGroupById[groupIds.First()];
                    DeviceGroup secondSymbol = deviceGroupById[groupIds.Last()];
                    double firstPointX = sheet.MoveRight(firstSymbol.PlacedPosition.X, firstSymbol.RightMargin);
                    double y = (firstSymbol.Level == Level.Bottom) ? sheet.MoveDown(bottomGroupOrdinate, bottomGroupBottomMargin) : sheet.MoveDown (topGroupOrdinate, topGroupBottomMargin);
                    Point firstPoint = new Point (firstPointX, y);
                    double secondPointX = sheet.MoveLeft(secondSymbol.PlacedPosition.X, secondSymbol.LeftMargin);
                    Point secondPoint = new Point(secondPointX, y);
                    double x = (firstPoint.X + secondPoint.X) / 2;
                    double cableSymbolHalfHeight = cableSymbol.Size.Height / 2;
                    y = sheet.MoveDown(y, cableSymbolHalfHeight + gridStep);
                    cableSymbol.Place(sheet, graphic, text, e3group, new Point(x, y));
                 }
            }
            if (topGroupHorizontalOffsetById.Count > 0)
            {
                double top = deviceGroupById[topGroupHorizontalOffsetById.Keys.First()].PlacedPosition.Y;
                DeviceGroup bottomSymbol = deviceGroupById[bottomGroupHorizontalOffsetById.Keys.First()];
                double bottom = sheet.MoveUp(bottomSymbol.PlacedPosition.Y, bottomSymbol.TopMargin);
                double center = (top + bottom) / 2;
                foreach (VerticalConnection verticalConnection in verticalConnections)
                    foreach (CableSymbol cableSymbol in verticalConnection.VerticalCableSymbols)
                    {
                        double y = sheet.MoveDown(center, (cableSymbol.Size.Height - cableSymbol.Diameter) / 2);
                        cableSymbol.Place(sheet, graphic, text, e3group, new Point(sheet.MoveRight(startAbsciss, verticalConnection.OffsetByCableId[cableSymbol.CableId]), y));
                    }
            }
        }

        private void PlaceConnectionLines(Sheet sheet, Graphic graphic, double topGroupOrdinate, double bottomGroupOrdinate)
        {
            double lineHeight = 0.2;
            int sheetId = sheet.Id;
            foreach (DeviceGroup symbol in deviceGroupById.Values)
            {
                List<CableLayout> bottomLayouts = symbol.CableLayoutById.Values.Where(cl => cl.Level == Level.Bottom).ToList();
                int bottomLayoutsCount = bottomLayouts.Count();
                if (bottomLayoutsCount > 0)
                {
                    double lastCableLayoutY = (symbol.Level == Level.Bottom) ? sheet.MoveDown(bottomGroupOrdinate, bottomGroupBottomMargin) : sheet.MoveDown(topGroupOrdinate, topGroupBottomMargin);
                    List<CableSymbol> bottomSymbols = bottomLayouts.Select(bl => cableSymbolById[bl.Id]).ToList();
                    bottomSymbols.Sort((bs1, bs2) => bs1.PlacedPosition.X.CompareTo(bs2.PlacedPosition.X));
                    List<List<double>> connectionAbscisses = new List<List<double>>(bottomLayoutsCount);
                    foreach (CableSymbol cableSymbol in bottomSymbols)
                        connectionAbscisses.Add(GetAbscisses(cableSymbol.PlacedPosition.X, symbol.CableLayoutById[cableSymbol.CableId]));
                    List<int> maxPositions = new List<int>(bottomLayoutsCount);
                    connectionAbscisses.ForEach(l => maxPositions.Add(l.Count));
                    Dictionary<int, double> abscissesByCableId = GetAbscissesByCableId(maxPositions, connectionAbscisses, bottomSymbols, bottomLayouts, symbol);
                    foreach (int cableId in abscissesByCableId.Keys)
                    {
                        double absciss = abscissesByCableId[cableId];
                        CableLayout cableLayout = symbol.CableLayoutById[cableId];
                        double cableLayoutY = cableLayout.PlacedPoints.First().Y;
                        if (cableLayout.StartOffsets.Count > 1)
                            graphic.CreateArc(sheetId, absciss, cableLayoutY, radius, 180, 0, lineHeight, connectionColorIndex);
                    }
                    IEnumerable<CableSymbol> bottomHorizontalSymbols = bottomSymbols.Where(bs => bs.Orientation == Orientation.Horizontal);
                    int horizontalCount = bottomHorizontalSymbols.Count();
                    if (horizontalCount == 1)
                    {
                        CableSymbol cableSymbol = bottomHorizontalSymbols.First();
                        int cableId = cableSymbol.CableId;
                        CableLayout cableLayout = symbol.CableLayoutById[cableId];
                        double firstLayoutPlacedY = cableLayout.PlacedPoints.First().Y;
                        if (cableLayout.StartOffsets.Count > 1)
                            firstLayoutPlacedY = sheet.MoveDown(firstLayoutPlacedY, radius);
                        CreateStraightConnectingLine(sheet, graphic, lineHeight, sheetId, abscissesByCableId, firstLayoutPlacedY, cableSymbol);
                    }
                    if (horizontalCount == 2)
                    {
                        CableSymbol firstCableSymbol = bottomHorizontalSymbols.First();
                        int firstCableId = firstCableSymbol.CableId;
                        double firstAbsciss = abscissesByCableId[firstCableId];
                        CableLayout firstCableLayout = symbol.CableLayoutById[firstCableId];
                        double firstLayoutPlacedY = firstCableLayout.PlacedPoints.First().Y;
                        if (firstCableLayout.StartOffsets.Count > 1)
                            firstLayoutPlacedY = sheet.MoveDown(firstLayoutPlacedY, radius);
                        CableSymbol secondCableSymbol = bottomHorizontalSymbols.Last();
                        int secondCableId = secondCableSymbol.CableId;
                        double secondAbsciss = abscissesByCableId[secondCableId];
                        CableLayout secondCableLayout = symbol.CableLayoutById[secondCableId];
                        double secondLayoutPlacedY = secondCableLayout.PlacedPoints.First().Y;
                        if (secondCableLayout.StartOffsets.Count > 1)
                            secondLayoutPlacedY = sheet.MoveDown(secondLayoutPlacedY, radius);
                        if ((firstCableSymbol.PlacedPosition.X > secondCableSymbol.PlacedPosition.X && firstAbsciss > secondAbsciss) || (firstCableSymbol.PlacedPosition.X < secondCableSymbol.PlacedPosition.X && firstAbsciss < secondAbsciss))
                        {
                            CreateStraightConnectingLine(sheet, graphic, lineHeight, sheetId, abscissesByCableId, firstLayoutPlacedY, firstCableSymbol);
                            CreateStraightConnectingLine(sheet, graphic, lineHeight, sheetId, abscissesByCableId, secondLayoutPlacedY, secondCableSymbol);
                        }
                        else
                        {
                            CableSymbol straightSymbol, turningSymbol;
                            double straightLayouPlacedY, turningLayoutPlacedY;
                            if (firstCableSymbol.PlacedPosition.X < secondCableSymbol.PlacedPosition.X)
                            {
                                straightSymbol = firstCableSymbol;
                                turningSymbol = secondCableSymbol;
                                straightLayouPlacedY = firstLayoutPlacedY;
                                turningLayoutPlacedY = secondLayoutPlacedY;
                            }
                            else
                            {
                                straightSymbol = secondCableSymbol;
                                turningSymbol = firstCableSymbol;
                                straightLayouPlacedY = secondLayoutPlacedY;
                                turningLayoutPlacedY = firstLayoutPlacedY;
                            }
                            CreateStraightConnectingLine(sheet, graphic, lineHeight, sheetId, abscissesByCableId, straightLayouPlacedY, straightSymbol);
                            CreateTurningConnectingLine(sheet, graphic, lineHeight, sheetId, abscissesByCableId, turningLayoutPlacedY, turningSymbol, symbol);
                        }
                    }
                    IEnumerable<CableSymbol> bottomVerticalSymbols = bottomSymbols.Where(bs => bs.Orientation == Orientation.Vertical);
                    if (bottomVerticalSymbols.Count() > 0)
                    {
                        double centerX = (bottomVerticalSymbols.Min(bs => bs.PlacedPosition.X) + bottomVerticalSymbols.Max(bs => bs.PlacedPosition.X)) / 2;
                        List<CableSymbol> leftSymbols = bottomVerticalSymbols.Where(ls => ls.PlacedPosition.X < abscissesByCableId[ls.CableId]).ToList();
                        List<CableSymbol> rightSymbols = bottomVerticalSymbols.Where(rs => rs.PlacedPosition.X > abscissesByCableId[rs.CableId]).ToList();
                        CableSymbol cableSymbol = bottomVerticalSymbols.First();
                        double symbolY = sheet.MoveUp(cableSymbol.PlacedPosition.Y, cableSymbol.Size.Height / 2);
                        double centerY = (lastCableLayoutY + symbolY) / 2;
                        double leftTurnY = sheet.MoveDown(centerY, (leftSymbols.Count - 1) * gridStep / 2);
                        double rightTurnY = sheet.MoveDown(centerY, (rightSymbols.Count - 1) * gridStep / 2);
                        PlaceSideSymbolsConnectionLine(symbol, sheet, graphic, lineHeight, sheetId, abscissesByCableId, centerX, leftSymbols, leftTurnY, Level.Top);
                        PlaceSideSymbolsConnectionLine(symbol, sheet, graphic, lineHeight, sheetId, abscissesByCableId, centerX, rightSymbols, rightTurnY, Level.Top);
                    }
                }
                List<CableLayout> topLayouts = symbol.CableLayoutById.Values.Where(cl => cl.Level == Level.Top).ToList();
                int topLayoutsCount = topLayouts.Count();
                if (topLayoutsCount > 0)
                {
                    double lastCableLayoutY = topLayouts.Where(tl=>tl.verticalOffset == topLayouts.Max(t => t.verticalOffset)).First().PlacedPoints.First().Y;
                    List<CableSymbol> topSymbols = topLayouts.Select(tl => cableSymbolById[tl.Id]).ToList();
                    topSymbols.Sort((ts1, ts2) => ts1.PlacedPosition.X.CompareTo(ts2.PlacedPosition.X));
                    List<List<double>> connectionAbscisses = new List<List<double>>(bottomLayoutsCount);
                    foreach (CableSymbol cableSymbol in topSymbols)
                        connectionAbscisses.Add(GetAbscisses(cableSymbol.PlacedPosition.X, symbol.CableLayoutById[cableSymbol.CableId]));
                    List<int> maxPositions = new List<int>(topLayoutsCount);
                    connectionAbscisses.ForEach(l => maxPositions.Add(l.Count));
                    Dictionary<int, double> abscissesByCableId = GetAbscissesByCableId(maxPositions, connectionAbscisses, topSymbols, topLayouts, symbol);
                    foreach (int cableId in abscissesByCableId.Keys)
                    {
                        double absciss = abscissesByCableId[cableId];
                        CableLayout cableLayout = symbol.CableLayoutById[cableId];
                        double cableLayoutY = cableLayout.PlacedPoints.First().Y;
                        if (cableLayout.StartOffsets.Count > 1)
                            graphic.CreateArc(sheetId, absciss, cableLayoutY, radius, 0, 180, lineHeight, connectionColorIndex);
                    }
                    double centerX = (topSymbols.Min(ts => ts.PlacedPosition.X) + topSymbols.Max(ts => ts.PlacedPosition.X)) / 2;
                    List<CableSymbol> leftSymbols = topSymbols.Where(ts => ts.PlacedPosition.X <= abscissesByCableId[ts.CableId]).ToList();
                    List<CableSymbol> rightSymbols = topSymbols.Where(ts => ts.PlacedPosition.X > abscissesByCableId[ts.CableId]).ToList();
                    PlaceSideSymbolsConnectionLine(symbol, sheet, graphic, lineHeight, sheetId, abscissesByCableId, centerX, leftSymbols, lastCableLayoutY, Level.Bottom);
                    PlaceSideSymbolsConnectionLine(symbol, sheet, graphic, lineHeight, sheetId, abscissesByCableId, centerX, rightSymbols, lastCableLayoutY, Level.Bottom);
                }
            }
        }

        private void PlaceSideSymbolsConnectionLine(DeviceGroup symbol, Sheet sheet, Graphic graphic, double lineHeight, int sheetId, Dictionary<int, double> abscissesByCableId, double centerX, List<CableSymbol> cableSymbols, double turnY, Level level)
        {
            int symbolsCount = cableSymbols.Count;
            if (symbolsCount > 0)
            {
                cableSymbols.Sort((bs1, bs2) => Math.Abs(bs1.PlacedPosition.X - centerX).CompareTo(Math.Abs(bs2.PlacedPosition.X - centerX))); // сортировка по удаленности от центра
                foreach (CableSymbol cableSymbol in cableSymbols)
                {
                    CableLayout cableLayout = symbol.CableLayoutById[cableSymbol.CableId];
                    double layoutY = cableLayout.PlacedPoints.First().Y;
                    if (cableLayout.StartOffsets.Count > 1)
                        layoutY = (level == Level.Top) ? sheet.MoveDown(layoutY, radius) : sheet.MoveUp(layoutY, radius);
                    double absciss = abscissesByCableId[cableSymbol.CableId];
                    double symbolX = cableSymbol.PlacedPosition.X;
                    double symbolY = (level == Level.Top) ? sheet.MoveUp(cableSymbol.PlacedPosition.Y, cableSymbol.Size.Height / 2) : sheet.MoveDown(cableSymbol.PlacedPosition.Y, cableSymbol.Size.Height / 2);
                    if (absciss == symbolX)
                        graphic.CreateLine(sheetId, absciss, layoutY, absciss, symbolY, lineHeight, connectionColorIndex);
                    else
                    {
                        graphic.CreateLine(sheetId, absciss, layoutY, absciss, turnY, lineHeight, connectionColorIndex);
                        graphic.CreateLine(sheetId, absciss, turnY, symbolX, turnY, lineHeight, connectionColorIndex);
                        graphic.CreateLine(sheetId, symbolX, turnY, symbolX, symbolY, lineHeight, connectionColorIndex);
                    }
                    turnY = sheet.MoveUp(turnY, gridStep);
                }
            }
        }

        private void CreateStraightConnectingLine(Sheet sheet, Graphic graphic, double lineHeight, int sheetId, Dictionary<int, double> abscissesByCableId, double layoutPlacedY, CableSymbol cableSymbol)
        {
            double absciss = abscissesByCableId[cableSymbol.CableId];
            double symbolX = (absciss > cableSymbol.PlacedPosition.X) ? sheet.MoveRight(cableSymbol.PlacedPosition.X, cableSymbol.Size.Width / 2) : sheet.MoveLeft(cableSymbol.PlacedPosition.X, cableSymbol.Size.Width / 2);
            double symbolY = cableSymbol.PlacedPosition.Y;
            graphic.CreateLine(sheetId, absciss, layoutPlacedY, absciss, symbolY, lineHeight, connectionColorIndex);
            graphic.CreateLine(sheetId, symbolX, symbolY, absciss, symbolY, lineHeight, connectionColorIndex);
        }

        private void CreateTurningConnectingLine(Sheet sheet, Graphic graphic, double lineHeight, int sheetId, Dictionary<int, double> abscissesByCableId, double lastCableLayoutY, CableSymbol cableSymbol, DeviceGroup symbol)
        {
            double absciss = abscissesByCableId[cableSymbol.CableId];
            double symbolX, turnX;
            if (absciss > cableSymbol.PlacedPosition.X)
            {
                symbolX = sheet.MoveRight(cableSymbol.PlacedPosition.X, cableSymbol.Size.Width / 2);
                turnX = sheet.MoveLeft(symbol.PlacedPosition.X, symbol.LeftMargin + gridStep);
            }
            else
            {
                symbolX = sheet.MoveLeft(cableSymbol.PlacedPosition.X, cableSymbol.Size.Width / 2);
                turnX = sheet.MoveRight(symbol.PlacedPosition.X, symbol.RightMargin + gridStep);
            }
            double symbolY = cableSymbol.PlacedPosition.Y;
            graphic.CreateLine(sheetId, absciss, lastCableLayoutY, turnX, lastCableLayoutY, lineHeight, connectionColorIndex);
            graphic.CreateLine(sheetId, turnX, lastCableLayoutY, turnX, symbolY, lineHeight, connectionColorIndex);
            graphic.CreateLine(sheetId, turnX, symbolY, symbolX, symbolY, lineHeight, connectionColorIndex);
        }

        private static List<double> GetAbscisses(double symbolPlacedX, CableLayout cableLayout)
        {
            List<double> abscisses = new List<double>();
            List<double> placedXes = cableLayout.PlacedPoints.Select(p => p.X).ToList();
            double min = placedXes.Min();
            double max = placedXes.Max();
            abscisses.Add(min);
            if (max>min)
            {
                abscisses.Add(max);
                double x = min + 1;
                while (x < max)
                {
                    abscisses.Add(x);
                    x += 1;
                }
                if (symbolPlacedX > min && symbolPlacedX < max)
                    abscisses.Add(symbolPlacedX);
            }
            return abscisses;
        }

        private Dictionary<int, double> GetAbscissesByCableId(List<int> maxPositions, List<List<double>> connectionAbscisses, List<CableSymbol> symbols, List<CableLayout> layouts, DeviceGroup symbolGroup)
        {
            List<int> positions = new List<int>(maxPositions.Count);
            maxPositions.ForEach(mp=>positions.Add(0));
            List<int> optimalPositions = new List<int>(positions);
            int lastIndex = maxPositions.Count - 1;
            Dictionary<int, Tuple<double, double>> minMaxByCableId = new Dictionary<int, Tuple<double, double>>(layouts.Count);
            foreach (CableLayout cableLayout in symbolGroup.CableLayoutById.Values)
                minMaxByCableId.Add(cableLayout.Id, new Tuple<double,double>(cableLayout.PlacedPoints.Min(p=>p.X), cableLayout.PlacedPoints.Max(p=>p.X)));
            int minFine = Int32.MaxValue;
            int variantCount = 1;
            foreach (int maxPosition in maxPositions)
            {
                variantCount *= maxPosition;
                if (variantCount > 1000000)
                    break;
            }
            if (variantCount < 1000000)
                do
                {
                    int fine = GetFine(positions, connectionAbscisses, symbols, layouts, symbolGroup, minMaxByCableId);
                    if (fine < minFine)
                    {
                        minFine = fine;
                        optimalPositions = new List<int>(positions);
                    }
                }
                while (NextVariant(positions, maxPositions, lastIndex));
            else
                optimalPositions = positions;
            Dictionary<int, double> abscissesByCableId = new Dictionary<int, double>();
            for (int i = 0; i < optimalPositions.Count; i++)
            {
                double absciss = connectionAbscisses[i][optimalPositions[i]];
                int cableId = symbols[i].CableId;
                abscissesByCableId.Add(cableId, absciss);
            }
            return abscissesByCableId;
        }

        private bool NextVariant(List<int> positions, List<int> maxPositionCount, int lastIndex)
        {
            positions[lastIndex]++;
            if (positions[lastIndex] < maxPositionCount[lastIndex])
                return true;
            else
            {
                positions[lastIndex] = 0;
                if (lastIndex > 0)
                    return NextVariant(positions, maxPositionCount, --lastIndex);
            }
            return false;
        }

        private static int GetFine(List<int> positions, List<List<double>> connectionAbscisses, List<CableSymbol> cableSymbols, List<CableLayout> bottomLayouts, DeviceGroup symbol, Dictionary<int, Tuple<double, double>> minMaxByCableId)
        {
            int fine;
            int positionsCount = positions.Count;
            List<double> abscisses = new List<double>(positionsCount);
            for (int i = 0; i < positionsCount; i++)
                abscisses.Add(connectionAbscisses[i][positions[i]]);
            int overlayCount = positionsCount - abscisses.Distinct().Count();
            int intersectionCount = 0;
            for (int i = 0; i < positionsCount; i++)
            {
                double placedX = cableSymbols[i].PlacedPosition.X;
                double absciss = abscisses[i];
                if (placedX < absciss)
                    intersectionCount+= abscisses.Where(a => (a > placedX && a < absciss)).Count();
                else
                    intersectionCount += abscisses.Where(a => (a < placedX && a > absciss)).Count();
            }
            int horizontalIntersectionCount = 0;
            double distanceFromCenterSum = 0;
            for (int i = 0; i < positionsCount; i++)
            {
                int cableId = cableSymbols[i].CableId;
                CableLayout layout = symbol.CableLayoutById[cableId];
                double layoutOffset = layout.verticalOffset;
                IEnumerable<int> layoutOuterOffsetIds = bottomLayouts.Where(bl => bl.verticalOffset > layoutOffset).Select(bl=>bl.Id);
                double absciss = abscisses[i];
                horizontalIntersectionCount += layoutOuterOffsetIds.Where(id => minMaxByCableId[id].Item1 < absciss && minMaxByCableId[id].Item2 > absciss).Count();
                distanceFromCenterSum += GetDistanceFromFreeSpaceCenter(minMaxByCableId, layoutOuterOffsetIds, cableId, absciss);
            }
            int straightConenctionCount = 0;
            for (int i = 0; i < positionsCount; i++)
                if (cableSymbols[i].PlacedPosition.X == abscisses[i])
                    straightConenctionCount++;
            fine = overlayCount * 10000 + intersectionCount * 1000 + horizontalIntersectionCount * 100 - straightConenctionCount * 10 + (int)distanceFromCenterSum;
            return fine;
        }

        private static double GetDistanceFromFreeSpaceCenter(Dictionary<int, Tuple<double, double>> minMaxByCableId, IEnumerable<int> layoutUnderOffsetIds, int cableId, double absciss)
        {
            double min = minMaxByCableId[cableId].Item1;
            double max = minMaxByCableId[cableId].Item2;
            if (min == max)
                return 0;
            List<double> boundaries = new List<double>(layoutUnderOffsetIds.Count() * 2);
            foreach (int id in layoutUnderOffsetIds)
            {
                boundaries.Add(minMaxByCableId[id].Item1);
                boundaries.Add(minMaxByCableId[id].Item2);
            }
            IEnumerable<double> leftBoundaries = boundaries.Where(b => b < absciss);
            IEnumerable<double> rightBoundaries = boundaries.Where(b => b > absciss);
            double left = leftBoundaries.Count() > 0 ? Math.Max(min, leftBoundaries.Max()) : min;
            double right = rightBoundaries.Count() > 0 ? Math.Min(max, rightBoundaries.Min()) : max;
            return Math.Abs(absciss - (left + right) / 2);
        }  
    }
}
