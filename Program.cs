using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Data.SQLite;
using System.Reflection;

namespace TileCutter
{
    class Program
    {
        private static TileHelper helper = new TileHelper();

        static void Main(string[] args)
        {
            string localCacheDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            int minz = 7;
            int maxz = 10;
            double minx = -95.844727;
            double miny = 35.978006;
            double maxx = -88.989258;
            double maxy = 40.563895;
            string defaultMapServiceUrl = "http://sampleserver1.arcgisonline.com/ArcGIS/rest/services/Demographics/ESRI_Census_USA/MapServer";
            int maxDegreeOfParallelism = 10;
            bool replaceExistingCacheDB = true;
            bool showHelp = false;

            var options = new OptionSet()
            {
                {"h|help", "Show this message and exits", h => showHelp = h != null},
                {"m|mapservice=", "Url of the ArcGIS Dynamic Map Service to be cached", m => defaultMapServiceUrl = m},
                {"o|output=", "Location on disk where the tile cache will be stored", o => localCacheDirectory = o},
                {"z|minz=", "Minimum zoom scale at which to begin caching", z => int.TryParse(z, out minz)},
                {"Z|maxz=", "Maximum zoom scale at which to end caching", Z => int.TryParse(Z, out maxz)},
                {"x|minx=", "Minimum X coordinate value of the extent to cache", x => double.TryParse(x, out minx)},
                {"y|miny=", "Minimum Y coordinate value of the extent to cache", y => double.TryParse(y, out miny)},
                {"X|maxx=", "Maximum X coordinate value of the extent to cache", X => double.TryParse(X, out maxx)},
                {"Y|maxy=", "Maximum Y coordinate value of the extent to cache", Y => double.TryParse(Y, out maxy)},
                {"p|parallelops=", "Limits the number of concurrent operations run by TileCutter", p => int.TryParse(p, out maxDegreeOfParallelism)},
                {"r|replace=", "Delete existing tile cache MBTiles database if already present and create a new one.", r => Boolean.TryParse(r, out replaceExistingCacheDB)}
            };
            options.Parse(args);

            if (showHelp)
            {
                ShowHelp(options);
                return;
            }

            //Get the sqlite db file location from the config
            //if not provided, default to executing assembly location
            string dbLocation = Path.Combine(localCacheDirectory, "tilecache.mbtiles");

            //if the user option to delete existing tile cache db is true delete it or else add to it
            if (replaceExistingCacheDB && File.Exists(dbLocation))
                File.Delete(dbLocation);

            //Connect to the sqlite database
            string connectionString = string.Format("Data Source={0}; FailIfMissing=False", dbLocation);
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                //Check if the 'metadata' table exists, if not create it
                var rows = connection.GetSchema("Tables").Select("Table_Name = 'metadata'");
                if (!rows.Any())
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "CREATE TABLE metadata (name text, value text);";
                    command.ExecuteNonQuery();
                    connection.Execute("CREATE UNIQUE INDEX name ON metadata ('name')");
                    connection.Execute(@"INSERT INTO metadata(name, value) values (@a, @b)",
                        new[] { 
                        new { a = "name", b = "cache" }, 
                        new { a = "type", b = "overlay" }, 
                        new { a = "version", b = "1" },
                        new { a = "description", b = "some info here" },
                        new { a = "format", b = "png" },
                        new { a = "bounds", b = string.Format("{0},{1},{2},{3}", minx, miny, maxx, maxy)}
                    });
                }

                //Check if the 'tiles' table exists, if not create it
                rows = connection.GetSchema("Tables").Select("Table_Name = 'tiles'");
                if (!rows.Any())
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "CREATE TABLE tiles (zoom_level integer, tile_column integer, tile_row integer, tile_data blob);";
                    command.ExecuteNonQuery();
                }
            }

            //prepare insert tile sql template
            string insertTileSqlTemplate = "INSERT INTO tiles(zoom_level, tile_column, tile_row, tile_data) values(@zoom, @col, @row, @data)";

            if (!Directory.Exists(localCacheDirectory))
                Directory.CreateDirectory(localCacheDirectory);

            Console.WriteLine("Output Cache Directory is: " + localCacheDirectory);
            var tiles = GetTiles(minz, maxz, minx, miny, maxx, maxy);
            ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism};
            Parallel.ForEach(tiles, parallelOptions, (tile) => {
                string tileUrl = helper.GetAGSDynamicUrlAddress(tile.Level, tile.Row, tile.Column, defaultMapServiceUrl);
                WebClient client = new WebClient();
                byte[] image = client.DownloadData(tileUrl);
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    connection.Execute(insertTileSqlTemplate, new { zoom = tile.Level, col = tile.Column, row = tile.Row, data = image });
                }
                client.Dispose();
                Console.WriteLine(string.Format("Tile Level:{0}, Row:{1}, Column:{2} downloaded.", tile.Level, tile.Row, tile.Column));
            });

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                connection.Execute("CREATE UNIQUE INDEX map_index on tiles (zoom_level, tile_column, tile_row)");
                connection.Close();
            }
            Console.WriteLine("All Done !!!");
        }

        static IEnumerable<TileCoordinate> GetTiles(int minz, int maxz, double minx, double miny, double maxx, double maxy)
        {
            for (int i = minz; i <= maxz; i++)
            {
                var tiles = helper.GetTilesFromLatLon(i, minx, miny, maxx, maxy);
                foreach (var coord in tiles)
                {
                    yield return new TileCoordinate() { Level = i, Column = coord.X, Row = coord.Y };
                }
            }
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: greet [OPTIONS]+ message");
            Console.WriteLine("Greet a list of individuals with an optional message.");
            Console.WriteLine("If no message is specified, a generic greeting is used.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }
}
