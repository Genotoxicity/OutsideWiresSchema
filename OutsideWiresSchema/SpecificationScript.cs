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

        public SpecificationScript(IEnumerable<int> ids, int firstSheetNumber, string subProjectAttribute, string subProject)
        {
            string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            using (TextWriter writer = new StreamWriter(fileName))
            {
                foreach (int id in ids)
                    writer.WriteLine(id);
            }
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo("wscript.exe", String.Format("{0} {1} {2} {3} {4}", "spec.vbs", fileName, firstSheetNumber, subProjectAttribute, subProject));
            process.Start();
            process.WaitForExit();
            File.Delete(fileName);
        }
    }
}
