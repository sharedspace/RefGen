using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using MethodAttributes = dnlib.DotNet.MethodAttributes;

namespace RefGen.Module
{
    internal static class Extensions
    {
        public static IEnumerable<TypeDef> GetAllTypes(this ModuleDefMD module)
        {
            return GetAllTypes(module.GetTypes());
        }

        private static IEnumerable<TypeDef> GetAllTypes(IEnumerable<TypeDef> types)
        {
            foreach (var type in types)
            {
                yield return type;
                foreach (var nestedType in GetAllTypes(type.NestedTypes))
                {
                    yield return nestedType;
                }
            }
        }

        public static void MakeInternalsVisibleTo(this ModuleDefMD module, ModuleDefMD other)
        {
            var assemblyName = other.Assembly.GetFullNameWithPublicKeyToken();
            var ivtoCtor = 
                module
                .Import(
                    typeof(InternalsVisibleToAttribute)
                    .GetConstructor(
                        new Type[] 
                        {
                            typeof(string)
                        })) as ICustomAttributeType;
            var ca = 
                new CustomAttribute(
                    ivtoCtor, 
                    new List<CAArgument>()
                    {
                        new CAArgument(
                            module.CorLibTypes.String, 
                            assemblyName)
                    });
            module.Assembly.CustomAttributes.Add(ca);
            module.IsStrongNameSigned = other.IsStrongNameSigned;
        }

        public static void RemoveInternalsVisibleTo(this ModuleDefMD module, ModuleDefMD other)
        {
            var assemblyName = other.Assembly.GetFullNameWithPublicKeyToken();
            var cAs = module.Assembly.CustomAttributes.FindAll("System.Runtime.CompilerServices.InternalsVisibleToAttribute");
            var ivtoCas =
                cAs.Where(
                    ca =>
                    ca.HasConstructorArguments &&
                    ca.ConstructorArguments.Count == 1 &&
                    ca.ConstructorArguments[0].Type == module.CorLibTypes.String &&
                    assemblyName.Equals(ca.ConstructorArguments[0].Value.ToString(), StringComparison.InvariantCultureIgnoreCase)).ToList();

            for (int i = 0; i < ivtoCas.Count(); i++)
            {
                module.Assembly.CustomAttributes.Remove(ivtoCas.ElementAt(i));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        /// <remarks>
        /// See https://stackoverflow.com/questions/4971213/how-to-use-reflection-to-determine-if-a-class-is-internal
        /// </remarks>
        public static bool IsPublic(this TypeDef t)
        {
            return
                t.IsVisible()
                && t.IsPublic
                && !t.IsNotPublic
                && !t.IsNested
                && !t.IsNestedPublic
                && !t.IsNestedFamily
                && !t.IsNestedPrivate
                && !t.IsNestedAssembly
                && !t.IsNestedFamilyOrAssembly
                && !t.IsNestedFamilyAndAssembly;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        /// <remarks>
        /// See https://stackoverflow.com/questions/4971213/how-to-use-reflection-to-determine-if-a-class-is-internal
        /// </remarks>
        public static bool IsInternal(this TypeDef t)
        {
            return
                !t.IsVisible()
                && !t.IsPublic
                && t.IsNotPublic
                && !t.IsNested
                && !t.IsNestedPublic
                && !t.IsNestedFamily
                && !t.IsNestedPrivate
                && !t.IsNestedAssembly
                && !t.IsNestedFamilyOrAssembly
                && !t.IsNestedFamilyAndAssembly;
        }

        /// <summary>
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        /// <remarks>
        /// Only nested types can be declared "protected"
        /// Also see https://stackoverflow.com/questions/4971213/how-to-use-reflection-to-determine-if-a-class-is-internal
        /// </remarks>
        public static bool IsProtected(this TypeDef t)
        {
            return
                !t.IsVisible()
                && !t.IsPublic
                && !t.IsNotPublic
                && t.IsNested
                && !t.IsNestedPublic
                && t.IsNestedFamily
                && !t.IsNestedPrivate
                && !t.IsNestedAssembly
                && !t.IsNestedFamilyOrAssembly
                && !t.IsNestedFamilyAndAssembly;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        /// <remarks>
        /// Only nested types can be declared "private"
        /// Also see https://stackoverflow.com/questions/4971213/how-to-use-reflection-to-determine-if-a-class-is-internal
        /// </remarks>
        public static bool IsPrivate(this TypeDef t)
        {
            return
                !t.IsVisible()
                && !t.IsPublic
                && !t.IsNotPublic
                && t.IsNested
                && !t.IsNestedPublic
                && !t.IsNestedFamily
                && t.IsNestedPrivate
                && !t.IsNestedAssembly
                && !t.IsNestedFamilyOrAssembly
                && !t.IsNestedFamilyAndAssembly;
        }

        private static bool IsVisible(this TypeDef t)
        {
            var declaringAssemblyLocation = (t.DefinitionAssembly as AssemblyDef)?.ManifestModule.Location;
            var type = Assembly.LoadFrom(declaringAssemblyLocation).GetType(t.ReflectionFullName, throwOnError: true);
            return type.IsVisible;
        }

        /// <summary>
        /// Public = IsPublic && FamAndAssem && Family 
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static bool IsPublic(this MethodDef m)
        {
            return
                m.IsPublic;
        }

        /// <summary>
        /// Internal = Assembly && FamAndAssem && Private
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static bool IsInternal(this MethodDef m)
        {
            return
                !m.IsPrivate
                && m.IsAssembly;
        }

        public static bool IsPrivate(this MethodDef m)
        {
            //return
            //    !m.IsPublic
            //    && !m.Access.HasFlag(MethodAttributes.Public)
            //    && !m.Access.HasFlag(MethodAttributes.FamANDAssem)
            //    && !m.Access.HasFlag(MethodAttributes.Family)
            //    && !m.Access.HasFlag(MethodAttributes.Assembly)
            //    && m.Access.HasFlag(MethodAttributes.Private)
            //    && !m.Access.HasFlag(MethodAttributes.PrivateScope);
            return m.Access.HasFlag(MethodAttributes.Private);
        }
    }
}
