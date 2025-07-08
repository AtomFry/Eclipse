using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Eclipse.Models;

namespace Eclipse.Helpers
{
    public static class GameListExtensions
    {
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder(source, property, "OrderBy");
        }

        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder(source, property, "OrderByDescending");
        }

        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string property)
        {
            return ApplyOrder(source, property, "ThenBy");
        }

        public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string property)
        {
            return ApplyOrder(source, property, "ThenByDescending");
        }

        static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> source, string property, string methodName)
        {
            string[] props = property.Split('.');
            Type type = typeof(T);
            ParameterExpression arg = Expression.Parameter(type, "x");
            Expression expr = arg;
            foreach (string prop in props)
            {
                PropertyInfo pi = type.GetProperty(prop);
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }
            Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            LambdaExpression lambda = Expression.Lambda(delegateType, expr, arg);

            // Get the specific method we need more safely
            var orderMethod = typeof(Queryable).GetMethod(methodName, new Type[] 
            { 
                typeof(IQueryable<>).MakeGenericType(typeof(T)), 
                typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(typeof(T), type))
            });

            if (orderMethod == null)
            {
                // Fallback to reflection if direct method lookup fails
                orderMethod = typeof(Queryable).GetMethods()
                    .Where(m => m.Name == methodName)
                    .Where(m => m.IsGenericMethodDefinition)
                    .Where(m => m.GetGenericArguments().Length == 2)
                    .Where(m => m.GetParameters().Length == 2)
                    .FirstOrDefault();
            }

            if (orderMethod != null && orderMethod.IsGenericMethodDefinition)
            {
                orderMethod = orderMethod.MakeGenericMethod(typeof(T), type);
            }

            object result = orderMethod?.Invoke(null, new object[] { source, lambda });
            return (IOrderedQueryable<T>)result;
        }

        public static IQueryable<T> ApplyDynamicFilter<T>(this IQueryable<T> source, string property, FilterFieldOperator filterFieldOperator, object value)
        {
            string[] props = property.Split('.');
            Type type = typeof(T);

            ParameterExpression arg = Expression.Parameter(type, "x");
            Expression expr = arg;
            foreach (string prop in props)
            {
                PropertyInfo pi = type.GetProperty(prop);
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }
            Expression left = expr;
            Expression constant = Expression.Constant(value);
            Expression right = Expression.Convert(constant, type);

            Expression whereExpression;
            switch (filterFieldOperator)
            {
                case FilterFieldOperator.Equal:
                    whereExpression = Expression.Equal(left, right);
                    break;

                case FilterFieldOperator.NotEqual:
                    whereExpression = Expression.NotEqual(left, right);
                    break;

                case FilterFieldOperator.GreaterThan:
                    whereExpression = Expression.GreaterThan(left, right);
                    break;

                case FilterFieldOperator.GreaterThanOrEqual:
                    whereExpression = Expression.GreaterThanOrEqual(left, right);
                    break;

                case FilterFieldOperator.LessThan:
                    whereExpression = Expression.LessThan(left, right);
                    break;

                case FilterFieldOperator.LessThanOrEqual:
                    whereExpression = Expression.LessThanOrEqual(left, right);
                    break;

                case FilterFieldOperator.IsNull:
                    right = Expression.Constant(null);
                    whereExpression = Expression.Equal(left, right);
                    break;

                case FilterFieldOperator.IsNotNull:
                    right = Expression.Constant(null);
                    whereExpression = Expression.NotEqual(left, right);
                    break;

                case FilterFieldOperator.Contains:
                    MethodInfo method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    whereExpression = Expression.Call(left, method, right);
                    break;

                default:
                    throw new ArgumentException($"Unsupported filter operator: {filterFieldOperator}");
            }

            // Create lambda expression directly
            var lambda = Expression.Lambda<Func<T, bool>>(whereExpression, arg);
            
            // Use the direct Where method instead of reflection
            return source.Where(lambda);
        }
    }
}