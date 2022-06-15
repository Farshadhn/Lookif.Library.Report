using Lookif.Library.Report.Enums;
using Lookif.Library.Report.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Lookif.Library.Report.Services;

public static class ExpressionTreeManufacturer
{
    public static Func<T, bool> GetLambdaFunc<T>(this List<ReportFilter> filters)
        where T : class
    {
        Expression exp = null;

        var parameter = Expression.Parameter(typeof(T));

        var firstFilter = filters.First();

        foreach (var filter in filters)
        {
            Expression parameterInnerProperty = parameter;

            var fields = filter.Property.Split('.');

            var types = Array.Empty<Type>();

            var filterField = fields.Last();

            Type cls;

            if (fields.Length > 1)
            {
                types = typeof(T).GetTypeInfo().Assembly
                    .GetTypes()
                    .Where(type => fields.Contains(type.Name))
                    .ToArray();

                cls = types.FirstOrDefault(t => t.Name == fields[^2]);
            }
            else
                cls = typeof(T);

            var nullProperty = false;
            var realType = cls.GetProperty(filterField).PropertyType;
            var notNullRealType = realType;
            if (Nullable.GetUnderlyingType(realType) != null)
            {
                nullProperty = true;
                notNullRealType = Nullable.GetUnderlyingType(realType);
            }

            var value = TypeDescriptor.GetConverter(notNullRealType).ConvertFromInvariantString(filter.Value);

            Expression classNullExp = null;
            foreach (var member in fields)
            {
                parameterInnerProperty = Expression.PropertyOrField(parameterInnerProperty, member);
                if (member == filterField) continue;

                var classNotNull = Expression.NotEqual(
                    parameterInnerProperty,
                    Expression.Constant(null, types.FirstOrDefault(t => t.Name == member)));

                classNullExp = classNullExp is null ? classNotNull : Expression.And(classNullExp, classNotNull);
            }

            var expConstant = Expression.Constant(value, realType);
            Expression eqExp = null;

            Expression fieldNullExp = null;
            if (nullProperty)
            {
                var fieldNotNull = Expression.NotEqual(parameterInnerProperty, Expression.Constant(null, realType));
                fieldNullExp = fieldNotNull;
            }

            //TODO : Set Other Functions
            switch (filter.FilterFunction)
            {
                case ExpressionFunction.Equal:
                    eqExp = Expression.Equal(parameterInnerProperty, expConstant);
                    break;
                case ExpressionFunction.GreaterThan:
                    eqExp = Expression.GreaterThan(parameterInnerProperty, expConstant);
                    break;
                case ExpressionFunction.Contains:
                    {
                        MethodInfo method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                        eqExp = Expression.Call(parameterInnerProperty, method, expConstant);
                    }
                    break;
                case ExpressionFunction.EqualDateTime:
                    {
                        DateTime parsedDate = (DateTime)Convert.ChangeType(value, typeof(DateTime));
                        var dayStart = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day, 0, 0, 0, 0);
                        var dayEnd = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day, 23, 59, 59, 999);

                        var left = Expression.GreaterThanOrEqual(parameterInnerProperty, Expression.Constant(dayStart));
                        var right = Expression.LessThanOrEqual(parameterInnerProperty, Expression.Constant(dayEnd));
                        eqExp = Expression.And(left, right);


                    }
                    break;
                case ExpressionFunction.LessThan:
                    eqExp = Expression.LessThan(parameterInnerProperty, expConstant);
                    break;
            }

            if (filter.FilterFunction == ExpressionFunction.Equal)
            {
                eqExp = Expression.Equal(parameterInnerProperty, expConstant);
            }

            if (fieldNullExp is not null)
                eqExp = Expression.AndAlso(fieldNullExp, eqExp);

            if (classNullExp is not null)
                eqExp = Expression.AndAlso(classNullExp, eqExp);

            //TODO : Set And or Or for expressions
            if (filter == firstFilter)
            {
                exp = eqExp;
            }
            else
            {
                switch (filter.ExpressionRelation)
                {
                    case ExpressionFunction.Or:
                        exp = Expression.Or(exp, eqExp);
                        break;
                    default:
                        exp = Expression.And(exp, eqExp);
                        break;
                }
            }
        }

        var lambda = Expression.Lambda<Func<T, bool>>(exp, parameter).Compile();
        return lambda;
    }


    
     


}
