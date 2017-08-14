// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Web.Routing;

namespace System.Web.Mvc
{
    public abstract class AreaRegistration
    {
        private const string TypeCacheName = "MVC-AreaRegistrationTypeCache.xml";

        public abstract string AreaName { get; }

        internal void CreateContextAndRegister(RouteCollection routes, object state)
        {
            AreaRegistrationContext context = new AreaRegistrationContext(AreaName, routes, state);

            string thisNamespace = GetType().Namespace;
            if (thisNamespace != null)
            {
                context.Namespaces.Add(thisNamespace + ".*");
            }

            RegisterArea(context);
        }

        /// <summary>
        /// isAssignableFrom 确定指定类型的实例是否可以分配给当前类型的实例。；isAssignableFrom针对class对象 
        /// instanceof 是判断物品X是否是由模具A生产出来的；instanceof 针对实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool IsAreaRegistrationType(Type type)
        {
            return
                typeof(AreaRegistration).IsAssignableFrom(type) &&
                type.GetConstructor(Type.EmptyTypes) != null;
        }

        public static void RegisterAllAreas()
        {
            RegisterAllAreas(null);
        }

        public static void RegisterAllAreas(object state)
        {
            RegisterAllAreas(RouteTable.Routes, new BuildManagerWrapper(), state);
        }

        internal static void RegisterAllAreas(RouteCollection routes, IBuildManager buildManager, object state)
        {
            List<Type> areaRegistrationTypes = TypeCacheUtil.GetFilteredTypesFromAssemblies(TypeCacheName, IsAreaRegistrationType, buildManager);
            foreach (Type areaRegistrationType in areaRegistrationTypes)
            {
                AreaRegistration registration = (AreaRegistration)Activator.CreateInstance(areaRegistrationType);
                registration.CreateContextAndRegister(routes, state);
            }
        }

        public abstract void RegisterArea(AreaRegistrationContext context);
    }
}
