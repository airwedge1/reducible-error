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

using Microsoft.EntityFrameworkCore.DynamicLinq;
using Telerik.Pivot.Core.Filtering;
using Telerik.Blazor.Components;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.Internal;

using LinqKit;
using Telerik.SvgIcons;

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
            try
            {

                var queryable = _db.WeatherForecasts.Select(p => new WeatherForecastModel()
                {
                    Date = p.Date.Value,
                    Id = p.Id,
                    Summary = p.Summary,
                    TemperatureC = p.TemperatureC.Value
                });

                

                //You have to clear the aggregates request or else it'll error out
                gridRequest.Aggregates.Clear();

                DataSourceResult processedData = await queryable.ToDataSourceResultAsync(gridRequest);

                DataEnvelope<WeatherForecastModel> dataToReturn;

                if (gridRequest.Groups.Count > 0)
                {
                    var x = new List<AggregateResult>();

                    dataToReturn = new DataEnvelope<WeatherForecastModel>
                    {
                        GroupedData = processedData.Data.Cast<AggregateFunctionsGroup>().ToList(),
                        TotalItemCount = processedData.Total
                    };

                    //Teleriks ToDataSourceResult function does not work.  It throws an error if Aggregates is populated
                    //This was adopted from https://stackoverflow.com/questions/62265283/entity-framework-core-dynamic-groupby
                    //It is complex, confusing and always needs to use the 3rd party AsExpandableEFCore from LinqKit library

                    var groupBy1 = GetGroupBy(gridRequest.Groups.First().Member);

                    if(gridRequest.Groups.Count()==1)
                    {
                        Expression<Func<WeatherForecastModel, string>> groupBy = g => groupBy1.Invoke(g);

                        dataToReturn.AggregateResults = queryable.AsExpandableEFCore()
                            .GroupBy(groupBy)
                            .Select(z => new EnvelopeAggregateResult
                            {
                                GroupKeys = new DoubleGroup() { Group1 = z.Key },
                                TempCSum = z.Sum(s => s.TemperatureC)
                            }).ToList();
                    }
                    else if (gridRequest.Groups.Count() == 2)
                    {

                        var groupBy2 = GetGroupBy(gridRequest.Groups[1].Member); ;

                        Expression<Func<WeatherForecastModel, DoubleGroup>> groupBy = g => new DoubleGroup { Group1 = groupBy1.Invoke(g), Group2 = groupBy2.Invoke(g) };

                        dataToReturn.AggregateResults = queryable.AsExpandableEFCore()
                            .GroupBy(groupBy)
                            .Select(z => new EnvelopeAggregateResult
                            {
                                GroupKeys = z.Key,
                                TempCSum = z.Sum(s => s.TemperatureC)
                            }).ToList();
                    }
                    else
                    {
                        throw new Exception("Can only group by up to 2 fields");
                    }

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

        private static Expression<Func<WeatherForecastModel, string>> GetGroupBy(string column)
        {
            if (column == nameof(WeatherForecastModel.Date))
            {
                return g => g.Date.ToString();
            }
            else if(column == nameof(WeatherForecastModel.Summary))
            {
                return g => g.Summary.ToString();
            }
            else if (column == nameof(WeatherForecastModel.TemperatureC))
            {
                return g => g.TemperatureC.ToString();
            }
            else
            {
                throw new Exception("Can only group by date, summary or temperatureC");
            }
        }
    }
}
