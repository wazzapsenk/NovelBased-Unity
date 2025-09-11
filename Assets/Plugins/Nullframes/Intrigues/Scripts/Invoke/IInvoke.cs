using System;
using Object = UnityEngine.Object;

namespace Nullframes.Intrigues
{
    public static class IInvoke
    {
        public static IResult Invoke(this Scheme scheme, string methodName)
        {
            _ = scheme ?? throw new ArgumentNullException();

            if (IM.SchemerInvokeMethods == null) return IResult.Null;

            if (!IM.IsSynced || IM.SchemerInvokeMethods == null || !IM.SchemerInvokeMethods.ContainsKey(methodName)) return IResult.Null;

            var methodData = IM.SchemerInvokeMethods[methodName];

            var objs = Object.FindObjectsOfType(methodData.DeclaringType, false);

            if (objs.Length is > 1 or < 1) return IResult.Null;

            return (IResult)methodData.Invoke(objs[0], new object[] { scheme });
        }
        
        public static IResult Invoke(Actor conspirator, Actor target, string methodName)
        {
            _ = conspirator ?? throw new ArgumentNullException();

            if (IM.RulerInvokeMethods == null) return IResult.Null;

            if (!IM.IsSynced || IM.RulerInvokeMethods == null || !IM.RulerInvokeMethods.ContainsKey(methodName)) return IResult.Null;
            
            var methodData = IM.RulerInvokeMethods[methodName];

            var objs = Object.FindObjectsOfType(methodData.DeclaringType, false);

            if (objs.Length is > 1 or < 1) return IResult.Null;

            var result = (IResult)methodData.Invoke(objs[0], new object[] { conspirator, target });
            
            return result;
        }

        public static Actor Invoke_GetActor(this Scheme scheme, string methodName)
        {
            _ = scheme ?? throw new ArgumentNullException();

            if (IM.SchemerGetActorMethods == null) return null;

            if (!IM.IsSynced || IM.SchemerGetActorMethods == null || !IM.SchemerGetActorMethods.ContainsKey(methodName)) return null;

            var methodData = IM.SchemerGetActorMethods[methodName];

            var objs = Object.FindObjectsOfType(methodData.DeclaringType, false);

            if (objs.Length is > 1 or < 1) return null;

            return (Actor)methodData.Invoke(objs[0], new object[] { scheme });
        }
        
        public static Actor Invoke_GetActor(Actor conspirator, Actor target, string methodName)
        {
            _ = conspirator ?? throw new ArgumentNullException();

            if (IM.RulerGetActorMethods == null) return null;

            if (!IM.IsSynced || IM.RulerGetActorMethods == null || !IM.RulerGetActorMethods.ContainsKey(methodName)) return null;

            var methodData = IM.RulerGetActorMethods[methodName];

            var objs = Object.FindObjectsOfType(methodData.DeclaringType, false);

            if (objs.Length is > 1 or < 1) return null;

            return (Actor)methodData.Invoke(objs[0], new object[] { conspirator, target });
        }

        public static (Actor, Actor) Invoke_GetDualActor(this Scheme scheme, string methodName)
        {
            _ = scheme ?? throw new ArgumentNullException();

            if (IM.SchemerDualActorMethods == null) return (null, null);

            if (!IM.IsSynced || IM.SchemerDualActorMethods == null || !IM.SchemerDualActorMethods.ContainsKey(methodName)) return (null, null);

            var methodData = IM.SchemerDualActorMethods[methodName];

            var objs = Object.FindObjectsOfType(methodData.DeclaringType, false);

            if (objs.Length is > 1 or < 1) return (null, null);

            return ((Actor, Actor))methodData.Invoke(objs[0], new object[] { scheme });
        }
        
        public static (Actor, Actor) Invoke_GetDualActor(Actor conspirator, Actor target, string methodName)
        {
            _ = conspirator ?? throw new ArgumentNullException();

            if (IM.RulerDualActorMethods == null) return (null, null);

            if (!IM.IsSynced || IM.RulerDualActorMethods == null || !IM.RulerDualActorMethods.ContainsKey(methodName)) return (null, null);

            var methodData = IM.RulerDualActorMethods[methodName];

            var objs = Object.FindObjectsOfType(methodData.DeclaringType, false);

            if (objs.Length is > 1 or < 1) return (null, null);

            return ((Actor, Actor))methodData.Invoke(objs[0], new object[] { conspirator, target });
        }

        public static Clan Invoke_GetClan(this Scheme scheme, string methodName)
        {
            _ = scheme ?? throw new ArgumentNullException();

            if (IM.SchemerGetClanMethods == null) return null;

            if (!IM.IsSynced || IM.SchemerGetClanMethods == null || !IM.SchemerGetClanMethods.ContainsKey(methodName)) return null;

            var methodData = IM.SchemerGetClanMethods[methodName];

            var objs = Object.FindObjectsOfType(methodData.DeclaringType, false);

            if (objs.Length is > 1 or < 1) return null;

            return (Clan)methodData.Invoke(objs[0], new object[] { scheme });
        }
        
        public static Clan Invoke_GetClan(Actor conspirator, Actor target, string methodName)
        {
            _ = conspirator ?? throw new ArgumentNullException();

            if (IM.RulerGetClanMethods == null) return null;

            if (!IM.IsSynced || IM.RulerGetClanMethods == null || !IM.RulerGetClanMethods.ContainsKey(methodName)) return null;

            var methodData = IM.RulerGetClanMethods[methodName];

            var objs = Object.FindObjectsOfType(methodData.DeclaringType, false);

            if (objs.Length is > 1 or < 1) return null;

            return (Clan)methodData.Invoke(objs[0], new object[] { conspirator, target });
        }
        
        public static Family Invoke_GetFamily(this Scheme scheme, string methodName)
        {
            _ = scheme ?? throw new ArgumentNullException();

            if (IM.SchemerGetFamilyMethods == null) return null;

            if (!IM.IsSynced || IM.SchemerGetFamilyMethods == null || !IM.SchemerGetFamilyMethods.ContainsKey(methodName)) return null;

            var methodData = IM.SchemerGetFamilyMethods[methodName];

            var objs = Object.FindObjectsOfType(methodData.DeclaringType, false);

            if (objs.Length is > 1 or < 1) return null;

            return (Family)methodData.Invoke(objs[0], new object[] { scheme });
        }
        
        public static Family Invoke_GetFamily(Actor conspirator, Actor target, string methodName)
        {
            _ = conspirator ?? throw new ArgumentNullException();

            if (IM.RulerGetFamilyMethods == null) return null;

            if (!IM.IsSynced || IM.RulerGetFamilyMethods == null || !IM.RulerGetFamilyMethods.ContainsKey(methodName)) return null;

            var methodData = IM.RulerGetFamilyMethods[methodName];

            var objs = Object.FindObjectsOfType(methodData.DeclaringType, false);

            if (objs.Length is > 1 or < 1) return null;

            return (Family)methodData.Invoke(objs[0], new object[] { conspirator, target });
        }
    }
}