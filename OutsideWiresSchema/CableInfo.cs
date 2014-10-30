using System;
using System.Collections.Generic;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    class CableInfo
    {
        public int Id { get; private set; }

        public string Name { get; private set; }

        public string Type { get; private set; }

        public string Length { get; private set; }

        public List<string> Signals { get; private set; }

        public int LoopId { get; set; }

        public CableInfo(CableDevice cable, string lengthAttribute)
        {
            Id = cable.Id;
            Name = cable.Name;
            Type = String.Intern(cable.ComponentName);
            Length = cable.GetAttributeValue(lengthAttribute);
            Length = String.IsNullOrEmpty(Length) ? "0 м" : Length + " м";
            Signals = new List<string>();
        }

        public CableInfo(WireCore wire, string lengthAttribute)
        {
            Id = wire.Id;
            Name = wire.Name;
            Type = String.Intern(wire.WireType);
            Length = wire.GetAttributeValue(lengthAttribute);
            Length = String.IsNullOrEmpty(Length) ? "0 м" : Length + " м";
            Signals = new List<string>();
        }
    }
}
