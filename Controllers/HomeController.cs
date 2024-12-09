using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using jquerydatatable_demo.Models;
using System.Dynamic;
using System.Globalization;
using System.Data;
using System.Linq;

namespace jquerydatatable_demo.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult PagingDemo()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    public IActionResult GetTableData()
    {
        // Define dynamic columns
        var columns = new List<Column>
        {
            new Column { Id = 1, ColName = "Id", DataType = "int" },
            new Column { Id = 2, ColName = "Name", DataType = "varchar" },
            new Column { Id = 3, ColName = "Dob", DataType = "datetime" },
            new Column { Id = 4, ColName = "City", DataType = "varchar" }
        };

        // Define dynamic rows
        var rowsRaw = new List<object>
        {
            new { Id = 1, Name = "John Doe", Dob = "1995/06/12", City = "New York" },
            new { Id = 2, Name = "Jane Smith", Dob = "1998/11/06", City = "Los Angeles" },
            new { Id = 3, Name = "Mike Johnson", Dob = "1985/09/18", City = "Chicago" }
        };

        for (int i = 4; i < 10000; i++)
        {
            rowsRaw.Add(new { Id = i, Name = "John Doe" + " " + i, Dob = "1995/06/12", City = "New York" });
        }

        var rows = new List<object>();


        foreach (var item in rowsRaw)
        {
            dynamic row = new ExpandoObject();
            foreach (var col in columns)
            {
                var colValue = item.GetType()?.GetProperty(col.ColName)?.GetValue(item, null);
                switch (col.DataType.ToLower())
                {
                    case "int":
                        ((IDictionary<string, object>)row).Add(col.ColName, Convert.ToInt32(colValue!));
                        break;
                    case "datetime":
                        ((IDictionary<string, object>)row).Add(col.ColName, Convert.ToDateTime(colValue!));
                        break;
                    default:
                        ((IDictionary<string, object>)row).Add(col.ColName, colValue!);
                        break;
                };
            }
            rows.Add(row);
        }
        return Json(new { columns, rows });
    }

    [HttpPost]
    public IActionResult GetPagintedTableData([FromForm] IFormCollection form)
    {
        // Safely retrieve values with null checks
        int draw = form.ContainsKey("draw") && int.TryParse(form["draw"], out int parsedDraw) ? parsedDraw : 0;
        int start = form.ContainsKey("start") && int.TryParse(form["start"], out int parsedStart) ? parsedStart : 0;
        int length = form.ContainsKey("length") && int.TryParse(form["length"], out int parsedLength) ? parsedLength : 10;

        string searchValue = form.ContainsKey("search[value]") ? form["search[value]"].ToString() : string.Empty;
        string sortColumnIndex = form.ContainsKey("order[0][column]") ? form["order[0][column]"].ToString() : string.Empty;
        string? sortColumn = !string.IsNullOrEmpty(sortColumnIndex)
            ? form[$"columns[{sortColumnIndex}][data]"]
            : string.Empty;
        string sortDirection = form.ContainsKey("order[0][dir]") ? form["order[0][dir]"].ToString() : "asc";


        var dataset = GetDataSet();
        
        // Define dynamic columns
        var columnsRaw = new List<Column>();        

        var columnsDict=GetListFromDataSet(dataset.Tables[0]);

        foreach (var item in columnsDict)
        {
            var col = new Column();
            var dictionary = item as Dictionary<string, object>;
            col.Id = (int)dictionary!.GetValueOrDefault("Id", -1);
            col.ColName = dictionary!.GetValueOrDefault("ColName", "").ToString() ?? "";
            col.DataType = dictionary!.GetValueOrDefault("DataType", "").ToString() ?? "";
            columnsRaw.Add(col);
        }

        // Define dynamic rows
        var rowsRaw = new List<object>
        {
            new { Id = 1, Name = "John Doe", Dob = "1995/06/12", City = "New York" },
            new { Id = 2, Name = "Jane Smith", Dob = "1998/11/06", City = "Los Angeles" },
            new { Id = 3, Name = "Mike Johnson", Dob = "1985/09/18", City = "Chicago" }
        };

        rowsRaw = GetListFromDataSet(dataset.Tables[1]);


        // Filter data
        var filteredData = Filter(rowsRaw, searchValue);

        // Sort data
        if (!string.IsNullOrEmpty(sortColumn))
        {
            sortColumn = new CultureInfo("en-US").TextInfo.ToTitleCase(sortColumn);
            filteredData = sortDirection == "asc"
                ? filteredData.OrderBy(d => ((IDictionary<string, object>)d)[sortColumn]).ToList()
                : filteredData.OrderByDescending(d => ((IDictionary<string, object>)d)[sortColumn]).ToList();
        }

        var rows = new List<object>();


        foreach (var item in filteredData)
        {
             var dictionary = item as Dictionary<string, object>;
            dynamic row = new ExpandoObject();
            foreach (var col in columnsRaw)
            {
                var colValue =dictionary?[col.ColName]??"";
                switch (col.DataType.ToLower())
                {
                    case "int":
                        ((IDictionary<string, object>)row).Add(col.ColName, Convert.ToInt32(colValue!));
                        break;
                    case "datetime":
                        ((IDictionary<string, object>)row).Add(col.ColName, Convert.ToDateTime(colValue!));
                        break;
                    default:
                        ((IDictionary<string, object>)row).Add(col.ColName, colValue!);
                        break;
                };
            }
            rows.Add(row);
        }

        // Paginate data
        var paginatedData = filteredData
            .Skip(start)
            .Take(length)
            .ToList();

        var columns = columnsRaw;
        return Json(new
        {
            columns,
            draw,
            recordsTotal = rowsRaw.Count,
            recordsFiltered = filteredData.Count(),
            data = paginatedData
        });
    }

    private IEnumerable<object> Filter(List<object> list, string searchValue)
    {
        if (string.IsNullOrEmpty(searchValue))
            return list; // If no search value is provided, return the original list.

        return list.Where(item =>
        {
           var dictionary= item as Dictionary<string, object>;
            // Check all properties of the object
           return dictionary.Any(kv => (kv.Value??"").ToString().ToLower().Contains(searchValue.ToLower()));
 // Exclude the item if no properties match
        });
    }

    private List<object> GetListFromDataSet(DataTable dataTable)
    {
        List<object> result = new List<object>();
        var columns = dataTable.Columns;

        foreach (DataRow row in dataTable.Rows)
        {
            Dictionary<string, object> rowData = new Dictionary<string, object>();

            foreach (var columnName in columns)
            {
                rowData[columnName.ToString()] = row[columnName.ToString()];
            }
            result.Add(rowData);
        }
        return result;
    }

    private DataSet GetDataSet()
    {
        DataSet dataSet = new DataSet();
        //columns
        DataTable columnsTable = new DataTable("columnsTable");
        columnsTable.Columns.Add("Id", typeof(int));
        columnsTable.Columns.Add("ColName", typeof(string));
        columnsTable.Columns.Add("DataType", typeof(string));
        columnsTable.Rows.Add(1, "Id", "int");
        columnsTable.Rows.Add(1, "Name", "varchar");
        columnsTable.Rows.Add(1, "Dob", "datetime");
        columnsTable.Rows.Add(1, "City", "varchar");
        dataSet.Tables.Add(columnsTable);

        DataTable rowsTable = new DataTable("rowsTable");
        rowsTable.Columns.Add("Id", typeof(int));
        rowsTable.Columns.Add("Name", typeof(string));
        rowsTable.Columns.Add("Dob", typeof(DateTime));
        rowsTable.Columns.Add("City", typeof(string));

        for (int i = 1; i <= 10000; i++)
        {
            rowsTable.Rows.Add(i, "John Doe" + " " + i, "1995/06/12", "New York");
        }

        dataSet.Tables.Add(rowsTable);

        return dataSet;
    }

}
