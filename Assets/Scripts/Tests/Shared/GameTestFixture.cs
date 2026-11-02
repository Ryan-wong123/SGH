using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SGH.Tests
{
    // Runtime scripts live in Assembly-CSharp. Reflection avoids moving serialized
    // MonoBehaviours into a new assembly merely to make them visible to tests.
    public abstract class GameTestFixture
    {
        protected GameObject Root;
        protected const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic |
                                                BindingFlags.Instance | BindingFlags.Static;

        public static Type RuntimeType(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp")
                .GetType(name, true);
        }

        public static object Get(object target, string name)
        {
            Type type = target as Type ?? target.GetType();
            FieldInfo field = FindField(type, name);
            return field != null ? field.GetValue(target is Type ? null : target) :
                type.GetProperty(name, Members).GetValue(target is Type ? null : target);
        }

        public static void Set(object target, string name, object value)
        {
            Type type = target as Type ?? target.GetType();
            FieldInfo field = FindField(type, name);
            Assert.That(field, Is.Not.Null, type.Name + "." + name);
            field.SetValue(target is Type ? null : target, value);
        }

        private static FieldInfo FindField(Type type, string name)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(name, Members);
                if (field != null) return field;
                type = type.BaseType;
            }
            return null;
        }

        public static object Call(object target, string name, params object[] args)
        {
            Type type = target as Type ?? target.GetType();
            MethodInfo[] candidates = type.GetMethods(Members).Where(m => m.Name == name &&
                m.GetParameters().Length == args.Length).Where(m => m.GetParameters().Select((p, i) =>
                    args[i] == null || p.ParameterType.IsByRef || p.ParameterType.IsInstanceOfType(args[i])).All(v => v)).ToArray();
            Assert.That(candidates.Length, Is.EqualTo(1), type.Name + "." + name);
            try { return candidates[0].Invoke(target is Type ? null : target, args); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }

        [SetUp]
        public void CreateRoot()
        {
            Root = new GameObject("SGH test fixture");
            Root.SetActive(false); // Configure dependencies before Unity lifecycle callbacks.
        }

        [TearDown]
        public void DestroyRoot()
        {
            BeforeDestroyRoot();
            UnityEngine.Object.DestroyImmediate(Root);
        }

        // NUnit TearDown can precede UnityTearDown. Release registered resources
        // while the inactive hierarchy still exists, before coroutine cleanup.
        protected virtual void BeforeDestroyRoot() { }

        protected GameObject Object(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(Root.transform, false);
            return go;
        }

        protected Component Component(string type, string name = null)
        {
            return Object(name ?? type).AddComponent(RuntimeType(type));
        }

        protected TMP_InputField Input(string text = "", string name = "Input")
        {
            GameObject go = Object(name);
            var image = go.AddComponent<Image>();
            var input = go.AddComponent<TMP_InputField>();
            input.targetGraphic = image;
            input.text = text;
            return input;
        }

        protected TMP_Dropdown Dropdown(int value = 0)
        {
            GameObject go = Object("Dropdown");
            var image = go.AddComponent<Image>();
            var dropdown = go.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = image;
            dropdown.options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("Select"), new TMP_Dropdown.OptionData("Low"),
                new TMP_Dropdown.OptionData("High")
            };
            dropdown.value = value;
            return dropdown;
        }

        protected static IList NestedList(string owner, string nested)
        {
            Type element = RuntimeType(owner).GetNestedType(nested, Members);
            return (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(element));
        }

        protected static object Nested(string owner, string nested)
        {
            return Activator.CreateInstance(RuntimeType(owner).GetNestedType(nested, Members));
        }
    }
}
