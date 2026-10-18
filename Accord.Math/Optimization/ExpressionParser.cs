// Accord.NET Framework (originally)
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Relicensed to MIT per the author's own explicit grant made when the
// upstream project was archived (2020-11-19):
// https://github.com/accord-net/framework
// "I am also willing to change the license of any file where I am the
// single author... to MIT so people can reuse individual pieces of code
// more easily." -- Cesar De Souza
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Accord.Math.Optimization
{
#if !NET35
    internal static class ExpressionParser
    {
        public static void Parse(SortedSet<string> list, Expression expr)
        {
            if (expr == null)
            {
                return;
            }

            var eb = expr as BinaryExpression;
            var em = expr as MemberExpression;
            var eu = expr as UnaryExpression;
            var ec = expr as MethodCallExpression;


            if (em != null) // member expression
            {
                list.Add(em.Member.Name);
            }
            else if (eb != null) // binary expression
            {
                Parse(list, eb.Left);
                Parse(list, eb.Right);
            }
            else if (eu != null) // unary expression
            {
                Parse(list, eu.Operand);
            }
            else if (ec != null) // call expression
            {
                foreach (var a in ec.Arguments)
                {
                    Parse(list, a);
                }
            }
        }

        public static Expression<Func<double[], double[]>> Replace(Expression<Func<double[]>> expr,
            IDictionary<string, int> variables)
        {
            var parameter = Expression.Parameter(typeof(double[]), "input");

            var newBody = Replace(parameter, expr.Body, variables);

            return Expression.Lambda<Func<double[], double[]>>(newBody, parameter);
        }

        public static Expression<Func<double[], double>> Replace(Expression<Func<double>> expr,
            IDictionary<string, int> variables)
        {
            var parameter = Expression.Parameter(typeof(double[]), "input");

            var newBody = Replace(parameter, expr.Body, variables);

            return Expression.Lambda<Func<double[], double>>(newBody, parameter);
        }

        public static Expression Replace(ParameterExpression parameter,
            Expression expr, IDictionary<string, int> variables)
        {
            if (expr == null)
            {
                return null;
            }

            var eb = expr as BinaryExpression;
            var em = expr as MemberExpression;
            var eu = expr as UnaryExpression;
            var ec = expr as MethodCallExpression;
            var ea = expr as NewArrayExpression;

            if (em != null) // member expression
            {
                var varName = em.Member.Name;
                var index = variables[varName];
                var indexExpression = Expression.Constant(index);
                return Expression.ArrayAccess(parameter, indexExpression);
            }

            if (eb != null) // binary expression
            {
                var left = Replace(parameter, eb.Left, variables);
                var right = Replace(parameter, eb.Right, variables);
                return eb.Update(left, eb.Conversion, right);
            }

            if (eu != null) // unary expression
            {
                var op = Replace(parameter, eu.Operand, variables);
                return eu.Update(op);
            }

            if (ec != null) // call expression
            {
                var args = new List<Expression>();

                foreach (var a in ec.Arguments)
                {
                    args.Add(Replace(parameter, a, variables));
                }

                return ec.Update(ec.Object, args);
            }

            if (ea != null) // new array expression
            {
                var values = new List<Expression>();

                foreach (var v in ea.Expressions)
                {
                    values.Add(Replace(parameter, v, variables));
                }

                return ea.Update(values);
            }

            return expr;
        }
    }
#endif
}