using System;
using System.Collections.Generic;
using System.Linq;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace CILExamining.Obfuscations {
    public class ReferenceProxyObfuscation : IObfuscation {
        protected readonly Random random;
        private Stack<Action> add_methods;

        public ReferenceProxyObfuscation() {
            this.random = new Random();
            this.add_methods = new Stack<Action>();
        }

        public ReferenceProxyObfuscation(int seed) {
            this.random = new Random(seed);
            this.add_methods = new Stack<Action>();
        }

        public ReferenceProxyObfuscation(Random random) {
            this.random = random;
            this.add_methods = new Stack<Action>();
        }

        public int Execute(IEnumerable<MethodDef> methods) {
            int sumOfAddedProxies = 0;

            MethodDef[] mds = methods.ToArray();

            for (int i = 0; i < mds.Length; i++)
                sumOfAddedProxies += setReferenceProxiesForInstructions(mds[i]);

            return sumOfAddedProxies;
        }

        protected int setReferenceProxiesForInstructions(MethodDef methodDef) {
            if (!methodDef.HasBody)
                return 0;

            Dictionary<string, IMethod> proxy = new Dictionary<string, IMethod>();
            methodDef.Body.KeepOldMaxStack = true;

            for (int i = 0; i < methodDef.Body.Instructions.Count; i++) {
                Instruction instruction = methodDef.Body.Instructions[i];

                if (instruction.OpCode != OpCodes.Call)
                    continue;

                IMethod call_target = (IMethod)instruction.Operand;

                if (call_target.ResolveMethodDef() == null)
                    continue;

                if (!proxy.ContainsKey(call_target.FullName))
                    proxy.Add(call_target.FullName, createProxy(methodDef.DeclaringType.Module, call_target.ResolveMethodDef()));

                //methodDef.Body.Instructions.Insert(i++, OpCodes.Ldstr.ToInstruction("Obfuscated by Wisser Tg"));

                instruction.Operand = proxy[call_target.FullName];
            }

            return proxy.Count;
        }

        protected MethodDef createProxy(ModuleDef moduleDef, MethodDef target) {
            MethodSig proxy_signature = createProxySig(moduleDef, target);

            TypeDefUser new_type = new TypeDefUser("DEFAULT_TYPE_DEF" + target.FullName);
            new_type.Attributes |= TypeAttributes.Public | TypeAttributes.AutoClass;

            // Creating delegate type.
            // TODO: Add caching system.
            // TODO: Add string parameter support to make string parameter "Obfuscated by Wisser Tg"

            TypeDefUser new_delegate = new TypeDefUser("Delegate_" + target.FullName, moduleDef.CorLibTypes.GetTypeRef("System", "MulticastDelegate"));
            new_delegate.Attributes = target.ResolveMethodDefThrow().DeclaringType.Attributes;
            {
                MethodDefUser ctor = new MethodDefUser(".ctor", MethodSig.CreateInstance(moduleDef.CorLibTypes.Void, moduleDef.CorLibTypes.Object, moduleDef.CorLibTypes.IntPtr));
                ctor.Attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
                ctor.ImplAttributes = MethodImplAttributes.Runtime;

                MethodDefUser invoke = new MethodDefUser("Invoke", proxy_signature.Clone());
                invoke.MethodSig.HasThis = true;
                invoke.ImplAttributes = MethodImplAttributes.Runtime;
                invoke.Attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Public;

                new_delegate.Methods.Add(invoke);
                new_delegate.Methods.Add(ctor);

                moduleDef.Types.Add(new_delegate);
            }

            FieldDefUser field = new FieldDefUser("Delegate_holder_", new FieldSig(new ClassSig(new_delegate)), FieldAttributes.Private | FieldAttributes.Static);

            {
                ModuleDefMD mscorlib = ModuleDefMD.Load(typeof(void).Assembly.Modules.First());
                MethodDef cctor = target.DeclaringType.FindOrCreateStaticConstructor();
                if (cctor.Body.Instructions.Last().OpCode == OpCodes.Ret)
                    cctor.Body.Instructions.Remove(cctor.Body.Instructions.Last());

                cctor.Body.Instructions.Add(OpCodes.Nop.ToInstruction());
                cctor.Body.Instructions.Add(OpCodes.Ldtoken.ToInstruction(new_delegate));
                cctor.Body.Instructions.Add(OpCodes.Call.ToInstruction(mscorlib.Find("System.Type", true).FindMethod("GetTypeFromHandle")));
                cctor.Body.Instructions.Add(OpCodes.Ldtoken.ToInstruction(moduleDef.Import(target.DeclaringType)));
                cctor.Body.Instructions.Add(OpCodes.Call.ToInstruction(mscorlib.Find("System.Type", true).FindMethod("GetTypeFromHandle")));
                cctor.Body.Instructions.Add(OpCodes.Callvirt.ToInstruction(mscorlib.Import(mscorlib.Find("System.Reflection.Module", true).FindMethod("get_Module"))));
                cctor.Body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction((int)target.MDToken.Raw));
                cctor.Body.Instructions.Add(OpCodes.Callvirt.ToInstruction(mscorlib.Import(mscorlib.Find("System.Reflection.Module", true).FindMethod("ResolveMethod"))));
                cctor.Body.Instructions.Add(OpCodes.Isinst.ToInstruction(mscorlib.Import(mscorlib.Find("System.Reflection.MethodInfo", true))));
                var b = mscorlib.Find("System.Delegate", true).FindMethod("CreateDelegate",
                    MethodSig.CreateStatic(mscorlib.Find("System.Delegate", true).ToTypeSig(), mscorlib.Find("System.Type", true).ToTypeSig(), mscorlib.Find("System.Reflection.MethodInfo", true).ToTypeSig()));
                cctor.Body.Instructions.Add(OpCodes.Call.ToInstruction(mscorlib.Find("System.Delegate", true).FindMethod("CreateDelegate",
                    MethodSig.CreateStatic(mscorlib.Find("System.Delegate", true).ToTypeSig(), mscorlib.Find("System.Type", true).ToTypeSig(), mscorlib.Find("System.Reflection.MethodInfo", true).ToTypeSig()))));
                cctor.Body.Instructions.Add(OpCodes.Castclass.ToInstruction(new_delegate));
                cctor.Body.Instructions.Add(OpCodes.Stsfld.ToInstruction(field));
                cctor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

                // TODO: Check for Ret instruction existance.
            }

            MethodDefUser proxy_methodDef = new MethodDefUser("Proxy_method__" + target.FullName, proxy_signature);
            proxy_methodDef.Attributes = MethodAttributes.PrivateScope | MethodAttributes.Static;
            proxy_methodDef.ImplAttributes = MethodImplAttributes.Managed | MethodImplAttributes.IL;

            proxy_methodDef.Body = new CilBody();
            proxy_methodDef.Body.Instructions.Add(OpCodes.Ldsfld.ToInstruction(field));
            for (int i = 0; i < target.Parameters.Count; i++)
                proxy_methodDef.Body.Instructions.Add(OpCodes.Ldarg.ToInstruction(proxy_methodDef.Parameters[i]));
            proxy_methodDef.Body.Instructions.Add(OpCodes.Callvirt.ToInstruction(new_delegate.FindMethod("Invoke")));
            proxy_methodDef.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

            target.DeclaringType.Methods.Add(proxy_methodDef);

            return proxy_methodDef;
        }

        protected MethodSig createProxySig(ModuleDef moduleDef, IMethod target) {
            List<TypeSig> parameters = new List<TypeSig>(target.MethodSig.Params);

            if (target.MethodSig.HasThis && !target.MethodSig.ExplicitThis)
                parameters.Insert(0, new Importer(moduleDef, ImporterOptions.TryToUseTypeDefs).Import(target.DeclaringType.ResolveTypeDefThrow()).ToTypeSig());

            return MethodSig.CreateStatic(target.MethodSig.RetType, parameters.ToArray());
        }

        public void Execute(ModuleDefMD moduleDef) {
            for (int i = 0; i < moduleDef.Types.Count; i++)
                Console.WriteLine($"Added {Execute(moduleDef.Types[i].Methods)} proxies to {moduleDef.Types[i].FullName}");

            while (add_methods.Count > 0)
                add_methods.Pop().Invoke();
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
