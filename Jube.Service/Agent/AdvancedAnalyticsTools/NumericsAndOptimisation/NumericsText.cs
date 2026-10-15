/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation
{
    public static class NumericsText
    {
        public const string Function =
            "Which built-in function to use. Polynomial: parameters are coefficients c0, c1, c2 and so on meaning c0 + c1 x + c2 x^2 + ... (up to 40). " +
            "Sine a*sin(b*x+c)+d and Cosine a*cos(b*x+c)+d (defaults a=1, b=1, c=0, d=0). Exponential a*exp(b*x)+c (defaults 1, 1, 0). NaturalLog a*ln(x)+b (defaults 1, 0). " +
            "SquareRoot a*sqrt(x)+b (defaults 1, 0). Power a*x^b+c (defaults 1, 2, 0). Reciprocal a/(x-b)+c (defaults 1, 0, 0). Gaussian a*exp(-(x-b)^2/(2 c^2)) (defaults 1, 0, 1). " +
            "Logistic a/(1+exp(-b*(x-c)))+d (defaults 1, 1, 0, 0). HyperbolicTangent a*tanh(b*x+c)+d (defaults 1, 1, 0, 0). AbsoluteValue a*|x-b|+c (defaults 1, 0, 0).";

        public const string FunctionParameters =
            "Numbers a, b, c, d for the chosen function, in that order; missing trailing values take the defaults listed for the function. For Polynomial these are the coefficients from constant term upwards.";

        public const string Objective =
            "Which built-in objective to use: Sphere (sum of squares, minimum 0 at the origin), Rosenbrock (banana valley, minimum 0 at all ones, 2 or more variables), Rastrigin (many local minima, minimum 0 at origin), " +
            "Ackley (minimum 0 at origin), Himmelblau (exactly 2 variables, four minima with value 0, one at (3, 2)), Booth (2 variables, minimum 0 at (1, 3)), Beale (2 variables, minimum 0 at (3, 0.5)), " +
            "Matyas (2 variables, minimum 0 at origin), StyblinskiTang (minimum about -39.166 per variable at -2.9035), Griewank (minimum 0 at origin), " +
            "or Quadratic: 0.5 x'Qx + c'x + constant using quadraticMatrix, linearTerms and constantTerm.";

        public const string QuadraticMatrix =
            "Only for the Quadratic objective: the square matrix Q with one row per variable (its symmetric part is used).";

        public const string LinearTerms =
            "Only for the Quadratic objective: the vector c of linear terms, one per variable; empty means zeros.";

        public const string ConstantTerm =
            "Only for the Quadratic objective: the constant added to the objective.";
    }
}