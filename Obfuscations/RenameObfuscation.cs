using System;
using System.Collections.Generic;
using System.Linq;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using CILExamining.Obfuscations.Utilities;
using CILExamining.Obfuscations.Details;

namespace CILExamining.Obfuscations {
    public class RenameObfuscation : IObfuscation {
        protected readonly RenameUtility renamer;
        protected Random random => renamer.Random;

        public RenameObfuscation() {
            this.renamer = new RenameUtility();
        }

        public RenameObfuscation(int seed) {
            this.renamer = new RenameUtility(seed);
        }

        public RenameObfuscation(RenameUtility renamer) {
            this.renamer = renamer;
        }

        public void Execute(IEnumerable<TypeDef> types, IEnumerable<MethodDef> methods = null, IEnumerable<FieldDef> fields = null, IEnumerable<PropertyDef> properties = null) {
            List<MethodDef> method_list = new List<MethodDef>();
            if (methods != null)
                method_list.AddRange(methods);
            foreach (IEnumerable<MethodDef> methods_i in types.Select(k => k.Methods))
                method_list.AddRange(methods_i);

            List<FieldDef> field_list = new List<FieldDef>();
            if (fields != null)
                field_list.AddRange(fields);
            foreach (IEnumerable<FieldDef> fields_i in types.Select(k => k.Fields))
                field_list.AddRange(fields_i);

            List<PropertyDef> property_list = new List<PropertyDef>();
            if (properties != null)
                property_list.AddRange(properties);
            foreach (IEnumerable<PropertyDef> properties_i in types.Select(k => k.Properties))
                property_list.AddRange(properties_i);

            executeTypes(types);
            executeMethods(method_list);
            executeFields(field_list);
            executeProperties(property_list);
        }

        protected void executeTypes(IEnumerable<TypeDef> types) {
            string current_namespace = "DEFAULT_IF_NOT_EXISTS";
            int current_namespace_indexer = 0;

            foreach (TypeDef td in types) {
                if (td.IsPublic)
                    continue;

                if (current_namespace_indexer++ % 4 == 0)
                    current_namespace = renamer.GetObfuscated(false);

                td.Namespace = current_namespace;
                td.Name = renamer.GetObfuscated(false);
            }
        }

        protected void executeMethods(IEnumerable<MethodDef> methods) {
            // TODO: Implement overrides support.
            // Example
            // public int hasfhdh() { }
            // public void hasfhdh(float b) { }

            foreach (MethodDef md in methods) {
                if (!((md.Access & MethodAttributes.Public) == MethodAttributes.Public
                 && (md.DeclaringType.Attributes & TypeAttributes.Public) == TypeAttributes.Public)) {
                    md.Name = renamer.GetObfuscated(false);
                }

                //if (md.Body != null)
                //    new MethodDefLocalsRename(md, renamer).ChangeLocals();

                new MethodDefParamsRename(md, renamer).ChangeParams();
            }
        }
        
        protected void executeFields(IEnumerable<FieldDef> fields) {
            foreach (FieldDef fd in fields) {
                if ((fd.Access & FieldAttributes.Public) == FieldAttributes.Public
                 && (fd.DeclaringType.Attributes & TypeAttributes.Public) == TypeAttributes.Public)
                    continue;

                fd.Name = renamer.GetObfuscated(false);
            }
        }

        protected void executeProperties(IEnumerable<PropertyDef> properties) {
            foreach (PropertyDef pd in properties) {
                if ((pd.DeclaringType.Attributes & TypeAttributes.Public) == TypeAttributes.Public)
                    if ((pd.GetMethod != null && pd.GetMethod.Access == MethodAttributes.Public) || (pd.SetMethod != null && pd.SetMethod.Access == MethodAttributes.Public))
                        continue;

                pd.Name = renamer.GetObfuscated(false);
            }
        }
        protected void executeModule(IModule module) {
            module.Name = renamer.GetObfuscated(false);
        }

        public void Execute(ModuleDefMD moduleDef) {
            Execute(moduleDef.Types);
            //executeModule(moduleDef);
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
