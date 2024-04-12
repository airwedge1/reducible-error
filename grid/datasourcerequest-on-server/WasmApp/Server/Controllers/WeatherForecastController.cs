using WasmApp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
// these two using statements provide the data source operations
using Telerik.DataSource;
using Telerik.DataSource.Extensions;
using System.Text.Json;
using WasmApp.Server.Models;

namespace WasmApp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

        private readonly ILogger<WeatherForecastController> logger;

        private WeatherForecastContext _db;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, WeatherForecastContext db)
        {
            this.logger = logger;
            _db = db;


        }

        // this static list acts as our "database" in this sample
        private static List<WeatherForecastModel> _forecasts { get; set; }

        [HttpPost]
        public async Task<DataEnvelope<WeatherForecastModel>> Post([FromBody] DataSourceRequest gridRequest)
        {
            // generate some data for the sake of this demo
            //if (_forecasts == null)
            //{
            //    var rng = new Random();
            //    var startDate = DateTime.Now.Date;
            //    _forecasts = Enumerable.Range(1, 150).Select(index => new WeatherForecastModel
            //    {
            //        Id = index,
            //        Date = startDate.AddDays(index),
            //        TemperatureC = rng.Next(-20, 55),
            //        Summary = Summaries[rng.Next(Summaries.Length)]
            //    }).ToList();
            //}

            //// we will cast the data to an IQueriable to simulate an actual database (EF) service
            //// in a real case, you would be fetching the data from the service, not generating it here
            //IQueryable<WeatherForecastModel> queriableData = _forecasts.AsQueryable();

            try
            {

                var queriableData = _db.WeatherForecasts.Select(p => new WeatherForecastModel()
                {
                    Date = p.Date.Value,
                    Id = p.Id,
                    Summary = p.Summary,
                    TemperatureC = p.TemperatureC.Value
                });




                // use the Telerik DataSource Extensions to perform the query on the data
                // the Telerik extension methods can also work on "regular" collections like List<T> and IQueriable<T>
                DataSourceResult processedData = await queriableData.ToDataSourceResultAsync(gridRequest);


                DataEnvelope<WeatherForecastModel> dataToReturn;

                if (gridRequest.Groups.Count > 0)
                {
                    // If there is grouping, use the field for grouped data
                    // The app must be able to serialize and deserialize it
                    // Example helper methods for this are available in this project
                    // See the GroupDataHelper.DeserializeGroups and JsonExtensions.Deserialize methods
                    dataToReturn = new DataEnvelope<WeatherForecastModel>
                    {
                        GroupedData = processedData.Data.Cast<AggregateFunctionsGroup>().ToList(),
                        TotalItemCount = processedData.Total
                    };
                }
                else
                {
                    // When there is no grouping, the simplistic approach of 
                    // just serializing and deserializing the flat data is enough
                    dataToReturn = new DataEnvelope<WeatherForecastModel>
                    {
                        CurrentPageData = processedData.Data.Cast<WeatherForecastModel>().ToList(),
                        TotalItemCount = processedData.Total
                    };
                }

                return dataToReturn;
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        // for brevity, CUD operations are not implemented, only Read
    }
}
