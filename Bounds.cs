using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileCutter
{
    public class Coordinate<T> where T : struct
    {
        public T X {get;set;}
        public T Y {get;set;}
    }

    public class TileCoordinate
    {
        public int Level { get; set; }
        public int Column { get; set; }
        public int Row { get; set; }
    }

    public class Bounds<T> where T : struct
    {
        public T XMin { get; set; }
        public T YMin { get; set; }
        public T XMax { get; set; }
        public T YMax { get; set; }
    }
}
