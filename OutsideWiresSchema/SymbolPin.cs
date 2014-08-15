using System.Collections.Generic;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    class SymbolPin
    {
        public string Name {get; private set;}

        public string Signal { get; private set; }

        public List<int> CableIds { get; private set; }

        public double SignalTextWidth { get; private set; }

        public double HorizontalOffset { get; private set; }

        public SymbolPin(string name, string signal, E3Text text, E3Font font)
        {
            Name = name;
            Signal = signal;
            CableIds = new List<int>();
            SignalTextWidth = text.GetTextLength(signal, font);
            SignalTextWidth = (SignalTextWidth % 1 > 0) ? (int)(SignalTextWidth + 1) : SignalTextWidth;
        }

        public void SetHorizontalOffset(double horizontalOffset)
        {
            HorizontalOffset = horizontalOffset;
        }
    }
}
