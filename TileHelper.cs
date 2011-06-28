using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace TileCutter
{
    public static class TileHelper
    {
        /// <summary>Available subdomains for tiles.</summary>
        public static readonly string[] OSM_SUB_DOMAINS = { "a", "b", "c" };
        /// <summary>Base URL used in GetTileUrl.</summary>
        public const string OSM_BASE_URL_TEMPLATE_WITH_SUBDOMAIN = "http://{0}.tile.openstreetmap.org/{1}/{2}/{3}.png";
        public const string OSM_BASE_URL_TEMPLATE = "http://tile.openstreetmap.org/{0}/{1}/{2}.png";

        private static double originShift = 2 * Math.PI * 6378137 / 2.0;

        public static Coordinate<double> ConvertLatLonToMeters(double lat, double lon)
        {
            double x = lon * originShift / 180.0;
            double y = Math.Log(Math.Tan((90 + lat) * Math.PI / 360.0)) / (Math.PI / 180.0);

            y = y * originShift / 180.0;
            return new Coordinate<double>() { X = x, Y = y };
        }

        public static Coordinate<double> ConvertMetersToLatLon(double x, double y)
        {
            double lon = (x / originShift) * 180.0;
            double lat = (y / originShift) * 180.0;

            lat = 180 / Math.PI * (2 * Math.Atan(Math.Exp(lat * Math.PI / 180.0)) - Math.PI / 2.0);
            return new Coordinate<double>() { X = lon, Y = lat };
        }

        /// <summary>
        /// Get meters per tile (measured at equator)
        /// </summary>
        /// <param name="tileSize"></param>
        /// <returns></returns>
        public static double GetTileResolution(int tileSize = 256)
        {
            return 2 * Math.PI * 6378137 / tileSize;
        }

        /// <summary>
        /// Get Meters per pixel for given zoom level (measured at equator)
        /// </summary>
        /// <param name="zoom"></param>
        /// <param name="tileSize"></param>
        /// <returns></returns>
        public static double GetResolution(int zoom, int tileSize = 256)
        {
            return GetTileResolution(tileSize) / (Math.Pow(2, zoom));
        }

        public static Coordinate<double> ConvertMetersToPixels(double x, double y, int zoom, int tileSize = 256)
        {
            double pixelResolution = GetResolution(zoom, tileSize);
            double px = (x + originShift) / pixelResolution;
            double py = (y + originShift) / pixelResolution;
            return new Coordinate<double>() { X = px, Y = py };
        }

        public static Coordinate<double> ConvertPixelsToMeters(double px, double py, int zoom, int tileSize = 256)
        {
            double pixelResolution = GetResolution(zoom, tileSize);
            double x = px * pixelResolution - originShift;
            double y = py * pixelResolution - originShift;
            return new Coordinate<double>() { X = x, Y = y };
        }

        public static Coordinate<int> ConvertPixelsToTile(double px, double py, int tileSize = 256)
        {
            int tx = (int) Math.Ceiling(px / ((float) tileSize)) -1;
            int ty = (int) Math.Ceiling(py / ((float)tileSize)) - 1;
            return new Coordinate<int>() { X = tx, Y = ty };
        }

        public static Coordinate<int> ConvertMetersToTile(double x, double y, int zoom, int tileSize = 256)
        {
            var pixelCoordinates = ConvertMetersToPixels(x, y, zoom, tileSize);
            return ConvertPixelsToTile(pixelCoordinates.X, pixelCoordinates.Y, tileSize);
        }

        public static Coordinate<int> ConvertLatLonToTile(double lat, double lon, int zoom, int tileSize = 256)
        {
            var coords = ConvertLatLonToMeters(lat, lon);
            var pixelCoordinates = ConvertMetersToPixels(coords.X, coords.Y, zoom, tileSize);
            return ConvertPixelsToTile(pixelCoordinates.X, pixelCoordinates.Y, tileSize);
        }

        public static Bounds<double> GetTileBounds(int tx, int ty, int zoom, int tileSize = 256)
        {
            var min = ConvertPixelsToMeters(tx * tileSize, ty * tileSize, zoom, tileSize);
            var max = ConvertPixelsToMeters((tx + 1) * tileSize, (ty + 1) * tileSize, zoom);
            return new Bounds<double>() { XMin = min.X, YMin = min.Y, XMax = max.X, YMax = max.Y };
        }

        public static Bounds<double> GetTileLatLonBounds(int tx, int ty, int zoom, int tileSize = 256)
        {
            var bounds = GetTileBounds(tx, ty, zoom, tileSize);
            var min = ConvertMetersToLatLon(bounds.XMin, bounds.YMin);
            var max = ConvertMetersToLatLon(bounds.XMax, bounds.YMax);
            return new Bounds<double>() { XMin = min.X, YMin = min.Y, XMax = max.X, YMax = max.Y };
        }

        public static IEnumerable<Coordinate<int>> GetTilesFromMeters(int zoomLevel, double minx, double miny, double maxx, double maxy)
        {
            var min = ConvertMetersToTile(minx, miny, zoomLevel);
            var max = ConvertMetersToTile(maxx, maxy, zoomLevel);

            return GetTilesFromTileBounds(min.X, min.Y, max.X, max.Y);
        }

        public static IEnumerable<Coordinate<int>> GetTilesFromLatLon(int zoomLevel, double minx, double miny, double maxx, double maxy)
        {
            var min = ConvertLatLonToTile(miny, minx, zoomLevel);
            var max = ConvertLatLonToTile(maxy, maxx, zoomLevel);

            return GetTilesFromTileBounds(min.X, min.Y, max.X, max.Y);
        }

        public static IEnumerable<Coordinate<int>> GetTilesFromTileBounds(int tminx, int tminy, int tmaxx, int tmaxy)
        {
            var tileMinX = tminx < tmaxx ? tminx : tmaxx;
            var tileMaxX = tminx > tmaxx ? tminx : tmaxx;
            var tileMinY = tminy < tmaxy ? tminy : tmaxy;
            var tileMaxY = tminy > tmaxy ? tminy : tmaxy;

            for (int y = tileMinY; y <= tileMaxY; y++)
            {
                for (int x = tileMinX; x <= tileMaxX; x++)
                {
                    yield return new Coordinate<int>() { X = x, Y = y };
                }
            }
        }

        public static string GetOSMTileUrlAddress(string baseUrl, TileCoordinate tile)
        {
            return GetOSMTileUrlAddress(baseUrl, tile.Level, tile.Row, tile.Column);
        }

        public static string GetOSMTileUrlAddress(string baseUrl, int level, int row, int col)
        {
            return baseUrl + string.Format("/{0}/{1}/{2}.png", level, col, row);
        }

        public static string GetOSMTileUrlAddressWithSubdomains(string baseUrl, List<string> subDomains, TileCoordinate tile)
        {
            return GetOSMTileUrlAddressWithSubdomains(baseUrl, subDomains, tile.Level, tile.Row, tile.Column);
        }

        public static string GetOSMTileUrlAddressWithSubdomains(string baseUrl, List<string> subDomains, int level, int row, int col)
        {
            // Select a subdomain based on level/row/column so that it will always
            // be the same for a specific tile. Multiple subdomains allows the user
            // to load more tiles simultanously. To take advantage of the browser cache
            // the following expression also makes sure that a specific tile will always 
            // hit the same subdomain.
            string subdomain = subDomains[(level + col + row) % subDomains.Count];
            return string.Format(baseUrl, subdomain, level, col, row);
        }

        public const string AGS_BBOX = "bbox";
        public const string AGS_BBOXSR = "bboxSR";
        public const string AGS_SIZE = "size";
        public const string AGS_IMAGESR = "imageSR";
        public const string AGS_LAYERS = "layers";
        public const string AGS_LAYERDEFS = "layerdefs";
        public const string AGS_FORMAT = "format";
        public const string AGS_TRANSPARENT = "transparent";
        public const string AGS_DPI = "dpi";
        public const string AGS_TIME = "time";
        public const string AGS_LAYERTIMEOPTIONS = "layerTimeOptions";
        public const string AGS_F = "f";
        public static string GetAGSDynamicUrlAddress(string baseUrl, NameValueCollection dict, TileCoordinate tile)
        {
            return GetAGSDynamicUrlAddress(baseUrl, dict, tile.Level, tile.Row, tile.Column);
        }

        public static string GetAGSDynamicUrlAddress(string baseUrl, NameValueCollection dict, int level, int row, int col)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentNullException("baseUrl", "The base url for the ArcGIS Dynamic Service cannot be NULL or empty");

            Uri requestUrl = new Uri(new Uri(baseUrl + "/"), "export");
            UriBuilder builder = new UriBuilder(requestUrl);
            
            var extent = GetTileLatLonBounds(col, row, level, tileSize: 256);
            string bbox = string.Format("{0},{1},{2},{3}", extent.XMin, extent.YMin, extent.XMax, extent.YMax);

            dict[AGS_BBOX] = bbox;
            dict[AGS_BBOXSR] = "4326";
            dict[AGS_SIZE] = "256,256";
            dict[AGS_IMAGESR] = "3857";
            if (dict != null)
                builder.Query = dict.ToQueryString();
            return builder.ToString();
        }

        public static Coordinate<int> ConvertTMSTileCoordinateToGoogleTileCoordinate(int zoomLevel, int tx, int ty)
        {
            return new Coordinate<int>(){X = tx, Y = (int)((Math.Pow(2, zoomLevel) - 1) - ty)};
        }

        public static Coordinate<int> ConvertGoogleTileCoordinateToTMSTileCoordinate(int zoomLevel, int tx, int ty)
        {
            return new Coordinate<int>() { X = tx, Y = (int)((Math.Pow(2, zoomLevel) - 1) - ty) };
        }

        public static string ConvertTMSTileCoordinateToQuadKey(int zoomLevel, int tx, int ty)
        {
            string QuadKey = "";
            ty = (int)((Math.Pow(2, zoomLevel) - 1) - ty);
            for (int i = zoomLevel; i > 0; i--)
            {
                int digit = 0;
                var mask = 1 << (i - 1);
                if ((tx & mask) != 0)
                    digit += 1;
                if ((ty & mask) != 0)
                    digit += 2;
                QuadKey += digit.ToString();
            }
            return QuadKey;
        }

        public const string WMS_VERSION = "VERSION";
        public const string WMS_VERSION_1_1_1 = "1.1.1";
        public const string WMS_VERSION_1_3_0 = "1.3.0";
        public const string WMS_REQUEST = "REQUEST";
        public const string WMS_SRS = "SRS";
        public const string WMS_CRS = "CRS";
        public const string WMS_BBOX = "BBOX";
        public const string WMS_WIDTH = "WIDTH";
        public const string WMS_HEIGHT = "HEIGHT";
        public const string WMS_LAYERS = "LAYERS";
        public const string WMS_STYLES = "STYLES";
        public const string WMS_FORMAT = "FORMAT";
        public const string WMS_BGCOLOR = "BGCOLOR";
        public const string WMS_TRANSPARENT = "TRANSPARENT";

        public static string GetWMS1_1_1UrlAddress(string MapServiceUrl, NameValueCollection dict, TileCoordinate tileCoordinate)
        {
            return GetWMS1_1_1UrlAddress(MapServiceUrl, dict, tileCoordinate.Level, tileCoordinate.Row, tileCoordinate.Column);
        }

        public static string GetWMS1_1_1UrlAddress(string MapServiceUrl, NameValueCollection dict, int level, int row, int col)
        {
            return GetWMSUrlAddress(WMS_VERSION_1_1_1, MapServiceUrl, dict, level, row, col);
        }

        public static string GetWMS1_3_0UrlAddress(string MapServiceUrl, NameValueCollection dict, TileCoordinate tileCoordinate)
        {
            return GetWMS1_3_0UrlAddress(MapServiceUrl, dict, tileCoordinate.Level, tileCoordinate.Row, tileCoordinate.Column);
        }

        public static string GetWMS1_3_0UrlAddress(string MapServiceUrl, NameValueCollection dict, int level, int row, int col)
        {
            return GetWMSUrlAddress(WMS_VERSION_1_3_0, MapServiceUrl, dict, level, row, col);
        }

        public static string GetWMSUrlAddress(string wmsVersion, string MapServiceUrl, NameValueCollection dict, TileCoordinate tileCoordinate)
        {
            return GetWMSUrlAddress(wmsVersion, MapServiceUrl, dict, tileCoordinate.Level, tileCoordinate.Row, tileCoordinate.Column);
        }

        public static string GetWMSUrlAddress(string wmsVersion, string MapServiceUrl, NameValueCollection dict, int level, int row, int col)
        {
            if (string.IsNullOrEmpty(MapServiceUrl))
                throw new ArgumentNullException("MapServiceUrl", "The base url for the WMS Service version 1.1.1 cannot be NULL or empty");

            var extent = GetTileLatLonBounds(col, row, level, tileSize: 256);
            UriBuilder builder = new UriBuilder(MapServiceUrl);
            NameValueCollection query = new NameValueCollection(dict);
            query[WMS_WIDTH] = "256";
            query[WMS_HEIGHT] = "256";
            query[WMS_VERSION] = wmsVersion;
            if (wmsVersion == WMS_VERSION_1_1_1)
            {
                query[WMS_BBOX] = string.Format("{0},{1},{2},{3}", extent.XMin, extent.YMin, extent.XMax, extent.YMax);
                query.Remove(WMS_CRS);
                query[WMS_SRS] = "EPSG:4326";
            }
            else if (wmsVersion == WMS_VERSION_1_3_0)
            {
                query[WMS_BBOX] = string.Format("{1},{0},{3},{2}", extent.XMin, extent.YMin, extent.XMax, extent.YMax);
                query.Remove(WMS_SRS);
                query[WMS_CRS] = "EPSG:4326";
            }
            builder.Query = query.ToQueryString();

            return builder.ToString();
        }
    }
}
