using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using jquerydatatable_demo.Models;
using System.Dynamic;

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
        rowsRaw.Add(new { Id = i, Name = "John Doe"+" "+i, Dob = "1995/06/12", City = "New York" });
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
}
