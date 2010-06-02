using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

/*
 * LONG GRID GAP: 0.00240325927734 (5239)
 * LAT GRID GAP: 0.0011107442982  (7879)

 * 7879 x 5239 = 41,278,081 points to check.
 * Not massive, each point should intersect with 0 or more postal sectors unless its outside a given range.
 * If outside a given distance range, then it should be not assigned to any postcode.
 */

namespace PostcodeMapper {
    class Point{
        public double Long;
        public double Lat;

        public override string ToString() {
            return "Lat: " + Lat + ", Long: " + Long;
        }
    }

    class Polygon {
        public List<Point> Points;

        public Polygon() { Points = new List<Point>(); }
    }

    class Postcode {
        public string Code;
        public Point Location;
        public Sector Sector;
    }

    class Sector {
        public string Code;
        public List<Point> Points; // raw points of all postcodes within the sector
        public List<Edge> Edges; // the edges before they are joined together to form a polygon
        public List<Polygon> Polygon; // the joined up edges, normally only one polygon, but potentially more

        public Sector() {
            Points = new List<Point>();
            Edges = new List<Edge>();
            Polygon = new List<Polygon>();
        }
    }

    class MapPoint {
        public Sector Sector;
        public Point Location;
    }

    class Edge {
        public List<Sector> Sectors;
        public Point Location;

        public bool SectorExists(Sector s) {
            for (int i = 0; i < Sectors.Count; i++) if (Sectors[i] == s) return true;
            return false;
        }

        public Edge() { Sectors = new List<Sector>(); }
    }

    class Program {
        static List<Postcode> Postcodes;
        static List<Sector> Sectors;

        static Postcode[] PostcodesArray;
        static Sector[] SectorsArray;

        static double rad2deg(double rad) {
            return (rad / Math.PI * 180.0);
        }

        static double deg2rad(double deg) {
            return (deg * Math.PI / 180.0);
        }

        static double distance(double lat1, double lon1, double lat2, double lon2, char unit) {
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist * 60 * 1.1515;
            if (unit == 'K') {
                dist = dist * 1.609344;
            }
            else if (unit == 'N') {
                dist = dist * 0.8684;
            }
            return (dist);
        }

        static Sector NearestSectorToPoint(Point p) {
            double min_dist = 100;
            Postcode min_postcode = null;
            for (int i = 0; i < PostcodesArray.Length; i++) {
                if (PostcodesArray[i].Location.Lat < p.Lat + 0.1 && PostcodesArray[i].Location.Lat > p.Lat - 0.1) {
                    if (PostcodesArray[i].Location.Long < p.Long + 0.1 && PostcodesArray[i].Location.Long > p.Long - 0.1) {
                        if (distance(PostcodesArray[i].Location.Lat, PostcodesArray[i].Location.Long, p.Lat, p.Long, 'K') < min_dist) {
                            min_dist = distance(PostcodesArray[i].Location.Lat, PostcodesArray[i].Location.Long, p.Lat, p.Long, 'K');
                            min_postcode = PostcodesArray[i];
                        }
                    }
                }
            }
            if(min_postcode != null && min_dist < 1){
                return min_postcode.Sector;
            }else{
                return null;
            }
        }

        static Sector GetSectorByCode(String Code) {
            for (int i = 0; i < SectorsArray.Length; i++) {
                if (SectorsArray[i].Code == Code) {
                    return SectorsArray[i];
                }
            }
            return null;
        }

