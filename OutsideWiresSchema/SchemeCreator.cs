using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    class SchemeCreator
    {
        public static Scheme GetScheme(IEnumerable<int> connectedDeviceIds, Dictionary<int, DeviceSymbol> deviceSymbolById, Dictionary<int, CableInfo> cableInfoById, E3Text text)
        {
            Dictionary<int, DeviceGroup> deviceGroupById = GetGroupById(connectedDeviceIds, deviceSymbolById, cableInfoById);
            Dictionary<int, List<int>> deviceGroupIdsByCableId = GetDeviceGroupIdsByCableId(cableInfoById.Count, deviceGroupById.Values);
            return new Scheme(deviceGroupById, deviceGroupIdsByCableId, cableInfoById, text);
        }

        private static Dictionary<int, DeviceGroup> GetGroupById(IEnumerable<int> connectedDeviceIds, Dictionary<int, DeviceSymbol> deviceSymbolById, Dictionary<int, CableInfo> cableInfoById)
        {
            Dictionary<int, DeviceGroup> groupById = new Dictionary<int, DeviceGroup>();
            foreach (int deviceId in connectedDeviceIds)
            {
                DeviceSymbol deviceSymbol = deviceSymbolById[deviceId];
                bool isAdded = false;
                foreach (DeviceGroup group in groupById.Values)
                    if (group.TryAddSymbol(deviceSymbol))
                    {
                        isAdded = true;
                        break;
                    }
                if (!isAdded)
                {
                    int count = groupById.Count;
                    if (!groupById.ContainsKey(count))
                        groupById.Add(count, new DeviceGroup(count));
                    groupById[count].TryAddSymbol(deviceSymbol);
                }
            }
            SetLoopsInGroupAndCableInfo(cableInfoById, groupById.Values);
            foreach (DeviceGroup group in groupById.Values)
                group.SetPosition();
            return groupById;
        }
        
        private static void SetLoopsInGroupAndCableInfo(Dictionary<int, CableInfo> cableInfoById, IEnumerable<DeviceGroup> groups)
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
            foreach (DeviceGroup group in groups)
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

        private static Dictionary<int, List<int>> GetDeviceGroupIdsByCableId(int capacity, IEnumerable<DeviceGroup> groups)
        {
            Dictionary<int, List<int>> groupsIdsByCableId = new Dictionary<int, List<int>>(capacity);
            foreach (DeviceGroup group in groups)
                foreach (int cableId in group.CableIds)
                    if (!groupsIdsByCableId.ContainsKey(cableId))
                        groupsIdsByCableId.Add(cableId, new List<int>(2) { group.Id });
                    else
                        groupsIdsByCableId[cableId].Add(group.Id);
            return groupsIdsByCableId;
        }

    }
}
