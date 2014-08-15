﻿using System.Collections.Generic;
using System.Linq;
using System.Windows;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    class Scheme
    {
        private Dictionary<int, List<int>> groupIdsByCableId;
        private Dictionary<int, SymbolGroup> groupById;
        private SchemeLayout layout;
        private double gridStep;

        public Size Size
        {
            get
            {
                return layout.Size;
            }
        }

        public Scheme(IEnumerable<int> schemeDeviceIds, Dictionary<int, DeviceSymbol> deviceSymbolById, Dictionary<int, CableInfo> cableInfoById, E3Text text, double sheetHeight)
        {
            gridStep = 4;
            groupById = new Dictionary<int, SymbolGroup>();
            foreach (int deviceId in schemeDeviceIds)
            {
                DeviceSymbol deviceSymbol = deviceSymbolById[deviceId];
                bool isAdded = false;
                foreach (SymbolGroup group in groupById.Values)
                    if (group.TryAddSymbol(deviceSymbol))
                    {
                        isAdded = true;
                        break;
                    }
                if (!isAdded)
                {
                    int count = groupById.Count;
                    if (!groupById.ContainsKey(count))
                        groupById.Add(count, new SymbolGroup(count));
                    groupById[count].TryAddSymbol(deviceSymbol);
                }
            }
            SetLoopsInGroupAndCableInfo(cableInfoById);
            foreach (SymbolGroup group in groupById.Values)
                group.SetPosition();
            groupIdsByCableId = GetGroupsByCableId(cableInfoById.Count);
            layout = new SchemeLayout(groupById, groupIdsByCableId, cableInfoById, gridStep, text, sheetHeight * 0.618);
        }

        public void Place(Sheet sheet, Graphic graphic, Group e3group, E3Text text, Point placePositon)
        {
            layout.Place(sheet, graphic, e3group, text, placePositon);
        }

        private void SetLoopsInGroupAndCableInfo(Dictionary<int, CableInfo> cableInfoById)
        {
            Dictionary<int, List<string>> signalsByLoopId = new Dictionary<int, List<string>>();
            Dictionary<int, int> loopCount = new Dictionary<int, int>();
            foreach (CableInfo cableInfo in cableInfoById.Values)
            {
                int loopId = GetLoopId(cableInfo.Signals, signalsByLoopId);
                cableInfo.LoopId = loopId;
                if (!loopCount.ContainsKey(loopId))
                    loopCount.Add(loopId, 1);
                else
                    loopCount[loopId]++;
            }
            foreach (SymbolGroup group in groupById.Values)
                foreach (int cableId in group.CableIds)
                {
                    int loopId = cableInfoById[cableId].LoopId;
                    if (loopCount[loopId] > 2)
                        if (!group.LoopIds.Contains(loopId))
                            group.LoopIds.Add(loopId);
                }         
        }

        private static int GetLoopId(List<string> edgeSignals, Dictionary<int, List<string>> signalsByLoopId)
        {
            foreach (int id in signalsByLoopId.Keys)
                if (IsListsOfStringEquals(edgeSignals, signalsByLoopId[id]))
                    return id;
            int count = signalsByLoopId.Count;
            signalsByLoopId.Add(count, edgeSignals);
            return count;
        }

        private static bool IsListsOfStringEquals(List<string> a, List<string> b)
        {
            int count = a.Count;
            if (count != b.Count)
                return false;
            return count == a.Intersect(b).Count<string>();
        }

        private Dictionary<int, List<int>> GetGroupsByCableId(int capacity)
        {
            Dictionary<int, List<int>> groupsIdsByCableId = new Dictionary<int, List<int>>(capacity);
            foreach (SymbolGroup group in groupById.Values)
                foreach (int cableId in group.CableIds)
                    if (!groupsIdsByCableId.ContainsKey(cableId))
                        groupsIdsByCableId.Add(cableId, new List<int>(2) { group.Id });
                    else
                        groupsIdsByCableId[cableId].Add(group.Id);
            return groupsIdsByCableId;
        }
    }
}