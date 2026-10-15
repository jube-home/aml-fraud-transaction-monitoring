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

// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Error List, point to "Suppress Message(s)", and click 
// "In Project Suppression File".
// You do not need to add suppressions to this file manually.

using System.Diagnostics.CodeAnalysis;

[assembly:
    SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue",
        Scope = "type", Target = "Accord.Math.Optimization.ConjugateGradientMethod")]
[assembly:
    SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters",
        MessageId = "value", Scope = "member", Target = "Accord.Math.Matrix.#Equals(System.Object)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly",
        MessageId = "IsEqual", Scope = "member", Target = "Accord.Math.Matrix.#Equals(System.Object)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "Function", Scope = "member", Target = "Accord.Math.Optimization.IObjectiveFunction.#Function")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "m", Scope = "member", Target = "Accord.Math.Special.#Log1m(System.Double)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "p", Scope = "member", Target = "Accord.Math.Special.#Log1p(System.Double)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "evl", Scope = "member",
        Target = "Accord.Math.Special.#P1evl(System.Double,System.Double[],System.Int32)")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue",
        Scope = "type", Target = "Accord.Math.ComparerDirection")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue",
        Scope = "type", Target = "Accord.Math.Comparers.ComparerDirection")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.Math.Kinematics.DenavitHartenbergModel")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "Function", Scope = "member",
        Target = "Accord.Math.Optimization.IGradientOptimizationMethod.#Function")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "Function", Scope = "member", Target = "Accord.Math.Optimization.ILeastSquaresMethod.#Function")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly",
        MessageId = "ParameterTolerance", Scope = "member",
        Target = "Accord.Math.Optimization.BroydenFletcherGoldfarbShanno.#Optimize()")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "Function", Scope = "member", Target = "Accord.Math.Integration.IUnivariateIntegration.#Function")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "Function", Scope = "member",
        Target = "Accord.Math.Integration.IMultidimensionalIntegration.#Function")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces", Scope = "type",
        Target = "Accord.Math.ISingleValueConvergence")]
[assembly:
    SuppressMessage("Microsoft.Design",
        "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member",
        Target = "Accord.Math.RelativeParameterConvergence.#Accord.Math.IConvergence`1<System.Double[]>.NewValue")]
[assembly:
    SuppressMessage("Microsoft.Design",
        "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member",
        Target = "Accord.Math.RelativeParameterConvergence.#Accord.Math.IConvergence`1<System.Double[]>.NewValue")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "DFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#DFT(Accord.Math.Compatibility.ComplexWrapper[],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "DFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#DFT2(Accord.Math.Compatibility.ComplexWrapper[][],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "FFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#FFT(Accord.Math.Compatibility.ComplexWrapper[],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "FFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#FFT2(Accord.Math.Compatibility.ComplexWrapper[][],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "DFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#DFT(System.Numerics.Complex[],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "DFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#DFT2(System.Numerics.Complex[][],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "FFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#FFT(System.Numerics.Complex[],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "FFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#FFT2(System.Numerics.Complex[][],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "DFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#DFT(AForge.Math.Complex[],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "DFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#DFT2(AForge.Math.Complex[][],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "FFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#FFT(AForge.Math.Complex[],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "FFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#FFT(System.Double[],System.Double[],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "FFT", Scope = "member",
        Target =
            "Accord.Math.Transforms.FourierTransform2.#FFT2(AForge.Math.Complex[][],AForge.Math.FourierTransform+Direction)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.IO.MatNode")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods",
        Scope = "member", Target = "Accord.IO.MatNode.#Value")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "Q", Scope = "member",
        Target =
            "Accord.Math.Optimization.FanChenLinQuadraticOptimization.#.ctor(System.Int32,System.Func`3<System.Int32,System.Int32,System.Double>,System.Double[],System.Int32[])")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "Q", Scope = "member",
        Target =
            "Accord.Math.Optimization.FanChenLinQuadraticOptimization.#.ctor(System.Int32,System.Func`3<System.Int32,System.Int32,System.Double>)")]