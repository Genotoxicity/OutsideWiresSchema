using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    class SchemeCreator
    {
        /*private Scheme layout;

        public Size Size
        {
            get
            {
                return layout.Size;
            }
        }*/

        public static List<SymbolScheme> GetSchemesForConnection(IEnumerable<int> connectedDeviceIds, Dictionary<int, DeviceSymbol> deviceSymbolById, Dictionary<int, CableInfo> cableInfoById, E3Text text)
        {
            Dictionary<int, DeviceGroup> deviceGroupById = GetGroupById(connectedDeviceIds, deviceSymbolById, cableInfoById);
            Dictionary<int, List<int>> deviceGroupIdsByCableId = GetDeviceGroupIdsByCableId(cableInfoById.Count, deviceGroupById.Values);
            string connectingBoxAssignment = "AssignmentForConnectingBox";
            Dictionary<string, List<DeviceGroup>> deviceGroupsByAssignment = GetDeviceGroupsByAssignment(deviceGroupById, connectingBoxAssignment);
            int schemeSymbolId = deviceGroupById.Count;
            List<SymbolScheme> schemes = new List<SymbolScheme>(deviceGroupsByAssignment.Count);
            foreach (string assignment in deviceGroupsByAssignment.Keys)
            {
                Dictionary<int, ISchemeSymbol> symbolById = new Dictionary<int,ISchemeSymbol>();
                Dictionary<int, List<int>> symbolIdsByCableId = new Dictionary<int,List<int>>();
                List<DeviceGroup> deviceGroups = deviceGroupsByAssignment[assignment];
                foreach (DeviceGroup deviceGroup in deviceGroups)
                    AddConnectedSchemeSymbols(deviceGroup, symbolById, symbolIdsByCableId, deviceGroupIdsByCableId, deviceGroupById, connectingBoxAssignment, assignment, ref schemeSymbolId, text);
                SymbolScheme scheme = new SymbolScheme(symbolById, symbolIdsByCableId, cableInfoById, text, assignment);
                schemes.Add(scheme);
            }
            return schemes;
            //layout = new Scheme(deviceGroupById, deviceGroupIdsByCableId, cableInfoById, text);
        }

        private static void AddConnectedSchemeSymbols(DeviceGroup deviceGroup, Dictionary<int, ISchemeSymbol> symbolById, Dictionary<int, List<int>> symbolIdsByCableId, Dictionary<int, List<int>> deviceGroupIdsByCableId, Dictionary<int, DeviceGroup> deviceGroupById, string connectingBoxAssignment, string assignment, ref int schemeSymbolId, E3Text text)
        {
            if (symbolById.ContainsKey(deviceGroup.Id))
                return;
            symbolById.Add(deviceGroup.Id, deviceGroup);
            foreach (int cableId in deviceGroup.CableIds)
            {
                if (symbolIdsByCableId.ContainsKey(cableId))
                    continue;
                List<int> ids = deviceGroupIdsByCableId[cableId];
                int mateId = (ids.First() == deviceGroup.Id) ? ids.Last() : ids.First();
                DeviceGroup mateGroup = deviceGroupById[mateId];
                if (symbolById.ContainsKey(mateId))
                {
                    if (String.IsNullOrEmpty(mateGroup.Assignment) || mateGroup.Assignment.Equals(connectingBoxAssignment) || mateGroup.Assignment.Equals(assignment))
                        symbolIdsByCableId.Add(cableId, ids);
                    continue;
                }
                symbolIdsByCableId.Add(cableId, new List<int>(2) { deviceGroup.Id });
                if (String.IsNullOrEmpty(mateGroup.Assignment) || mateGroup.Assignment.Equals(connectingBoxAssignment) || mateGroup.Assignment.Equals(assignment))
                {
                    AddConnectedSchemeSymbols(mateGroup, symbolById, symbolIdsByCableId, deviceGroupIdsByCableId, deviceGroupById, connectingBoxAssignment, assignment, ref schemeSymbolId,text);
                    symbolIdsByCableId[cableId].Add(mateId);
                    //schemeSymbolById.Add(mateId, deviceGroupById[mateId]);
                }
                else
                {
                    symbolById.Add(schemeSymbolId, new AssignmentReferenceSymbol(mateGroup.Assignment, schemeSymbolId, cableId, text));
                    symbolIdsByCableId[cableId].Add(schemeSymbolId);
                    schemeSymbolId++;
                }
            }
        }

        private static Dictionary<string, List<DeviceGroup>> GetDeviceGroupsByAssignment(Dictionary<int, DeviceGroup> groupById, string connectingBoxAssignment)
        {
            Dictionary<string, List<DeviceGroup>> deviceGroupsByAssignment = new Dictionary<string, List<DeviceGroup>>();
            foreach (DeviceGroup deviceGroup in groupById.Values)
            {
                string assignment = deviceGroup.Assignment;
                if (String.IsNullOrEmpty(assignment) || assignment.Equals(connectingBoxAssignment))
                    continue;
                if (deviceGroupsByAssignment.ContainsKey(assignment))
                    deviceGroupsByAssignment[assignment].Add(deviceGroup);
                else
                    deviceGroupsByAssignment.Add(assignment, new List<DeviceGroup>() { deviceGroup });
            }
            return deviceGroupsByAssignment;
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

        public void Place(Sheet sheet, Graphic graphic, Group e3group, E3Text text, Point placePositon)
        {
            //layout.Place(sheet, graphic, e3group, text, placePositon);
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
