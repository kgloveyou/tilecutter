using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileCutter
{
    public interface ITileUrlSource
    {
        string GetTileUrl(TileCoordinate tile);
    }

    public class AGSDynamicTileUrlSource : ITileUrlSource
    {
        public string MapServiceUrl { get; set; }

        public string GetTileUrl(TileCoordinate tile)
        {
            var gtile = TileHelper.ConvertTMSTileCoordinateToGoogleTileCoordinate(tile.Level, tile.Column, tile.Row);
            return TileHelper.GetAGSDynamicUrlAddress(MapServiceUrl, new TileCoordinate()
            {
                Level = tile.Level,
                Column = gtile.X,
                Row = gtile.Y
            });
        }
    }

    public class OSMTileUrlSource : ITileUrlSource
    {
        public string MapServiceUrl { get; set; }

        public string GetTileUrl(TileCoordinate tile)
        {
            var gtile = TileHelper.ConvertTMSTileCoordinateToGoogleTileCoordinate(tile.Level, tile.Column, tile.Row);
            return TileHelper.GetOSMTileUrlAddress(MapServiceUrl, new TileCoordinate() {
                Level = tile.Level,
                Column = gtile.X,
                Row = gtile.Y
            });
        }
    }

    public class OSMSubdomainsTileUrlSource : ITileUrlSource
    {
        public List<string> SubDomains { get; set; }
        public string UrlTemplate { get; set; }

        public string GetTileUrl(TileCoordinate tile)
        {
            return TileHelper.GetOSMTileUrlAddressWithSubdomains(UrlTemplate, SubDomains, tile);
        }
    }
}
