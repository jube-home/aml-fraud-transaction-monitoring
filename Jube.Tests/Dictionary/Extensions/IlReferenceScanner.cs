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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Jube.Test.Dictionary.Extensions
{
    public static class IlReferenceScanner
    {
        private static readonly Dictionary<short, OpCode> opCodesByValue = typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (OpCode)f.GetValue(null)!)
            .ToDictionary(o => o.Value);

        public static IEnumerable<(MethodBase Caller, MemberInfo Reference)> References(Type type)
        {
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                     BindingFlags.Static | BindingFlags.DeclaredOnly;
            var methods = type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all));
            foreach (var method in methods)
            {
                foreach (var reference in References(method))
                {
                    yield return (method, reference);
                }
            }
        }

        public static bool IsPlatformInvoke(MethodBase method)
        {
            return (method.Attributes & MethodAttributes.PinvokeImpl) != 0 ||
                   method.GetCustomAttributesData().Any(a =>
                       a.AttributeType.Name is "DllImportAttribute" or "LibraryImportAttribute");
        }

        private static IEnumerable<MemberInfo> References(MethodBase method)
        {
            var il = method.GetMethodBody()?.GetILAsByteArray();
            if (il is null)
            {
                yield break;
            }

            var typeArguments = method.DeclaringType?.IsGenericType == true
                ? method.DeclaringType.GetGenericArguments()
                : null;
            var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;
            var position = 0;
            while (position < il.Length)
            {
                short value = il[position++];
                if (value == 0xFE)
                {
                    value = (short)(0xFE00 | il[position++]);
                }

                var opCode = opCodesByValue[value];
                switch (opCode.OperandType)
                {
                    case OperandType.InlineMethod:
                    case OperandType.InlineField:
                    case OperandType.InlineType:
                    case OperandType.InlineTok:
                        var token = BitConverter.ToInt32(il, position);
                        position += 4;
                        MemberInfo? member = null;
                        try
                        {
                            member = method.Module.ResolveMember(token, typeArguments, methodArguments);
                        }
                        catch (ArgumentException)
                        {
                        }

                        if (member is not null)
                        {
                            yield return member;
                        }

                        break;
                    case OperandType.InlineSwitch:
                        var count = BitConverter.ToInt32(il, position);
                        position += 4 + count * 4;
                        break;
                    case OperandType.InlineNone:
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        position += 1;
                        break;
                    case OperandType.InlineVar:
                        position += 2;
                        break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        position += 8;
                        break;
                    default:
                        position += 4;
                        break;
                }
            }
        }
    }
}