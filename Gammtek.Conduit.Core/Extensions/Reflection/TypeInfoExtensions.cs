﻿#if NETFX_CORE || PCL || (NET && !NET40)

using System;
using System.Linq;
using System.Reflection;
using Gammtek.Conduit.Helpers;

namespace Gammtek.Conduit.Extensions.Reflection
{
	public static class TypeInfoExtensions
	{
		/// <summary>
		///     Gets the constructor with the specified types.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="types">The types of the constructor.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>An array of <see cref="ConstructorInfo" />.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		public static ConstructorInfo GetConstructor(this TypeInfo typeInfo, Type[] types, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);

			return (from x in GetConstructors(typeInfo, bindingFlags)
				where CollectionHelper.IsEqualTo(types, from parameterInfo in x.GetParameters()
					select parameterInfo.ParameterType)
				select x).FirstOrDefault();
		}

		/// <summary>
		///     Gets the constructors.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>An array of <see cref="ConstructorInfo" />.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		public static ConstructorInfo[] GetConstructors(this TypeInfo typeInfo, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);

#if NETFX_CORE
            var source = typeInfo.DeclaredConstructors.ToArray();
#else
			var source = typeInfo.GetConstructors(bindingFlags);
#endif

			var includeStatics = Enum<BindingFlags>.Flags.IsFlagSet(bindingFlags, BindingFlags.Static);

			if (!includeStatics)
			{
				source = (from x in source
					where !x.IsStatic
					select x).ToArray();
			}

