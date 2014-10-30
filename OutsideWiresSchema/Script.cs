using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    public class Script
    {
        private string subProjectAttribute;
        private string sheetMarkAttribute;

        public Script()
        {
            subProjectAttribute = "SubProj";
            sheetMarkAttribute = "marka2";
        }
        
        public void Main(int processId)
        {
            int electricSchemeTypeCode = 0;
            E3Project project = new E3Project(processId);
            Sheet sheet = project.GetSheetById(0);
            E3Text text = project.GetTextById(0);
            HashSet<int> electricSchemeSheetIds = GetElectricSchemeSheetIds(project, sheet, electricSchemeTypeCode);
            Dictionary<int, DeviceConnection> deviceConnectionById;
            Dictionary<int, CableInfo> cableInfoById;
            GetDeviceConnectionByIdAndCableInfoById(project, electricSchemeSheetIds, out deviceConnectionById, out cableInfoById);
            Dictionary<int, DeviceSymbol> deviceSymbolById = GetDeviceSymbolById(project, text, deviceConnectionById);
            List<Scheme> schemes = GetSchemes(deviceConnectionById, deviceSymbolById, cableInfoById, sheet, text);
            PlaceSchemes(schemes, project, sheet, text, String.Empty);
            //List<int> pipeIds = GetPipeIds(project);
            //new SpecificationScript(deviceConnectionById.Values, pipeIds);
            project.Release();
        }

        private List<int> GetPipeIds(E3Project project)
        {
            NormalDevice device = project.GetNormalDeviceById(0);
            List<int> pipeIds = new List<int>();
            foreach (int id in project.DeviceIds)
            {
                device.Id = id;
                string function = device.GetComponentAttributeValue("Function");
                if (ContainsIgnoreCase(function,"труб", StringComparison.OrdinalIgnoreCase) || ContainsIgnoreCase(function, "металлорукав", StringComparison.OrdinalIgnoreCase))
                    pipeIds.Add(id);
            }
            return pipeIds;
        }

        private static HashSet<int> GetElectricSchemeSheetIds(E3Project project ,Sheet sheet, int electricShemeTypeCode)
        {
            HashSet<int> electricSchemeSheetIds = new HashSet<int>();
            foreach (int sheetId in project.SheetIds)
            {
                sheet.Id = sheetId;
                if (sheet.IsSchematicTypeOf(electricShemeTypeCode))
                    electricSchemeSheetIds.Add(sheetId);
            }
            return electricSchemeSheetIds;
        }

        private static void GetDeviceConnectionByIdAndCableInfoById(E3Project project, HashSet<int> electricSchemeSheetIds, out Dictionary<int, DeviceConnection> deviceConnectionById, out Dictionary<int, CableInfo> cableInfoById)
        {
            CableDevice cable = project.GetCableDeviceById(0);
            CableCore core = project.GetCableCoreById(0);
            NormalDevice startDevice = project.GetNormalDeviceById(0);
            NormalDevice endDevice = project.GetNormalDeviceById(0);
            DevicePin startPin = project.GetDevicePinById(0);
            DevicePin endPin = project.GetDevicePinById(0);
            WireCore wire = project.GetWireCoreById(0);
            deviceConnectionById = new Dictionary<int, DeviceConnection>();
            cableInfoById = new Dictionary<int, CableInfo>();
            int connectionId = 0;
            foreach (int cableId in project.CableIds)
            {
                cable.Id = cableId;
                foreach (int coreId in cable.CoreIds)
                {
                    core.Id = coreId;
                    int startPinId = core.StartPinId;
                    int endPinId = core.EndPinId;
                    if (endPinId == 0 || startPinId == 0)   // проверка на неподключенные концы
                        continue;
                    startPin.Id = startPinId;  // id вывода
                    if (!electricSchemeSheetIds.Contains(startPin.SheetId)) // проверка на то, что вывод расположен на принципиальной схеме
                        continue;
                    endPin.Id = endPinId;
                    if (!electricSchemeSheetIds.Contains(endPin.SheetId))
                        continue;
                    startDevice.Id = startPinId;
                    endDevice.Id = endPinId;
                    if (startDevice.Assignment.Equals(endDevice.Assignment) && !(startDevice.GetAttributeValue("IncludeInOWS").Equals("1") || endDevice.GetAttributeValue("IncludeInOWS").Equals("1")))
                        continue;
                    string signal = startPin.SignalName;
                    deviceConnectionById.Add(connectionId++, new DeviceConnection(startDevice.Id, startPin.Name, endDevice.Id, endPin.Name, cableId, signal));
                    string lengthAttribute = "Lenght_m_sp";
                    if (!cableInfoById.ContainsKey(cableId))
                        cableInfoById.Add(cableId, new CableInfo(cable, lengthAttribute));
                    cableInfoById[cableId].Signals.Add(signal);
                }
            }
            foreach (int wireId in project.WireIds)
            {
                wire.Id = wireId;
                int startPinId = wire.StartPinId;
                int endPinId = wire.EndPinId;
                if (endPinId == 0 || startPinId == 0)   // проверка на неподключенные концы
                    continue;
                startPin.Id = startPinId;  // id вывода
                if (!electricSchemeSheetIds.Contains(startPin.SheetId)) // проверка на то, что вывод расположен на принципиальной схеме
                    continue;
                endPin.Id = endPinId;
                if (!electricSchemeSheetIds.Contains(endPin.SheetId))
                    continue;
                startDevice.Id = startPinId;
                endDevice.Id = endPinId;
                if (startDevice.Assignment.Equals(endDevice.Assignment) && !(startDevice.GetAttributeValue("IncludeInOWS").Equals("1") || endDevice.GetAttributeValue("IncludeInOWS").Equals("1")))
                    continue;
                string signal = startPin.SignalName;
                deviceConnectionById.Add(connectionId++, new DeviceConnection(startDevice.Id, startPin.Name, endDevice.Id, endPin.Name, wireId, signal));
                string lengthAttribute = "Lenght_m_sp";
                if (!cableInfoById.ContainsKey(wireId))
                    cableInfoById.Add(wireId, new CableInfo(wire, lengthAttribute));
                cableInfoById[wireId].Signals.Add(signal);
            }
        }

        private static Dictionary<int, DeviceSymbol> GetDeviceSymbolById(E3Project project, E3Text text, Dictionary<int, DeviceConnection> deviceConnectionById)
        {
            NormalDevice device = project.GetNormalDeviceById(0);
            DevicePin pin = project.GetDevicePinById(0);
            Dictionary<int, DeviceSymbol> deviceSymbolById = new Dictionary<int,DeviceSymbol>();
            foreach (int connectionId in deviceConnectionById.Keys)
            {
                DeviceConnection deviceConnection =  deviceConnectionById[connectionId];
                int startId = deviceConnection.StartDeviceId;
                int endId = deviceConnection.EndDeviceId;
                if (!deviceSymbolById.ContainsKey(startId))
                {
                    device.Id = startId;
                    deviceSymbolById.Add(startId, new DeviceSymbol(device, pin));
                }
                if (!deviceSymbolById.ContainsKey(endId))
                {
                    device.Id = endId;
                    deviceSymbolById.Add(endId, new DeviceSymbol(device, pin));
                }
                deviceSymbolById[startId].ConnectionIds.Add(connectionId);
                deviceSymbolById[endId].ConnectionIds.Add(connectionId);
            }
            foreach (DeviceSymbol deviceSymbol in deviceSymbolById.Values)
            {
                deviceSymbol.SetCableIds(deviceConnectionById);
                deviceSymbol.SetPinsAndHeightAndNameWidth(deviceConnectionById, deviceSymbolById, text);
            }
            return deviceSymbolById;
        }

        private List<Scheme> GetSchemes(Dictionary<int, DeviceConnection> deviceConnectionById, Dictionary<int, DeviceSymbol> deviceSymbolById, Dictionary<int, CableInfo> cableInfoById, Sheet sheet, E3Text text)
        {
            List<HashSet<int>> schemeSymbolIds = GetSchemeSymbolIds(deviceConnectionById.Values);
            List<Scheme> schemes = new List<Scheme>(schemeSymbolIds.Count);
            foreach (HashSet<int> deviceIds in schemeSymbolIds)
                schemes.Add(new Scheme(deviceIds, deviceSymbolById, cableInfoById, text));
            return schemes;
        }

        private static List<HashSet<int>> GetSchemeSymbolIds(IEnumerable<DeviceConnection> deviceConnections)
        {
            IEnumerable<HashSet<int>> deviceIdsGroupedByCable = GetDeviceIdsGroupedByCable(deviceConnections);
            Dictionary<int, HashSet<int>> deviceIdsByGroupId = new Dictionary<int, HashSet<int>>();
            foreach (HashSet<int> deviceIds in deviceIdsGroupedByCable)
            {
                List<int> intersectedGroupIds = GetIntersectedGroupIds(deviceIds, deviceIdsByGroupId);
                int intersectionCount = intersectedGroupIds.Count;
                int firstGroupId;
                switch (intersectionCount)
                {
                    case (0):
                        int key;
                        if (deviceIdsByGroupId.Count == 0)
                            key = 0;
                        else
                            key = deviceIdsByGroupId.Keys.Last() + 1;
                        if (deviceIdsByGroupId.ContainsKey(key))
                            key++;
                        deviceIdsByGroupId.Add(key, new HashSet<int>(deviceIds));
                        break;
                    case (1):
                        firstGroupId = intersectedGroupIds[0];
                        AddRange(deviceIds, deviceIdsByGroupId[firstGroupId]);
                        break;
                    default:
                        firstGroupId = intersectedGroupIds[0];
                        AddRange(deviceIds, deviceIdsByGroupId[firstGroupId]);
                        for (int i = 1; i < intersectionCount; i++)
                        {
                            int groupId = intersectedGroupIds[i];
                            AddRange(deviceIdsByGroupId[groupId], deviceIdsByGroupId[firstGroupId]);
                            deviceIdsByGroupId.Remove(groupId);
                        }
                        break;
                }
            }
            return deviceIdsByGroupId.Values.ToList();
        }

        private static IEnumerable<HashSet<int>> GetDeviceIdsGroupedByCable(IEnumerable<DeviceConnection> deviceConnections)
        {
            Dictionary<int, HashSet<int>> devicedsByCableId = new Dictionary<int, HashSet<int>>();
            foreach (DeviceConnection deviceConnection in deviceConnections)
            {
                int cableId = deviceConnection.CableId;
                if (!devicedsByCableId.ContainsKey(cableId))
                    devicedsByCableId.Add(cableId, new HashSet<int>() { deviceConnection.StartDeviceId, deviceConnection.EndDeviceId });
                else
                {
                    devicedsByCableId[cableId].Add(deviceConnection.StartDeviceId);
                    devicedsByCableId[cableId].Add(deviceConnection.EndDeviceId);
                }
            }
            return devicedsByCableId.Values;
        }

        private static List<int> GetIntersectedGroupIds(HashSet<int> deviceIds, Dictionary<int, HashSet<int>> deviceIdsByGroupId)
        {
            List<int> intersectedGroupIds = new List<int>();
            foreach (int groupId in deviceIdsByGroupId.Keys)
                if (deviceIdsByGroupId[groupId].Overlaps(deviceIds))
                    intersectedGroupIds.Add(groupId);
            return intersectedGroupIds;
        }

        private static void AddRange(HashSet<int> from, HashSet<int> to)
        {
            foreach (int id in from)
                to.Add(id);
        }

        private void PlaceSchemes(List<Scheme> schemes, E3Project project, Sheet sheet, E3Text text, string sheetMark)
        {
            double gridStep = 4;
            Group group = project.GetGroupById(0);
            Graphic graphic = project.GetGraphicById(0);
            int sheetCount=1;
            CreateSheet(sheet, sheetMark, ref sheetCount);
            bool needToCreate = false;
            double availableWith = sheet.DrawingArea.Width * 0.9;
            double startX = sheet.MoveRight(sheet.DrawingArea.Left, sheet.DrawingArea.Width * 0.05);
            Point sheetCenter = new Point(sheet.MoveRight(sheet.DrawingArea.Left, sheet.DrawingArea.Width / 2), sheet.MoveDown(sheet.DrawingArea.Top, sheet.DrawingArea.Height / 2));
            double totalSchemeWidth = gridStep;
            List<Scheme> notPlacedSchemes = new List<Scheme>();
            foreach (Scheme scheme in schemes)
            {
                if (scheme.Size.Width > availableWith)
                {
                    if (needToCreate)
                        CreateSheet(sheet, sheetMark, ref sheetCount);
                    else
                        needToCreate = true;
                    scheme.Place(sheet, graphic, group, text, sheetCenter);
                    continue;
                }
                totalSchemeWidth += scheme.Size.Width + gridStep;
                if (totalSchemeWidth < availableWith)
                    notPlacedSchemes.Add(scheme);
                else
                {
                    if (needToCreate)
                        CreateSheet(sheet, sheetMark, ref sheetCount);
                    else
                        needToCreate = true;
                    double width = notPlacedSchemes.Sum(s => s.Size.Width);
                    double totalGap = availableWith - width;
                    double gap = totalGap / (notPlacedSchemes.Count + 1);
                    double x = startX;
                    foreach (Scheme notPlacedScheme in notPlacedSchemes)
                    {
                        x = sheet.MoveRight(x, gap + notPlacedScheme.Size.Width / 2 );
                        notPlacedScheme.Place(sheet, graphic, group, text, new Point(x, sheetCenter.Y));
                        x = sheet.MoveRight(x, notPlacedScheme.Size.Width / 2);
                    }
                    notPlacedSchemes.Clear();
                    totalSchemeWidth = gridStep + scheme.Size.Width;
                    notPlacedSchemes.Add(scheme);
                } 
            }
            if (notPlacedSchemes.Count > 0)
            {
                if (needToCreate)
                    CreateSheet(sheet, sheetMark, ref sheetCount);
                double width = notPlacedSchemes.Sum(s => s.Size.Width);
                double totalGap = availableWith - width;
                double gap = totalGap / (notPlacedSchemes.Count + 1);
                double x = startX;
                foreach (Scheme notPlacedScheme in notPlacedSchemes)
                {
                    x = sheet.MoveRight(x, gap + notPlacedScheme.Size.Width / 2);
                    notPlacedScheme.Place(sheet, graphic, group, text, new Point(x, sheetCenter.Y));
                    x = sheet.MoveRight(x, notPlacedScheme.Size.Width / 2);
                }
            }
        }

        private void CreateSheet(Sheet sheet, string sheetMark, ref int sheetCount)
        {
            sheet.Id = sheet.Create((sheetCount++).ToString(), "Формат А3 послед. листы");
            sheet.SetAttribute(subProjectAttribute, "СВП");
            if (!String.IsNullOrEmpty(sheetMark))
                sheet.SetAttribute(sheetMarkAttribute, sheetMark);
        }

        public bool ContainsIgnoreCase(string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }
    }
}