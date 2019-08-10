using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using CILExamining.Obfuscations.Utilities;

namespace CILExamining.Obfuscations {
    public class JunkObfuscation : IObfuscation {
        protected readonly int junksAmount;
        protected readonly int junksMethodsA, junksMethodsB;
        protected readonly int junksFieldsA, junksFieldsB;
        protected readonly int junksNamespaces;

        protected readonly RenameUtility renamer;
        protected Random random => renamer.Random;

        public JunkObfuscation() {
            this.junksAmount = 128;
            this.junksMethodsA = 32;
            this.junksMethodsB = 64;
            this.junksNamespaces = 32;
            this.junksFieldsA = 8;
            this.junksFieldsB = 12;
            this.renamer = new RenameUtility();
        }

        public JunkObfuscation(RenameUtility renamer) {
            this.junksAmount = 32;
            this.junksMethodsA = 32;
            this.junksMethodsB = 64;
            this.junksFieldsA = 8;
            this.junksFieldsB = 12;
            this.junksNamespaces = 8;
            this.renamer = renamer;
        }

        public JunkObfuscation(int seed) {
            this.junksAmount = 32;
            this.junksMethodsA = 32;
            this.junksMethodsB = 64;
            this.junksFieldsA = 8;
            this.junksFieldsB = 12;
            this.junksNamespaces = 8;
            this.renamer = new RenameUtility(seed);
        }

        public JunkObfuscation(int seed, int junksAmount, int junksMethodsA, int junksMethodsB, int junksFieldsA, int junksFieldsB, int junksNamespaces) {
            this.junksAmount = junksAmount;
            this.junksMethodsA = junksMethodsA;
            this.junksMethodsB = junksMethodsB;
            this.junksFieldsA = junksFieldsA;
            this.junksFieldsB = junksFieldsB;
            this.junksNamespaces = junksNamespaces;
            this.renamer = new RenameUtility(seed);
        }

        public JunkObfuscation(RenameUtility renamer, int junksAmount, int junksMethodsA, int junksMethodsB, int junksFieldsA, int junksFieldsB, int junksNamespaces) {
            this.junksAmount = junksAmount;
            this.junksMethodsA = junksMethodsA;
            this.junksMethodsB = junksMethodsB;
            this.junksFieldsA = junksFieldsA;
            this.junksFieldsB = junksFieldsB;
            this.junksNamespaces = junksNamespaces;
            this.renamer = renamer;
        }

        public IEnumerable<TypeDefUser> Execute(ICorLibTypes types) {
            string current_namespace = "DEFAULT_IF_NOT_EXISTS";
            for (int i = 0; i < junksAmount; i++) {
                if (i % 4 == 0)
                    current_namespace = renamer.GetObfuscated(false);

                TypeDefUser tdu = new TypeDefUser(current_namespace, renamer.GetObfuscated(false));
                tdu.Attributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;

                MethodDef md_obf = createObfuscatedByMethod(types, tdu);

                for (int fields_i = 0; fields_i < random.Next(junksFieldsA, junksFieldsB); fields_i++)
                    tdu.Fields.Add(new FieldDefUser(renamer.GetObfuscated(false), new FieldSig(getRandomTypeSigForField(types)), fields_i % 3 == 0 ? FieldAttributes.Private : FieldAttributes.Public));

                for (int methods_i = 0; methods_i < random.Next(junksFieldsA, junksFieldsB); methods_i++) {
                    MethodDefUser mdu = new MethodDefUser(renamer.GetObfuscated(false), new MethodSig(CallingConvention.Default, 0, types.String), methods_i % 3 == 0 ? MethodAttributes.Public : MethodAttributes.Private);
                    mdu.Body = new CilBody(true, new Instruction[] {
                        OpCodes.Nop.ToInstruction(),
                        OpCodes.Ldstr.ToInstruction("Obfuscated by Wisser Tg"),
                        OpCodes.Callvirt.ToInstruction(md_obf),
                        OpCodes.Ret.ToInstruction()
                    }, new ExceptionHandler[0], new Local[0]);
                    tdu.Methods.Add(mdu);
                }

                yield return tdu;
            }
        }

        private MethodDef createObfuscatedByMethod(ICorLibTypes types, TypeDefUser typeDef) {
            MethodDefUser mdu = new MethodDefUser("You are fooled!", new MethodSig(CallingConvention.Default, 0, types.String, types.String), MethodAttributes.Public);
            mdu.Body = new CilBody(false, new Instruction[] {
                OpCodes.Nop.ToInstruction(),
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ret.ToInstruction()
            }, new ExceptionHandler[0], new Local[0]);

            mdu.Parameters[0].CreateParamDef();
            mdu.Parameters[0].ParamDef.Name = "wisser_tg";

            typeDef.Methods.Add(mdu);
            return mdu;
        }

        private TypeSig getRandomTypeSigForField(ICorLibTypes types) {
            switch (random.Next(6)) {
                case 0:
                    return types.Byte;
                case 1:
                    return types.Char;
                case 2:
                    return types.Boolean;
                case 3:
                    return types.String;
                case 4:
                    return types.Object;
                case 5:
                    return types.Int16;
                default:
                    return types.Byte;
            }
        }

        public void Execute(ModuleDefMD moduleDef) {
            foreach (TypeDefUser tdu in Execute(moduleDef.CorLibTypes))
                moduleDef.Types.Add(tdu);
        }

        public bool TryExecute(ModuleDefMD moduleDef) {
            try {
                Execute(moduleDef);
            }
            catch {
                return false;
            }
            return true;
        }
    }
}
