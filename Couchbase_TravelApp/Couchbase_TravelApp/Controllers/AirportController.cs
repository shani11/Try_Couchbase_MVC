using Couchbase.Core;
using Couchbase.Extensions.DependencyInjection;
using Couchbase_TravelApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using HttpGetAttribute = Microsoft.AspNetCore.Mvc.HttpGetAttribute;

namespace Couchbase_TravelApp.Controllers
{

    public class AirportController : Controller
    {
        private readonly IBucket _bucket;
        public AirportController(IBucketProvider bucketProvider)
        {
            _bucket = bucketProvider.GetBucket("travel-sample", "Administrator");
        }
       
        [HttpGet("airports/Search")]
        public async Task<IActionResult> Search(string search="")
        {
            string query;
            IEnumerable<Airport> airports;
            if (IsFaaCode(search))
            {
                query = $"SELECT airportname FROM `travel-sample` WHERE type = 'airport' AND faa = '{search.ToUpper()}'";
                airports =  _bucket.QueryAsync<Airport>(query).Result;
                
            }
            else if (IsIcaoCode(search))
            {
                query = $"SELECT airportname FROM `travel-sample` WHERE type = 'airport' AND icao = '{search.ToUpper()}'";
                airports = await _bucket.QueryAsync<Airport>(query);
            }
            else
            {
                query = $"SELECT airportname FROM `travel-sample` WHERE type = 'airport' AND airportname LIKE '%{search}%'";
                airports = await _bucket.QueryAsync<Airport>(query);
            }

            return View(airports);
        }

        private static bool IsFaaCode(string search)
        {
            return search.Length == 3;
        }

        private static bool IsIcaoCode(string search)
        {
            return search.Length == 4 && (search == search.ToLower() || search == search.ToUpper());
        }

    }

}