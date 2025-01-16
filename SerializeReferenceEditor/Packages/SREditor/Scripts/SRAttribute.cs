﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SerializeReferenceEditor
{
	[AttributeUsage(AttributeTargets.Field)]
	public class SRAttribute : PropertyAttribute
	{
		private static readonly Dictionary<Type, Type[]> _typeCache = new();

		public class TypeInfo
		{
			public Type Type;
			public string Path;
		}

		public TypeInfo[] Types { get; protected set; }

		public SRAttribute()
		{
			Types = null;
		}

		// ReSharper disable once MemberCanBeProtected.Global
		public SRAttribute(Type baseType)
		{
			if(baseType == null)
			{
				Debug.LogError("[SRAttribute] Incorrect type.");
			}

			Types = GetTypeInfos(GetChildTypes(baseType));
		}

		// ReSharper disable once MemberCanBeProtected.Global
		public SRAttribute(params Type[] types)
		{
			if(types == null || types.Length <= 0)
			{
				Debug.LogError("[SRAttribute] Incorrect types.");
			}

			Types = GetTypeInfos(types);
		}

		public void SetTypeByName(string typeName)
		{
			if(string.IsNullOrEmpty(typeName))
			{
				Debug.LogError("[SRAttribute] Incorrect type name.");
			}
			var type = GetTypeByName(typeName);
			if(type == null)
			{
				Debug.LogError("[SRAttribute] Incorrect type.");
			}

			Types = GetTypeInfos(GetChildTypes(type));
		}

		public TypeInfo TypeInfoByPath(string path)
		{
			return Types != null ? Array.Find(Types, p => p.Path == path) : null;
		}

		public static TypeInfo[] GetTypeInfos(Type[] types)
		{
			if(types == null)
				return null;

			TypeInfo[] result = new TypeInfo[types.Length];
			for(int i = 0; i < types.Length; ++i)
			{
				var type = types[i];
				var typeName = type.FullName;
				var nameAttribute = type.GetCustomAttributes(typeof(SRNameAttribute), false)
					.Select(attr=> attr as SRNameAttribute)
					.FirstOrDefault();
			
				if (nameAttribute != null) 
					typeName = nameAttribute.FullName;
			
				result[i] = new TypeInfo { 
					Type = type, 
					Path = typeName 
				};
			}

			return result;
		}

		private static bool IsCorrectChildTypeForSearchTree(Type baseType, Type childType)
		{
			return !childType.IsAbstract
			       && !childType.IsInterface
			       && childType != baseType
			       && (baseType.IsInterface
				       ? baseType.IsAssignableFrom(childType)
				       : childType.IsSubclassOf(baseType));
		}

		protected static Type[] GetChildTypes(Type baseType)
		{
			if (_typeCache.TryGetValue(baseType, out var result))
				return result;

			result = AppDomain.CurrentDomain
				.GetAssemblies()
				.SelectMany(s => s.GetTypes())
				.Where(childType => IsCorrectChildTypeForSearchTree(baseType, childType))
				.ToArray();

			_typeCache[baseType] = result;

			return result;
		}

		private static Type GetTypeByName(string typeName)
		{
			if(string.IsNullOrEmpty(typeName))
				return null;

			var typeSplit = typeName.Split(char.Parse(" "));
			var typeAssembly = typeSplit[0];
			var typeClass = typeSplit[1];

			return Type.GetType(typeClass + ", " + typeAssembly);
		}

		public virtual void OnCreate(object instance, UnityEngine.Object target)
		{
		}
	}
}
