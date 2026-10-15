using System;
using System.Collections.Generic;
using System.Data;

namespace Accord.Statistics.Filters
{
    public partial class Codification<T>
    {
#if !NETSTANDARD1_4
        private void buildResultSchema(DataTable data, DataTable result)
        {
            var order = new List<DataColumn>();

            // For each column having a mapping
            foreach (DataColumn column in data.Columns)
            {
                addResultColumns(result, order, column);
            }

            // Put the columns back in the same order
            result.Columns.Clear();
            result.Columns.AddRange(order.ToArray());
        }

        private void addResultColumns(DataTable result, List<DataColumn> order, DataColumn column)
        {
            var currentColumn = result.Columns[column.ColumnName];
            order.Add(currentColumn);

            if (!Columns.Contains(column.ColumnName))
            {
                return;
            }

            var options = Columns[column.ColumnName];

            // If we are just converting strings to integer codes
            if (options.VariableType == CodificationVariable.Ordinal)
            {
                // Change its type from string to integer
                currentColumn.MaxLength = -1;
                if (options.HasMissingValue && options.MissingValueReplacement != null &&
                    options.MissingValueReplacement != DBNull.Value)
                {
                    currentColumn.DataType = options.MissingValueReplacement.GetType();
                }
                else
                {
                    currentColumn.DataType = typeof(int);
                }
            }

            // If we want to avoid implying an order relationship between them
            else if (options.VariableType == CodificationVariable.Categorical)
            {
                addFactorColumns(order, currentColumn, options, 0);
            }

            // If we want to avoid implying an order relationship between them
            else if (options.VariableType == CodificationVariable.CategoricalWithBaseline)
            {
                addFactorColumns(order, currentColumn, options, 1);
            }

            else if (options.VariableType == CodificationVariable.Continuous)
            {
                // Change its type from to double
                currentColumn.MaxLength = -1;
                currentColumn.DataType = typeof(double);
            }

            else if (options.VariableType == CodificationVariable.Discrete)
            {
                // Change its type from to int
                currentColumn.MaxLength = -1;
                currentColumn.DataType = typeof(double);
            }

            else
            {
                throw new InvalidOperationException("Unknown variable type: " + options.VariableType);
            }
        }

        private static void addFactorColumns(List<DataColumn> order, DataColumn currentColumn, Options options,
            int firstSymbol)
        {
            // Remove the column from the schema
            order.Remove(currentColumn);

            // Create extra columns for each possible value
            for (var i = 0; i < options.NumberOfOutputs; i++)
            {
                // Except for the first, that should be the baseline value
                var symbolName = options.Mapping.Reverse[i + firstSymbol];
                var factorName = getFactorName(options, symbolName);

                order.Add(new DataColumn(factorName, typeof(int))
                {
                    DefaultValue = 0
                });
            }
        }

        private void copyRows(DataTable data, DataTable result)
        {
            // Now for each row on the original table
            foreach (DataRow inputRow in data.Rows)
            {
                // We'll import to the result table
                var resultRow = result.NewRow();

                // For each column in original table
                foreach (DataColumn column in data.Columns)
                {
                    translateCell(inputRow, resultRow, column);
                }

                // Finally, add the row into the result table
                result.Rows.Add(resultRow);
            }
        }

        private void translateCell(DataRow inputRow, DataRow resultRow, DataColumn column)
        {
            var name = column.ColumnName;

            // If the column has a mapping
            if (Columns.Contains(name))
            {
                var options = Columns[name];
                var map = options.Mapping;
                var obj = inputRow[name];

                // Retrieve string value
                if (options.IsMissingValue(obj))
                {
                    resultRow[name] = options.MissingValueReplacement;
                    return;
                }

                var label = (T)obj;

                if (options.VariableType == CodificationVariable.Ordinal)
                {
                    int value;

                    // Get its corresponding integer
                    try
                    {
                        value = map[label];
                    }
                    catch
                    {
                        value = map.Values.Count + 1;
                        map[label] = value;
                    }

                    // Set the row to the integer
                    resultRow[name] = value;
                }
                else if (options.VariableType == CodificationVariable.CategoricalWithBaseline)
                {
                    if (options.Mapping[label] > 0)
                    {
                        // Find the corresponding column
                        var factorName = getFactorName(options, label);

                        try
                        {
                            resultRow[factorName] = 1;
                        }
                        catch
                        {
                        }
                    }
                }
                else if (options.VariableType == CodificationVariable.Categorical)
                {
                    // Find the corresponding column
                    var factorName = getFactorName(options, label);

                    try
                    {
                        resultRow[factorName] = 1;
                    }
                    catch
                    {
                    }
                }
                else if (options.VariableType == CodificationVariable.Continuous)
                {
                    resultRow[name] = obj;
                }
                else if (options.VariableType == CodificationVariable.Discrete)
                {
                    resultRow[name] = obj;
                }
                else
                {
                    throw new InvalidOperationException("Unknown variable type: " + options.VariableType);
                }
            }
            else
            {
                // The column does not have a mapping
                //  so we'll just copy the value over
                resultRow[name] = inputRow[name];
            }
        }
#endif
    }
}