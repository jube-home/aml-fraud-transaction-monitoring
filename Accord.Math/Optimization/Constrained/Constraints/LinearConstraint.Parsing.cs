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
    public partial class LinearConstraint
    {
        private static string parseMultiply(Dictionary<string, double> terms, BinaryExpression eb, ref double value)
        {
            // This could be either a constant times and expression or a quadratic member
            var a = eb.Left as ConstantExpression ?? eb.Right as ConstantExpression;
            var m = eb.Left as MemberExpression ?? eb.Right as MemberExpression;
            var u = eb.Left as UnaryExpression ?? eb.Right as UnaryExpression;

            if (a != null)
            {
                // This is a constant times an expression
                var scalar = (double)a.Value;

                var term = parse(terms, (Expression)m ?? u, ref value);
                terms[term] = scalar;

                return term;
            }

            throw new FormatException("Unexpected expression.");
        }

        private static void parseAdd(Dictionary<string, double> terms, BinaryExpression eb, ref double value)
        {
            // This could be an expression + term, a term + expression or an expression + expression
            var lb = eb.Left as BinaryExpression;
            var lm = eb.Left as MemberExpression;
            var lu = eb.Left as UnaryExpression;
            var lc = eb.Left as ConstantExpression;

            var rb = eb.Right as BinaryExpression;
            var rm = eb.Right as MemberExpression;
            var rc = eb.Right as ConstantExpression;

            if (lb != null)
            {
                parse(terms, lb, ref value);
            }
            else if (lm != null)
            {
                terms[lm.Member.Name] = 1;
            }
            else if (lu != null)
            {
                parse(terms, lu, ref value);
            }
            else if (lc != null)
            {
                value += (double)lc.Value;
            }
            else
            {
                throw new FormatException("Unexpected expression.");
            }

            if (rb != null)
            {
                parse(terms, rb, ref value);
            }
            else if (rm != null)
            {
                terms[rm.Member.Name] = 1;
            }
            else if (rc != null)
            {
                value += (double)rc.Value;
            }
            else
            {
                throw new FormatException("Unexpected expression.");
            }
        }

        private static void parseSubtract(Dictionary<string, double> terms, BinaryExpression eb, ref double value)
        {
            // This could be an expression - term, a term - expression or an expression - expression
            var lb = eb.Left as BinaryExpression;
            var lm = eb.Left as MemberExpression;
            var lu = eb.Left as UnaryExpression;

            var rb = eb.Right as BinaryExpression;
            var rm = eb.Right as MemberExpression;
            var rc = eb.Right as ConstantExpression;

            if (lb != null)
            {
                parse(terms, lb, ref value);
            }
            else if (lm != null)
            {
                terms[lm.Member.Name] = 1;
            }
            else if (lu != null)
            {
                parse(terms, lu, ref value);
            }
            else
            {
                throw new FormatException("Unexpected expression.");
            }

            if (rb != null)
            {
                var term = parse(terms, rb, ref value);
                terms[term] = -terms[term];
            }
            else if (rm != null)
            {
                terms[rm.Member.Name] = -1;
            }
            else if (rc != null)
            {
                value -= (double)rc.Value;
            }
            else
            {
                throw new FormatException("Unexpected expression.");
            }
        }

        private static void parseUnaryPlus(Dictionary<string, double> terms, UnaryExpression eu, ref double value)
        {
            var lb = eu.Operand as BinaryExpression;
            var lm = eu.Operand as MemberExpression;
            var lc = eu.Operand as ConstantExpression;

            if (lm != null)
            {
                terms[lm.Member.Name] = 1;
            }
            else if (lb != null)
            {
                parse(terms, lb, ref value);
            }
            else if (lc != null)
            {
                value += (double)lc.Value;
            }
            else
            {
                throw new FormatException("Unexpected expression.");
            }
        }

        private static void parseNegate(Dictionary<string, double> terms, UnaryExpression eu, ref double value)
        {
            var lb = eu.Operand as BinaryExpression;
            var lm = eu.Operand as MemberExpression;
            var lc = eu.Operand as ConstantExpression;

            if (lm != null)
            {
                terms[lm.Member.Name] = -1;
            }
            else if (lb != null)
            {
                var term = parse(terms, lb, ref value);
                terms[term] = -terms[term];
            }
            else if (lc != null)
            {
                value -= (double)lc.Value;
            }
            else
            {
                throw new FormatException("Unexpected expression.");
            }
        }
    }
}