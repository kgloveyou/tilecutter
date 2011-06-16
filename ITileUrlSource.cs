using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

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
            return TileHelper.GetAGSDynamicUrlAddress(MapServiceUrl, new TileCoordinate()
            {
                Level = tile.Level,
                Column = tile.Column,
                Row = tile.Row
            });
        }
    }

    public class WMSTileUrlSource : ITileUrlSource
    {
        public string WMSVersion { get; set; }
        public string MapServiceUrl { get; set; }
        public NameValueCollection DefaultQueryStringValues { get; set; }
        public NameValueCollection QueryStringValues { get; set; }

        public WMSTileUrlSource():this(TileHelper.WMS_VERSION_1_1_1)
        {

        }

        public WMSTileUrlSource(string wmsVersion)
        {
            WMSVersion = wmsVersion;
            DefaultQueryStringValues = new NameValueCollection();
            DefaultQueryStringValues.Add(TileHelper.WMS_VERSION, WMSVersion);
            DefaultQueryStringValues.Add(TileHelper.WMS_REQUEST, "GetMap");
            DefaultQueryStringValues.Add(TileHelper.WMS_SRS, "EPSG:4326");
            DefaultQueryStringValues.Add(TileHelper.WMS_BBOX, "-178.217598,18.924782,-66.969271,71.406235");
            DefaultQueryStringValues.Add(TileHelper.WMS_WIDTH, "256");
            DefaultQueryStringValues.Add(TileHelper.WMS_HEIGHT, "256");
            DefaultQueryStringValues.Add(TileHelper.WMS_LAYERS, ",");
            DefaultQueryStringValues.Add(TileHelper.WMS_STYLES, ",");
            DefaultQueryStringValues.Add(TileHelper.WMS_FORMAT, "image/png");
            DefaultQueryStringValues.Add(TileHelper.WMS_BGCOLOR, "0xFFFFFF");
            DefaultQueryStringValues.Add(TileHelper.WMS_TRANSPARENT, "TRUE");

            QueryStringValues = new NameValueCollection();
        }

        public string GetTileUrl(TileCoordinate tile)
        {
            //combine the user supplied values & defaults and override defaults
            NameValueCollection dict = new NameValueCollection();
            foreach (string item in DefaultQueryStringValues)
                dict[item] = DefaultQueryStringValues[item];
            foreach (string item in QueryStringValues)
                dict[item] = QueryStringValues[item];

            return TileHelper.GetWMSUrlAddress(WMSVersion, MapServiceUrl, dict, new TileCoordinate()
            {
                Level = tile.Level,
                Column = tile.Column,
                Row = tile.Row
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
