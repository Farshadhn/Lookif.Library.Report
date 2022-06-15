using Lookif.Library.Report.Enums; 

namespace Lookif.Library.Report.Models;

public class ReportFilter  
{
    public ExpressionFunction? ExpressionRelation { get; set; }
    public ExpressionFunction FilterFunction { get; set; }
    public string Property { get; set; }
    public string Value { get; set; }
}
