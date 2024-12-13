namespace jquerydatatable_demo.Models;
public class FormField
{
    public string Name { get; set; }          // Field name
    public string Label { get; set; }         // Field label
    public string Type { get; set; }     
    public string DataType { get; set; }      // Field type (e.g., text, number, date, dropdown, checkbox, radio)
    public bool IsRequired { get; set; }      // Whether the field is required
    public string Placeholder { get; set; }  // Placeholder text
    public object Value { get; set; }         // Field value
    public List<string> Options { get; set; } // Options for dropdown, checkbox, or radio

    public FormField()
    {
        Options = new List<string>();
    }
}
