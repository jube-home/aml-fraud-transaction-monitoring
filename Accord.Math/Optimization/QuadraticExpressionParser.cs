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
using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Accord.Math.Optimization
{
    internal static class QuadraticExpressionParser
    {
        public static string ToVariable(this int num, char prefix = 'x')
        {
            var number = num.ToString();
            var chars = new char[number.Length + 1];
            chars[0] = prefix;

            for (var i = 0; i < number.Length; i++)
            {
                chars[i + 1] = (char)(number[i] + (0x2080 - '0'));
            }

            return new string(chars);
        }

        public static Dictionary<Tuple<string, string>, double> ParseString(string f, CultureInfo culture)
        {
            f = f.Replace("*", string.Empty).Replace(" ", string.Empty);

            var terms = new Dictionary<Tuple<string, string>, double>();

            var replaceQuad = new Regex(@"([a-zA-Z])(²)");
            f = replaceQuad.Replace(f, "$1$1");


            var separator = culture.NumberFormat.NumberDecimalSeparator;

            var r = new Regex(@"[\-\+]?[\s]*((\d*\" + separator + @"{0,1}\d+)|[a-zA-Z][²]?)+");
            var number = new Regex(@"\d*\" + separator + @"{0,1}\d+");
            var symbol = new Regex(@"[a-zA-Z]");


            var matches = r.Matches(f, 0);

            foreach (Match m in matches)
            {
                var term = m.Value;

                double scalar = term[0] == '-' ? -1 : 1;

                // Extract value
                var coeff = number.Matches(term);

                foreach (Match c in coeff)
                {
                    scalar *= double.Parse(c.Value, culture);
                }

                // Extract symbols
                var symbols = symbol.Matches(term);

                if (symbols.Count == 1)
                {
                    terms.Add(Tuple.Create(symbols[0].Value, (string)null), scalar);
                }
                else if (symbols.Count == 2)
                {
                    terms.Add(Tuple.Create(symbols[0].Value, symbols[1].Value), scalar);
                }
                else
                {
                    terms.Add(Tuple.Create((string)null, (string)null), scalar);
                }
            }

            return terms;
        }

        public static Tuple<string, string> ParseExpression(
            Dictionary<Tuple<string, string>, double> terms,
            Expression expr, out double scalar,
            bool dontAdd = false)
        {
            scalar = 0;

            if (expr == null)
            {
                return null;
            }

            var eb = expr as BinaryExpression;
            var em = expr as MemberExpression;
            var eu = expr as UnaryExpression;

            if (em != null) // member expression
            {
                var term = Tuple.Create(em.Member.Name, (string)null);
                terms[term] = 1;
                return term;
            }

            if (eb != null) // binary expression
            {
                if (expr.NodeType == ExpressionType.Multiply)
                {
                    // This could be either a constant*expression, expression*constant or expression*expression
                    var c = eb.Left as ConstantExpression ?? eb.Right as ConstantExpression;

                    var lm = eb.Left as MemberExpression;
                    var lb = eb.Left as BinaryExpression;
                    var lu = eb.Left as UnaryExpression;

                    var rm = eb.Right as MemberExpression;
                    var rb = eb.Right as BinaryExpression;
                    var ru = eb.Right as UnaryExpression;


                    if (c != null)
                    {
                        // This is constant*expression or expression*constant
                        scalar = (double)c.Value;

                        if ((lm ?? rm) != null)
                        {
                            var term = Tuple.Create((lm ?? rm).Member.Name, (string)null);
                            if (!dontAdd)
                            {
                                terms[term] = scalar;
                            }

                            return term;
                        }

                        if ((lb ?? rb ?? (Expression)lm ?? lu) != null)
                        {
                            var term = ParseExpression(terms, lb ?? lu ?? (Expression)rb ?? ru, out _);
                            if (!dontAdd)
                            {
                                terms[term] = scalar;
                            }

                            return term;
                        }

                        throw new FormatException("Unexpected expression.");
                    }

                    // This is x * x
                    if (lm != null && rm != null)
                    {
                        scalar = 1;
                        return addTuple(terms, scalar, lm.Member.Name, rm.Member.Name);
                    }

                    if ((lb ?? rb ?? lu ?? (Expression)ru) != null && (lm ?? rm) != null)
                    {
                        // This is expression * x
                        var term = ParseExpression(terms, lb ?? rb ?? lu ?? (Expression)ru, out scalar,
                            true);
                        return addTuple(terms, scalar, term.Item1, (lm ?? rm).Member.Name);
                    }

                    throw new FormatException("Unexpected expression.");
                }

                if (expr.NodeType == ExpressionType.Add)
                {
                    // This could be an expression + term, a term + expression or an expression + expression
                    var lb = eb.Left as BinaryExpression;
                    var lm = eb.Left as MemberExpression;
                    var lu = eb.Left as UnaryExpression;

                    var rb = eb.Right as BinaryExpression;
                    var rm = eb.Right as MemberExpression;
                    var rc = eb.Right as ConstantExpression;

                    scalar = 1;
                    if (lb != null)
                    {
                        ParseExpression(terms, lb, out scalar);
                    }
                    else if (lm != null)
                    {
                        var term = Tuple.Create(lm.Member.Name, (string)null);
                        if (!dontAdd)
                        {
                            terms[term] = scalar;
                        }
                    }
                    else if (lu != null)
                    {
                        ParseExpression(terms, lu, out scalar);
                    }
                    else
                    {
                        throw new FormatException("Unexpected expression.");
                    }

                    scalar = 1;
                    if (rb != null)
                    {
                        ParseExpression(terms, rb, out scalar);
                    }
                    else if (rm != null)
                    {
                        var term = Tuple.Create(rm.Member.Name, (string)null);
                        if (!dontAdd)
                        {
                            terms[term] = scalar;
                        }
                    }
                    else if (rc != null)
                    {
                        scalar = (double)rc.Value;
                        var term = Tuple.Create((string)null, (string)null);
                        if (!dontAdd)
                        {
                            terms[term] = scalar;
                        }
                    }
                    else
                    {
                        throw new FormatException("Unexpected expression.");
                    }
                }
                else if (expr.NodeType == ExpressionType.Subtract)
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
                        ParseExpression(terms, lb, out scalar);
                    }
                    else if (lm != null)
                    {
                        scalar = 1;
                        var term = Tuple.Create(lm.Member.Name, (string)null);
                        if (!dontAdd)
                        {
                            terms[term] = scalar;
                        }
                    }
                    else if (lu != null)
                    {
                        ParseExpression(terms, lu, out scalar);
                    }
                    else
                    {
                        throw new FormatException("Unexpected expression.");
                    }

                    if (rb != null)
                    {
                        var term = ParseExpression(terms, rb, out scalar);
                        terms[term] = -scalar;
                    }
                    else if (rm != null)
                    {
                        scalar = -1;
                        var term = Tuple.Create(rm.Member.Name, (string)null);
                        if (!dontAdd)
                        {
                            terms[term] = scalar;
                        }
                    }
                    else if (rc != null)
                    {
                        scalar = (double)rc.Value;
                        var term = Tuple.Create((string)null, (string)null);
                        terms[term] = -scalar;
                    }
                    else
                    {
                        throw new FormatException("Unexpected expression.");
                    }
                }
            }
            else if (eu != null) // unary expression
            {
                if (expr.NodeType == ExpressionType.UnaryPlus)
                {
                    var lb = eu.Operand as BinaryExpression;
                    var lm = eu.Operand as MemberExpression;

                    if (lm != null)
                    {
                        scalar = 1;
                        var term = Tuple.Create(lm.Member.Name, (string)null);
                        if (!dontAdd)
                        {
                            terms[term] = scalar;
                        }

                        return term;
                    }

                    if (lb != null)
                    {
                        var term = ParseExpression(terms, lb, out scalar);
                        if (!dontAdd)
                        {
                            terms[term] = scalar;
                        }
                    }
                    else
                    {
                        throw new FormatException("Unexpected expression.");
                    }
                }
                else if (expr.NodeType == ExpressionType.Negate)
                {
                    var lb = eu.Operand as BinaryExpression;
                    var lm = eu.Operand as MemberExpression;

                    if (lm != null)
                    {
                        scalar = -1;
                        var term = Tuple.Create(lm.Member.Name, (string)null);
                        if (!dontAdd)
                        {
                            terms[term] = scalar;
                        }

                        return term;
                    }

                    if (lb != null)
                    {
                        var term = ParseExpression(terms, lb, out scalar);
                        terms[term] = -scalar;
                    }
                    else
                    {
                        throw new FormatException("Unexpected expression.");
                    }
                }
                else
                {
                    throw new FormatException("Unexpected expression.");
                }
            }
            else
            {
                throw new FormatException("Unexpected expression.");
            }

            return null;
        }

        private static Tuple<string, string> addTuple(Dictionary<Tuple<string, string>,
            double> terms, double v, string v1, string v2)
        {
            var t1 = Tuple.Create(v1, v2);
            var t2 = Tuple.Create(v2, v1);

            if (!terms.ContainsKey(t1) && !terms.ContainsKey(t2))
            {
                terms[t1] = v;
            }

            return t1;
        }
    }
}