        static void Main(string[] args) {
            Postcodes = new List<Postcode>();
            Sectors = new List<Sector>();

            double min_long = 100;
            double max_long = -100;
            double min_lat = 100;
            double max_lat = -100;

            double grid_long = 0.00240325927734;
            double grid_lat = 0.0011107442982;

            grid_long = grid_long * 1.5;
            grid_lat = grid_lat * 1.5;

            Sector CurSector = null;

            System.IO.DirectoryInfo d = new System.IO.DirectoryInfo("C:\\Documents and Settings\\David.hallettarendt\\My Documents\\Downloads\\Code-Point Open\\data\\CSV");
            System.IO.FileInfo[] files = null;
            try {
                files = d.GetFiles("*.csv");
                TDPG.GeoCoordConversion.GridReference gr;
                TDPG.GeoCoordConversion.PolarGeoCoordinate pr;
                for (int i = 0; i < files.Length; i++) {
                    StreamReader re = File.OpenText(files[i].FullName);
                    string csv_line = null;
                    string[] data = null;
                    while ((csv_line = re.ReadLine()) != null) {
                        data = csv_line.Split(',');
                        if (data.Length == 19) {
                            // convert location to lat / long
                            if (long.Parse(data[10]) > 0 && long.Parse(data[11]) > 0) {
                                gr = new TDPG.GeoCoordConversion.GridReference(long.Parse(data[10]), long.Parse(data[11]));
                                pr = TDPG.GeoCoordConversion.GridReference.ChangeToPolarGeo(gr);
                                TDPG.GeoCoordConversion.PolarGeoCoordinate.ChangeCoordinateSystem(pr, TDPG.GeoCoordConversion.CoordinateSystems.WGS84);
                                // add postcode
                                Postcode p = new Postcode();
                                p.Code = data[0].Replace("\"", "");
                                p.Location = new Point();
                                p.Location.Lat = pr.Lat;
                                p.Location.Long = pr.Lon;
                                if (pr.Lat < min_lat) min_lat = pr.Lat;
                                if (pr.Lat > max_lat) max_lat = pr.Lat;
                                if (pr.Lon < min_long) min_long = pr.Lon;
                                if (pr.Lon > max_long) max_long = pr.Lon;
                                Postcodes.Add(p);
                                if (CurSector == null || CurSector.Code != p.Code.Substring(0, 5)) {
                                    CurSector = new Sector();
                                    CurSector.Code = p.Code.Substring(0, 5);
                                    Sectors.Add(CurSector);
                                }
                                CurSector.Points.Add(p.Location);
                                p.Sector = CurSector;
                            }
                        }
                    }
                    re.Close();                    
                }
                Console.WriteLine("Total Sectors: " + Sectors.Count);

                PostcodesArray = Postcodes.ToArray();
                SectorsArray = Sectors.ToArray();

                Point p1, p2, p3, p4, mid_p;
                Sector s1, s2, s3, s4;
                p1 = new Point();
                p2 = new Point();
                p3 = new Point();
                p4 = new Point();

                int total_cycles = (int)((((max_lat - min_lat) / grid_lat) + 1) * (((max_long - min_long) / grid_long) + 1));
                int cycle_count = 0;

                for (double x = min_lat - grid_lat; x < max_lat + grid_lat; x = x + (grid_lat * 2)) {
                    // Console.WriteLine(cycle_count + "/" + total_cycles);
                    for (double y = min_long - grid_long; y < max_long + grid_long; y = y + (grid_long * 2)) {
                        cycle_count++;    
                        p1.Lat = x; p1.Long = y;
                        p2.Lat = x + grid_lat; p2.Long = y;
                        p3.Lat = x; p3.Long = y + grid_long;
                        p4.Lat = x + grid_lat; p4.Long = y + grid_long;
                        mid_p = new Point();
                        mid_p.Lat = x + (grid_lat / 2);
                        mid_p.Long = y + (grid_long / 2);
                        s1 = NearestSectorToPoint(p1);
                        s2 = NearestSectorToPoint(p2);
                        s3 = NearestSectorToPoint(p3);
                        s4 = NearestSectorToPoint(p4);
                        if (!(s1 == s2 && s2 == s3 && s3 == s4)) {
                            Edge e = new Edge();
                            e.Location = mid_p;
                            if (s1 != null) {
                                e.Sectors.Add(s1);
                                s1.Edges.Add(e);
                                Console.WriteLine(s1.Code + "," + e.Location.Lat + "," + e.Location.Long);
                            }
                            if (s2 != null && !e.SectorExists(s2)) {
                                e.Sectors.Add(s2);
                                s2.Edges.Add(e);
                                Console.WriteLine(s2.Code + "," + e.Location.Lat + "," + e.Location.Long);
                            }
                            if (s3 != null && !e.SectorExists(s3)) {
                                e.Sectors.Add(s3);
                                s3.Edges.Add(e);
                                Console.WriteLine(s3.Code + "," + e.Location.Lat + "," + e.Location.Long);
                            }
                            if (s4 != null && !e.SectorExists(s4)) {
                                e.Sectors.Add(s4);
                                s4.Edges.Add(e);
                                Console.WriteLine(s4.Code + "," + e.Location.Lat + "," + e.Location.Long);
                            }
                        }
                    }
                }

                //Sector r = GetSectorByCode("SO316");
                //if (r != null) {
                //    for (int i = 0; i < r.Edges.Count; i++) {
                //        Console.WriteLine(r.Edges[i].Location.Lat + "," + r.Edges[i].Location.Long);
                //    }
                //} else {
                //    Console.WriteLine("Could not find SO316");
                //}
            }catch (UnauthorizedAccessException e) {
                Console.WriteLine("Error: " + e.Message);
            }
        }
    }
}
