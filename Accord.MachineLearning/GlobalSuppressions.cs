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
    SuppressMessage("Microsoft.Design",
        "CA1000:DoNotDeclareStaticMembersOnGenericTypes", Scope = "member",
        Target = "Accord.MachineLearning.CrossValidationResult`1.#Load(System.IO.Stream)")]
[assembly:
    SuppressMessage("Microsoft.Design",
        "CA1000:DoNotDeclareStaticMembersOnGenericTypes", Scope = "member",
        Target = "Accord.MachineLearning.CrossValidationResult`1.#Load(System.String)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "ID", Scope = "type", Target = "Accord.MachineLearning.DecisionTrees.Learning.ID3Learning")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
        Scope = "member", Target = "Accord.MachineLearning.GaussianCluster.#ToDistribution()")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
        Scope = "member", Target = "Accord.MachineLearning.GaussianMixtureModel.#GetMixtureDistribution()")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
        Scope = "member",
        Target = "Accord.MachineLearning.VectorMachines.MulticlassSupportVectorMachine.#GetLastKernelEvaluations()")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional",
        Scope = "member",
        Target =
            "Accord.MachineLearning.VectorMachines.MulticlassSupportVectorMachine.#Item[System.Int32,System.Int32]")]
[assembly:
    SuppressMessage("Microsoft.Design",
        "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member",
        Target =
            "Accord.MachineLearning.GaussianMixtureModel.#Accord.MachineLearning.IClusteringAlgorithm`1<System.Double[]>.Clusters")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.MachineLearning.VectorMachines.MulticlassSupportVectorMachine")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.MachineLearning.VectorMachines.MultilabelSupportVectorMachine")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.MachineLearning.Structures.KDTree`1")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
        Scope = "member",
        Target =
            "Accord.MachineLearning.DecisionTrees.DecisionVariable.#FromCodebook(Accord.Statistics.Filters.Codification,System.String[])")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.MachineLearning.DecisionTrees.DecisionTree")]
[assembly:
    SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly",
        Scope = "member", Target = "Accord.MachineLearning.DecisionTrees.DecisionNode.#Branches")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.MachineLearning.Boosting.Boost`1")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
        Scope = "member",
        Target = "Accord.MachineLearning.VectorMachines.MultilabelSupportVectorMachine.#GetLastKernelEvaluations()")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.MachineLearning.Structures.KDTreeNodeList`1")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1043:UseIntegralOrStringArgumentForIndexers",
        Scope = "member", Target = "Accord.MachineLearning.Structures.KDTreeNodeCollection`1.#Item[System.Double]")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.MachineLearning.DecisionTrees.Rules.DecisionSet")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.MachineLearning.DecisionTrees.Rules.DecisionRule")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.MachineLearning.Structures.KDTree")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
        Scope = "member",
        Target =
            "Accord.MachineLearning.VectorMachines.KernelSupportVectorMachine.#FromLogisticRegression(Accord.Statistics.Models.Regression.LogisticRegression)")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
        Scope = "member",
        Target =
            "Accord.MachineLearning.VectorMachines.SupportVectorMachine.#FromLogisticRegression(Accord.Statistics.Models.Regression.LogisticRegression)")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
        Scope = "member", Target = "Accord.MachineLearning.DecisionTrees.DecisionTree.#GetHeight()")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
        Scope = "member", Target = "Accord.MachineLearning.DecisionTrees.DecisionNode.#GetHeight()")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.MachineLearning.DecisionTrees.DecisionNode")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "Nu", Scope = "member",
        Target = "Accord.MachineLearning.VectorMachines.Learning.OneclassSupportVectorLearning.#Nu")]