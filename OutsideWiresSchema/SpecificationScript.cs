using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace OutsideConnectionsSchema
{
    public class SpecificationScript
    {

        public SpecificationScript(IEnumerable<DeviceConnection> deviceConnections, List<int> pipeIds)
        {
            List<int> deviceIds = new List<int>();
            List<int> cableIds = new List<int>();
            deviceConnections.ToList().ForEach(dc => { deviceIds.Add(dc.StartDeviceId); deviceIds.Add(dc.EndDeviceId); cableIds.Add(dc.CableId); });
            deviceIds.AddRange(pipeIds);
            deviceIds = deviceIds.Distinct().ToList();
            cableIds = cableIds.Distinct().ToList();
            string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            using (TextWriter writer = new StreamWriter(fileName))
            {
                deviceIds.ForEach(dId => writer.WriteLine(dId));
                cableIds.ForEach(cId => writer.WriteLine(cId));
            }
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo("wscript.exe", String.Format("{0} {1}", "spec.vbs", fileName));
            process.Start();
            process.WaitForExit();
            File.Delete(fileName);
        }
    }
}
