using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Couchbase.Core;
using Couchbase.Extensions.DependencyInjection;
using Couchbase.N1QL;
using Couchbase_TravelApp.Models;
using GeoCoordinatePortable;
using Microsoft.AspNetCore.Mvc;
using try_cb_dotnet.Models;

namespace Couchbase_TravelApp.Controllers
{
    public class FlightController : Controller
    {
        private readonly IBucket _bucket;
        private readonly Random _random = new Random();
        public FlightController(IBucketProvider bucketProvider)
        {
            _bucket = bucketProvider.GetBucket("travel-sample", "Administrator");
        }
        [HttpGet]
        public IActionResult GetFlights()
        {
            List<Route> Listroutes = new List<Route>();
            IEnumerable<Route> routes = Listroutes;
           
            return View(routes);
        }
        [HttpPost]
        public async Task<IActionResult> GetFlights(string from, string to, string leave)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                return Ok("Missing or invalid from and/or to airports");
            }

            DateTime leaveDate;
            if (!DateTime.TryParse(leave, out leaveDate))
            {
                return Ok("Missing or invalid leave date");
            }

            var queries = new List<string>();
            var dayOfWeek = (int)leaveDate.DayOfWeek + 1; // Get weekday number

            var airportQuery = new QueryRequest()
                .Statement("SELECT faa AS fromAirport, geo.lat, geo.lon " +
                           "FROM `travel-sample` " +
                           "WHERE airportname = $1 " +
                           "UNION " +
                           "SELECT faa AS toAirport, geo.lat, geo.lon " +
                           "FROM `travel-sample` " +
                           "WHERE airportname = $2;")
                .AddPositionalParameter(from, to);

            queries.Add(airportQuery.GetOriginalStatement());

            var airportQueryResult = await _bucket.QueryAsync<dynamic>(airportQuery);
            if (!airportQueryResult.Success)
            {
                return Ok("Interval Server error!!");
            }

            if (airportQueryResult.Rows.Count != 2)
            {
                return Ok("Could not find both source from and destination to airports");
            }

            dynamic fromAirport = airportQueryResult.Rows.First(x => x.fromAirport != null);
            dynamic toAirport = airportQueryResult.Rows.First(x => x.toAirport != null);

            var fromCoordinate = new GeoCoordinate((double)fromAirport.lat, (double)fromAirport.lon);
            var toCoordinate = new GeoCoordinate((double)toAirport.lat, (double)toAirport.lon);
            var distance = fromCoordinate.GetDistanceTo(toCoordinate);
            var flightTime = Math.Round(distance / 800, 2);

            var flightQuery = new QueryRequest()
                .Statement("SELECT a.name, s.flight, s.utc, r.sourceairport, r.destinationairport, r.equipment " +
                           "FROM `travel-sample` AS r " +
                           "UNNEST r.schedule AS s " +
                           "JOIN `travel-sample` AS a ON KEYS r.airlineid " +
                           "WHERE r.sourceairport = $1 " +
                           "AND r.destinationairport = $2 " +
                            "AND s.day = $3 " +
                           "ORDER BY a.name ASC;")
                .AddPositionalParameter((string)fromAirport.fromAirport, (string)toAirport.toAirport,dayOfWeek);
            queries.Add(flightQuery.GetOriginalStatement());

            var flightQueryResult = await _bucket.QueryAsync<Route>(flightQuery);
            if (!flightQueryResult.Success)
            {
                return Ok(flightQueryResult.Message);
            }

            var flights = flightQueryResult.Rows;
            foreach (var flight in flights)
            {
                flight.FlightTime = flightTime;
                flight.Price = _random.Next(2000);
            }

            IEnumerable<Route> Listflights = flights;
           // return Content(HttpStatusCode.OK, new Result(flights, queries.ToArray()));
            return View(Listflights);
        }
    }
}