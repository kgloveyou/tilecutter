﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileCutter
{
    public class TileHelper
    {
        /// <summary>Available subdomains for tiles.</summary>
        private static readonly string[] OSM_SUB_DOMAINS = { "a", "b", "c" };
        /// <summary>Base URL used in GetTileUrl.</summary>
        private const string OSM_BASE_URL = "http://{0}.tile.openstreetmap.org/{1}/{2}/{3}.png";

        private double originShift = 2 * Math.PI * 6378137 / 2.0;

        public Coordinate<double> ConvertLatLonToMeters(double lat, double lon)
        {
            double x = lon * originShift / 180.0;
            double y = Math.Log(Math.Tan((90 + lat) * Math.PI / 360.0)) / (Math.PI / 180.0);

            y = y * originShift / 180.0;
            return new Coordinate<double>() { X = x, Y = y };
        }

        public Coordinate<double> ConvertMetersToLatLon(double x, double y)
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
        public double GetTileResolution(int tileSize = 256)
        {
            return 2 * Math.PI * 6378137 / tileSize;
        }

        /// <summary>
        /// Get Meters per pixel for given zoom level (measured at equator)
        /// </summary>
        /// <param name="zoom"></param>
        /// <param name="tileSize"></param>
        /// <returns></returns>
        public double GetResolution(int zoom, int tileSize = 256)
        {
            return GetTileResolution(tileSize) / (Math.Pow(2, zoom));
        }

        public Coordinate<double> ConvertMetersToPixels(double x, double y, int zoom, int tileSize = 256)
        {
            double pixelResolution = GetResolution(zoom, tileSize);
            double px = (x + originShift) / pixelResolution;
            double py = (y + originShift) / pixelResolution;
            return new Coordinate<double>() { X = px, Y = py };
        }

        public Coordinate<double> ConvertPixelsToMeters(double px, double py, int zoom, int tileSize = 256)
        {
            double pixelResolution = GetResolution(zoom, tileSize);
            double x = px * pixelResolution - originShift;
            double y = py * pixelResolution - originShift;
            return new Coordinate<double>() { X = x, Y = y };
        }

        public Coordinate<int> ConvertPixelsToTile(double px, double py, int tileSize = 256)
        {
            int tx = (int) Math.Ceiling(px / ((float) tileSize)) -1;
            int ty = (int) Math.Ceiling(py / ((float)tileSize)) - 1;
            return new Coordinate<int>() { X = tx, Y = ty };
        }

        public Coordinate<int> ConvertMetersToTile(double x, double y, int zoom, int tileSize = 256)
        {
            var pixelCoordinates = ConvertMetersToPixels(x, y, zoom, tileSize);
            return ConvertPixelsToTile(pixelCoordinates.X, pixelCoordinates.Y, tileSize);
        }

        public Coordinate<int> ConvertLatLonToTile(double lat, double lon, int zoom, int tileSize = 256)
        {
            var coords = ConvertLatLonToMeters(lat, lon);
            var pixelCoordinates = ConvertMetersToPixels(coords.X, coords.Y, zoom, tileSize);
            return ConvertPixelsToTile(pixelCoordinates.X, pixelCoordinates.Y, tileSize);
        }

        public Bounds<double> GetTileBounds(int tx, int ty, int zoom, int tileSize = 256)
        {
            var min = ConvertPixelsToMeters(tx * tileSize, ty * tileSize, zoom, tileSize);
            var max = ConvertPixelsToMeters((tx + 1) * tileSize, (ty + 1) * tileSize, zoom);
            return new Bounds<double>() { XMin = min.X, YMin = min.Y, XMax = max.X, YMax = max.Y };
        }

        public Bounds<double> GetTileLatLonBounds(int tx, int ty, int zoom, int tileSize = 256)
        {
            var bounds = GetTileBounds(tx, ty, zoom, tileSize);
            var min = ConvertMetersToLatLon(bounds.XMin, bounds.YMin);
            var max = ConvertMetersToLatLon(bounds.XMax, bounds.YMax);
            return new Bounds<double>() { XMin = min.X, YMin = min.Y, XMax = max.X, YMax = max.Y };
        }

        public IEnumerable<Coordinate<int>> GetTilesFromMeters(int zoomLevel, double minx, double miny, double maxx, double maxy)
        {
            var min = ConvertMetersToTile(minx, miny, zoomLevel);
            var max = ConvertMetersToTile(maxx, maxy, zoomLevel);

            return GetTilesFromTileBounds(min.X, min.Y, max.X, max.Y);
        }

        public IEnumerable<Coordinate<int>> GetTilesFromLatLon(int zoomLevel, double minx, double miny, double maxx, double maxy)
        {
            var min = ConvertLatLonToTile(miny, minx, zoomLevel);
            var max = ConvertLatLonToTile(maxy, maxx, zoomLevel);

            return GetTilesFromTileBounds(min.X, min.Y, max.X, max.Y);
        }

        public IEnumerable<Coordinate<int>> GetTilesFromTileBounds(int tminx, int tminy, int tmaxx, int tmaxy)
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

        public string GetOSMTileUrlAddress(int level, int row, int col, string[] subDomains = null, string baseUrl = null)
        {
            subDomains = subDomains ?? OSM_SUB_DOMAINS;
            baseUrl = baseUrl ?? OSM_BASE_URL;
            // Select a subdomain based on level/row/column so that it will always
            // be the same for a specific tile. Multiple subdomains allows the user
            // to load more tiles simultanously. To take advantage of the browser cache
            // the following expression also makes sure that a specific tile will always 
            // hit the same subdomain.
            string subdomain = subDomains[(level + col + row) % subDomains.Length];
            return string.Format(baseUrl, subdomain, level, col, row);
        }

        public string GetAGSDynamicUrlAddress(int level, int row, int col, string baseUrl = null)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentNullException("baseUrl", "The base url for the ArcGIS Dynamic Service cannot be NULL or empty");

            string requestUrl = baseUrl + "/export?bbox={0},{1},{2},{3}&bboxSR=4326&layers=&layerdefs=&size=256%2C256&imageSR=&format=png&transparent=true&dpi=&time=&layerTimeOptions=&f=image";
            var extent = GetTileLatLonBounds(col, row, level, tileSize: 256);
            return string.Format(requestUrl, extent.XMin, extent.YMin, extent.XMax, extent.YMax);
        }

        public Coordinate<int> ConvertTMSTileCoordinateToGoogleTileCoordinate(int zoomLevel, int tx, int ty)
        {
            return new Coordinate<int>(){X = tx, Y = (int)((Math.Pow(2, zoomLevel) - 1) - ty)};
        }

        public string ConvertTMSTileCoordinateToQuadKey(int zoomLevel, int tx, int ty)
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
    }
}