			return (from x in source
				select x).ToArray();
		}

		/// <summary>
		///     Gets the event with the specified name.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="name">The name of the member to retrieve.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>The <see cref="EventInfo" /> or <c>null</c> if the member is not found.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">The <paramref name="name" /> is <c>null</c> or whitespace.</exception>
		public static EventInfo GetEvent(this TypeInfo typeInfo, string name, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);
			Argument.IsNotNullOrWhitespace(nameof(name), name);

			return (from x in GetEvents(typeInfo, bindingFlags)
				where x.Name == name
				select x).FirstOrDefault();
		}

		/// <summary>
		///     Gets the events.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>An array of <see cref="EventInfo" />.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		public static EventInfo[] GetEvents(this TypeInfo typeInfo, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);

			var flattenHierarchy = ShouldFlattenHierarchy(bindingFlags);
			var eventsSource = flattenHierarchy ? typeInfo.AsType().GetRuntimeEvents() : typeInfo.DeclaredEvents;

			return (from x in eventsSource
				select x).ToArray();
		}

		/// <summary>
		///     Gets the field with the specified name.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="name">The name of the member to retrieve.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>The <see cref="FieldInfo" /> or <c>null</c> if the member is not found.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">The <paramref name="name" /> is <c>null</c> or whitespace.</exception>
		public static FieldInfo GetField(this TypeInfo typeInfo, string name, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);
			Argument.IsNotNullOrWhitespace(nameof(name), name);

			return (from x in GetFields(typeInfo, bindingFlags)
				where x.Name == name
				select x).FirstOrDefault();
		}

		/// <summary>
		///     Gets the fields.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>An array of <see cref="FieldInfo" />.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		public static FieldInfo[] GetFields(this TypeInfo typeInfo, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);

			var flattenHierarchy = ShouldFlattenHierarchy(bindingFlags);
			var source = flattenHierarchy ? typeInfo.AsType().GetRuntimeFields().ToList() : typeInfo.DeclaredFields.ToList();

			var includeStatics = Enum<BindingFlags>.Flags.IsFlagSet(bindingFlags, BindingFlags.Static);

			// TODO: This is a fix because static members are not included in FlattenHierarcy, remove when this is fixed in WinRT
			if (flattenHierarchy)
			{
				var baseType = typeInfo.BaseType;

				if ((baseType != null) && (baseType != typeof (object)))
				{
					source.AddRange(from member in GetFields(baseType.GetTypeInfo(), bindingFlags)
						where member.IsStatic
						select member);
				}
			}

			return (from x in source
				where x.IsStatic == includeStatics
				select x).ToArray();
		}

		/// <summary>
		///     Gets the member with the specified name.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="name">The name of the member to retrieve.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>The <see cref="FieldInfo" /> or <c>null</c> if the member is not found.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">The <paramref name="name" /> is <c>null</c> or whitespace.</exception>
		public static MemberInfo[] GetMember(this TypeInfo typeInfo, string name, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);
			Argument.IsNotNullOrWhitespace(nameof(name), name);

			return (from x in GetMembers(typeInfo, bindingFlags)
				where x.Name == name
				select x).ToArray();
		}

		/// <summary>
		///     Gets the members.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>An array of <see cref="FieldInfo" />.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		public static MemberInfo[] GetMembers(this TypeInfo typeInfo, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);

			var flattenHierarchy = ShouldFlattenHierarchy(bindingFlags);
			var source = typeInfo.DeclaredMembers.ToList();

			// TODO: This is a fix because static members are not included in FlattenHierarcy, remove when this is fixed in WinRT
			if (flattenHierarchy)
			{
				var baseType = typeInfo.BaseType;

				if ((baseType != null) && (baseType != typeof (object)))
				{
					source.AddRange(from member in GetMembers(baseType.GetTypeInfo(), bindingFlags)
						select member);
				}
			}

			return (from x in source
				select x).ToArray();
		}

		/// <summary>
		///     Gets the method with the specified name.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="name">The name of the member to retrieve.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>The <see cref="MethodInfo" /> or <c>null</c> if the member is not found.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">The <paramref name="name" /> is <c>null</c> or whitespace.</exception>
		public static MethodInfo GetMethod(this TypeInfo typeInfo, string name, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);
			Argument.IsNotNullOrWhitespace(nameof(name), name);

			return (from x in GetMethods(typeInfo, bindingFlags)
				where x.Name == name
				select x).FirstOrDefault();
		}

		/// <summary>
		///     Gets the method with the specified name and types.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="name">The name of the member to retrieve.</param>
		/// <param name="types">The types of the method.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>The <see cref="MethodInfo" /> or <c>null</c> if the member is not found.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">The <paramref name="name" /> is <c>null</c> or whitespace.</exception>
		public static MethodInfo GetMethod(this TypeInfo typeInfo, string name, Type[] types, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);
			Argument.IsNotNullOrWhitespace(nameof(name), name);

			return (from x in GetMethods(typeInfo, bindingFlags)
				where x.Name == name && CollectionHelper.IsEqualTo(types, from parameterInfo in x.GetParameters()
					select parameterInfo.ParameterType)
				select x).FirstOrDefault();
		}

		/// <summary>
		///     Gets the methods.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>An array of <see cref="MethodInfo" />.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		public static MethodInfo[] GetMethods(this TypeInfo typeInfo, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);

			var flattenHierarchy = ShouldFlattenHierarchy(bindingFlags);
			var source = flattenHierarchy ? typeInfo.AsType().GetRuntimeMethods().ToList() : typeInfo.DeclaredMethods.ToList();

			var includeStatics = Enum<BindingFlags>.Flags.IsFlagSet(bindingFlags, BindingFlags.Static);

			// TODO: This is a fix because static members are not included in FlattenHierarcy, remove when this is fixed in WinRT
			if (flattenHierarchy)
			{
				var baseType = typeInfo.BaseType;

				if ((baseType != null) && (baseType != typeof (object)))
				{
					source.AddRange(from member in GetMethods(baseType.GetTypeInfo(), bindingFlags)
						where member.IsStatic
						select member);
				}
			}

			if (!includeStatics)
			{
				source = (from x in source
					where !x.IsStatic
					select x).ToList();
			}

			return (from x in source
				select x).ToArray();
		}

		/// <summary>
		///     Gets the properties.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>An array of <see cref="PropertyInfo" />.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		public static PropertyInfo[] GetProperties(this TypeInfo typeInfo, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);

			var flattenHierarchy = ShouldFlattenHierarchy(bindingFlags);
			var source = (flattenHierarchy ? typeInfo.AsType().GetRuntimeProperties() : typeInfo.DeclaredProperties).ToList();

			var includeStatics = Enum<BindingFlags>.Flags.IsFlagSet(bindingFlags, BindingFlags.Static);

			// TODO: This is a fix because static members are not included in FlattenHierarcy, remove when this is fixed in WinRT
			if (flattenHierarchy)
			{
				var baseType = typeInfo.BaseType;

				if ((baseType != null) && (baseType != typeof (object)))
				{
					source.AddRange(from member in GetProperties(baseType.GetTypeInfo(), bindingFlags)
						where member.IsStatic()
						select member);
				}
			}

			return (from x in source where x.IsStatic() == includeStatics select x).ToArray();
		}

		/// <summary>
		///     Gets the property with the specified name.
		/// </summary>
		/// <param name="typeInfo">The <see cref="TypeInfo" />.</param>
		/// <param name="name">The name of the member to retrieve.</param>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns>The <see cref="PropertyInfo" /> or <c>null</c> if the member is not found.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="typeInfo" /> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">The <paramref name="name" /> is <c>null</c> or whitespace.</exception>
		public static PropertyInfo GetProperty(this TypeInfo typeInfo, string name, BindingFlags bindingFlags)
		{
			Argument.IsNotNull(nameof(typeInfo), typeInfo);
			Argument.IsNotNullOrWhitespace(nameof(name), name);

			return (from x in GetProperties(typeInfo, bindingFlags)
				where x.Name == name
				select x).FirstOrDefault();
		}

		//private static readonly ILog Log = LogManager.GetCurrentClassLogger();

		/// <summary>
		///     Determines whether the hierarchy should be flattened based on the specified binding flags.
		/// </summary>
		/// <param name="bindingFlags">The binding flags.</param>
		/// <returns><c>true</c> if the hierarchy should be flattened; otherwise <c>false</c>.</returns>
		private static bool ShouldFlattenHierarchy(BindingFlags bindingFlags)
		{
			return Enum<BindingFlags>.Flags.IsFlagSet(bindingFlags, BindingFlags.FlattenHierarchy);
		}
	}
}

#endif
