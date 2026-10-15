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
    SuppressMessage("Microsoft.Naming",
        "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Scope = "type", Target = "Accord.ReadOnlyKeyedCollection`2")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.ReadOnlyKeyedCollection`2")]
[assembly:
    SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters",
        MessageId = "item", Scope = "member",
        Target = "Accord.ReadOnlyKeyedCollection`2.#Add(System.Collections.Generic.KeyValuePair`2<!1,!0>)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.Collections.RedBlackTree`1")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.Collections.RedBlackTree`2")]
[assembly:
    SuppressMessage("Microsoft.Design",
        "CA1000:DoNotDeclareStaticMembersOnGenericTypes", Scope = "member",
        Target = "Accord.Collections.KeyValuePairComparer`2.#Default")]
[assembly:
    SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters",
        MessageId = "item", Scope = "member",
        Target = "Accord.Collections.ReadOnlyKeyedCollection`2.#Add(System.Collections.Generic.KeyValuePair`2<!1,!0>)")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Scope = "type", Target = "Accord.Collections.ReadOnlyKeyedCollection`2")]
[assembly:
    SuppressMessage("Microsoft.Naming",
        "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Scope = "type",
        Target = "Accord.Collections.ReadOnlyKeyedCollection`2")]
[assembly:
    SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors", Scope = "member",
        Target = "Accord.Collections.ReadOnlyKeyedCollection`2.#.ctor(!1[])")]
[assembly:
    SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors", Scope = "member",
        Target = "Accord.Collections.ReadOnlyKeyedCollection`2.#.ctor(System.Collections.Generic.IList`1<!1>)")]
[assembly:
    SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework",
        MessageId =
            "System.ComponentModel.DataAnnotations, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
[assembly:
    SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes",
        Scope = "type", Target = "Accord.IntegerAttribute")]
[assembly:
    SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes",
        Scope = "type", Target = "Accord.RealAttribute")]