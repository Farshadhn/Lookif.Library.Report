using Lookif.Library.Common.Utilities;
using Lookif.Library.Report.Enums;
using Lookif.Library.Report.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Lookif.Library.Report.Interfaces;

public interface IReportFilter
{
    public List<ReportFilter> GetReportFilter()
    {
        try
        {
            var listOfFilter = new List<ReportFilter>();
            var lst = this.GetProperties();
            foreach (var item in lst)
            {
                var type = item.PropertyType;
                var data = item.GetValue(this, null)!;
                if (data is null)
                    continue;
                var value = TypeDescriptor.GetConverter(type).ConvertFromInvariantString(data?.ToString());
                var defaultValue = (type == typeof(string)) ? "" : Activator.CreateInstance(type);
                if (value.Equals(defaultValue))
                    continue;
                if (type == typeof(DateTime))
                    listOfFilter.Add(new ReportFilter()
                    {
                        Property = item.Name.ToString(),
                        Value = value.ToString(),
                        FilterFunction = ExpressionFunction.EqualDateTime
                    });

                else if (value.ToString() != defaultValue.ToString())
                    listOfFilter.Add(new ReportFilter()
                    {
                        Property = item.Name.ToString(),
                        Value = value.ToString(),
                        FilterFunction = (type == typeof(string)) ? ExpressionFunction.Contains : ExpressionFunction.Equal,
                        ExpressionRelation = ExpressionFunction.And
                    });


            }
            return listOfFilter;
        }
        catch (Exception e)
        {

            throw;
        }
      

    }
}
