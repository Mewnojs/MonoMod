﻿#if !CECIL0_9
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MonoMod.DebugIL {
    public static class DebugILGeneratorExt {

        public static readonly Type t_MetadataType = typeof(MetadataType);

        public static ScopeDebugInformation GetOrAddScope(this MethodDebugInformation mdi) {
            if (mdi.Scope != null)
                return mdi.Scope;
            return mdi.Scope = new ScopeDebugInformation(
                mdi.Method.Body.Instructions[0],
                mdi.Method.Body.Instructions[mdi.Method.Body.Instructions.Count - 1]
            );
        }

        public static string GenerateVariableName(this VariableDefinition @var) {
            TypeReference type = @var.VariableType;
            while (type is TypeSpecification)
                type = ((TypeSpecification) type).ElementType;

            string name = type.Name;

            if (type.MetadataType == MetadataType.Boolean)
                name = "flag";
            else if (type.IsPrimitive)
                name = Enum.GetName(t_MetadataType, type.MetadataType);

            return name.Substring(0, 1).ToLower(CultureInfo.InvariantCulture) + name.Substring(1) + @var.Index;
        }

        public static string ToRelativeString(this Instruction self) {
            StringBuilder instruction = new StringBuilder();

            instruction.Append(self.OpCode.Name);

            if (self.Operand == null)
                return instruction.ToString();

            instruction.Append(' ');

            switch (self.OpCode.OperandType) {
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    AppendRelativeLabel(instruction, self, (Instruction) self.Operand);
                    break;

                case OperandType.InlineSwitch:
                    var labels = (Instruction[]) self.Operand;
                    for (int i = 0; i < labels.Length; i++) {
                        if (i > 0)
                            instruction.Append(',');

                        AppendRelativeLabel(instruction, self, labels[i]);
                    }
                    break;

                case OperandType.InlineString:
                    instruction.Append('\"');
                    instruction.Append(self.Operand);
                    instruction.Append('\"');
                    break;

                default:
                    instruction.Append(self.Operand);
                    break;
            }

            return instruction.ToString();
        }

        static void AppendRelativeLabel(StringBuilder builder, Instruction from, Instruction to) {
            builder.Append("IL_Rel");

            int offset = to.Offset - from.Offset;
            Instruction instr;
            if (offset < 0) {
                builder.Append("-");
                offset = 0;
                for (instr = from; instr != to && instr != null; instr = instr.Previous)
                    offset++;
            } else {
                builder.Append("+");
                offset = 0;
                for (instr = from; instr != to && instr != null; instr = instr.Next)
                    offset++;
            }

            if (instr == null) {
                builder.Append("?(").Append((to.Offset - from.Offset).ToString("x4", CultureInfo.InvariantCulture)).Append(")");
                return;
            }
            
            builder.Append(offset);

            switch (instr.OpCode.OperandType) {
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                case OperandType.InlineSwitch:
                    break;

                default:
                    builder.Append(" // ").Append(instr.ToRelativeString());
                    break;
            }
        }

    }
}
#endif
