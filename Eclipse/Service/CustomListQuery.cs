using Eclipse.Models;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Eclipse.Service
{
    // Builds the filter of a user-defined custom list.
    //
    // The comparison is assembled as an expression tree exactly as it was before B-15b, so that
    // every combination of field, operator and value behaves the way it always has - including
    // the combinations that throw. What changed is where the left-hand side comes from: it used
    // to be a property-name string walked by reflection, and it is now a compile-checked
    // expression from GameFields.
    //
    // The throwing combinations are deliberately left throwing. IsNull against a non-nullable
    // field, Contains against a number, a JSON value that will not convert to the property's
    // type - all of these fail today, and a user's CustomLists.json can contain any of them.
    // Making them fail softly is a product decision, not part of removing the reflection.
    public static class CustomListQuery
    {
        public static IQueryable<GameMatch> ApplyFilter(this IQueryable<GameMatch> source, FilterExpression filterExpression)
        {
            return source.ApplyFilter(filterExpression.GameFieldEnum,
                                      filterExpression.FilterFieldOperator,
                                      filterExpression.FilterFieldValue);
        }

        public static IQueryable<GameMatch> ApplyFilter(this IQueryable<GameMatch> source,
                                                        GameFieldEnum gameFieldEnum,
                                                        FilterFieldOperator filterFieldOperator,
                                                        object value)
        {
            // The field's own lambda supplies both halves the comparison needs: its body is the
            // property access, and its parameter is what the finished predicate is built over.
            LambdaExpression fieldExpression = GameFields.For(gameFieldEnum).Expression;

            ParameterExpression arg = fieldExpression.Parameters[0];
            Expression left = fieldExpression.Body;
            Type type = left.Type;

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
                    whereExpression = null;
                    break;
            }

            if (whereExpression == null)
            {
                return source;
            }

            var lambda = Expression.Lambda<Func<GameMatch, bool>>(whereExpression, arg).Compile();

            return source.Where(lambda).AsQueryable();
        }
    }
}
