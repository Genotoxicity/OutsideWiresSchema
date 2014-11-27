using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{
    public interface ISchemeSymbol
    {
        int Id { get; }

        Level Level { get; }

        List<int> CableIds { get; }

        double OutlineHeight { get; }

        double OutlineWidth { get; }

        double Height { get; }

        double TopMargin { get; }

        double BottomMargin { get; }

        double RightMargin { get; }

        double LeftMargin { get; }

        Point PlacedPosition { get; }

        Dictionary<int, CableLayout> CableLayoutById { get; }

        void Place(Point placePosition, Sheet sheet, Graphic graphic, Group group, E3Text text);
    }
}
